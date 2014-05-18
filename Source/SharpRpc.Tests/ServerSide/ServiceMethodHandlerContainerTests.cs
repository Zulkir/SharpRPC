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
using SharpRpc.Interaction;
using SharpRpc.Reflection;
using SharpRpc.ServerSide.Handler;

namespace SharpRpc.Tests.ServerSide
{
    [TestFixture]
    public class ServiceMethodHandlerContainerTests
    {
        private IHandlerFactory factory;
        private IHandlerContainer container;

        [SetUp]
        public void Setup()
        {
            factory = Substitute.For<IHandlerFactory>();
            container = new HandlerContainer(factory);
        }

        public interface ITrivialService
        {
             
        }

        [Test]
        public void Trivial()
        {
            var serviceDescription = new ServiceDescriptionBuilder(new MethodDescriptionBuilder()).Build(typeof(ITrivialService));
            var path = new ServicePath("MyService", "MyMethod");
            var handler = Substitute.For<IHandler>();
            factory.CreateHandler(serviceDescription, path).Returns(handler);

            var handler1 = container.GetHandler(serviceDescription, path);
            var handler2 = container.GetHandler(serviceDescription, path);

            Assert.That(handler1, Is.EqualTo(handler2));
        }
    }
}