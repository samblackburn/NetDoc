using NUnit.Framework;

namespace NetDoc.Tests
{
    /// <summary>
    /// Ideally we'd produce assertions for inheritance hierarchies,
    /// but it doesn't fit the one-liner style of contract assertions.
    /// For now, we'll just ignore these since we don't often see
    /// inheritance across API boundaries.
    /// </summary>
    [TestFixture]
    class AbstractTests : TestMethods
    {
        [Test]
        public void CallToBaseConstructorIsIgnored()
        {
            var referenced = "public abstract class ReferencedClass {}";
            var referencing = "public class Derived : ReferencedClass {}";
            ContractAssertionShouldBeEmpty(referencing, referenced);
        }

        [Test]
        public void CallToBaseMethodIsIgnored()
        {
            var referenced = "public abstract class ReferencedClass { protected void Foo() {} }";
            var referencing = "public class Derived : ReferencedClass { void Bar() => Foo(); }";
            ContractAssertionShouldBeEmpty(referencing, referenced);
        }
    }
}