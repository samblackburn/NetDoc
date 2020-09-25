using NUnit.Framework;
using Tests.TestFramework;

namespace Tests
{
    class ParametersTests : TestMethods
    {
        [Test]
        public void ParameterTypeInReferencedAssembly()
        {
            var referenced = Class(@"public static void Foo(Bar b) {}", "ReferencedClass")
                             + Class("", "Bar");
            var referencing = Class(@"public void Foo() => ReferencedClass.Foo(new Bar());");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void ParameterTypeInMscorlib()
        {
            var referenced = Class(@"public static void Foo(int b) {}", "ReferencedClass");
            var referencing = Class(@"public void Foo() => ReferencedClass.Foo(3);");
            ContractAssertionShouldCompile(referencing, referenced);
        }
    }
}
