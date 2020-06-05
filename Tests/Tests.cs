using System;
using System.IO;
using NUnit.Framework;
using RedGate.SQLCompare.Engine.TestUtils;

namespace NetDoc.Tests
{
    [TestFixture]
    class Tests
    {
        [Test]
        public void MethodReference()
        {
            var referenced = Class("public int Foo() => 3;", "ReferencedClass");
            var referencing = Class("public int Bar(ReferencedClass x) {return x.Foo();}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        private static void ContractAssertionShouldCompile(string referencing, string referenced)
        {
            var dlls = ClrAssemblyCompiler.CompileDlls(referencing, referenced);
            using var writer = new StringWriter();
            Program.CreateContractAssertions(writer, dlls[0], new[] {dlls[1]});
            Console.WriteLine(writer.ToString());
            ClrAssemblyCompiler.CompileDlls(writer.ToString(), referenced);
        }

        [TestCase("int x;", "Class2", null, ExpectedResult = "public class Class2 {int x;}")]
        [TestCase("int x;", "Class2", "ns", ExpectedResult = "namespace ns {public class Class2 {int x;}}")]
        public static string Class(string contents, string className = "Class1", string ns = "Name.Space")
        {
            return string.IsNullOrEmpty(ns)
                ? $"public class {className} {{{contents}}}"
                : $"namespace {ns} {{{Class(contents, className, null)}}}";
        }
    }
}
