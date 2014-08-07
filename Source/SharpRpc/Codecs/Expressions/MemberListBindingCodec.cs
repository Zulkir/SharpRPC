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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpRpc.Codecs.Expressions
{
    public unsafe class MemberListBindingCodec : IManualCodec<MemberListBinding>
    {
        private readonly IManualCodec<MemberInfo> memberInfoCodec;
        private readonly IManualCodec<ElementInit[]> elementInitArrayCodec; 

        public Type Type { get { return typeof(MemberListBinding); } }
        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return null; } }

        public MemberListBindingCodec(ICodecContainer codecContainer)
        {
            memberInfoCodec = codecContainer.GetManualCodecFor<MemberInfo>();
            elementInitArrayCodec = codecContainer.GetManualCodecFor<ElementInit[]>();
        }

        public int CalculateSize(MemberListBinding value)
        {
            int result = 0;
            result += memberInfoCodec.CalculateSize(value.Member);
            result += elementInitArrayCodec.CalculateSize(value.Initializers.ToArray());
            return result;
        }

        public void Encode(ref byte* data, MemberListBinding value)
        {
            memberInfoCodec.Encode(ref data, value.Member);
            elementInitArrayCodec.Encode(ref data, value.Initializers.ToArray());
        }

        public MemberListBinding Decode(ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            var member = memberInfoCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var initializers = elementInitArrayCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            return Expression.ListBind(member, initializers);
        }
    }
}