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
using NUnit.Framework;
using SharpRpc.Codecs;
using SharpRpc.Interaction;
using SharpRpc.Reflection;
using SharpRpc.ServerSide;
using System.Linq;

namespace SharpRpc.Tests.ServerSide
{
    [TestFixture]
    public unsafe class ServiceMethodHandlerFactoryTests
    {
        #region Service interfaces and implementations
        public interface IGlobalService
        {
            IMiddleService Middle { get; }
            void Trivial();
            void MethodWithArgs(int a, double b);
            double MethodWithRetval();
        }

        public interface IMiddleService
        {
            ILocalService Local { get; }
            void Trivial();
        }

        public interface ILocalService
        {
            void Trivial();
        }

        public interface IGlobalServiceImplementation : IGlobalService, IServiceImplementation
        {
             
        }
        #endregion

        private ICodecContainer codecContainer;
        private ServiceMethodHandlerFactory factory;
        private IGlobalServiceImplementation service;
        private ServiceImplementationInfo globalServiceImplementationInfo;

        [SetUp]
        public void Setup()
        {
            codecContainer = new CodecContainer();
            factory = new ServiceMethodHandlerFactory(codecContainer);
            service = Substitute.For<IGlobalServiceImplementation>();
            var globalServiceDescription = 
                new ServiceDescriptionBuilder(new MethodDescriptionBuilder()).Build(typeof(IGlobalService));
            globalServiceImplementationInfo = 
                new ServiceImplementationInfo(typeof(IGlobalService), globalServiceDescription, service);
        }

        [Test]
        public void Trivial()
        {
            var handler = factory.CreateMethodHandler(globalServiceImplementationInfo, new ServicePath("MyService", "Trivial"));
            var result = handler(service, new byte[0]);
            Assert.That(result.Length, Is.EqualTo(0));
            Assert.That(service.ReceivedCalls().Single().GetMethodInfo().Name, Is.EqualTo("Trivial"));
        }

        [Test]
        public void SubService()
        {
            var middleService = Substitute.For<IMiddleService>();
            service.Middle.Returns(middleService);
            var handler = factory.CreateMethodHandler(globalServiceImplementationInfo, new ServicePath("MyService", "Middle", "Trivial"));
            handler(service, new byte[0]);
            Assert.That(middleService.ReceivedCalls().Single().GetMethodInfo().Name, Is.EqualTo("Trivial"));
        }

        [Test]
        public void DeepSubService()
        {
            var localService = Substitute.For<ILocalService>();
            var middleService = Substitute.For<IMiddleService>();
            middleService.Local.Returns(localService);
            service.Middle.Returns(middleService);
            var handler = factory.CreateMethodHandler(globalServiceImplementationInfo, new ServicePath("MyService", "Middle", "Local", "Trivial"));
            handler(service, new byte[0]);
            Assert.That(localService.ReceivedCalls().Single().GetMethodInfo().Name, Is.EqualTo("Trivial"));
        }

        [Test]
        public void Arguments()
        {
            var handler = factory.CreateMethodHandler(globalServiceImplementationInfo, new ServicePath("MyService", "MethodWithArgs"));

            var data = new byte[12];
            fixed (byte* pData = data)
            {
                *(int*) pData = 123;
                *(double*) (pData + 4) = 234.567;
            }

            var result = handler(service, data);
            Assert.That(result.Length, Is.EqualTo(0));
            var serviceCall = service.ReceivedCalls().Single();
            var arguments = serviceCall.GetArguments();
            Assert.That(serviceCall.GetMethodInfo().Name, Is.EqualTo("MethodWithArgs"));
            Assert.That(arguments[0], Is.EqualTo(123));
            Assert.That(arguments[1], Is.EqualTo(234.567));
        }

        [Test]
        public void Retval()
        {
            var handler = factory.CreateMethodHandler(globalServiceImplementationInfo, new ServicePath("MyService", "MethodWithRetval"));

            var expectedData = new byte[8];
            fixed (byte* pData = expectedData)
            {
                *(double*)pData = 123.456;
            }

            service.MethodWithRetval().Returns(123.456);

            var result = handler(service, new byte[0]);
            Assert.That(result, Is.EquivalentTo(expectedData));
        }
    }
}