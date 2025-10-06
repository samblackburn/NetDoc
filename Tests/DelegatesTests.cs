using NUnit.Framework;
using Tests.TestFramework;

namespace Tests;

[TestFixture]
internal class DelegatesTests : TestMethods
{
    [Test]
    public void Delegate()
    {
        var referenced = "public delegate bool MyDelegate(int i);";
        var referencing = Class("MyDelegate MakeDelegate() => i => i > 3;", "ReferencingClass");
        ContractAssertionShouldCompile(referencing, referenced);
    }
}