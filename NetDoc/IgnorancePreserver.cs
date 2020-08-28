using System.IO;
using System.Linq;

namespace NetDoc
{
    public class IgnorancePreserver
    {
        public const string AssertionSuppressor = "//IGNORE ";

        public static void PreserveIgnoredAssertions(string oldContractAssertion, string newlyGeneratedAssertions, TextWriter writer)
        {
            var commented = oldContractAssertion.Split("\n")
                .Where(s => s.Contains(AssertionSuppressor))
                .ToDictionary(KeySelector);

            foreach (var line in newlyGeneratedAssertions.Split("\n"))
            {
                var lineOrComment = commented.TryGetValue(KeySelector(line), out var commentedOutLine)
                    ? commentedOutLine
                    : line;
                writer.WriteLine(lineOrComment.TrimEnd('\r'));
            }
        }

        private static string KeySelector(string s)
        {
            return s.Replace(AssertionSuppressor, "").Trim('\r', '\t', ' ');
        }
    }
}
