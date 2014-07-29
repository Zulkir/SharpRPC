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

using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpRpc.Codecs.Expressions
{
    public unsafe class NewExpressionSubcodec : ExpressionSubcodecBase<NewExpression>
    {
        private readonly IManualCodec<ConstructorInfo> constructorInfoCodec;
        private readonly IManualCodec<Expression[]> expressionArrayCodec;
        private readonly IManualCodec<MemberInfo[]> memberInfoArrayCodec;

        public NewExpressionSubcodec(ExpressionCodec commonCodec, ICodecContainer codecContainer) 
            : base(commonCodec, codecContainer)
        {
            constructorInfoCodec = codecContainer.GetManualCodecFor<ConstructorInfo>();
            expressionArrayCodec = codecContainer.GetManualCodecFor<Expression[]>();
            memberInfoArrayCodec = codecContainer.GetManualCodecFor<MemberInfo[]>();
        }

        protected override int CalculateSizeTyped(NewExpression expression)
        {
            int result = 0;
            result += constructorInfoCodec.CalculateSize(expression.Constructor);
            result += expressionArrayCodec.CalculateSize(expression.Arguments.ToArray());
            result += memberInfoArrayCodec.CalculateSize(expression.Members != null ? expression.Members.ToArray() : null);
            return result;
        }

        protected override void EncodeTyped(ref byte* data, NewExpression expression)
        {
            constructorInfoCodec.Encode(ref data, expression.Constructor);
            expressionArrayCodec.Encode(ref data, expression.Arguments.ToArray());
            memberInfoArrayCodec.Encode(ref data, expression.Members != null ? expression.Members.ToArray() : null);
        }

        protected override NewExpression DecodeTyped(ExpressionType expressionType, ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            var constructor = constructorInfoCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var arguments = expressionArrayCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var members = memberInfoArrayCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            return members != null 
                ? Expression.New(constructor, arguments, members)
                : Expression.New(constructor, arguments);
        }
    }
}