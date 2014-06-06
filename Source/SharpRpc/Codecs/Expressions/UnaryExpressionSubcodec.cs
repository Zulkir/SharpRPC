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
    public unsafe class UnaryExpressionSubcodec : ExpressionSubcodecBase<UnaryExpression>
    {
        private readonly IManualCodec<Type> typeCodec;
        private readonly IManualCodec<MethodInfo> methodInfoCodec;

        public UnaryExpressionSubcodec(ExpressionCodec commonCodec, ICodecContainer codecContainer) : base(commonCodec, codecContainer)
        {
            typeCodec = codecContainer.GetManualCodecFor<Type>();
            methodInfoCodec = codecContainer.GetManualCodecFor<MethodInfo>();
        }

        protected override int CalculateSizeTyped(UnaryExpression expression)
        {
            int result = 0;
            result += CommonCodec.CalculateSize(expression.Operand);
            result += typeCodec.CalculateSize(expression.Type);
            result += methodInfoCodec.CalculateSize(expression.Method);
            return result;
        }

        protected override void EncodeTyped(ref byte* data, UnaryExpression expression)
        {
            CommonCodec.Encode(ref data, expression.Operand);
            typeCodec.Encode(ref data, expression.Type);
            methodInfoCodec.Encode(ref data, expression.Method);
        }

        protected override UnaryExpression DecodeTyped(ExpressionType expressionType, ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            var operand = CommonCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var type = typeCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var method = methodInfoCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            return Expression.MakeUnary(expressionType, operand, type, method);
        }
    }
}