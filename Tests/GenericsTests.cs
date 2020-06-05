using NUnit.Framework;

namespace NetDoc.Tests
{
    [TestFixture]
    class GenericsTests : TestMethods
    {
        [Test]
        public void ObjectFactoryMethod()
        {
            var referenced = Class("public T Foo<T>() => default;", "ReferencedClass");
            var referencing = Class("public int Bar(ReferencedClass x) {return x.Foo<int>();}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }
    }
}
