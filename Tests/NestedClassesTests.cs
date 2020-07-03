using NUnit.Framework;
using Tests.TestFramework;

namespace Tests
{
    [TestFixture]
    class NestedClassesTests : TestMethods
    {
        [Test]
        public void Constructor()
        {
            var referenced = Class(Class("public InnerClass(int i) {}", "InnerClass", null), "OuterClass");
            var referencing = Class("public OuterClass.InnerClass Bar() { return new OuterClass.InnerClass(3); }", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void VoidMethod()
        {
            var referenced = Class(Class("public void Foo(int i) {}", "InnerClass", null), "OuterClass");
            var referencing = Class("public void Bar(OuterClass.InnerClass x) { x.Foo(3); }", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }
    }
}
