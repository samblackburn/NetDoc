using NUnit.Framework;
using Tests.TestFramework;

namespace Tests
{
    class InterfaceTests : TestMethods
    {
        [Test]
        public void InterfaceDuplicatedBetweenDlls()
        {
            var inBoth = @"public interface IFoo {string Method();}";
            var referencing = Class("public string Consumer(IFoo foo) => foo.Method();");
            ContractAssertionShouldBeEmpty(referencing + inBoth, inBoth);
        }

        [Test]
        public void NestedClass()
        {
            var inBoth = Class(Class("public NestedClass(int i) {}", "NestedClass : BaseClass", null), "MyClass") +
                         Class("", "BaseClass");
            var referencing = Class("internal void Foo() {new MyClass.NestedClass(0);}");
            ContractAssertionShouldBeEmpty(referencing + inBoth, inBoth);
        }
    }
}
