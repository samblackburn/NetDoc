using NUnit.Framework;
using Tests.TestFramework;

namespace Tests
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

        [Test]
        public void ImplicitTypeArgument()
        {
            var referenced = Class(@"public void Method<T>(T param) {}", "ReferencedClass");
            var referencing = Class("public void Method() => new ReferencedClass().Method(2);");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void ParameterizedFactoryMethod()
        {
            var referenced = Class("public T Foo() => default;", "ReferencedClass<T>");
            var referencing = Class("public OnlyInReferenced Bar(ReferencedClass<OnlyInReferenced> x) {return x.Foo();}", "ReferencingClass")
                              + Class("", "OnlyInReferenced");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void AssertionMethod()
        {
            var referenced = Class("public void Foo(T param) {}", "ReferencedClass<T>");
            var referencing = Class("public void Bar(ReferencedClass<int> x) {x.Foo(3);}", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void TypeConstraint()
        {
            var referenced = Class("public void Foo(T param) {}", "ReferencedClass<T, U> where T : System.Collections.Generic.IEnumerable<U>");
            var referencing = Class("public void Bar(ReferencedClass<DerivedList, int> x) {x.Foo(new DerivedList());}", "ReferencingClass")
                + Class("", "DerivedList : System.Collections.Generic.List<int>");
            ContractAssertionShouldCompile(referencing, referenced);
        }
        
        [Test]
        public void TypeConstraintAlsoInReferencingMethod()
        {
            var referenced = Class("public T Foo() => default;", "ReferencedClass<T> where T : System.IDisposable");
            var referencing = Class("void Blah<T>(ReferencedClass<T> x) where T : System.IDisposable => x.Foo();");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void GenericCaller()
        {
            var referenced = Class("public void Method() {}", "ReferencedClass<T>");
            var referencing = Class("public void Method(ReferencedClass<T> x) => x.Method();", "ReferencingClass<T>");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void GenericParameterNestedInsideFuncParameter()
        {
            var referenced = Class("public static string GetOptionsPayload<T>(System.Func<T, bool> predicate) => default;", "ReferencedClass")
                              + Class("", "OnlyInReferenced", "Name.Space", "struct");
            var referencing = Class("public static void Bar() { ReferencedClass.GetOptionsPayload<OnlyInReferenced>(x => true); }", "ReferencingClass");
            ContractAssertionShouldCompile(referencing, referenced);
        }
    }
}