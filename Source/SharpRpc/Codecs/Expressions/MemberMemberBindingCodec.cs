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
using System.Linq.Expressions;
using System.Reflection;

namespace SharpRpc.Codecs.Expressions
{
    public unsafe class MemberMemberBindingCodec : IManualCodec<MemberMemberBinding>
    {
        private readonly MemberBindingCollectionCodec memberBindingCollectionCodec;
        private readonly IManualCodec<MemberInfo> memberInfoCodec;

        public Type Type { get { return typeof(MemberMemberBinding); } }
        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return null; } }

        public MemberMemberBindingCodec(MemberBindingCollectionCodec memberBindingCollectionCodec, ICodecContainer codecContainer)
        {
            this.memberBindingCollectionCodec = memberBindingCollectionCodec;
            memberInfoCodec = codecContainer.GetManualCodecFor<MemberInfo>();
        }

        public int CalculateSize(MemberMemberBinding value)
        {
            int result = 0;
            result += memberInfoCodec.CalculateSize(value.Member);
            result += memberBindingCollectionCodec.CalculateSize(value.Bindings);
            return result;
        }

        public void Encode(ref byte* data, MemberMemberBinding value)
        {
            memberInfoCodec.Encode(ref data, value.Member);
            memberBindingCollectionCodec.Encode(ref data, value.Bindings);
        }

        public MemberMemberBinding Decode(ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            var member = memberInfoCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var bindings = memberBindingCollectionCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            return Expression.MemberBind(member, bindings);
        }
    }
}