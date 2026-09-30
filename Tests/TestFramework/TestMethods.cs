using System;
using System.IO;
using System.Linq;
using NetDoc;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Tests.TestFramework
{
    class TestMethods
    {
        /// <summary>
        /// Generates a contract assertion and asserts that it compiles
        /// </summary>
        /// <param name="referencing">C# source for the referencing class(es)</param>
        /// <param name="referenced">C# source for the referenced class(es)</param>
        /// <returns>C# source for the contract assertion</returns>
        protected static string ContractAssertionShouldCompile(string referencing, string referenced)
        {
            var (referencedDll, referencingDll) = ClrAssemblyCompiler.CompileDlls(referencing, referenced);
            using var writer = new StringWriter();
            ContractClassWriter.CreateContractAssertions(writer, "", new[] {referencedDll}, new[] {referencingDll});
            Console.WriteLine(writer.ToString());
            ClrAssemblyCompiler.CompileDlls(writer + ContractClassWriter.UtilsSource, referenced);
            StringAssert.Contains("private void UsedByTestAssembly()", writer.ToString(),
                "We should have created a method to contain the assertions for this assembly");
            return writer.ToString();
        }

        /// <summary>
        /// Generates a contract assertion for a consumer that also references another dll, and asserts that it compiles
        /// </summary>
        /// <param name="referencing">C# source for the referencing class(es)</param>
        /// <param name="referenced">C# source for the referenced class(es)</param>
        /// <param name="other">C# source for a dll the consumer also references</param>
        /// <param name="referencedDependsOnOther">
        /// True if the referenced dll depends on the other dll, so the assertion can see it too;
        /// false if it's only used by the consumer, e.g. another dll in the consuming product
        /// </param>
        protected static void ContractAssertionShouldCompileWithOtherDll(string referencing, string referenced, string other, bool referencedDependsOnOther)
        {
            var otherDll = ClrAssemblyCompiler.CompileDll(TempDir.Get(), NetFrameworkVersion.Net45, "OtherAssembly", other);
            var visibleToAssertion = referencedDependsOnOther ? new[] {otherDll} : new string[0];
            var referencedDll = ClrAssemblyCompiler.CompileDll(TempDir.Get(), NetFrameworkVersion.Net45, "ReferencedAssembly", referenced, visibleToAssertion);
            var referencingDll = ClrAssemblyCompiler.CompileDll(TempDir.Get(), NetFrameworkVersion.Net45, "TestAssembly", referencing, referencedDll, otherDll);
            using var writer = new StringWriter();
            ContractClassWriter.CreateContractAssertions(writer, "", new[] {referencedDll}, new[] {referencingDll});
            Console.WriteLine(writer.ToString());
            ClrAssemblyCompiler.CompileDll(TempDir.Get(), NetFrameworkVersion.Net45, "TestAssembly",
                writer + ContractClassWriter.UtilsSource, visibleToAssertion.Prepend(referencedDll).ToArray());
            StringAssert.Contains("private void UsedByTestAssembly()", writer.ToString(),
                "We should have created a method to contain the assertions for this assembly");
        }

        protected static void ContractAssertionShouldBeEmpty(string referencing, string referenced)
        {
            var (referencedDll, referencingDll) = ClrAssemblyCompiler.CompileDlls(referencing, referenced);
            using var writer = new StringWriter();
            ContractClassWriter.CreateContractAssertions(writer, "", new[] {referencedDll}, new[] {referencingDll});
            Console.WriteLine(writer.ToString());
            StringAssert.DoesNotContain("private void UsedByTestAssembly()", writer.ToString(),
                "There shouldn't be a contract assertion");
        }

        [TestCase("int x;", "Class2", null, ExpectedResult = "public class Class2 {int x;}")]
        [TestCase("", "INterface2", null, "interface", ExpectedResult = "public interface INterface2 {}")]
        [TestCase("int x;", "Class2", "ns", ExpectedResult = "namespace ns {public class Class2 {int x;}}")]
        public static string Class(string contents, string className = "Class1", string? ns = "Name.Space",
            string notAClass = "class")
        {
            return string.IsNullOrEmpty(ns)
                ? $"public {notAClass} {className} {{{contents}}}"
                : $"namespace {ns} {{{Class(contents, className, null, notAClass)}}}";
        }

        [TearDown]
        public void TearDown()
        {
            TempDir.CleanUp();
        }
    }
}
