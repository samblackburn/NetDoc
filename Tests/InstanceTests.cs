using NUnit.Framework;

namespace NetDoc.Tests
{
    [TestFixture]
    class InstanceTests : TestMethods
    {
        [Test]
        public void Constructor()
        {
            var referenced = Class("public ReferencedClass(int i) {}", "ReferencedClass");
            var referencing = Class("public ReferencedClass Bar() { return new ReferencedClass(3); }", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void FieldGetter()
        {
            var referenced = Class("public int Foo;", "ReferencedClass");
            var referencing = Class("public int Bar(ReferencedClass x) {return x.Foo;}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void FieldSetter()
        {
            var referenced = Class("public int Foo;", "ReferencedClass");
            var referencing = Class("public void Bar(ReferencedClass x) {x.Foo = 3;}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void Method()
        {
            var referenced = Class("public int Foo() => 3;", "ReferencedClass");
            var referencing = Class("public int Bar(ReferencedClass x) {return x.Foo();}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void VoidMethod()
        {
            var referenced = Class("public void Foo() {}", "ReferencedClass");
            var referencing = Class("public void Bar(ReferencedClass x) {x.Foo();}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void Getter()
        {
            var referenced = Class("public int Foo => 3;", "ReferencedClass");
            var referencing = Class("public int Bar(ReferencedClass x) {return x.Foo;}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void Setter()
        {
            var referenced = Class("public int Foo { get; set; }", "ReferencedClass");
            var referencing = Class("public void Bar(ReferencedClass x) {x.Foo = 3;}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void IndexerGetter()
        {
            var referenced = Class("public bool this[int x] { get {return true;} set {} }", "ReferencedClass");
            var referencing = Class("public bool Bar(ReferencedClass x) {return x[3];}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void IndexerSetter()
        {
            var referenced = Class("public bool this[int x] { get {return true;} set {} }", "ReferencedClass");
            var referencing = Class("public void Bar(ReferencedClass x) {x[3] = false;}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }
    }
}
