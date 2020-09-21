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
    }
}
