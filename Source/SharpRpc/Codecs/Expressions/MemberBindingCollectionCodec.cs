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

using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace SharpRpc.Codecs.Expressions
{
    public unsafe class MemberBindingCollectionCodec
    {
        private readonly MemberBindingCodec memberBindingCodec;
        private readonly IManualCodec<int> intCodec;

        public MemberBindingCollectionCodec(ExpressionCodec expressionCodec, ICodecContainer codecContainer)
        {
            memberBindingCodec = new MemberBindingCodec(this, expressionCodec, codecContainer);
            intCodec = codecContainer.GetManualCodecFor<int>();
        }

        public int CalculateSize(ReadOnlyCollection<MemberBinding> value)
        {
            if (value == null)
                return intCodec.CalculateSize(1);
            int result = 0;
            result += intCodec.CalculateSize(value.Count);
            foreach (var memberBinding in value)
                result += memberBindingCodec.CalculateSize(memberBinding);
            return result;
        }

        public void Encode(ref byte* data, ReadOnlyCollection<MemberBinding> value)
        {
            if (value == null)
            {
                intCodec.Encode(ref data, -1);
                return;
            }
            intCodec.Encode(ref data, value.Count);
            foreach (var memberBinding in value)
                memberBindingCodec.Encode(ref data, memberBinding);
        }

        public MemberBinding[] Decode(ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            int count = intCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            if (count == -1)
                return null;
            var result = new MemberBinding[count];
            for (int i = 0; i < count; i++)
                result[i] = memberBindingCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            return result;
        }
    }
}