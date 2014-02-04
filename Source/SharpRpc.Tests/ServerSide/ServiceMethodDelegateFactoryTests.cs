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

using System;
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
    public unsafe class ServiceMethodDelegateFactoryTests
    {
        #region Service interfaces and implementations
        public interface IGlobalServiceBase
        {
            void DoBaseStuff();
        }

        public interface IGlobalService : IGlobalServiceBase
        {
            IMiddleService Middle { get; }
            void Trivial();
            void MethodWithArgs(int a, double b);
            double MethodWithRetval();
            void ModifyByRef(ref double d);
            void GetSomethingOut(out int o);
            double MixedParameterTypes(int a, ref bool b, out ushort c);
            void ReferenceInRef(ref string s);
            void EmptyGeneric<T>();
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

        #endregion

        private ICodecContainer codecContainer;
        private IServiceMethodDelegateFactory factory;
        private IGlobalService service;
        private ServiceDescription globalServiceDescription;

        [SetUp]
        public void Setup()
        {
            codecContainer = new CodecContainer();
            factory = new ServiceMethodDelegateFactory();
            service = Substitute.For<IGlobalService>();
            var serviceDescriptionBuilder = new ServiceDescriptionBuilder(new MethodDescriptionBuilder());
            globalServiceDescription = serviceDescriptionBuilder.Build(typeof(IGlobalService));
        }

        [Test]
        public void Trivial()
        {
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "Trivial"), Type.EmptyTypes);
            var result = methodDelegate(codecContainer, service, (byte*)0, 0);
            Assert.That(result.Length, Is.EqualTo(0));
            Assert.That(service.ReceivedCalls().Single().GetMethodInfo().Name, Is.EqualTo("Trivial"));
        }

        [Test]
        public void SubService()
        {
            var middleService = Substitute.For<IMiddleService>();
            service.Middle.Returns(middleService);
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "Middle", "Trivial"), Type.EmptyTypes);
            methodDelegate(codecContainer, service, (byte*)0, 0);
            Assert.That(middleService.ReceivedCalls().Single().GetMethodInfo().Name, Is.EqualTo("Trivial"));
        }

        [Test]
        public void DeepSubService()
        {
            var localService = Substitute.For<ILocalService>();
            var middleService = Substitute.For<IMiddleService>();
            middleService.Local.Returns(localService);
            service.Middle.Returns(middleService);
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "Middle", "Local", "Trivial"), Type.EmptyTypes);
            methodDelegate(codecContainer, service, (byte*)0, 0);
            Assert.That(localService.ReceivedCalls().Single().GetMethodInfo().Name, Is.EqualTo("Trivial"));
        }

        [Test]
        public void Arguments()
        {
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "MethodWithArgs"), Type.EmptyTypes);

            var data = new byte[12];
            fixed (byte* pData = data)
            {
                *(int*) pData = 123;
                *(double*) (pData + 4) = 234.567;
            }

            byte[] result;
            fixed (byte* pData = data)
            {
                result = methodDelegate(codecContainer, service, pData, data.Length);
            }
            
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
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "MethodWithRetval"), Type.EmptyTypes);

            var expectedData = new byte[8];
            fixed (byte* pData = expectedData)
            {
                *(double*)pData = 123.456;
            }

            service.MethodWithRetval().Returns(123.456);
            var result = methodDelegate(codecContainer, service, (byte*)0, 0);
            Assert.That(result, Is.EquivalentTo(expectedData));
        }

        [Test]
        public void Ref()
        {
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "ModifyByRef"), Type.EmptyTypes);

            var data = new byte[8];
            fixed (byte* pData = data)
            {
                *(double*)pData = 12.34;
            }

            var expectedData = new byte[8];
            fixed (byte* pData = expectedData)
            {
                *(double*)pData = 24.68;
            }

            double dummy = 0;
            service.WhenForAnyArgs(x => x.ModifyByRef(ref dummy)).Do(x => { x[0] = (double)x[0] * 2; });

            byte[] result;
            fixed (byte* pData = data)
            {
                result = methodDelegate(codecContainer, service, pData, data.Length);
            }
            Assert.That(result, Is.EquivalentTo(expectedData));
        }

        [Test]
        public void Out()
        {
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "GetSomethingOut"), Type.EmptyTypes);

            var expectedData = new byte[4];
            fixed (byte* pData = expectedData)
            {
                *(int*)pData = 1234;
            }

            int dummy;
            service.When(x => x.GetSomethingOut(out dummy)).Do(x => { x[0] = 1234; });

            var result = methodDelegate(codecContainer, service, (byte*)0, 0);
            Assert.That(result, Is.EquivalentTo(expectedData));
        }

        [Test]
        public void MixedParameterTypes()
        {
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "MixedParameterTypes"), Type.EmptyTypes);

            var data = new byte[8];
            fixed (byte* pData = data)
            {
                *(int*)pData = 123;
                *(bool*)(pData + 4) = false;
            }

            var expectedData = new byte[14];
            fixed (byte* pData = expectedData)
            {
                *(bool*)pData = true;
                *(ushort*)(pData + 4) = 246;
                *(double*)(pData + 6) = 123.456;
            }

            bool dummyBool = false;
            ushort dummyUshort;
            service.MixedParameterTypes(0, ref dummyBool, out dummyUshort).ReturnsForAnyArgs(x =>
                {
                    x[1] = true;
                    x[2] = (ushort)((int)x[0] * 2);
                    return 123.456;
                });

            byte[] result;
            fixed (byte* pData = data)
            {
                result = methodDelegate(codecContainer, service, pData, data.Length);
            }
            Assert.That(result, Is.EquivalentTo(expectedData));
        }

        [Test]
        public void ReferenceInRef()
        {
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "ReferenceInRef"), Type.EmptyTypes);

            var data = new byte[4];

            var expectedData = new byte[8];
            fixed (byte* pData = expectedData)
            {
                *(int*)pData = 4;
                *(char*)(pData + 4) = 'O';
                *(char*)(pData + 6) = 'K';
            }

            string s= "";
            service.WhenForAnyArgs(x => x.ReferenceInRef(ref s)).Do(x =>
                {
                    x[0] = "OK";
                });

            byte[] result;
            fixed (byte* pData = data)
            {
                result = methodDelegate(codecContainer, service, pData, data.Length);
            }
            Assert.That(result, Is.EquivalentTo(expectedData));
        }

        [Test]
        public void Inheritance()
        {
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "DoBaseStuff"), Type.EmptyTypes);
            methodDelegate(codecContainer, service, (byte*)0, 0);
        }

        [Test]
        public void EmptyGeneric()
        {
            DoTestEmptyGeneric<int>();
            DoTestEmptyGeneric<string>();
        }

        private void DoTestEmptyGeneric<T>()
        {
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "EmptyGeneric"), new[] { typeof(T) });

            var typeCodec = codecContainer.GetManualCodecFor<Type>();

            var data = new byte[typeCodec.CalculateSize(typeof(T))];
            fixed (byte* pData = data)
            {
                var p = pData;
                typeCodec.Encode(ref p, typeof(T));
            }

            fixed (byte* pData = data)
            {
                methodDelegate(codecContainer, service, pData, data.Length);
            }

            var serviceCall = service.ReceivedCalls().Last();
            Assert.That(serviceCall.GetMethodInfo(), Is.EqualTo(typeof(IGlobalService).GetMethod("EmptyGeneric").MakeGenericMethod(new[] { typeof(T) })));
        }
    }
}