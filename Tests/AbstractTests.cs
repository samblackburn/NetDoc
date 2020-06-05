using NUnit.Framework;

namespace NetDoc.Tests
{
    [TestFixture]
    class AbstractTests : TestMethods
    {
        [Test]
        public void ObjectFactoryMethod()
        {
            var referenced = "public abstract class ReferencedClass {}";
            var referencing = "public class Derived : ReferencedClass {}";
            ContractAssertionShouldBeEmpty(referencing, referenced);
        }
    }
}