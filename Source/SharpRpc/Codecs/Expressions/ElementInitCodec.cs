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
    public unsafe class ElementInitCodec : IManualCodec<ElementInit>
    {
        private readonly IManualCodec<MethodInfo> methodInfoCodec;
        private readonly IManualCodec<Expression[]> expressionArrayCodec;

        public Type Type { get { return typeof(ElementInit); } }
        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return null; } }

        public ElementInitCodec(ICodecContainer codecContainer)
        {
            methodInfoCodec = codecContainer.GetManualCodecFor<MethodInfo>();
            expressionArrayCodec = codecContainer.GetManualCodecFor<Expression[]>();
        }

        public int CalculateSize(ElementInit value)
        {
            int result = 0;
            result += methodInfoCodec.CalculateSize(value.AddMethod);
            result += expressionArrayCodec.CalculateSize(value.Arguments.ToArray());
            return result;
        }

        public void Encode(ref byte* data, ElementInit value)
        {
            methodInfoCodec.Encode(ref data, value.AddMethod);
            expressionArrayCodec.Encode(ref data, value.Arguments.ToArray());
        }

        public ElementInit Decode(ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            var addMethod = methodInfoCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var arguments = expressionArrayCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            return Expression.ElementInit(addMethod, arguments);
        }
    }
}