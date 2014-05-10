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

        private void DoTest<T>(T[] array)
        {
            DoTest(new ReferenceStructArrayCodec(typeof(T), CodecContainer), array, (b, a) => Assert.That(b, Is.EqualTo(a)));
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