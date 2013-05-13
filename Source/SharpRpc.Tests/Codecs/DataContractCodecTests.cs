using System;
using System.Runtime.Serialization;
using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    [TestFixture]
    public class DataContractCodecTests : CodecTestsBase
    {
        #region Contracts
        private static bool ArraysAreEqual<T>(T[] a1, T[] a2, Func<T[], T[], bool> areEqual)
        {
            if (ReferenceEquals(a1, a2))
                return true;
            if (a1 == null || a2 == null)
                return false;
            if (a1.Length != a2.Length)
                return false;
            for (int i = 0; i < a1.Length; i++)
                if (!areEqual(a1, a2))
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

        [DataContract]
        public class EmptyContract : IEquatable<EmptyContract>
        {
            public bool Equals(EmptyContract other)
            {
                return true;
            }

            public override bool Equals(object obj) { return obj is EmptyContract && Equals((EmptyContract)obj); }
            public override int GetHashCode() { return 0; }
        }

        [DataContract]
        public class FixedContract : IEquatable<FixedContract>
        {
            [DataMember]
            public int A { get; set; }

            [DataMember]
            public double B { get; set; }

            public bool Equals(FixedContract other)
            {
                return A == other.A && B == other.B;
            }
            public override bool Equals(object obj) { return obj is FixedContract && Equals((FixedContract)obj); }
            public override int GetHashCode() { return 0; }
        }

        [DataContract]
        public class DynamicContract : IEquatable<DynamicContract>
        {
            [DataMember]
            public string A { get; set; }

            [DataMember]
            public int[] B { get; set; }

            public bool Equals(DynamicContract other)
            {
                return NullablesAreEqual(A, other.A) && ArraysAreEqual(B, other.B, (x, y) => x == y);
            }
            public override bool Equals(object obj) { return obj is DynamicContract && Equals((DynamicContract)obj); }
            public override int GetHashCode() { return 0; }
        }

        [DataContract]
        public class MixedContract : IEquatable<MixedContract>
        {
            [DataMember]
            public decimal A { get; set; }

            [DataMember]
            public float[] B { get; set; }

            [DataMember]
            public DateTime C { get; set; }

            [DataMember]
            public string D { get; set; }

            public bool Equals(MixedContract other)
            {
                return A == other.A && C == other.C && D == other.D && ArraysAreEqual(B, other.B, (x, y) => x == y);
            }
            public override bool Equals(object obj) { return obj is MixedContract && Equals((MixedContract)obj); }
            public override int GetHashCode() { return 0; }
        }

        [DataContract]
        public class NestedContract : IEquatable<NestedContract>
        {
            public int A { get; set; }
            public string B { get; set; }
            public FixedContract C { get; set; }
            public DynamicContract D { get; set; }
            public string E { get; set; }
            public MixedContract F { get; set; }

            public bool Equals(NestedContract other)
            {
                return A == other.A && B == other.B &&
                    NullablesAreEqual(C, other.C) &&
                    NullablesAreEqual(D, other.D) &&
                    E == other.E &&
                    NullablesAreEqual(F, other.F);
            }
            public override bool Equals(object obj) { return obj is NestedContract && Equals((NestedContract)obj); }
            public override int GetHashCode() { return 0; }
        }

        #endregion

        private ICodecContainer codecContainer;

        [Test]
        public void Setup()
        {
            codecContainer = new CodecContainer();
        }

        private void DoTest<T>(T value) where T : class
        {
            DoTest(new DataContractCodec(typeof(T), codecContainer), value, (o1, o2) =>
                {
                    if (ReferenceEquals(o1, null))
                        Assert.That(o2, Is.Null);
                    else
                        Assert.That(o2, Is.EqualTo(o1));
                });
        }

        [Test]
        public void Empty()
        {
            DoTest((EmptyContract)null);
            DoTest(new EmptyContract());
        }

        [Test]
        public void Fixed()
        {
            DoTest((FixedContract)null);
            DoTest(new FixedContract{ A = 123, B = 567.890 });
        }

        [Test]
        public void Dynamic()
        {
            DoTest((DynamicContract)null);
            DoTest(new DynamicContract { A = null, B = null });
            DoTest(new DynamicContract { A = null, B = new[] { 213, 34, 45, -123, 0 } });
            DoTest(new DynamicContract { A = "asdasd qw eq", B = null });
            DoTest(new DynamicContract { A = " ASd ow w iw as", B = new [] { 92112313, 0x923387f, -1231233} });
        }

        [Test]
        public void Mixed()
        {
            DoTest((MixedContract)null);
            DoTest(new MixedContract { A = 12313122934710298419737319273.45576m, B = null, C = DateTime.Now, D = null});
            DoTest(new MixedContract { A = -131.911267318361863m, B = null, C = DateTime.Now, D = @"  idsjf 293u ij902 2    8 s n\(*&^%$#@"});
            DoTest(new MixedContract { A = decimal.MinValue, B = new[] { 123.54f, 0f, 3455f, -876f }, C = DateTime.MinValue, D = null});
            DoTest(new MixedContract { A = decimal.MaxValue, B = new[] { -123.254f, float.PositiveInfinity }, C = DateTime.MinValue, D = "For the Horde!!!"});
        }

        [Test]
        public void Nested()
        {
            DoTest((NestedContract)null);
            DoTest(new NestedContract { A = 123, B = null, C = null, D = null, E = null, F = null });
            DoTest(new NestedContract { A = 123, B = "asd", C = null, D = null, E = "qwe", F = null });
            DoTest(new NestedContract 
            { 
                A = 234, 
                B = null, 
                C = new FixedContract { A = 34, B = 1.23 }, 
                D = null, E = "wer", 
                F = new MixedContract { A = 0m, B = null, C = DateTime.Now, D = "sdf"}
            });
            DoTest(new NestedContract
            {
                A = 345, 
                B = null, 
                C = new FixedContract { A = 34, B = 1.23 }, 
                D = null, 
                E = "wer", 
                F = new MixedContract { A = 0m, B = null, C = DateTime.Now, D = "sdf"}
            });
            DoTest(new NestedContract
            {
                A = 765,
                B = "zxc",
                C = null,
                D = new DynamicContract { A = null, B = new[] {-123, 54, 567, 0} },
                E = null,
                F = new MixedContract { A = 123m, B = null, C = DateTime.Now, D = "qweqwe"}
            });
            DoTest(new NestedContract
            {
                A = 321,
                B = "asdasd",
                C = new FixedContract { A = 23424, B = -123.123 },
                D = new DynamicContract { A = "zxczxc", B = new int[0] },
                E = null,
                F = null
            });
            DoTest(new NestedContract
            {
                A = 234,
                B = "qweqe",
                C = new FixedContract {A = 34534, B = 876.123},
                D = new DynamicContract { A = "asdasd", B = new[] {234, 456, 567} },
                E = "zxczxc",
                F = new MixedContract { A = 987.123m, B = new[] {234.56f, 123.23f, 78.34f}, C = DateTime.Now, D = "sdfsdf"}
            });
        }
    }
}
