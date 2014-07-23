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
    public unsafe class MethodCallExpressionSubcodec : ExpressionSubcodecBase<MethodCallExpression>
    {
        private readonly IManualCodec<MethodInfo> methodInfoCodec;
        private readonly IManualCodec<Expression[]> expressionArrayCodec;

        public MethodCallExpressionSubcodec(ExpressionCodec commonCodec, ICodecContainer codecContainer) : base(commonCodec, codecContainer)
        {
            methodInfoCodec = codecContainer.GetManualCodecFor<MethodInfo>();
            expressionArrayCodec = codecContainer.GetManualCodecFor<Expression[]>();
        }

        protected override int CalculateSizeTyped(MethodCallExpression expression)
        {
            int result = 0;
            result += CommonCodec.CalculateSize(expression.Object);
            result += methodInfoCodec.CalculateSize(expression.Method);
            result += expressionArrayCodec.CalculateSize(expression.Arguments.ToArray());
            return result;
        }

        protected override void EncodeTyped(ref byte* data, MethodCallExpression expression)
        {
            CommonCodec.Encode(ref data, expression.Object);
            methodInfoCodec.Encode(ref data, expression.Method);
            expressionArrayCodec.Encode(ref data, expression.Arguments.ToArray());
        }

        protected override MethodCallExpression DecodeTyped(ExpressionType expressionType, ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            var instance = CommonCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var method = methodInfoCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var arguments = expressionArrayCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            return Expression.Call(instance, method, arguments);
        }
    }
}