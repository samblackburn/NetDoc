using NUnit.Framework;
using Tests.TestFramework;

namespace Tests
{
    [TestFixture]
    class StaticTests : TestMethods
    {
        [Test]
        public void StaticFieldGetter()
        {
            var referenced = Class("public static int Foo;", "ReferencedClass");
            var referencing = Class("public static int Bar() {return ReferencedClass.Foo;}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void StaticFieldSetter()
        {
            var referenced = Class("public static int Foo;", "ReferencedClass");
            var referencing = Class("public static void Bar() {ReferencedClass.Foo = 3;}", "ReferencingClass");
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
    }
}
