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
        public void Constructor()
        {
            var referenced = Class("public ReferencedClass(int i) {}", "ReferencedClass");
            var referencing = Class("public void Bar() { var _ = new ReferencedClass(3); }", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void Field()
        {
            var referenced = Class("public int Foo;", "ReferencedClass");
            var referencing = Class("public int Bar(ReferencedClass x) {return x.Foo;}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void Method()
        {
            var referenced = Class("public int Foo() => 3;", "ReferencedClass");
            var referencing = Class("public int Bar(ReferencedClass x) {return x.Foo();}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void VoidMethod()
        {
            var referenced = Class("public void Foo() {}", "ReferencedClass");
            var referencing = Class("public void Bar(ReferencedClass x) {x.Foo();}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void Getter()
        {
            var referenced = Class("public int Foo => 3;", "ReferencedClass");
            var referencing = Class("public int Bar(ReferencedClass x) {return x.Foo;}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void Setter()
        {
            var referenced = Class("public int Foo { get; set; }", "ReferencedClass");
            var referencing = Class("public void Bar(ReferencedClass x) {x.Foo = 3;}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void IndexerGetter()
        {
            var referenced = Class("public bool this[int x] { get {return true;} set {} }", "ReferencedClass");
            var referencing = Class("public bool Bar(ReferencedClass x) {return x[3];}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void IndexerSetter()
        {
            var referenced = Class("public bool this[int x] { get {return true;} set {} }", "ReferencedClass");
            var referencing = Class("public void Bar(ReferencedClass x) {x[3] = false;}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void StaticField()
        {
            var referenced = Class("public static int Foo;", "ReferencedClass");
            var referencing = Class("public static int Bar() {return ReferencedClass.Foo;}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void StaticMethod()
        {
            var referenced = Class("public static int Foo() => 3;", "ReferencedClass");
            var referencing = Class("public static int Bar() {return ReferencedClass.Foo();}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void StaticVoidMethod()
        {
            var referenced = Class("public static void Foo() {}", "ReferencedClass");
            var referencing = Class("public static void Bar() {ReferencedClass.Foo();}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void StaticGetter()
        {
            var referenced = Class("public static int Foo => 3;", "ReferencedClass");
            var referencing = Class("public static int Bar() {return ReferencedClass.Foo;}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void StaticSetter()
        {
            var referenced = Class("public static int Foo { get; set; }", "ReferencedClass");
            var referencing = Class("public static void Bar() {ReferencedClass.Foo = 3;}", "ReferencingClass");
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
