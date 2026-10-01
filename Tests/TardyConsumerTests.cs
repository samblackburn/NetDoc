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
        public void SignatureTypeFromDependencyTheReferencedDllNoLongerHasIsNamedAsTheConsumerReferencesIt()
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

            using var writer = new StringWriter();
            ContractClassWriter.CreateContractAssertions(writer, "", new[] { newReferencedDll }, new[] { referencingDll });
            Console.WriteLine(writer.ToString());
            // object would never compile here, so name the type the consumer expects, keeping the break visible
            StringAssert.Contains("Status = Create<Name.Space.OldStatusHandler>()", writer.ToString());
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
