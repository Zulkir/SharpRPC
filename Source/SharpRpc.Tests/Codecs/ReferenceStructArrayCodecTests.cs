using System;
using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    public class ReferenceStructArrayCodecTests : CodecTestsBase
    {
        #region Custom element types
        public struct StructWithReferences : IEquatable<StructWithReferences>
        {
            public double A;
            public string B;

            public bool Equals(StructWithReferences other) { return A == other.A && B == other.B; }
            public override bool Equals(object obj) { return obj is StructWithReferences && Equals((StructWithReferences)obj); }
            public override int GetHashCode() { return 0; }
        }
        #endregion

        private ICodecContainer codecContainer;

        [SetUp]
        public void Setup()
        {
            codecContainer = new CodecContainer();
        }

        private void DoTest<T>(T[] array)
        {
            DoTest(new ReferenceStructArrayCodec(typeof(T), codecContainer), array, (b, a) => Assert.That(b, Is.EqualTo(a)));
        }

        [Test]
        public void StructsWithReferences()
        {
            DoTest((StructWithReferences[])null);
            DoTest(new StructWithReferences[0]);
            DoTest(new[] { new StructWithReferences { A = 123.456, B = null } });
            DoTest(new[] { new StructWithReferences { A = 123.456, B = "ASdasd sdh qiwu diqwh d" } });
        }
    }
}