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

using System;
using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    [TestFixture]
    public class PointableStructArrayCodecTests : CodecTestsBase
    {
        private void DoTest<T>(T[] value) where T : struct
        {
            DoTest(new PointableStructArrayCodec(), value, (a1, a2) =>
                {
                    if (a2 == null)
                        Assert.That(a1, Is.Null);
                    else
                        Assert.That(a1, Is.EquivalentTo(a2));
                });
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

        [Test]
        public void Null()
        {
            DoTest<byte>(null);
            DoTest<int>(null);
            DoTest<double>(null);
            DoTest<MySimpleStruct>(null);
        }

        [Test]
        public void Empty()
        {
            DoTest(new byte[0]);
            DoTest(new int[0]);
            DoTest(new double[0]);
            DoTest(new MySimpleStruct[0]);
        }

        [Test]
        public void SingleElement()
        {
            DoTest(new byte[] { 123 });
            DoTest(new[] { 234234123 });
            DoTest(new[] { 234.567 });
            DoTest(new[] { new MySimpleStruct { A = 123.123, B = 9873} });
        }

        [Test]
        public void ManyElements()
        {
            DoTest(new byte[] { 123, 4, 36, 13, 75 });
            DoTest(new[] { 234234123, 353456,136616, -45715, 34623442134, -234, -87642 });
            DoTest(new[] { 234.567, 987.234, 3.123, -45656.21, 234 });
            DoTest(new[]
                {
                    new MySimpleStruct { A = 123.123, B = 9873 }, 
                    new MySimpleStruct { A = -985.189, B = 999 },
                    new MySimpleStruct { A = double.NaN, B = ushort.MaxValue }
                });
        }
    }
}