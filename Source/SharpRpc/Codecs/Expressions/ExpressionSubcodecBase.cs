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

namespace SharpRpc.Codecs.Expressions
{
    public unsafe abstract class ExpressionSubcodecBase<T> : IExpressionSubcodec where T : Expression
    {
        protected ExpressionCodec CommonCodec { get; private set; }
        protected ICodecContainer CodecContainer { get; private set; }
        protected IManualCodec<ExpressionType> ExpressionTypeCodec { get; private set; } 

        protected ExpressionSubcodecBase(ExpressionCodec commonCodec, ICodecContainer codecContainer)
        {
            CommonCodec = commonCodec;
            CodecContainer = codecContainer;
            ExpressionTypeCodec = codecContainer.GetManualCodecFor<ExpressionType>();
        }

        protected abstract int CalculateSizeTyped(T expression);
        protected abstract void EncodeTyped(ref byte* data, T expression);
        protected abstract T DecodeTyped(ExpressionType expressionType, ref byte* data, ref int remainingBytes, bool doNotCheckBounds);

        public int CalculateSize(Expression value)
        {
            return ExpressionTypeCodec.CalculateSize(value.NodeType) + CalculateSizeTyped((T)value);
        }

        public void Encode(ref byte* data, Expression value)
        {
            ExpressionTypeCodec.Encode(ref data, value.NodeType);
            EncodeTyped(ref data, (T)value);
        }

        public Expression Decode(ExpressionType expressionType, ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            return DecodeTyped(expressionType, ref data, ref remainingBytes, doNotCheckBounds);
        }
    }
}