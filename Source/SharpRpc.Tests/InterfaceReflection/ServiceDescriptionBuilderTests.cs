using System;
using NSubstitute;
using NUnit.Framework;
using SharpRpc.Reflection;

namespace SharpRpc.Tests.InterfaceReflection
{
    [TestFixture]
    public class ServiceDescriptionBuilderTests
    {
        private ServiceDescriptionBuilder builder;
        private IMethodDescriptionBuilder methodDescriptionBuilder;

        [SetUp]
        public void Setup()
        {
            methodDescriptionBuilder = Substitute.For<IMethodDescriptionBuilder>();
            builder = new ServiceDescriptionBuilder(methodDescriptionBuilder);
        }

        private interface IFooBar
        {
            void Foo();
            void Bar();
        }

        [Test]
        public void BuildSimple()
        {
            var fooDesc = new MethodDescription(null, "Foo", new MethodParameterDescription[0]);
            var barDesc = new MethodDescription(null, "Bar", new MethodParameterDescription[0]);
            methodDescriptionBuilder.Build(typeof(IFooBar).GetMethod("Foo")).Returns(fooDesc);
            methodDescriptionBuilder.Build(typeof(IFooBar).GetMethod("Bar")).Returns(barDesc);

            var desc = builder.Build(typeof (IFooBar));
            Assert.That(desc.Type, Is.EqualTo(typeof(IFooBar)));
            Assert.That(desc.Name, Is.EqualTo("FooBar"));
            Assert.That(desc.Subservices, Is.Empty);
            Assert.That(desc.Methods, Is.EquivalentTo(new[] { fooDesc, barDesc }));
        }

        interface ISomethingWithSetters
        {
            int SetMe { get; set; }
        }

        [Test]
        public void ThrowOnPropertiesWithSetters()
        {
            Assert.Throws<ArgumentException>(() => builder.Build(typeof(ISomethingWithSetters)));
        }

        interface IComplex
        {
            IFooBar MyFooBar { get; } 
        }

        [Test]
        public void Nested()
        {
            var fooDesc = new MethodDescription(null, "Foo", new MethodParameterDescription[0]);
            var barDesc = new MethodDescription(null, "Bar", new MethodParameterDescription[0]);
            methodDescriptionBuilder.Build(typeof(IFooBar).GetMethod("Foo")).Returns(fooDesc);
            methodDescriptionBuilder.Build(typeof(IFooBar).GetMethod("Bar")).Returns(barDesc);

            var desc = builder.Build(typeof(IComplex));
            Assert.That(desc.Subservices.Count, Is.EqualTo(1));
            Assert.That(desc.Subservices[0].Name, Is.EqualTo("MyFooBar"));
            Assert.That(desc.Subservices[0].Service.Name, Is.EqualTo("FooBar"));
            Assert.That(desc.Subservices[0].Service.Methods, Is.EquivalentTo(new[] { fooDesc, barDesc }));
        }

        [Test]
        public void PropertiesAreNotMethods()
        {
            var fooDesc = new MethodDescription(null, "Foo", new MethodParameterDescription[0]);
            var barDesc = new MethodDescription(null, "Bar", new MethodParameterDescription[0]);
            methodDescriptionBuilder.Build(typeof(IFooBar).GetMethod("Foo")).Returns(fooDesc);
            methodDescriptionBuilder.Build(typeof(IFooBar).GetMethod("Bar")).Returns(barDesc);

            var desc = builder.Build(typeof(IComplex));
            Assert.That(desc.Methods, Is.Empty);
        }
    }
}