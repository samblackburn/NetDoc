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
        public void VoidMethodWithOnlyTypeArgument()
        {
            var referenced = Class("public static void Foo<T>() {}", "ReferencedClass");
            var referencing = Class("public void Bar() => ReferencedClass.Foo<int>();", "ReferencingClass");
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

        [Test]
        public void GenericReturnTypeOverStructOnlyInReferencing()
        {
            var referenced = Class("public Wrapper<T> Foo() => null;", "ReferencedClass<T>")
                             + Class("", "Wrapper<T>");
            var referencing = Class("public object Bar(ReferencedClass<OnlyInReferencing> x) => x.Foo();", "ReferencingClass")
                              + Class("", "OnlyInReferencing", "Name.Space", "struct");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void StructConstrainedGenericMethodOverEnumOnlyInReferencing()
        {
            var referenced = Class("public static string Foo<T>(System.Func<T, bool> predicate) where T : struct => default;", "ReferencedClass");
            var referencing = Class("public static string Bar() => ReferencedClass.Foo<OnlyInReferencing>(x => true);", "ReferencingClass")
                              + Class("", "OnlyInReferencing", "Name.Space", "enum");
            ContractAssertionShouldCompile(referencing, referenced);
        }

        [Test]
        public void ConstructorParameterGenericOverInterfaceOnlyInReferencing()
        {
            var referenced = Class("public Foo(System.Collections.Generic.ISet<T> inner) {}", "Foo<T>");
            var referencing = Class("public void Bar() { new Foo<IOnlyInReferencing>(new System.Collections.Generic.HashSet<IOnlyInReferencing>()); }", "ReferencingClass")
                              + Class("", "IOnlyInReferencing", "Name.Space", "interface");
            ContractAssertionShouldCompile(referencing, referenced);
        }
    }
}