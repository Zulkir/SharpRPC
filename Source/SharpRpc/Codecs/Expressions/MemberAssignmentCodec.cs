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
    public unsafe class MemberAssignmentCodec : IManualCodec<MemberAssignment>
    {
        private readonly ExpressionCodec expressionCodec;
        private readonly IManualCodec<MemberInfo> memberInfoCodec;

        public Type Type { get { return typeof(MemberAssignment); } }
        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return null; } }

        public MemberAssignmentCodec(ExpressionCodec expressionCodec, ICodecContainer codecContainer)
        {
            this.expressionCodec = expressionCodec;
            memberInfoCodec = codecContainer.GetManualCodecFor<MemberInfo>();
        }

        public int CalculateSize(MemberAssignment value)
        {
            int result = 0;
            result += memberInfoCodec.CalculateSize(value.Member);
            result += expressionCodec.CalculateSize(value.Expression);
            return result;
        }

        public void Encode(ref byte* data, MemberAssignment value)
        {
            memberInfoCodec.Encode(ref data, value.Member);
            expressionCodec.Encode(ref data, value.Expression);
        }

        public MemberAssignment Decode(ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            var member = memberInfoCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var expression = expressionCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            return Expression.Bind(member, expression);
        }
    }
}