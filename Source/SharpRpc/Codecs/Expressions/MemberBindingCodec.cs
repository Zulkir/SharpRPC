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

namespace SharpRpc.Codecs.Expressions
{
    public unsafe class MemberBindingCodec : IManualCodec<MemberBinding>
    {
        private readonly IManualCodec<MemberBindingType> memberBindingTypeCodec;
        private readonly MemberAssignmentCodec memberAssignmentCodec;
        private readonly MemberListBindingCodec memberListBindingCodec;
        private readonly MemberMemberBindingCodec memberMemberBindingCodec;

        public Type Type { get { return typeof(MemberBinding); } }
        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return null; } }

        public MemberBindingCodec(MemberBindingCollectionCodec memberBindingCollectionCodec, ExpressionCodec expressionCodec, ICodecContainer codecContainer)
        {
            memberBindingTypeCodec = codecContainer.GetManualCodecFor<MemberBindingType>();
            memberAssignmentCodec = new MemberAssignmentCodec(expressionCodec, codecContainer);
            memberListBindingCodec = new MemberListBindingCodec(codecContainer);
            memberMemberBindingCodec = new MemberMemberBindingCodec(memberBindingCollectionCodec, codecContainer);
        }

        public int CalculateSize(MemberBinding value)
        {
            int result = 0;
            result += memberBindingTypeCodec.CalculateSize(value.BindingType);
            switch (value.BindingType)
            {
                case MemberBindingType.Assignment:
                    result += memberAssignmentCodec.CalculateSize((MemberAssignment)value);
                    break;
                case MemberBindingType.MemberBinding:
                    result += memberMemberBindingCodec.CalculateSize((MemberMemberBinding)value);
                    break;
                case MemberBindingType.ListBinding:
                    result += memberListBindingCodec.CalculateSize((MemberListBinding)value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return result;
        }

        public void Encode(ref byte* data, MemberBinding value)
        {
            memberBindingTypeCodec.Encode(ref data, value.BindingType);
            switch (value.BindingType)
            {
                case MemberBindingType.Assignment:
                    memberAssignmentCodec.Encode(ref data, (MemberAssignment)value);
                    return;
                case MemberBindingType.MemberBinding:
                    memberMemberBindingCodec.Encode(ref data, (MemberMemberBinding)value);
                    return;
                case MemberBindingType.ListBinding:
                    memberListBindingCodec.Encode(ref data, (MemberListBinding)value);
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public MemberBinding Decode(ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            var type = memberBindingTypeCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            switch (type)
            {
                case MemberBindingType.Assignment:
                    return memberAssignmentCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
                case MemberBindingType.MemberBinding:
                    return memberMemberBindingCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
                case MemberBindingType.ListBinding:
                    return memberListBindingCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}