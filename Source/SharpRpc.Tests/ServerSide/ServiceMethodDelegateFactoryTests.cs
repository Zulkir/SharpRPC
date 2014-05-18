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
using System.Globalization;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using SharpRpc.Codecs;
using SharpRpc.Interaction;
using SharpRpc.Reflection;
using System.Linq;
using SharpRpc.ServerSide.Handler;

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
            Task<int> RetvalAsync();
            Task<Dictionary<TKey, TValue>> GenericRetvalAsync<TKey, TValue>();
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
        private IRawHandlerFactory factory;
        private IGlobalService service;
        private ServiceDescription globalServiceDescription;

        [SetUp]
        public void Setup()
        {
            var serviceDescriptionBuilder = new ServiceDescriptionBuilder(new MethodDescriptionBuilder());
            codecContainer = new CodecContainer();
            factory = new RawHandlerFactory(codecContainer);
            service = Substitute.For<IGlobalService>();
            globalServiceDescription = serviceDescriptionBuilder.Build(typeof(IGlobalService));
        }

        private Func<Type[], IHandler> CreateClass(params string[] path)
        {
            var serviceDescriptionChain = new List<ServiceDescription> { globalServiceDescription };
            for (int i = 1; i < path.Length - 1; i++)
                serviceDescriptionChain.Add(serviceDescriptionChain.Last().Subservices.Single(x => x.Name == path[i]));
            var methodDescription = serviceDescriptionChain.Last().Methods.Single(x => x.Name == path.Last());
            var servicePath = new ServicePath(path);
            return factory.CreateGenericClass(serviceDescriptionChain, methodDescription, servicePath);
        }

        [Test]
        public void Trivial()
        {
            var handler = CreateClass("MyService", "Trivial")(null);
            var result = handler.Handle(service, null, 0).Result;
            Assert.That(result.Length, Is.EqualTo(0));
            Assert.That(service.ReceivedCalls().Single().GetMethodInfo().Name, Is.EqualTo("Trivial"));
        }

        [Test]
        public void SubService()
        {
            var middleService = Substitute.For<IMiddleService>();
            service.Middle.Returns(middleService);
            var handler = CreateClass("MyService", "Middle", "Trivial")(null);
            handler.Handle(service, null, 0);
            Assert.That(middleService.ReceivedCalls().Single().GetMethodInfo().Name, Is.EqualTo("Trivial"));
        }

        [Test]
        public void DeepSubService()
        {
            var localService = Substitute.For<ILocalService>();
            var middleService = Substitute.For<IMiddleService>();
            middleService.Local.Returns(localService);
            service.Middle.Returns(middleService);
            var handler = CreateClass("MyService", "Middle", "Local", "Trivial")(null);
            handler.Handle(service, null, 0);
            Assert.That(localService.ReceivedCalls().Single().GetMethodInfo().Name, Is.EqualTo("Trivial"));
        }

        [Test]
        public void Arguments()
        {
            var handler = CreateClass("MyService", "MethodWithArgs")(null);

            var data = new byte[12];
            fixed (byte* pData = data)
            {
                *(int*) pData = 123;
                *(double*) (pData + 4) = 234.567;
            }

            var result = handler.Handle(service, data, 0).Result;
            
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
            var handler = CreateClass("MyService", "MethodWithArgs")(null);

            var data = new byte[offset + 12];
            fixed (byte* pData = data)
            {
                *(int*)(pData + offset) = 123;
                *(double*)(pData + 4 + offset) = 234.567;
            }

            var result = handler.Handle(service, data, offset).Result;

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
            var handler = CreateClass("MyService", "MethodWithRetval")(null);

            var expectedData = new byte[8];
            fixed (byte* pData = expectedData)
            {
                *(double*)pData = 123.456;
            }

            service.MethodWithRetval().Returns(123.456);
            var result = handler.Handle(service, null, 0).Result;
            Assert.That(result, Is.EquivalentTo(expectedData));
        }

        [Test]
        public void Ref()
        {
            var handler = CreateClass("MyService", "ModifyByRef")(null);

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

            var result = handler.Handle(service, data, 0).Result;
            Assert.That(result, Is.EquivalentTo(expectedData));
        }

        [Test]
        public void Out()
        {
            var handler = CreateClass("MyService", "GetSomethingOut")(null);

            var expectedData = new byte[4];
            fixed (byte* pData = expectedData)
            {
                *(int*)pData = 1234;
            }

            int dummy;
            service.When(x => x.GetSomethingOut(out dummy)).Do(x => { x[0] = 1234; });

            var result = handler.Handle(service, null, 0).Result;
            Assert.That(result, Is.EquivalentTo(expectedData));
        }

        [Test]
        public void MixedParameterTypes()
        {
            var handler = CreateClass("MyService", "MixedParameterTypes")(null);

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

            var result = handler.Handle(service, data, 0).Result;
            Assert.That(result, Is.EquivalentTo(expectedData));
        }

        [Test]
        public void ReferenceInRef()
        {
            var handler = CreateClass("MyService", "ReferenceInRef")(null);

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

            var result = handler.Handle(service, data, 0).Result;
            Assert.That(result, Is.EquivalentTo(expectedData));
        }

        [Test]
        public void Inheritance()
        {
            var handler = CreateClass("MyService", "DoBaseStuff")(null);
            handler.Handle(service, null, 0);
        }

        [Test]
        public void EmptyGeneric()
        {
            DoTestEmptyGeneric<int>();
            DoTestEmptyGeneric<string>();
        }

        private void DoTestEmptyGeneric<T>()
        {
            var handler = CreateClass("MyService", "EmptyGeneric")(new[] { typeof(T) });

            handler.Handle(service, null, 0);

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
            var handler = CreateClass("MyService", "GenericParameters")(new[] { typeof(T) });

            var argCodec = codecContainer.GetManualCodecFor<T>();

            var data = new byte[argCodec.CalculateSize(arg)];
            fixed (byte* pData = data)
            {
                var p = pData;
                argCodec.Encode(ref p, arg);
            }

            handler.Handle(service, data, 0);

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
            var handler = CreateClass("MyService", "GenericRetval")(new[] { typeof(T) });

            var retvalCodec = codecContainer.GetManualCodecFor<T>();

            var expectedData = new byte[retvalCodec.CalculateSize(expectedRetval)];
            fixed (byte* pData = expectedData)
            {
                var p = pData;
                retvalCodec.Encode(ref p, expectedRetval);
            }

            service.GenericRetval<T>().ReturnsForAnyArgs(expectedRetval);

            var result = handler.Handle(service, null, 0).Result;

            var serviceCall = service.ReceivedCalls().Last();
            Assert.That(serviceCall.GetMethodInfo(), Is.EqualTo(typeof(IGlobalService).GetMethod("GenericRetval").MakeGenericMethod(new[] { typeof(T) })));
            Assert.That(result, Is.EquivalentTo(expectedData));
        }

        [Test]
        public void NestedGenerics()
        {
            var intKeys = new[] {123, 234, 345};
            DoTestNestedGenerics(intKeys, intKeys.ToDictionary(x => x, x => x.ToString(CultureInfo.InvariantCulture)));
            var stringKeys = new[] { "123", "234", "345" };
            DoTestNestedGenerics(stringKeys, stringKeys.ToDictionary(x => x, int.Parse));
        }

        private void DoTestNestedGenerics<TKey, TValue>(TKey[] arg, Dictionary<TKey, TValue> expectedRetval)
        {
            var handler = CreateClass("MyService", "NestedGenerics")(new[] { typeof(TKey), typeof(TValue) });

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

            var result = handler.Handle(service, data, 0).Result;

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
            var handler = CreateClass("MyService", "MixedGenerics")(new[] { typeof(T1), typeof(T2), typeof(T3) });

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

            var result = handler.Handle(service, data, 0).Result;

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
            var handler = CreateClass("MyService", "VoidAsync")(null);
            var task = new Task(() => { });
            task.Start();
            service.VoidAsync().Returns(task);
            var resultingTask = handler.Handle(service, null, 0);
            Assert.That(resultingTask.Result, Is.EqualTo(new byte[0]));
        }

        [Test]
        public void RetvalAsync()
        {
            var handler = CreateClass("MyService", "RetvalAsync")(null);
            service.RetvalAsync().Returns(Task.FromResult(123456789));

            var expectedData = new byte[4];
            fixed (byte* pData = expectedData)
            {
                *(int*)pData = 123456789;
            }

            var resultingTask = handler.Handle(service, null, 0);
            Assert.That(resultingTask.Result, Is.EqualTo(expectedData));
        }

        [Test]
        public void GenericRetvalAsync()
        {
            var handler = CreateClass("MyService", "GenericRetvalAsync")(new[] { typeof(string), typeof(int) });
            var pureRetval = new Dictionary<string, int> { {"one", 1}, {"two", 2} };
            
            service.GenericRetvalAsync<string, int>().Returns(Task.FromResult(pureRetval));

            var retvalCodec = codecContainer.GetManualCodecFor<Dictionary<string, int>>();
            var expectedData = retvalCodec.EncodeSingle(pureRetval);

            var resultingTask = handler.Handle(service, null, 0);
            Assert.That(resultingTask.Result, Is.EqualTo(expectedData));
        }
    }
}