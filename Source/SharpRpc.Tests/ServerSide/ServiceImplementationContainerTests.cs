#region License
/*
Copyright (c) 2013-2014 Daniil Rodin of Buhgalteria.Kontur team of SKB Kontur

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
        private IServiceImplementationFactory serviceImplementationFactory;
        private ServiceImplementationContainer container;

        [SetUp]
        public void Setup()
        {
            serviceImplementationFactory = Substitute.For<IServiceImplementationFactory>();
            serviceImplementationFactory.CreateImplementation(null, null).ReturnsForAnyArgs(CreateImplementationInfo);
            container = new ServiceImplementationContainer(serviceImplementationFactory);
        }

        private ServiceImplementationInfo CreateImplementationInfo(CallInfo callInfo)
        {
            return new ServiceImplementationInfo(null, Substitute.For<object>());
        }

        [Test]
        public void Unscoped()
        {
            var implementationInfo = container.GetImplementation("MyService", null);
            var oneMoreImplementationinfo = container.GetImplementation("MyService", null);

            serviceImplementationFactory.Received(1).CreateImplementation("MyService", null);
            Assert.That(implementationInfo, Is.EqualTo(oneMoreImplementationinfo));
        }

        [Test]
        public void Scopes()
        {
            var implementationInfo1 = container.GetImplementation("MyService", "scope1");
            var implementationInfo2 = container.GetImplementation("MyService", "scope2");
            var oneMoreImplementationInfo1 = container.GetImplementation("MyService", "scope1");

            serviceImplementationFactory.Received(1).CreateImplementation("MyService", "scope1");
            serviceImplementationFactory.Received(1).CreateImplementation("MyService", "scope2");

            Assert.That(implementationInfo1, Is.Not.EqualTo(implementationInfo2));
            Assert.That(implementationInfo1, Is.EqualTo(oneMoreImplementationInfo1));
        }

        [Test]
        public void DifferentTypes()
        {
            var implementationInfo = container.GetImplementation("MyService", null);
            var otherImplementationInfo = container.GetImplementation("MyOtherService", null);

            serviceImplementationFactory.Received(1).CreateImplementation("MyService", null);
            serviceImplementationFactory.Received(1).CreateImplementation("MyOtherService", null);
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

            Assert.That(container.GetInitializedScopesFor("MyService"), Is.EquivalentTo(new[] { "S1", "S2", "S3" }));
            Assert.That(container.GetInitializedScopesFor("MyOtherService"), Is.EquivalentTo(new[] { "otherS", "otherS2" }));
        }
    }
}