using System;
using System.IO;
using NetDoc;
using NUnit.Framework;
using Tests.TestFramework;

namespace Tests
{
    class MissingLibraryTests : TestMethods
    {
        [Test]
        public void MissingLibrariesDoNotCauseError()
        {
            var missing = Class("public void Foo() {}", "MissingClass");
            var referencing = Class("public void Foo() {new MissingClass().Foo();}");
            ContractAssertionShouldBeEmptyWithMissingLibrary(referencing, missing);
        }

        [Test]
        public void TwoReferencedDlls ()
        {
            var referenced2 = Class("", "ReferencedClass2");
            var referenced1 = Class("public static System.Collections.Generic.IEnumerable<ReferencedClass1<T>> Foo() => null;", "ReferencedClass1<T>");
            var referencing = Class("public System.Collections.Generic.IEnumerable<ReferencedClass1<ReferencedClass2>> Foo() => ReferencedClass1<ReferencedClass2>.Foo();");
            ContractAssertionShouldCompileWithTwoReferenced(referencing, referenced1, referenced2);
        }

        private static void ContractAssertionShouldBeEmptyWithMissingLibrary(string referencing, string missing)
        {
            var (missingDll, referencingDll) = ClrAssemblyCompiler.CompileDlls(referencing, missing);
            File.Delete(missingDll);
            using var writer = new StringWriter();
            ContractClassWriter.CreateContractAssertions(writer, "", new string[] { }, new[] { referencingDll });
            Console.WriteLine(writer.ToString());
            StringAssert.DoesNotContain("private void UsedByTestAssembly()", writer.ToString(),
                "There shouldn't be a contract assertion");
        }

        private static void ContractAssertionShouldCompileWithTwoReferenced(string referencing, string referenced1, string referenced2)
        {
            var referencedDll2 = ClrAssemblyCompiler.CompileDll(TempDir.Get(), NetFrameworkVersion.Net45, "ReferencedAssembly2", referenced2);
            var referencedDll1 = ClrAssemblyCompiler.CompileDll(TempDir.Get(), NetFrameworkVersion.Net45, "ReferencedAssembly1", referenced1, referencedDll2);
            var referencingDll = ClrAssemblyCompiler.CompileDll(TempDir.Get(), NetFrameworkVersion.Net45, "TestAssembly", referencing, referencedDll1, referencedDll2);
            using var writer = new StringWriter();
            ContractClassWriter.CreateContractAssertions(writer, "", new[] {referencedDll1, referencedDll2}, new[] {referencingDll});
            Console.WriteLine(writer.ToString());
            ClrAssemblyCompiler.CompileDll(TempDir.Get(), NetFrameworkVersion.Net45, "TestAssembly",
                writer + ContractClassWriter.UtilsSource, referencedDll1, referencedDll2);
            StringAssert.Contains("private void UsedByTestAssembly()", writer.ToString(),
                "We should have created a method to contain the assertions for this assembly");
        }
    }
}
