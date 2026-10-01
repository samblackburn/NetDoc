using System;
using System.IO;
using NetDoc;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Tests.TestFramework;

namespace Tests
{
    class TardyConsumerTests : TestMethods
    {
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
            StringAssert.Contains(IgnorancePreserver.AssertionSuppressor, newAssertion, "The new assertion should be commented out");
            Assert.AreEqual(commentedAssertion, newAssertion, "The new assertion should match the old one, since nothing else in the code has changed");
        }

        [Test]
        public void AssertionsCanBeIgnoredWithAComment()
        {
            var oldReferenced = Class("public void OldMethod() {}", "Consumed");
            var newReferenced = Class("public void NewMethod() {}", "Consumed");

            var referencing = Class("public void CallsOldMethod() { new Consumed().OldMethod(); }");

            var oldContractAssertion = ContractAssertionShouldCompile(referencing, oldReferenced);
            //Assert.Throws<AssertionException>(() => ContractAssertionShouldCompile(referencing, newReferenced));
            var commentedAssertion = oldContractAssertion.Replace("    Create", "    //IGNORE This is disabled with a comment Create");

            var newAssertion = UpdatedContractAssertionShouldCompile(referencing, oldReferenced, newReferenced, commentedAssertion);
            StringAssert.Contains(IgnorancePreserver.AssertionSuppressor, newAssertion, "The new assertion should be commented out");
            Assert.AreEqual(commentedAssertion, newAssertion, "The new assertion should match the old one, since nothing else in the code has changed");
        }

        [Test]
        public void IgnoredAssertionUsingTypeFromDependencyTheReferencedDllNoLongerHasRemainsCommentedOut()
        {
            // The consumer was built against an old version, whose property type came from a dependency
            const string oldDependency = "namespace Name.Space { public delegate void OldStatusHandler(); }";
            var oldReferenced = Class("public OldStatusHandler Status { get; set; }", "Consumed");
            // The new version has its own property type, and no longer depends on the old dependency
            var newReferenced = Class("public NewStatusHandler Status { get; set; }", "Consumed")
                                + "namespace Name.Space { public delegate void NewStatusHandler(); }";
            var referencing = Class("public void SetsStatus(Consumed x) { x.Status = () => {}; }");

            var consumerDir = TempDir.Get();
            var oldDependencyDll = ClrAssemblyCompiler.CompileDll(consumerDir, NetFrameworkVersion.Net45, "DependencyAssembly", oldDependency);
            var oldReferencedDll = ClrAssemblyCompiler.CompileDll(TempDir.Get(), NetFrameworkVersion.Net45, "ReferencedAssembly", oldReferenced, oldDependencyDll);
            var referencingDll = ClrAssemblyCompiler.CompileDll(consumerDir, NetFrameworkVersion.Net45, "TestAssembly", referencing, oldReferencedDll, oldDependencyDll);
            var newReferencedDll = ClrAssemblyCompiler.CompileDll(TempDir.Get(), NetFrameworkVersion.Net45, "ReferencedAssembly", newReferenced);

            // When the consumer was up to date, the assertion named the consumer's property type, and was then ignored
            using var oldWriter = new StringWriter();
            ContractClassWriter.CreateContractAssertions(oldWriter, "", new[] { oldReferencedDll }, new[] { referencingDll });
            Console.WriteLine("*** Old assertion:");
            Console.WriteLine(oldWriter.ToString());
            StringAssert.Contains("Status = Create<Name.Space.OldStatusHandler>()", oldWriter.ToString());
            var commentedAssertion = oldWriter.ToString().Replace("    Create", "    //IGNORE Create");

            // Regenerating against the new version should still match the ignored line
            using var newWriter = new StringWriter();
            ContractClassWriter.CreateContractAssertions(newWriter, "", new[] { newReferencedDll }, new[] { referencingDll });
            Console.WriteLine("*** Newly generated assertion:");
            Console.WriteLine(newWriter.ToString());
            using var preservedWriter = new StringWriter();
            IgnorancePreserver.PreserveIgnoredAssertions(commentedAssertion, newWriter.ToString(), preservedWriter);
            Assert.AreEqual(commentedAssertion.TrimEnd(), preservedWriter.ToString().TrimEnd(),
                "The ignored assertion should stay ignored, since the consumer hasn't changed");
        }

        private string UpdatedContractAssertionShouldCompile(string referencing, string oldReferenced, string newReferenced, string oldContractAssertion)
        {
            var (oldReferencedDll, referencingDll) = ClrAssemblyCompiler.CompileDlls(referencing, oldReferenced);
            using var writer = new StringWriter();
            ContractClassWriter.CreateContractAssertions(writer, "", new[] { oldReferencedDll }, new[] { referencingDll });
            var newlyGeneratedAssertions = writer.ToString();

            using var writer2 = new StringWriter();
            IgnorancePreserver.PreserveIgnoredAssertions(oldContractAssertion, newlyGeneratedAssertions, writer2);
            Console.WriteLine("*** Commented out assertion:");
            Console.WriteLine(writer2.ToString());
            ClrAssemblyCompiler.CompileDlls(writer2 + ContractClassWriter.UtilsSource, newReferenced);
            StringAssert.Contains("private void UsedByTestAssembly()", writer2.ToString(),
                "We should have created a method to contain the assertions for this assembly");
            return writer2.ToString();
        }
    }
}
