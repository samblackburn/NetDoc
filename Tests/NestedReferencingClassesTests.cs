using NUnit.Framework;
using Tests.TestFramework;

namespace Tests
{
    class NestedReferencingClassesTests : TestMethods
    {
        [Test]
        public void ConsumerIsYieldReturnStateMachine()
        {
            var referenced = Class("public static int Foo() => 1;", "ReferencedClass");
            var referencing = Class(@"System.Collections.Generic.IEnumerable<int> Bar() { yield return 1; yield return ReferencedClass.Foo(); }");

            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void ConsumerHasNestedClass()
        {
            var referenced = Class("public static int Foo() => 1;", "ReferencedClass");
            var referencing = Class(Class(@"int Bar() => ReferencedClass.Foo();", "NestedClass", null), "OuterClass");

            ContractAssertionShouldCompile(referencing, referenced);
        }
    }
}
