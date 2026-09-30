using NUnit.Framework;
using Tests.TestFramework;

namespace Tests
{
    /// <summary>
    /// Consumers that use a referenced dll's internals should stay visible in the contract assertions.
    /// To make them compile, the referenced dll must also make its internals visible to the contract assertions.
    /// </summary>
    [TestFixture]
    class InternalsVisibleToTests : TestMethods
    {
        // The consumer and the contract assertion are both compiled as TestAssembly
        private const string InternalsVisibleToConsumerAndAssertion =
            "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"TestAssembly\")]";

        [Test]
        public void InternalStaticProperty()
        {
            var referenced = InternalsVisibleToConsumerAndAssertion + Class("internal static int Foo { get; set; }", "ReferencedClass");
            var referencing = Class("public int Bar() => ReferencedClass.Foo;", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void InternalConstructor()
        {
            var referenced = InternalsVisibleToConsumerAndAssertion + Class("public ReferencedClass() {} internal ReferencedClass(int x) {}", "ReferencedClass");
            var referencing = Class("public ReferencedClass Bar() => new ReferencedClass(3);", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void InternalField()
        {
            var referenced = InternalsVisibleToConsumerAndAssertion + Class("internal int Foo;", "ReferencedClass");
            var referencing = Class("public int Bar(ReferencedClass x) => x.Foo;", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }
    }
}
