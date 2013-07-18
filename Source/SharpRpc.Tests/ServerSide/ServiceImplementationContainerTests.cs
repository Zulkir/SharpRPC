#region License
/*
Copyright (c) 2013 Daniil Rodin of Buhgalteria.Kontur team of SKB Kontur

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion

using System;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using SharpRpc.Reflection;
using SharpRpc.ServerSide;

namespace SharpRpc.Tests.ServerSide
{
    [TestFixture]
    public class ServiceImplementationContainerTests
    {
        private ServiceDescription serviceDescription;
        private IServiceDescriptionBuilder serviceDescriptionBuilder;
        private ServiceImplementationContainer container;

        interface IMyService
        {
            void DoSomething();
        }

        interface IMyOtherService
        {
             
        }

        class MyServiceImplementation : IMyService, IMyOtherService, IServiceImplementation
        {
            public ServiceImplementationState State { get; private set; }
            public void Initialize(IRpcKernel kernel, IReadOnlyDictionary<string, string> settings, string scope) { throw new NotImplementedException(); }
            public void Dispose() { throw new NotImplementedException(); }
            public void DoSomething() { throw new NotImplementedException(); }
        }

        [SetUp]
        public void Setup()
        {
            serviceDescription = new ServiceDescription(typeof(IMyService), "MyService", new SubserviceDescription[0], 
                new[] {new MethodDescription(typeof(void), "DoSomething", new MethodParameterDescription[0])});
            serviceDescriptionBuilder = Substitute.For<IServiceDescriptionBuilder>();
            serviceDescriptionBuilder.Build(typeof(IMyService)).Returns(serviceDescription);
            container = new ServiceImplementationContainer(serviceDescriptionBuilder);
        }

        [Test]
        public void Basic()
        {
            container.RegisterImplementation(typeof(IMyService), typeof(MyServiceImplementation));

            var implementationInfo = container.GetImplementation("MyService", null);
            Assert.That(implementationInfo.Interface, Is.EqualTo(typeof(IMyService)));
            Assert.That(implementationInfo.Description, Is.EqualTo(serviceDescription));
            Assert.That(implementationInfo.Implementation, Is.TypeOf<MyServiceImplementation>());
        }

        [Test]
        public void BasicScoped()
        {
            container.RegisterImplementation(typeof(IMyService), typeof(MyServiceImplementation));

            var implementationInfo = container.GetImplementation("MyService", "myscope");
            Assert.That(implementationInfo.Interface, Is.EqualTo(typeof(IMyService)));
            Assert.That(implementationInfo.Description, Is.EqualTo(serviceDescription));
            Assert.That(implementationInfo.Implementation, Is.TypeOf<MyServiceImplementation>());
        }

        [Test]
        public void Unscoped()
        {
            container.RegisterImplementation(typeof(IMyService), typeof(MyServiceImplementation));

            var implementationInfo = container.GetImplementation("MyService", null);
            var oneMoreImplementationinfo = container.GetImplementation("MyService", null);

            Assert.That(implementationInfo, Is.EqualTo(oneMoreImplementationinfo));
        }

        [Test]
        public void Scopes()
        {
            container.RegisterImplementation(typeof(IMyService), typeof(MyServiceImplementation));

            var implementationInfo1 = container.GetImplementation("MyService", "scope1");
            var implementationInfo2 = container.GetImplementation("MyService", "scope2");

            var oneMoreImplementationInfo1 = container.GetImplementation("MyService", "scope1");

            Assert.That(implementationInfo1, Is.Not.EqualTo(implementationInfo2));
            Assert.That(implementationInfo1, Is.EqualTo(oneMoreImplementationInfo1));
        }

        [Test]
        public void DifferentTypes()
        {
            container.RegisterImplementation(typeof(IMyService), typeof(MyServiceImplementation));
            container.RegisterImplementation(typeof(IMyOtherService), typeof(MyServiceImplementation));

            var implementationInfo = container.GetImplementation("MyService", null);
            var otherImplementationInfo = container.GetImplementation("MyOtherService", null);
            
            Assert.That(implementationInfo.Interface, Is.Not.EqualTo(otherImplementationInfo.Interface));
            Assert.That(implementationInfo.Implementation, Is.Not.EqualTo(otherImplementationInfo.Implementation));
        }

        [Test]
        public void InitializedScopes()
        {
            container.RegisterImplementation(typeof(IMyService), typeof(MyServiceImplementation));
            container.RegisterImplementation(typeof(IMyOtherService), typeof(MyServiceImplementation));

            container.GetImplementation("MyService", "S1");
            container.GetImplementation("MyService", "S2");
            container.GetImplementation("MyService", "S3");
            container.GetImplementation("MyOtherService", "otherS");
            container.GetImplementation("MyOtherService", "otherS2");

            Assert.That(container.GetInitializedScopesFor("MyService"), Is.EquivalentTo(new[] { "S1", "S2", "S3" }));
            Assert.That(container.GetInitializedScopesFor("MyOtherService"), Is.EquivalentTo(new[] { "otherS", "otherS2" }));
        }
    }
}