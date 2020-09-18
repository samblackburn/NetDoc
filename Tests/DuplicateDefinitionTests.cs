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
    }
}
