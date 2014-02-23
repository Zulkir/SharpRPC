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
using System.Reflection;
using System.Reflection.Emit;
using SharpRpc.Codecs;

namespace SharpRpc.ClientSide
{
    public class GenericServiceProxyMethodIoCodec : IServiceProxyMethodIoCodec
    {
        private readonly Type substitutedType;
        private readonly IServiceProxyMethodParameterAccessor parameterAccessor; 

        public GenericServiceProxyMethodIoCodec(Type substitutedType, IServiceProxyMethodParameterAccessor parameterAccessor)
        {
            this.substitutedType = substitutedType;
            this.parameterAccessor = parameterAccessor;
        }

        private static readonly MethodInfo GetManualCodecForMethod = typeof(ICodecContainer).GetMethod("GetManualCodecFor");

        public void EmitCalculateSize(IServiceProxyClassBuildingContext classContext, IEmittingContext emittingContext)
        {
            var il = emittingContext.IL;
            il.Emit_Ldarg(0);
            il.Emit(OpCodes.Ldfld, classContext.CodecContainerField);
            il.Emit(OpCodes.Call, GetManualCodecForMethod.MakeGenericMethod(substitutedType));
            parameterAccessor.EmitLoad(il);
            var methodInfo = typeof(IManualCodec<>).GetMethod("CalculateSize");
            var concreteCodecType = typeof(IManualCodec<>).MakeGenericType(substitutedType);
            var genericMethodInfo = TypeBuilder.GetMethod(concreteCodecType, methodInfo);
            il.Emit(OpCodes.Callvirt, genericMethodInfo);
        }

        public void EmitEncode(IServiceProxyClassBuildingContext classContext, IEmittingContext emittingContext)
        {
            var il = emittingContext.IL;
            il.Emit_Ldarg(0);
            il.Emit(OpCodes.Ldfld, classContext.CodecContainerField);
            il.Emit(OpCodes.Call, GetManualCodecForMethod.MakeGenericMethod(substitutedType));
            il.Emit(OpCodes.Ldloca, emittingContext.DataPointerVar);
            parameterAccessor.EmitLoad(il);
            var methodInfo = typeof(IManualCodec<>).GetMethod("Encode");
            var concreteCodecType = typeof(IManualCodec<>).MakeGenericType(substitutedType);
            var genericMethodInfo = TypeBuilder.GetMethod(concreteCodecType, methodInfo);
            il.Emit(OpCodes.Callvirt, genericMethodInfo);
        }

        public void EmitDecode(IServiceProxyClassBuildingContext classContext, IEmittingContext emittingContext)
        {
            var il = emittingContext.IL;
            il.Emit_Ldarg(0);
            il.Emit(OpCodes.Ldfld, classContext.CodecContainerField);
            il.Emit(OpCodes.Call, GetManualCodecForMethod.MakeGenericMethod(substitutedType));
            il.Emit(OpCodes.Ldloca, emittingContext.DataPointerVar);
            il.Emit(OpCodes.Ldloca, emittingContext.RemainingBytesVar);
            il.Emit_Ldc_I4(0);
            var methodInfo = typeof(IManualCodec<>).GetMethod("Decode");
            var concreteCodecType = typeof(IManualCodec<>).MakeGenericType(substitutedType);
            var genericMethodInfo = TypeBuilder.GetMethod(concreteCodecType, methodInfo);
            il.Emit(OpCodes.Callvirt, genericMethodInfo);
        }

        public void EmitDecodeAndStore(IServiceProxyClassBuildingContext classContext, IEmittingContext emittingContext)
        {
            parameterAccessor.EmitBeginStore(emittingContext.IL);
            EmitDecode(classContext, emittingContext);
            parameterAccessor.EmitEndStore(emittingContext.IL);
        }
    }
}