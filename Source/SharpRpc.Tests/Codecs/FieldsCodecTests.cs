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
using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    public class FieldsCodecTests : CodecTestsBase
    {
        #region Contracts
        private static bool ArraysAreEqual<T>(T[] a1, T[] a2, Func<T, T, bool> areEqual)
        {
            if (ReferenceEquals(a1, a2))
                return true;
            if (a1 == null || a2 == null)
                return false;
            if (a1.Length != a2.Length)
                return false;
            for (int i = 0; i < a1.Length; i++)
                if (!areEqual(a1[i], a2[i]))
                    return false;
            return true;
        }

        private static bool NullablesAreEqual<T>(T o1, T o2) where T : class, IEquatable<T>
        {
            if (ReferenceEquals(o1, o2))
                return true;
            if (o1 == null || o2 == null)
                return false;
            return o1.Equals(o2);
        }

        public struct EmptyStruct : IEquatable<EmptyStruct>
        {
            public bool Equals(EmptyStruct other)
            {
                return true;
            }

            public override bool Equals(object obj) { return obj is EmptyStruct && Equals((EmptyStruct)obj); }
            public override int GetHashCode() { return 0; }
        }

        public struct FixedStruct : IEquatable<FixedStruct>
        {
            public int A;
            public double B;

            public bool Equals(FixedStruct other)
            {
                return A == other.A && B == other.B;
            }
            public override bool Equals(object obj) { return obj is FixedStruct && Equals((FixedStruct)obj); }
            public override int GetHashCode() { return 0; }
        }

        public struct DynamicStruct : IEquatable<DynamicStruct>
        {
            public string A;
            public int[] B;

            public bool Equals(DynamicStruct other)
            {
                return NullablesAreEqual(A, other.A) && ArraysAreEqual(B, other.B, (x, y) => x == y);
            }
            public override bool Equals(object obj) { return obj is DynamicStruct && Equals((DynamicStruct)obj); }
            public override int GetHashCode() { return 0; }
            public override string ToString() { return string.Format("{0} {1}", A, string.Join("-", B)); }
        }

        public struct MixedStruct : IEquatable<MixedStruct>
        {
            public decimal A;
            public float[] B;
            public DateTime C;
            public string D;

            public bool Equals(MixedStruct other)
            {
                return A == other.A && C == other.C && D == other.D && ArraysAreEqual(B, other.B, (x, y) => x == y);
            }
            public override bool Equals(object obj) { return obj is MixedStruct && Equals((MixedStruct)obj); }
            public override int GetHashCode() { return 0; }
        }

        public struct NestedStruct : IEquatable<NestedStruct>
        {
            public int A;
            public string B;
            public FixedStruct C;
            public DynamicStruct D;
            public string E;
            public MixedStruct F;

            public bool Equals(NestedStruct other)
            {
                return A == other.A && B == other.B &&
                    C.Equals(other.C) &&
                    D.Equals(other.D) &&
                    E == other.E &&
                    F.Equals(other.F);
            }
            public override bool Equals(object obj) { return obj is NestedStruct && Equals((NestedStruct)obj); }
            public override int GetHashCode() { return 0; }
        }

        public struct StructWithPrivateFields
        {
            public int A;
            private int B;

            public StructWithPrivateFields(int a, int b)
            {
                A = a; B = b;
            }

            public bool Equals(StructWithPrivateFields other) { return A == other.A && B == other.B; }
            public override bool Equals(object obj) { return obj is StructWithPrivateFields && Equals((StructWithPrivateFields)obj); }
            public override int GetHashCode() { return 0; }
        }

        #endregion

        private void DoTest<T>(T value) where T : struct
        {
            DoTest(new FieldsCodec(typeof(T), CodecContainer), value, (o1, o2) => Assert.That(o2, Is.EqualTo(o1)));
        }

        [Test]
        public void Empty()
        {
            DoTest(new EmptyStruct());
        }

        [Test]
        public void Fixed()
        {
            DoTest(new FixedStruct());
            DoTest(new FixedStruct{ A = 123, B = 567.890 });
        }

        [Test]
        public void Dynamic()
        {
            DoTest(new DynamicStruct());
            DoTest(new DynamicStruct { A = null, B = null });
            DoTest(new DynamicStruct { A = null, B = new[] { 213, 34, 45, -123, 0 } });
            DoTest(new DynamicStruct { A = "asdasd qw eq", B = null });
            DoTest(new DynamicStruct { A = " ASd ow w iw as", B = new [] { 92112313, 0x923387f, -1231233} });
        }

        [Test]
        public void Mixed()
        {
            DoTest(new MixedStruct());
            DoTest(new MixedStruct { A = 12313122934710298419737319273.45576m, B = null, C = DateTime.Now, D = null});
            DoTest(new MixedStruct { A = -131.911267318361863m, B = null, C = DateTime.Now, D = @"  idsjf 293u ij902 2    8 s n\(*&^%$#@"});
            DoTest(new MixedStruct { A = decimal.MinValue, B = new[] { 123.54f, 0f, 3455f, -876f }, C = DateTime.MinValue, D = null});
            DoTest(new MixedStruct { A = decimal.MaxValue, B = new[] { -123.254f, float.PositiveInfinity }, C = DateTime.MinValue, D = "For the Horde!!!"});
        }

        [Test]
        public void Nested()
        {
            DoTest(new NestedStruct());
            DoTest(new NestedStruct {A = 123, B = null, C = new FixedStruct(), D = new DynamicStruct(), E = null, F = new MixedStruct()});
            DoTest(new NestedStruct {A = 123, B = "asd", C = new FixedStruct(), D = new DynamicStruct(), E = "qwe", F = new MixedStruct()});
            DoTest(new NestedStruct
                {
                    A = 234,
                    B = null,
                    C = new FixedStruct {A = 34, B = 1.23},
                    D = new DynamicStruct(),
                    E = "wer",
                    F = new MixedStruct {A = 0m, B = null, C = DateTime.Now, D = "sdf"}
                });
            DoTest(new NestedStruct
                {
                    A = 345,
                    B = null,
                    C = new FixedStruct {A = 34, B = 1.23},
                    D = new DynamicStruct(),
                    E = "wer",
                    F = new MixedStruct {A = 0m, B = null, C = DateTime.Now, D = "sdf"}
                });
            DoTest(new NestedStruct
                {
                    A = 765,
                    B = "zxc",
                    C = new FixedStruct(),
                    D = new DynamicStruct {A = null, B = new[] {-123, 54, 567, 0}},
                    E = null,
                    F = new MixedStruct {A = 123m, B = null, C = DateTime.Now, D = "qweqwe"}
                });
            DoTest(new NestedStruct
                {
                    A = 321,
                    B = "asdasd",
                    C = new FixedStruct {A = 23424, B = -123.123},
                    D = new DynamicStruct {A = "zxczxc", B = new int[0]},
                    E = null,
                    F = new MixedStruct()
                });
            DoTest(new NestedStruct
                {
                    A = 234,
                    B = "qweqe",
                    C = new FixedStruct {A = 34534, B = 876.123},
                    D = new DynamicStruct {A = "asdasd", B = new[] {234, 456, 567}},
                    E = "zxczxc",
                    F = new MixedStruct {A = 987.123m, B = new[] {234.56f, 123.23f, 78.34f}, C = DateTime.Now, D = "sdfsdf"}
                });
        }

        [Test]
        public void PrivateFields()
        {
            DoTest(new StructWithPrivateFields());
            DoTest(new StructWithPrivateFields(123, 234));
        }
    }
}