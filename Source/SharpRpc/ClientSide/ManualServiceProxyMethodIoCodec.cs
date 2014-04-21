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
using SharpRpc.Codecs;

namespace SharpRpc.ClientSide
{
    public class ManualServiceProxyMethodIoCodec : IServiceProxyMethodIoCodec
    {
        private readonly Type type;
        private readonly IServiceProxyMethodParameterAccessor parameterAccessor;

        public ManualServiceProxyMethodIoCodec(Type type, IServiceProxyMethodParameterAccessor parameterAccessor)
        {
            this.type = type;
            this.parameterAccessor = parameterAccessor;
        }

        public void EmitCalculateSize(IServiceProxyClassBuildingContext classContext, IEmittingContext emittingContext)
        {
            int indexOfCodec = classContext.GetManualCodecIndex(type);
            var il = emittingContext.IL;
            il.Ldarg(0);
            il.Ldfld(classContext.ManualCodecsField);
            il.Ldc_I4(indexOfCodec);
            il.Ldelem_Ref();
            var concreteCodecType = typeof(IManualCodec<>).MakeGenericType(type);
            il.Isinst(concreteCodecType);
            parameterAccessor.EmitLoad(il);
            il.Callvirt(concreteCodecType.GetMethod("CalculateSize"));
        }

        public void EmitEncode(IServiceProxyClassBuildingContext classContext, IEmittingContext emittingContext)
        {
            int indexOfCodec = classContext.GetManualCodecIndex(type);
            var il = emittingContext.IL;
            il.Ldarg(0);
            il.Ldfld(classContext.ManualCodecsField);
            il.Ldc_I4(indexOfCodec);
            il.Ldelem_Ref();
            var concreteCodecType = typeof(IManualCodec<>).MakeGenericType(type);
            il.Isinst(concreteCodecType);
            il.Ldloca(emittingContext.DataPointerVar);
            parameterAccessor.EmitLoad(il);
            il.Callvirt(concreteCodecType.GetMethod("Encode"));
        }

        public void EmitDecode(IServiceProxyClassBuildingContext classContext, IEmittingContext emittingContext)
        {
            int indexOfCodec = classContext.GetManualCodecIndex(type);
            var il = emittingContext.IL;
            il.Ldarg(0);
            il.Ldfld(classContext.ManualCodecsField);
            il.Ldc_I4(indexOfCodec);
            il.Ldelem_Ref();
            var concreteCodecType = typeof(IManualCodec<>).MakeGenericType(type);
            il.Isinst(concreteCodecType);
            il.Ldloca(emittingContext.DataPointerVar);
            il.Ldloca(emittingContext.RemainingBytesVar);
            il.Ldc_I4(0);
            il.Callvirt(concreteCodecType.GetMethod("Decode"));
        }

        public void EmitDecodeAndStore(IServiceProxyClassBuildingContext classContext, IEmittingContext emittingContext)
        {
            parameterAccessor.EmitBeginStore(emittingContext.IL);
            EmitDecode(classContext, emittingContext);
            parameterAccessor.EmitEndStore(emittingContext.IL);
        }
    }
}