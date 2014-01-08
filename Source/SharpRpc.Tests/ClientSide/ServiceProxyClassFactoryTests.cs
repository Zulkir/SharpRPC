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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NSubstitute;
using NUnit.Framework;
using SharpRpc.ClientSide;
using System.Linq;
using SharpRpc.Codecs;
using SharpRpc.Reflection;

namespace SharpRpc.Tests.ClientSide
{
    [TestFixture]
    public unsafe class ServiceProxyClassFactoryTests
    {
        private IServiceDescriptionBuilder serviceDescriptionBuilder;
        private ICodecContainer codecContainer;
        private IOutgoingMethodCallProcessor methodCallProcessor;
        private ServiceProxyClassFactory factory;

        [SetUp]
        public void Setup()
        {
            serviceDescriptionBuilder = new ServiceDescriptionBuilder(new MethodDescriptionBuilder());
            codecContainer = new CodecContainer();
            methodCallProcessor = Substitute.For<IOutgoingMethodCallProcessor>();
            factory = new ServiceProxyClassFactory(serviceDescriptionBuilder, codecContainer);
        }

        public interface ITrivialService
        {
            void DoSomething();
        }

        [Test]
        public void Trivial()
        {
            var proxy = factory.CreateProxyClass<ITrivialService>()(methodCallProcessor, null, null);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs((byte[])null);

            proxy.DoSomething();

            var arguments = methodCallProcessor.ReceivedCalls().Single().GetArguments();
            Assert.That(arguments[0], Is.EqualTo(typeof(ITrivialService)));
            Assert.That(arguments[1], Is.EqualTo("TrivialService/DoSomething"));
            Assert.That(arguments[2], Is.Null);
            Assert.That(arguments[3], Is.Null);
            Assert.That(arguments[4], Is.Null);
        }

        [Test]
        public void TrivialScoped()
        {
            var proxy = factory.CreateProxyClass<ITrivialService>()(methodCallProcessor, "my scope", null);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs((byte[])null);

            proxy.DoSomething();

            var arguments = methodCallProcessor.ReceivedCalls().Single().GetArguments();
            Assert.That(arguments[0], Is.EqualTo(typeof(ITrivialService)));
            Assert.That(arguments[1], Is.EqualTo("TrivialService/DoSomething"));
            Assert.That(arguments[2], Is.EqualTo("my scope"));
            Assert.That(arguments[3], Is.Null);
            Assert.That(arguments[4], Is.Null);
        }

        [Test]
        public void TrivialWithScopeAndTimeout()
        {
            var timeoutSettings = new TimeoutSettings();
            var proxy = factory.CreateProxyClass<ITrivialService>()(methodCallProcessor, "my scope", timeoutSettings);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs((byte[])null);

            proxy.DoSomething();

            var arguments = methodCallProcessor.ReceivedCalls().Single().GetArguments();
            Assert.That(arguments[0], Is.EqualTo(typeof(ITrivialService)));
            Assert.That(arguments[1], Is.EqualTo("TrivialService/DoSomething"));
            Assert.That(arguments[2], Is.EqualTo("my scope"));
            Assert.That(arguments[3], Is.Null);
            Assert.That(arguments[4], Is.EqualTo(timeoutSettings));
        }

        public interface IArgumentsService
        {
            void MethodWithArgs(int a, double b);
            double MethodWithRetval();
        }

        [Test]
        public void Arguments()
        {
            var proxy = factory.CreateProxyClass<IArgumentsService>()(methodCallProcessor, null, null);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs((byte[])null);

            proxy.MethodWithArgs(123, 234.567);

            var expectedArgsData = new byte[12];
            fixed (byte* pData = expectedArgsData)
            {
                *(int*)pData = 123;
                *(double*)(pData + 4) = 234.567;
            }

            var arguments = methodCallProcessor.ReceivedCalls().Single().GetArguments();
            Assert.That(arguments[0], Is.EqualTo(typeof(IArgumentsService)));
            Assert.That(arguments[1], Is.EqualTo("ArgumentsService/MethodWithArgs"));
            Assert.That(arguments[2], Is.Null);
            Assert.That(arguments[3], Is.EqualTo(expectedArgsData));
        }

        public interface IRetvalService
        {
            double MethodWithRetval();
        }

        [Test]
        public void Retval()
        {
            var retvalData = new byte[sizeof(double)];
            fixed (byte* pData = retvalData)
            {
                *(double*)pData = 123.456;
            }

            var proxy = factory.CreateProxyClass<IRetvalService>()(methodCallProcessor, null, null);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs(retvalData);

            var retval = proxy.MethodWithRetval();

            var arguments = methodCallProcessor.ReceivedCalls().Single().GetArguments();
            Assert.That(arguments[0], Is.EqualTo(typeof(IRetvalService)));
            Assert.That(arguments[1], Is.EqualTo("RetvalService/MethodWithRetval"));
            Assert.That(arguments[2], Is.Null);
            Assert.That(arguments[3], Is.Null);
            Assert.That(retval, Is.EqualTo(123.456));
        }

        public interface ISuperService
        {
            ITrivialService Trivial { get; }
        }

        [Test]
        public void Subservice()
        {
            var proxy = factory.CreateProxyClass<ISuperService>()(methodCallProcessor, null, null);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs((byte[])null);

            proxy.Trivial.DoSomething();

            var arguments = methodCallProcessor.ReceivedCalls().Single().GetArguments();
            Assert.That(arguments[0], Is.EqualTo(typeof(ISuperService)));
            Assert.That(arguments[1], Is.EqualTo("SuperService/Trivial/DoSomething"));
            Assert.That(arguments[2], Is.Null);
            Assert.That(arguments[3], Is.Null);
        }

        public interface IServiceWithMixedParameters
        {
            double DoSomethingAwesome(int a, ref bool b, out ushort c); 
        }

        [Test]
        public void MixedParameters()
        {
            var expectedArgsData = new byte[8];
            fixed (byte* pData = expectedArgsData)
            {
                *(int*)pData = 123;
                *(bool*)(pData + 4) = false;
            }

            var returnData = new byte[14];
            fixed (byte* pData = returnData)
            {
                *(bool*)pData = true;
                *(ushort*)(pData + 4) = 246;
                *(double*)(pData + 6) = 123.456;
            }

            var proxy = factory.CreateProxyClass<IServiceWithMixedParameters>()(methodCallProcessor, null, null);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs(returnData);

            bool b = false;
            ushort c;
            var result = proxy.DoSomethingAwesome(123, ref b, out c);

            var arguments = methodCallProcessor.ReceivedCalls().Single().GetArguments();
            Assert.That(arguments[3], Is.EquivalentTo(expectedArgsData));
            Assert.That(b, Is.EqualTo(true));
            Assert.That(c, Is.EqualTo(246));
            Assert.That(result, Is.EqualTo(123.456));
        }

        public interface IReferenceInRefService
        {
            void ReferenceInRef(ref string s);
        }

        [Test]
        public void ReferenceInRef()
        {
            var expectedArgsData = new byte[4];

            var returnData = new byte[8];
            fixed (byte* pData = returnData)
            {
                *(int*) pData = 4;
                *(char*) (pData + 4) = 'O';
                *(char*) (pData + 6) = 'K';
            }

            var proxy = factory.CreateProxyClass<IReferenceInRefService>()(methodCallProcessor, null, null);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs(returnData);

            string s = "";
            proxy.ReferenceInRef(ref s);

            var arguments = methodCallProcessor.ReceivedCalls().Single().GetArguments();
            Assert.That(arguments[3], Is.EquivalentTo(expectedArgsData));
            Assert.That(s, Is.EqualTo("OK"));
        }

        [DataContract]
        public class MyUninlineableContract
        {
            public MyUninlineableContract(int a) { A = a; }

            [DataMember]
            public int A { get; private set; }
        }

        public interface IUninlineableStuffService
        {
            void DoWithArg(MyUninlineableContract a);
            MyUninlineableContract GetRetval();
            MyUninlineableContract Modify(MyUninlineableContract a);
            MyUninlineableContract DoComplexStuff(int a, MyUninlineableContract b, ref MyUninlineableContract c, out MyUninlineableContract d);
        }

        [Test]
        public void UninlineableArgs()
        {
            var expectedArgsData = new byte[8];
            fixed (byte* pData = expectedArgsData)
            {
                *(int*)pData = 1;
                *(int*)(pData + 4) = 123;
            }

            var proxy = factory.CreateProxyClass<IUninlineableStuffService>()(methodCallProcessor, null, null);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs((byte[])null);

            proxy.DoWithArg(new MyUninlineableContract(123));

            var arguments = methodCallProcessor.ReceivedCalls().Single().GetArguments();
            Assert.That(arguments[3], Is.EquivalentTo(expectedArgsData));
        }

        [Test]
        public void UninlineableRetval()
        {
            var returnData = new byte[8];
            fixed (byte* pData = returnData)
            {
                *(int*)pData = 1;
                *(int*)(pData + 4) = 123;
            }

            var proxy = factory.CreateProxyClass<IUninlineableStuffService>()(methodCallProcessor, null, null);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs(returnData);

            var result = proxy.GetRetval();

            var arguments = methodCallProcessor.ReceivedCalls().Single().GetArguments();
            Assert.That(arguments[3], Is.Null);
            Assert.That(result.A, Is.EqualTo(123));
        }

        [Test]
        public void UninlineableArgsAndRetval()
        {
            var returnData = new byte[8];
            fixed (byte* pData = returnData)
            {
                *(int*)pData = 1;
                *(int*)(pData + 4) = 123;
            }

            var proxy = factory.CreateProxyClass<IUninlineableStuffService>()(methodCallProcessor, null, null);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs(returnData);

            var result = proxy.Modify(new MyUninlineableContract(123));
        }

        [Test]
        public void UninlineableMixed()
        {
            var expectedArgsData = new byte[20];
            fixed (byte* pData = expectedArgsData)
            {
                *(int*)(pData + 0) = 123;
                *(int*)(pData + 4) = 1;
                *(int*)(pData + 8) = 234;
                *(int*)(pData + 12) = 1;
                *(int*)(pData + 16) = 345;
            }

            var returnData = new byte[24];
            fixed (byte* pData = returnData)
            {
                *(int*)(pData + 0) = 1;
                *(int*)(pData + 4) = 456;
                *(int*)(pData + 8) = 1;
                *(int*)(pData + 12) = 567;
                *(int*)(pData + 16) = 1;
                *(int*)(pData + 20) = 678;
            }

            var proxy = factory.CreateProxyClass<IUninlineableStuffService>()(methodCallProcessor, null, null);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs(returnData);

            var c = new MyUninlineableContract(345);
            MyUninlineableContract d;
            var result = proxy.DoComplexStuff(123, new MyUninlineableContract(234), ref c, out d);

            var arguments = methodCallProcessor.ReceivedCalls().Single().GetArguments();
            Assert.That(arguments[3], Is.EquivalentTo(expectedArgsData));
            Assert.That(c.A, Is.EqualTo(456));
            Assert.That(d.A, Is.EqualTo(567));
            Assert.That(result.A, Is.EqualTo(678));
        }

        public class BadData
        {
            public IntPtr A { get; set; }
            public Func<int, string> B { get; set; }
        }

        public interface IBadService
        {
            BadData DoBadStuff(BadData arg);
        }

        [Test]
        public void ExceptionConsistency()
        {
            Assert.Throws<NotSupportedException>(() => factory.CreateProxyClass<IBadService>());   
            Assert.Throws<NotSupportedException>(() => factory.CreateProxyClass<IBadService>());   
        }

        public interface IServiceWithEmptyGenerics
        {
            void DoTypedNothing<T>();
        }

        [Test]
        public void EmptyGenerics()
        {
            DoTestEmptyGenerics<int>();
            DoTestEmptyGenerics<string>();
        }

        private void DoTestEmptyGenerics<T>()
        {
            var typeCodec = codecContainer.GetManualCodecFor<Type>();

            var sizeOfType = typeCodec.CalculateSize(typeof(T));
            var expectedArgsData = new byte[sizeOfType];
            fixed (byte* pData = expectedArgsData)
            {
                var p = pData;
                typeCodec.Encode(ref p, typeof(T));
            }

            var proxy = factory.CreateProxyClass<IServiceWithEmptyGenerics>()(methodCallProcessor, null, null);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs((byte[])null);

            proxy.DoTypedNothing<T>();

            var arguments = methodCallProcessor.ReceivedCalls().Last().GetArguments();
            Assert.That(arguments[3], Is.EquivalentTo(expectedArgsData));
        }

        public interface IServiceWithSimpleGenerics
        {
            void DoSomethingSimple<T>(T arg);
        }

        [Test]
        public void SimpleGenerics()
        {
            DoTestSimpleGenerics(123);
            DoTestSimpleGenerics("asd");
        }

        private void DoTestSimpleGenerics<T>(T argValue)
        {
            var typeCodec = codecContainer.GetManualCodecFor<Type>();
            var argCodec = codecContainer.GetManualCodecFor<T>();

            var sizeOfType = typeCodec.CalculateSize(typeof(T));
            var sizeOfArg = argCodec.CalculateSize(argValue);
            var expectedArgsData = new byte[sizeOfType + sizeOfArg];
            fixed (byte* pData = expectedArgsData)
            {
                var p = pData;
                typeCodec.Encode(ref p, typeof(T));
                argCodec.Encode(ref p, argValue);            
            }

            var proxy = factory.CreateProxyClass<IServiceWithSimpleGenerics>()(methodCallProcessor, null, null);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs((byte[])null);

            proxy.DoSomethingSimple(argValue);

            var arguments = methodCallProcessor.ReceivedCalls().Last().GetArguments();
            Assert.That(arguments[3], Is.EquivalentTo(expectedArgsData));
        }

        public interface IServiceWithGenericRetval
        {
            T GetSomething<T>();
        }

        [Test]
        public void GenericRetval()
        {
            DoTestGenericRetval(123);
            DoTestGenericRetval("asd");
        }

        private void DoTestGenericRetval<T>(T expectedRetval)
        {
            var typeCodec = codecContainer.GetManualCodecFor<Type>();

            var expectedArgsData = new byte[typeCodec.CalculateSize(typeof(T))];
            fixed (byte* pData = expectedArgsData)
            {
                var p = pData;
                typeCodec.Encode(ref p, typeof(T));
            }

            var retvalCodec = codecContainer.GetManualCodecFor<T>();
            var responseData = new byte[retvalCodec.CalculateSize(expectedRetval)];
            fixed (byte* pData = responseData)
            {
                var p = pData;
                retvalCodec.Encode(ref p, expectedRetval);
            }

            var proxy = factory.CreateProxyClass<IServiceWithGenericRetval>()(methodCallProcessor, null, null);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs(responseData);

            var retval = proxy.GetSomething<T>();

            var arguments = methodCallProcessor.ReceivedCalls().Last().GetArguments();
            Assert.That(arguments[3], Is.EquivalentTo(expectedArgsData));

            Assert.That(retval, Is.EqualTo(expectedRetval));
        }

        public interface IServiceWithMixedGenerics
        {
            TFirst DoSomethingMixed<TFirst, TSecond>(int a, TFirst b, ref TSecond c, out TSecond d);
        }

        [Test]
        public void MixedGenerics()
        {
            DoTestMixedGenerics(123, 234, "asd", "sdf", "dfg", 345);
            DoTestMixedGenerics(123, "asd", 234, 345, 456, "sdf");
        }

        private void DoTestMixedGenerics<TFirst, TSecond>(int a, TFirst b, TSecond c, TSecond expectedC, TSecond expectedD, TFirst expectedRetval)
        {
            var typeCodec = codecContainer.GetManualCodecFor<Type>();
            var intCodec = codecContainer.GetManualCodecFor<int>();
            var firstCodec = codecContainer.GetManualCodecFor<TFirst>();
            var secondCodec = codecContainer.GetManualCodecFor<TSecond>();

            int sizeOfTFirst = typeCodec.CalculateSize(typeof(TFirst));
            int sizeOfTSecond = typeCodec.CalculateSize(typeof(TSecond));
            int sizeOfA = intCodec.CalculateSize(a);
            int sizeOfB = firstCodec.CalculateSize(b);
            int sizeOfC = secondCodec.CalculateSize(c);
            var expectedArgsData = new byte[sizeOfTFirst + sizeOfTSecond + sizeOfA + sizeOfB + sizeOfC];
            fixed (byte* pData = expectedArgsData)
            {
                var p = pData;
                typeCodec.Encode(ref p, typeof(TFirst));
                typeCodec.Encode(ref p, typeof(TSecond));
                intCodec.Encode(ref p, a);
                firstCodec.Encode(ref p, b);
                secondCodec.Encode(ref p, c);
            }

            int sizeOfReturningC = secondCodec.CalculateSize(expectedC);
            int sizeOfReturningD = secondCodec.CalculateSize(expectedD);
            int sizeOfRetval = firstCodec.CalculateSize(expectedRetval);
            var responseData = new byte[sizeOfReturningC + sizeOfReturningD + sizeOfRetval];
            fixed (byte* pData = responseData)
            {
                var p = pData;
                secondCodec.Encode(ref p, expectedC);
                secondCodec.Encode(ref p, expectedD);
                firstCodec.Encode(ref p, expectedRetval);
            }

            var proxy = factory.CreateProxyClass<IServiceWithMixedGenerics>()(methodCallProcessor, null, null);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs(responseData);

            TSecond d;
            var retval = proxy.DoSomethingMixed(a, b, ref c, out d);

            var arguments = methodCallProcessor.ReceivedCalls().Last().GetArguments();
            Assert.That(arguments[3], Is.EquivalentTo(expectedArgsData));

            Assert.That(c, Is.EqualTo(expectedC));
            Assert.That(d, Is.EqualTo(expectedD));
            Assert.That(retval, Is.EqualTo(expectedRetval));
        }

        public interface IServiceWithSimpleGenericConstraint
        {
            void DoNothing<T>() where T : struct;
        }

        [Test]
        public void SimpleGenericConstraint()
        {
            var proxy = factory.CreateProxyClass<IServiceWithSimpleGenericConstraint>()(methodCallProcessor, null, null);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs((byte[])null);
            proxy.DoNothing<int>();
        }

        public interface IServiceWithComplexGenericConstraints
        {
            void DoNothing<T>() where T : class, IEnumerable<T>;
        }

        public class ClassMeetingConstraints : IEnumerable<ClassMeetingConstraints>
        {
            public IEnumerator<ClassMeetingConstraints> GetEnumerator() { throw new NotImplementedException(); }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }

        [Test]
        public void ComplexGenericConstraints()
        {
            var proxy = factory.CreateProxyClass<IServiceWithComplexGenericConstraints>()(methodCallProcessor, null, null);
            methodCallProcessor.Process(null, null, null, null, null).ReturnsForAnyArgs((byte[])null);
            proxy.DoNothing<ClassMeetingConstraints>();
        }
    }
}
