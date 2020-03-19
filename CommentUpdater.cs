using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NetDoc;
using NUnit.Framework;

internal static class CommentUpdater
{
    private static readonly Regex s_IsUsage = new Regex("/// Used by ", RegexOptions.Compiled);

    [TestCase(null, "///", "", ExpectedResult = "///")]
    [TestCase(null, "   ///\r\n", "   ", ExpectedResult = "   ///\r\n")]
    [TestCase(null, "   ///\r\n", "   \r\n", ExpectedResult = "   ///\r\n")]
    [TestCase(null, "   ///\r\n   ", "   ", ExpectedResult = "   ///\r\n   ")]
    public static string UpdateXmlComment(IEnumerable<string> consumers, string probablyBlankLine,
        string existingComment)
    {
        var idiomaticWhitespace = probablyBlankLine.TrimEnd('\n').TrimEnd('\r');
        var hasNewline = probablyBlankLine.EndsWith("\n");

        var existingLines = existingComment.Split('\n').Select(x => x.TrimEnd('\r'));
        var notUsages = existingLines.Where(x => !s_IsUsage.IsMatch(x)).ToList();

        var newComment = new StringBuilder();
        foreach (var call in consumers ?? new string[0])
        {
            newComment.AppendLine($@"{idiomaticWhitespace}/// Called by {call}");
        }

        if (!String.IsNullOrWhiteSpace(notUsages.Last()))
        {
            throw new Exception("Expected last line of leading trivia to be whitespace");
        }

        var oldComment = String.Join(Environment.NewLine, notUsages.SkipLast()) +
                         (hasNewline ? Environment.NewLine : String.Empty);
        return $"{oldComment}{newComment}{notUsages.LastOrDefault()}";
    }
}