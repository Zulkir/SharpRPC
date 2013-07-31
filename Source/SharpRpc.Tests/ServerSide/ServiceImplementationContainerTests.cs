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

using NSubstitute;
using NSubstitute.Core;
using NUnit.Framework;
using SharpRpc.ServerSide;

namespace SharpRpc.Tests.ServerSide
{
    [TestFixture]
    public class ServiceImplementationContainerTests
    {
        private IRpcClientServer clientServer;
        private IServiceImplementationFactory serviceImplementationFactory;
        private ServiceImplementationContainer container;

        [SetUp]
        public void Setup()
        {
            clientServer = Substitute.For<IRpcClientServer>();
            serviceImplementationFactory = Substitute.For<IServiceImplementationFactory>();
            serviceImplementationFactory.CreateImplementation(null).ReturnsForAnyArgs(CreateImplementationInfo);
            container = new ServiceImplementationContainer(clientServer, serviceImplementationFactory);
        }

        private ServiceImplementationInfo CreateImplementationInfo(CallInfo callInfo)
        {
            var serviceImplementation = Substitute.For<IServiceImplementation>();
            serviceImplementation.WhenForAnyArgs(x => x.Initialize(null, null)).Do(x => serviceImplementation.State.Returns(ServiceImplementationState.Running));
            return new ServiceImplementationInfo(null, serviceImplementation);
        }
        
        [Test]
        public void Initialization()
        {
            var implementationInfo = container.GetImplementation("MyService", null);
            Assert.That(implementationInfo.Implementation.State, Is.EqualTo(ServiceImplementationState.Running));
        }

        [Test]
        public void Unscoped()
        {
            var implementationInfo = container.GetImplementation("MyService", null);
            var oneMoreImplementationinfo = container.GetImplementation("MyService", null);

            serviceImplementationFactory.Received(1).CreateImplementation("MyService");
            Assert.That(implementationInfo, Is.EqualTo(oneMoreImplementationinfo));
        }

        [Test]
        public void Scopes()
        {
            var implementationInfo1 = container.GetImplementation("MyService", "scope1");
            var implementationInfo2 = container.GetImplementation("MyService", "scope2");

            var oneMoreImplementationInfo1 = container.GetImplementation("MyService", "scope1");

            serviceImplementationFactory.Received(2).CreateImplementation("MyService");
            Assert.That(implementationInfo1, Is.Not.EqualTo(implementationInfo2));
            Assert.That(implementationInfo1, Is.EqualTo(oneMoreImplementationInfo1));
        }

        [Test]
        public void DifferentTypes()
        {
            var implementationInfo = container.GetImplementation("MyService", null);
            var otherImplementationInfo = container.GetImplementation("MyOtherService", null);

            serviceImplementationFactory.Received(1).CreateImplementation("MyService");
            serviceImplementationFactory.Received(1).CreateImplementation("MyOtherService");
            Assert.That(implementationInfo, Is.Not.EqualTo(otherImplementationInfo));
        }

        [Test]
        public void InitializedScopes()
        {
            container.GetImplementation("MyService", "S1");
            container.GetImplementation("MyService", "S2");
            container.GetImplementation("MyService", "S3");
            container.GetImplementation("MyOtherService", "otherS");
            container.GetImplementation("MyOtherService", "otherS2");

            serviceImplementationFactory.Received(3).CreateImplementation("MyService");
            serviceImplementationFactory.Received(2).CreateImplementation("MyOtherService");
            Assert.That(container.GetInitializedScopesFor("MyService"), Is.EquivalentTo(new[] { "S1", "S2", "S3" }));
            Assert.That(container.GetInitializedScopesFor("MyOtherService"), Is.EquivalentTo(new[] { "otherS", "otherS2" }));
        }
    }
}