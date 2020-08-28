using System;
using System.IO;
using System.Linq;
using NetDoc;
using NUnit.Framework;
using Tests.TestFramework;

namespace Tests
{
    class TardyConsumerTests : TestMethods
    {
        private const string AssertionSuppressor = "//IGNORE ";

        [Test]
        public void PreviouslyIgnoredAssertionsRemainCommentedOut()
        {
            var oldReferenced = Class("public void OldMethod() {}", "Consumed");
            var newReferenced = Class("public void NewMethod() {}", "Consumed");

            var referencing = Class("public void CallsOldMethod() { new Consumed().OldMethod(); }");

            var oldContractAssertion = ContractAssertionShouldCompile(referencing, oldReferenced);
            //Assert.Throws<AssertionException>(() => ContractAssertionShouldCompile(referencing, newReferenced));
            var commentedAssertion = oldContractAssertion.Replace("    Create", "    //IGNORE Create");

            var newAssertion = UpdatedContractAssertionShouldCompile(referencing, oldReferenced, newReferenced, commentedAssertion);
            StringAssert.Contains(AssertionSuppressor, newAssertion, "The new assertion should be commented out");
        }

        private string UpdatedContractAssertionShouldCompile(string referencing, string oldReferenced, string newReferenced, string oldContractAssertion)
        {
            var (oldReferencedDll, referencingDll) = ClrAssemblyCompiler.CompileDlls(referencing, oldReferenced);
            using var writer = new StringWriter();
            Program.CreateContractAssertions(writer, "", new[] { oldReferencedDll }, new[] { referencingDll });
            var newlyGeneratedAssertions = writer.ToString();

            using var writer2 = new StringWriter();
            PreserveIgnoredAssertions(oldContractAssertion, newlyGeneratedAssertions, writer2);
            Console.WriteLine("*** Commented out assertion:");
            Console.WriteLine(writer2.ToString());
            ClrAssemblyCompiler.CompileDlls(writer2.ToString(), newReferenced);
            StringAssert.Contains("private void UsedByTestAssembly()", writer2.ToString(),
                "We should have created a method to contain the assertions for this assembly");
            return writer2.ToString();
        }

        private static void PreserveIgnoredAssertions(string oldContractAssertion, string newlyGeneratedAssertions, TextWriter writer)
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
