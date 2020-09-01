using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NetDoc
{
    public class IgnorancePreserver
    {
        public const string AssertionSuppressor = "//IGNORE ";

        /// <summary>
        /// If a contract assertion already exists and contains ignored lines (defined by the <see cref="AssertionSuppressor"/>)
        /// this method will modify a newly generated contract assertion to also ignore those lines.
        /// </summary>
        /// <remarks>
        /// This feature is designed to account for tardy consumers, which have not taken the latest version of your library
        /// but might do so eventually.
        /// The ignored assertion will be removed when the consumer pulls in the breaking change.
        /// </remarks>
        /// <param name="oldContractAssertion">The contents of the old C# contract assertion</param>
        /// <param name="newlyGeneratedAssertions">The contents of the newly generated C# contract assertion</param>
        /// <param name="writer">Where to output the resulting contract assertion</param>
        public static void PreserveIgnoredAssertions(string oldContractAssertion, string newlyGeneratedAssertions, TextWriter writer)
        {
            var commented = oldContractAssertion.Split("\n")
                .Where(s => s.Contains(AssertionSuppressor))
                .ToDictionary(KeySelector);

            foreach (var line in newlyGeneratedAssertions.TrimEnd('\r', '\n').Split("\n"))
            {
                var lineOrComment = CommentOutIfNeeded(commented, line);
                writer.WriteLine(lineOrComment.TrimEnd('\r'));
            }
        }

        private static string CommentOutIfNeeded(Dictionary<string, string> commented, string line) =>
            commented.FirstOrDefault(kvp => kvp.Key.EndsWith(KeySelector(line))).Value ?? line;

        private static string KeySelector(string s) => s.Replace(AssertionSuppressor, "").Trim('\r', '\t', ' ');
    }
}
