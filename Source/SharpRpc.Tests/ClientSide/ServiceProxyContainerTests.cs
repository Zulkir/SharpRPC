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
using NUnit.Framework;
using SharpRpc.ClientSide;

namespace SharpRpc.Tests.ClientSide
{
    [TestFixture]
    public class ServiceProxyContainerTests
    {
        public interface IMyService
        {
             
        }

        public interface IMyOtherService
        {
            
        }

        public class MyServiceProxy : IMyService, IMyOtherService
        {
            public IOutgoingMethodCallProcessor Processor { get; private set; }
            public string Scope { get; private set; }
            public TimeoutSettings TimeoutSettings { get; private set; }

            public MyServiceProxy(IOutgoingMethodCallProcessor processor, string scope, TimeoutSettings timeoutSettings)
            {
                Processor = processor;
                Scope = scope;
                TimeoutSettings = timeoutSettings;
            }
        }

        private IOutgoingMethodCallProcessor processor;
        private IServiceProxyClassFactory factory; 
        private ServiceProxyContainer container;

        [SetUp]
        public void Setup()
        {
            processor = Substitute.For<IOutgoingMethodCallProcessor>();
            factory = Substitute.For<IServiceProxyClassFactory>();
            factory.CreateProxyClass<IMyService>().Returns((p, s, t) => new MyServiceProxy(p, s, t));
            container = new ServiceProxyContainer(processor, factory);
        }

        [Test]
        public void Basic()
        {
            var proxy = container.GetProxy<IMyService>(null, null);
            Assert.That(proxy, Is.TypeOf<MyServiceProxy>());
            var typedProxy = (MyServiceProxy)proxy;
            Assert.That(typedProxy.Processor, Is.EqualTo(processor));
            Assert.That(typedProxy.Scope, Is.Null);
            Assert.That(typedProxy.TimeoutSettings, Is.Null);
        }

        [Test]
        public void BasicScoped()
        {
            var proxy = container.GetProxy<IMyService>("myscope", null);
            Assert.That(proxy, Is.TypeOf<MyServiceProxy>());
            var typedProxy = (MyServiceProxy)proxy;
            Assert.That(typedProxy.Processor, Is.EqualTo(processor));
            Assert.That(typedProxy.Scope, Is.EqualTo("myscope"));
        }

        [Test]
        public void Unscoped()
        {
            var proxy1 = container.GetProxy<IMyService>(null, null);
            var proxy2 = container.GetProxy<IMyService>(null, null);
            Assert.That(proxy1, Is.EqualTo(proxy2));
        }

        [Test]
        public void Scoped()
        {
            var proxy1 = container.GetProxy<IMyService>("mycope", null);
            var proxy2 = container.GetProxy<IMyService>("mycope", null);
            Assert.That(proxy1, Is.EqualTo(proxy2));
        }

        [Test]
        public void MixedScopes()
        {
            var proxyNull = container.GetProxy<IMyService>(null, null);
            var proxy1 = container.GetProxy<IMyService>("scope1", null);
            var proxy2 = container.GetProxy<IMyService>("scope2", null);
            var anotherProxy1 = container.GetProxy<IMyService>("scope1", null);

            Assert.That(proxyNull, Is.Not.EqualTo(proxy1));
            Assert.That(proxyNull, Is.Not.EqualTo(proxy2));
            Assert.That(proxy1, Is.Not.EqualTo(proxy2));
            Assert.That(proxy1, Is.EqualTo(anotherProxy1));
        }

        [Test]
        public void DifferrentTypes()
        {
            var proxy1 = container.GetProxy<IMyService>(null, null);
            var proxy2 = container.GetProxy<IMyOtherService>(null, null);
            Assert.That(proxy1, Is.Not.EqualTo(proxy2));
        }

        [Test]
        public void DifferentTimeouts()
        {
            var proxy1 = container.GetProxy<IMyService>(null, new TimeoutSettings(123));
            var proxy2 = container.GetProxy<IMyService>(null, new TimeoutSettings(234));
            Assert.That(proxy1, Is.Not.EqualTo(proxy2));
        }
    }
}