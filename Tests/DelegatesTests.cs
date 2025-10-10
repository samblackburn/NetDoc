using NUnit.Framework;
using Tests.TestFramework;

namespace Tests;

[TestFixture]
internal class DelegatesTests : TestMethods
{
    [Test]
    public void DelegateWithOneArgument()
    {
        var referenced = "public delegate bool MyDelegate(int i);";
        var referencing = Class("MyDelegate MakeDelegate() => Foo; bool Foo(int i) => i > 3;", "ReferencingClass");
        ContractAssertionShouldCompile(referencing, referenced);
    }

    [Test, Ignore("Not sure how to infer the delegate's signature")]
    public void DelegateWithTwoArguments()
    {
        var referenced = "public delegate bool MyDelegate(int i, int j);";
        var referencing = Class("MyDelegate MakeDelegate() => (i, j) => i > j;", "ReferencingClass");
        ContractAssertionShouldCompile(referencing, referenced);
    }

    [Test, Ignore("Not sure how to infer the delegate's signature")]
    public void VoidDelegate()
    {
        var referenced = "public delegate void MyDelegate(int i);";
        var referencing = Class("MyDelegate MakeDelegate() => i => {};", "ReferencingClass");
        ContractAssertionShouldCompile(referencing, referenced);
    }
}
