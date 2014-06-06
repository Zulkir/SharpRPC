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

namespace SharpRpc.Codecs.Expressions
{
    public unsafe class LambdaExpressionSubcodec : ExpressionSubcodecBase<LambdaExpression>
    {
        private readonly IManualCodec<string> stringCodec;
        private readonly IManualCodec<bool> boolCodec;
        private readonly IManualCodec<Expression[]> expressionArrayCodec; 

        public LambdaExpressionSubcodec(ExpressionCodec commonCodec, ICodecContainer codecContainer) : base(commonCodec, codecContainer)
        {
            stringCodec = codecContainer.GetManualCodecFor<string>();
            boolCodec = codecContainer.GetManualCodecFor<bool>();
            expressionArrayCodec = codecContainer.GetManualCodecFor<Expression[]>();
        }

        protected override int CalculateSizeTyped(LambdaExpression expression)
        {
            int result = 0;
            result += CommonCodec.CalculateSize(expression.Body);
            result += stringCodec.CalculateSize(expression.Name);
            result += boolCodec.CalculateSize(expression.TailCall);
            result += expressionArrayCodec.CalculateSize(expression.Parameters.Select(x => (Expression)x).ToArray());
            return result;
        }

        protected override void EncodeTyped(ref byte* data, LambdaExpression expression)
        {
            CommonCodec.Encode(ref data, expression.Body);
            stringCodec.Encode(ref data, expression.Name);
            boolCodec.Encode(ref data, expression.TailCall);
            expressionArrayCodec.Encode(ref data, expression.Parameters.Select(x => (Expression)x).ToArray());
        }

        protected override LambdaExpression DecodeTyped(ExpressionType expressionType, ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            //var delegateType = typeCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var body = CommonCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var name = stringCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var tailCall = boolCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var parameters = expressionArrayCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds).Select(x => (ParameterExpression)x);
            return Expression.Lambda(/*delegateType, */body, name, tailCall, parameters);
        }
    }
}