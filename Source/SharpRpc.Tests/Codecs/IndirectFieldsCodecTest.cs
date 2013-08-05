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

using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    public class IndirectFieldsCodecTest : CodecTestsBase
    {
        #region Contracts

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

        private ICodecContainer codecContainer;

        [SetUp]
        public void Setup()
        {
            codecContainer = new CodecContainer();
        }

        private void DoTest<T>(T value) where T : struct 
        {
            DoTest(new IndirectCodec(typeof(T), new FieldsCodec(typeof(T), codecContainer)), value, (o1, o2) => Assert.That(o2, Is.EqualTo(o1)));
        }

        [Test]
        public void PrivateFields()
        {
            DoTest(new StructWithPrivateFields());
            DoTest(new StructWithPrivateFields(123, 234));
        }
    }
}