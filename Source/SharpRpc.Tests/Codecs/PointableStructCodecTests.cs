using System;
using System.Reflection.Emit;
using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    [TestFixture]
    public unsafe class PointableStructCodecTests : CodecTestsBase
    {
        private void DoTest<T>(T value) where T : struct
        {
            DoTest(new PointableStructCodec(typeof(T)), value);
        }

        [Test]
        public void Byte()
        {
            DoTest((byte)0);
            DoTest((byte)37);
            DoTest((byte)123);
            DoTest((byte)255);
        }

        [Test]
        public void Short()
        {
            DoTest((short)0);
            DoTest((short)1231);
            DoTest((short)-12321);
            DoTest(short.MaxValue);
            DoTest(short.MinValue);
        }

        [Test]
        public void Int()
        {
            DoTest(0);
            DoTest(1312313);
            DoTest(-8766556);
            DoTest(int.MinValue);
            DoTest(int.MaxValue);
        }

        [Test]
        public void Long()
        {
            DoTest((long)0);
            DoTest(1312319392374234923);
            DoTest(-2736424827348766556);
            DoTest(long.MinValue);
            DoTest(long.MaxValue);
        }

        [Test]
        public void Ulong()
        {
            DoTest((ulong)0);
            DoTest(13123193923742349233);
            DoTest(ulong.MaxValue);
        }

        [Test]
        public void Float()
        {
            DoTest(0f);
            DoTest(123123f);
            DoTest(-345345f);
            DoTest(123123.123123f);
            DoTest(-234234.234234f);
            DoTest(float.MinValue);
            DoTest(float.MaxValue);
            DoTest(float.NegativeInfinity);
            DoTest(float.PositiveInfinity);
            DoTest(float.NaN);
        }

        [Test]
        public void Double()
        {
            DoTest(0);
            DoTest(123123);
            DoTest(-345345);
            DoTest(123123.123123);
            DoTest(-234234.234234);
            DoTest(double.MinValue);
            DoTest(double.MaxValue);
            DoTest(double.NegativeInfinity);
            DoTest(double.PositiveInfinity);
            DoTest(double.NaN);
        }

        public struct MySimpleStruct : IEquatable<MySimpleStruct>
        {
            public double A;
            public ushort B;

            public bool Equals(MySimpleStruct other)
            {
                return A == other.A && B == other.B;
            }

            public override bool Equals(object obj)
            {
                return Equals((MySimpleStruct)obj);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        public struct MyComplexStruct : IEquatable<MyComplexStruct>
        {
            public byte A;
            public MySimpleStruct B;
            public ushort C;
            public MySimpleStruct D;
            public byte E;
            public long F;

            public bool Equals(MyComplexStruct other)
            {
                return A == other.A && B.Equals(other.B) && C == other.C && D.Equals(other.D) && E == other.E && F == other.F;
            }

            public override bool Equals(object obj)
            {
                return base.Equals((MyComplexStruct)obj);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        [Test]
        public void SimpleStruct()
        {
            DoTest(new MySimpleStruct
            {
                A = 123123.2354345,
                B = 23542
            });
        }

        [Test]
        public void ComplexStruct()
        {
            DoTest(new MyComplexStruct
            {
                A = 234,
                B = new MySimpleStruct
                {
                    A = 567567.56756757,
                    B = 8768
                },
                C = 56767,
                D = new MySimpleStruct
                {
                    A = -9872974.2374,
                    B = 65467
                },
                E = 22,
                F = 98237238734578112
            });
        }

        delegate void Experiment(ref object o1, ref object o2);

        [Test]
        public void Experiments()
        {
            var dynamicMethod = new DynamicMethod("asdasd_exp", typeof(void), new[] {typeof(object).MakeByRefType(), typeof(object).MakeByRefType()});
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldind_Ref);
            var temp = il.DeclareLocal(typeof(object));
            il.Emit(OpCodes.Stloc, temp);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldind_Ref);
            il.Emit(OpCodes.Stind_Ref);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Stind_Ref);
            il.Emit(OpCodes.Ret);
            var method = (Experiment)dynamicMethod.CreateDelegate(typeof(Experiment));
            var o1 = new object();
            var o2 = new object();
            var eo1 = o2;
            var eo2 = o1;
            method(ref o1, ref o2);
            Assert.That(o1, Is.EqualTo(eo1));
            Assert.That(o2, Is.EqualTo(eo2));
        }
    }
}