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
using System.Collections.Generic;
using System.Threading.Tasks;
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
            void GenericParameters<T>(T arg);
            T GenericRetval<T>();
            Dictionary<TKey, TValue> NestedGenerics<TKey, TValue>(TKey[] keys); 
            T1 MixedGenerics<T1, T2, T3>(int a, T1 b, ref T2 c, out T3 d);
            Task VoidAsync();
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
            var result = methodDelegate(codecContainer, service, null, 0).Result;
            Assert.That(result.Length, Is.EqualTo(0));
            Assert.That(service.ReceivedCalls().Single().GetMethodInfo().Name, Is.EqualTo("Trivial"));
        }

        [Test]
        public void SubService()
        {
            var middleService = Substitute.For<IMiddleService>();
            service.Middle.Returns(middleService);
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "Middle", "Trivial"), Type.EmptyTypes);
            methodDelegate(codecContainer, service, null, 0);
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
            methodDelegate(codecContainer, service, null, 0);
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

            var result = methodDelegate(codecContainer, service, data, 0).Result;
            
            Assert.That(result.Length, Is.EqualTo(0));
            var serviceCall = service.ReceivedCalls().Single();
            var arguments = serviceCall.GetArguments();
            Assert.That(serviceCall.GetMethodInfo().Name, Is.EqualTo("MethodWithArgs"));
            Assert.That(arguments[0], Is.EqualTo(123));
            Assert.That(arguments[1], Is.EqualTo(234.567));
        }

        [Test]
        public void Offset()
        {
            const int offset = 123;
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "MethodWithArgs"), Type.EmptyTypes);

            var data = new byte[offset + 12];
            fixed (byte* pData = data)
            {
                *(int*)(pData + offset) = 123;
                *(double*)(pData + 4 + offset) = 234.567;
            }

            var result = methodDelegate(codecContainer, service, data, offset).Result;

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
            var result = methodDelegate(codecContainer, service, null, 0).Result;
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

            var result = methodDelegate(codecContainer, service, data, 0).Result;
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

            var result = methodDelegate(codecContainer, service, null, 0).Result;
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

            var result = methodDelegate(codecContainer, service, data, 0).Result;
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

            var result = methodDelegate(codecContainer, service, data, 0).Result;
            Assert.That(result, Is.EquivalentTo(expectedData));
        }

        [Test]
        public void Inheritance()
        {
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "DoBaseStuff"), Type.EmptyTypes);
            methodDelegate(codecContainer, service, null, 0);
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

            methodDelegate(codecContainer, service, null, 0);

            var serviceCall = service.ReceivedCalls().Last();
            Assert.That(serviceCall.GetMethodInfo(), Is.EqualTo(typeof(IGlobalService).GetMethod("EmptyGeneric").MakeGenericMethod(new[] { typeof(T) })));
        }

        [Test]
        public void GenericParameters()
        {
            DoTestGenericParameters(123);
            DoTestGenericParameters("asd");
        }

        private void DoTestGenericParameters<T>(T arg)
        {
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "GenericParameters"), new[] { typeof(T) });

            var argCodec = codecContainer.GetManualCodecFor<T>();

            var data = new byte[argCodec.CalculateSize(arg)];
            fixed (byte* pData = data)
            {
                var p = pData;
                argCodec.Encode(ref p, arg);
            }

            methodDelegate(codecContainer, service, data, 0);

            var serviceCall = service.ReceivedCalls().Last();
            Assert.That(serviceCall.GetMethodInfo(), Is.EqualTo(typeof(IGlobalService).GetMethod("GenericParameters").MakeGenericMethod(new[] { typeof(T) })));
            Assert.That(serviceCall.GetArguments()[0], Is.EqualTo(arg));
        }

        [Test]
        public void GenericRetval()
        {
            DoTestGenericRetval(123);
            DoTestGenericRetval("asd");
        }

        private void DoTestGenericRetval<T>(T expectedRetval)
        {
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "GenericRetval"), new[] { typeof(T) });

            var retvalCodec = codecContainer.GetManualCodecFor<T>();

            var expectedData = new byte[retvalCodec.CalculateSize(expectedRetval)];
            fixed (byte* pData = expectedData)
            {
                var p = pData;
                retvalCodec.Encode(ref p, expectedRetval);
            }

            service.GenericRetval<T>().ReturnsForAnyArgs(expectedRetval);

            var result = methodDelegate(codecContainer, service, null, 0).Result;

            var serviceCall = service.ReceivedCalls().Last();
            Assert.That(serviceCall.GetMethodInfo(), Is.EqualTo(typeof(IGlobalService).GetMethod("GenericRetval").MakeGenericMethod(new[] { typeof(T) })));
            Assert.That(result, Is.EquivalentTo(expectedData));
        }

        [Test]
        public void NestedGenerics()
        {
            var intKeys = new[] {123, 234, 345};
            DoTestNestedGenerics(intKeys, intKeys.ToDictionary(x => x, x => x.ToString()));
            var stringKeys = new[] { "123", "234", "345" };
            DoTestNestedGenerics(stringKeys, stringKeys.ToDictionary(x => x, int.Parse));
        }

        private void DoTestNestedGenerics<TKey, TValue>(TKey[] arg, Dictionary<TKey, TValue> expectedRetval)
        {
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "NestedGenerics"), new[] { typeof(TKey), typeof(TValue) });

            var argCodec = codecContainer.GetManualCodecFor<TKey[]>();
            var resultCodec = codecContainer.GetManualCodecFor<Dictionary<TKey, TValue>>();

            var data = new byte[argCodec.CalculateSize(arg)];
            fixed (byte* pData = data)
            {
                var p = pData;
                argCodec.Encode(ref p, arg);
            }

            var expectedData = new byte[resultCodec.CalculateSize(expectedRetval)];
            fixed (byte* pData = expectedData)
            {
                var p = pData;
                resultCodec.Encode(ref p, expectedRetval);
            }

            service.NestedGenerics<TKey, TValue>(arg).ReturnsForAnyArgs(expectedRetval);

            var result = methodDelegate(codecContainer, service, data, 0).Result;

            var serviceCall = service.ReceivedCalls().Last();
            Assert.That(serviceCall.GetMethodInfo(), Is.EqualTo(typeof(IGlobalService).GetMethod("NestedGenerics").MakeGenericMethod(new[] { typeof(TKey), typeof(TValue) })));
            Assert.That(serviceCall.GetArguments()[0], Is.EquivalentTo(arg));
            Assert.That(result, Is.EquivalentTo(expectedData));
        }

        [Test]
        public void MixedGenerics()
        {
            DoTestMixedGenerics(123, 234, 345, "asd", "qwe", 456.789);
            DoTestMixedGenerics(123, "asd", "qwe", 234.0, 345.0, 467);
        }

        private void DoTestMixedGenerics<T1, T2, T3>(int argA, T1 expectedRetval, T1 argB, T2 argC, T2 expectedC, T3 expectedD)
        {
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "MixedGenerics"), new[] { typeof(T1), typeof(T2), typeof(T3) });

            var intCodec = codecContainer.GetManualCodecFor<int>();
            var t1Codec = codecContainer.GetManualCodecFor<T1>();
            var t2Codec = codecContainer.GetManualCodecFor<T2>();
            var t3Codec = codecContainer.GetManualCodecFor<T3>();

            var data = new byte[intCodec.CalculateSize(argA) + t1Codec.CalculateSize(argB) + t2Codec.CalculateSize(argC)];
            fixed (byte* pData = data)
            {
                var p = pData;
                intCodec.Encode(ref p, argA);
                t1Codec.Encode(ref p, argB);
                t2Codec.Encode(ref p, argC);
            }

            var expectedData = new byte[t2Codec.CalculateSize(expectedC) + t3Codec.CalculateSize(expectedD) + t1Codec.CalculateSize(expectedRetval)];
            fixed (byte* pData = expectedData)
            {
                var p = pData;
                t2Codec.Encode(ref p, expectedC);
                t3Codec.Encode(ref p, expectedD);
                t1Codec.Encode(ref p, expectedRetval);
            }

            T2 dummyC = default(T2);
            T3 dummyD;
            int realA = default(int);
            T1 realB = default(T1);
            T2 realC = default(T2);
            service.MixedGenerics(0, default(T1), ref dummyC, out dummyD).ReturnsForAnyArgs(x =>
            {
                realA = (int)x[0];
                realB = (T1)x[1];
                realC = (T2)x[2];
                x[2] = expectedC;
                x[3] = expectedD;
                return expectedRetval;
            });

            var result = methodDelegate(codecContainer, service, data, 0).Result;

            var serviceCall = service.ReceivedCalls().Last();
            Assert.That(serviceCall.GetMethodInfo(), Is.EqualTo(typeof(IGlobalService).GetMethod("MixedGenerics").MakeGenericMethod(new[] { typeof(T1), typeof(T2), typeof(T3) })));

            Assert.That(realA, Is.EqualTo(argA));
            Assert.That(realB, Is.EqualTo(argB));
            Assert.That(realC, Is.EqualTo(argC));

            Assert.That(result, Is.EquivalentTo(expectedData));
        }

        [Test]
        public void VoidAsync()
        {
            var methodDelegate = factory.CreateMethodDelegate(codecContainer, globalServiceDescription, new ServicePath("MyService", "VoidAsync"), Type.EmptyTypes);
            var task = new Task(() => { });
            service.VoidAsync().Returns(task);
            var resultingTask = methodDelegate(codecContainer, service, null, 0);
            Assert.That(resultingTask, Is.EqualTo(task));
        }
    }
}