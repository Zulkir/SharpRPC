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

namespace SharpRpc.Codecs
{
    public unsafe class ManualCodec<T> : ManualCodecBase, IManualCodec<T>
    {
        private delegate int CalculateSizeDelegate(T value);
        private delegate void EncodeDelegate(ref byte* data, T value);
        private delegate T DecodeDelegate(ref byte* data, ref int remainingBytes);

        private readonly CalculateSizeDelegate calculateSizeDelegate;
        private readonly EncodeDelegate encodeDelegate;
        private readonly DecodeDelegate decodeDelegate;
        private readonly DecodeDelegate decodeFastDelegate;

        public ManualCodec(IEmittingCodec emittingCodec) : base(typeof(T), emittingCodec)
        {
            calculateSizeDelegate = (CalculateSizeDelegate)CalculateSizeMethod.CreateDelegate(typeof(CalculateSizeDelegate));
            encodeDelegate = (EncodeDelegate)EncodeMethod.CreateDelegate(typeof(EncodeDelegate));
            decodeDelegate = (DecodeDelegate)DecodeMethod.CreateDelegate(typeof(DecodeDelegate));
            decodeFastDelegate = (DecodeDelegate)DecodeFastMethod.CreateDelegate(typeof(DecodeDelegate));
        }

        public int CalculateSize(T value)
        {
            return calculateSizeDelegate(value);
        }

        public void Encode(ref byte* data, T value)
        {
            encodeDelegate(ref data, value);
        }

        public T Decode(ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            return doNotCheckBounds
                ? decodeFastDelegate(ref data, ref remainingBytes)
                : decodeDelegate(ref data, ref remainingBytes);
        }
    }
}