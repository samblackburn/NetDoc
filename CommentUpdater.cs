using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

internal static class CommentUpdater
{
    private static readonly Regex s_IsUsage = new Regex(@"^[ \t]*\/\/\/ Used by .*\r\n", RegexOptions.Compiled | RegexOptions.Multiline);

    [TestCase(null, "\r\n", "", ExpectedResult = "\r\n", TestName = "Non-indented method")]
    [TestCase(null, "\r\n    ", "    ", ExpectedResult = "\r\n    ", TestName = "Spaces before method")]
    [TestCase(null, "\r\n    /// Blah\r\n    ", "    ", ExpectedResult = "\r\n    /// Blah\r\n    ", TestName = "Comment before method")]
    [TestCase(null, "\r\n    /// Used by blah\r\n    ", "    ", ExpectedResult = "\r\n    ", TestName = "Obsolete usage")]
    [TestCaseSource(nameof(TestCasesWithUsage))]
    public static string UpdateXmlComment(IEnumerable<string> consumers, string existingComment,
        string idiomaticWhitespace)
    {
        string FormatUsage(string consumer) => $"/// Used by {consumer}\r\n{idiomaticWhitespace}";
        var allConsumers = string.Join("", (consumers ?? new string[0]).Select(FormatUsage));
        var removeOldUsages = s_IsUsage.Replace(existingComment, "");
        return $"{removeOldUsages}{allConsumers}";
    }

    public static IEnumerable<TestCaseData> TestCasesWithUsage
    {
        get
        {
            yield return new TestCaseData(new[] {"Foo()"}, "    \r\n    ", "    ")
                {ExpectedResult = "    \r\n    /// Used by Foo()\r\n    ", TestName = "Indented, 1 usage"};
        }
    }
}