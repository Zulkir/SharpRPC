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
using System.IO;
using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    [TestFixture]
    public abstract unsafe class CodecTestsBase
    {
        protected ICodecContainer CodecContainer { get; private set; }

        [SetUp]
        public virtual void Setup()
        {
            CodecContainer = new CodecContainer();
        }

        protected void DoTest<T>(IEmittingCodec codec, T value)
        {
            DoTest(codec, value, (a, b) => Assert.That(a, Is.EqualTo(b)));
        }

        protected void DoTest<T>(IEmittingCodec codec, T value, Action<T, T> assert)
        {
            Assert.That(codec.Type, Is.EqualTo(typeof(T)));

            var manualCodec = new ManualCodec<T>(CodecContainer, codec);

            if (codec.FixedSize.HasValue)
                Assert.That(manualCodec.CalculateSize(value), Is.EqualTo(codec.FixedSize));
            int size = manualCodec.CalculateSize(value);

            var data = new byte[size];
            fixed (byte* pData = data)
            {
                var p = pData;
                manualCodec.Encode(ref p, value);
                Assert.That(p - pData, Is.EqualTo(size));

                p = pData;
                int remainingBytes = size;
                var decodedValue = manualCodec.Decode(ref p, ref remainingBytes, false);
                assert(decodedValue, value);
                Assert.That(p - pData, Is.EqualTo(size));
                Assert.That(remainingBytes, Is.EqualTo(0));

                p = pData;
                remainingBytes = size;
                decodedValue = manualCodec.Decode(ref p, ref remainingBytes, true);
                assert(decodedValue, value);
                Assert.That(p - pData, Is.EqualTo(size));
                Assert.That(remainingBytes, Is.EqualTo(0));

                if (size > 0)
                {
                    p = pData;
                    remainingBytes = size - 1;
                    Assert.Throws<InvalidDataException>(() => manualCodec.Decode(ref p, ref remainingBytes, false));
                }
            }
        } 
    }
}