using System;
using System.IO;
using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    public unsafe class CodecTestsBase
    {
        protected void DoTest<T>(IEmittingCodec codec, T value)
        {
            DoTest(codec, value, (a, b) => Assert.That(b, Is.EqualTo(a)));
        }

        protected void DoTest<T>(IEmittingCodec codec, T value, Action<T, T> assert)
        {
            var manualCodec = new ManualCodec<T>(codec);

            if (codec.HasFixedSize)
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

                p = pData;
                remainingBytes = size - 1;
                Assert.Throws<InvalidDataException>(() => manualCodec.Decode(ref p, ref remainingBytes, false));
            }
        } 
    }
}