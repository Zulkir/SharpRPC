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
using SharpRpc.Utility;

namespace SharpRpc.Codecs
{
    public class IndirectCodec : IEmittingCodec
    {
        private readonly Type type;

        public IndirectCodec(Type type)
        {
            this.type = type;
        }

        public Type Type { get { return type; } }
        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return null; } }
        public bool CanBeInlined { get { return true; } }
        public int EncodingComplexity { get { return 1; } }

        public void EmitCalculateSize(IEmittingContext context, Action<MyILGenerator> emitLoad)
        {
            var il = context.IL;
            context.EmitLoadManualCodecFor(type);
            emitLoad(il);
            il.Callvirt(ManualCodecMethods.CalculateSize(type));
        }

        public void EmitEncode(IEmittingContext context, Action<MyILGenerator> emitLoad)
        {
            var il = context.IL;
            context.EmitLoadManualCodecFor(type);
            il.Ldloca(context.DataPointerVar);
            emitLoad(il);
            il.Callvirt(ManualCodecMethods.Encode(type));
        }

        public void EmitDecode(IEmittingContext context, bool doNotCheckBounds)
        {
            var il = context.IL;
            context.EmitLoadManualCodecFor(type);
            il.Ldloca(context.DataPointerVar);
            il.Ldloca(context.RemainingBytesVar);
            il.Ldc_I4(0);
            il.Callvirt(ManualCodecMethods.Decode(type));
        }
    }
}
