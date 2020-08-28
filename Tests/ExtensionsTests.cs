using NetDoc;
using NetDoc.Utils;
using NUnit.Framework;

namespace Tests
{
    class ExtensionsTests
    {
        [Test]
        public void ToTitleCase()
        {
            Assert.AreEqual("FooBarBaz", "foo-bar.baz".ToTitleCase());
        }
    }
}
