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

using System.Linq.Expressions;
using System.Reflection;

namespace SharpRpc.Codecs.Expressions
{
    public unsafe class BinaryExpressionSubcodec : ExpressionSubcodecBase<BinaryExpression>
    {
        private readonly IManualCodec<bool> boolCodec;
        private readonly IManualCodec<MethodInfo> methodInfoCodec;

        public BinaryExpressionSubcodec(ExpressionCodec commonCodec, ICodecContainer codecContainer) : base(commonCodec, codecContainer)
        {
            boolCodec = codecContainer.GetManualCodecFor<bool>();
            methodInfoCodec = codecContainer.GetManualCodecFor<MethodInfo>();
        }

        protected override int CalculateSizeTyped(BinaryExpression expression)
        {
            int result = 0;
            result += CommonCodec.CalculateSize(expression.Left);
            result += CommonCodec.CalculateSize(expression.Right);
            result += boolCodec.CalculateSize(expression.IsLiftedToNull);
            result += methodInfoCodec.CalculateSize(expression.Method);
            result += CommonCodec.CalculateSize(expression.Conversion);
            return result;
        }

        protected override void EncodeTyped(ref byte* data, BinaryExpression expression)
        {
            CommonCodec.Encode(ref data, expression.Left);
            CommonCodec.Encode(ref data, expression.Right);
            boolCodec.Encode(ref data, expression.IsLiftedToNull);
            methodInfoCodec.Encode(ref data, expression.Method);
            CommonCodec.Encode(ref data, expression.Conversion);
        }

        protected override BinaryExpression DecodeTyped(ExpressionType expressionType, ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            var left = CommonCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var right = CommonCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var liftToNull = boolCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var method = methodInfoCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var conversion = (LambdaExpression)CommonCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            return Expression.MakeBinary(expressionType, left, right, liftToNull, method, conversion);
        }
    }
}