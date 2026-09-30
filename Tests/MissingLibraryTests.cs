using System;
using System.IO;
using System.Linq;
using NetDoc;
using NUnit.Framework;
using NUnit.Framework.Legacy;
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

        [Test]
        public void GenericArgumentFromAnotherReferencingDll()
        {
            var sibling = Class("", "SiblingClass");
            var referenced = Class("public T Foo<T>(System.Func<Wrapper<T>> func) => default;", "ReferencedClass")
                             + Class("", "Wrapper<T>");
            var referencing = Class("public SiblingClass Bar(ReferencedClass x) => x.Foo<SiblingClass>(() => null);");
            ContractAssertionShouldCompileWithOtherDll(referencing, referenced, sibling, referencedDependsOnOther: false);
        }

        [Test]
        public void ParameterFromDependencyOfReferencedDll()
        {
            var dependency = Class("", "DependencyClass");
            var referenced = Class("public void Foo(DependencyClass x) {}", "ReferencedClass");
            var referencing = Class("public void Bar(ReferencedClass x) => x.Foo(new DependencyClass());");
            ContractAssertionShouldCompileWithOtherDll(referencing, referenced, dependency, referencedDependsOnOther: true);
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

        /// <param name="other">C# source for a dll the consumer also references</param>
        /// <param name="referencedDependsOnOther">
        /// True if the referenced dll depends on the other dll, so the assertion can see it too;
        /// false if it's only used by the consumer, e.g. another dll in the consuming product
        /// </param>
        private static void ContractAssertionShouldCompileWithOtherDll(string referencing, string referenced, string other, bool referencedDependsOnOther)
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
    }
}
