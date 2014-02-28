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

namespace SharpRpc.Codecs
{
    public class ManualCodecBase : IMethodBasedManualCodec
    {
        private readonly Type type;
        private readonly IEmittingCodec emittingCodec;

        private readonly DynamicMethod calculateSizeMethod;
        private readonly DynamicMethod encodeMethod;
        private readonly DynamicMethod decodeMethod;
        private readonly DynamicMethod decodeFastMethod;

        public ManualCodecBase(Type type, IEmittingCodec emittingCodec)
        {
            this.type = type;
            this.emittingCodec = emittingCodec;

            calculateSizeMethod = EmitCalculateSize();
            encodeMethod = EmitEncode();
            decodeMethod = EmitDecode(false);
            decodeFastMethod = EmitDecode(true);
        }

        public DynamicMethod CalculateSizeMethod { get { return calculateSizeMethod; } }
        public DynamicMethod EncodeMethod { get { return encodeMethod; } }
        public DynamicMethod DecodeMethod { get { return decodeMethod; } }
        public DynamicMethod DecodeFastMethod { get { return decodeFastMethod; } }

        public Type Type { get { return emittingCodec.Type; } }
        public int? FixedSize { get { return emittingCodec.FixedSize; } }
        public int? MaxSize { get { return emittingCodec.MaxSize; } }

        private DynamicMethod EmitCalculateSize()
        {
            var dynamicMethod = new DynamicMethod("_calculate_size_manual_" + type.FullName,
                                                  typeof(int), new[] { type }, Assembly.GetExecutingAssembly().ManifestModule, true);
            var il = dynamicMethod.GetILGenerator();
            var context = new EmittingContext(il);
            emittingCodec.EmitCalculateSize(context, 0);
            il.Emit(OpCodes.Ret);

            return dynamicMethod;
        }

        private DynamicMethod EmitEncode()
        {
            var dynamicMethod = new DynamicMethod("_encode_manual_" + type.FullName,
                                                  typeof(void), new[] { typeof(byte*).MakeByRefType(), type }, Assembly.GetExecutingAssembly().ManifestModule, true);
            var il = dynamicMethod.GetILGenerator();
            var context = new EmittingContext(il);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldind_I);
            il.Emit(OpCodes.Stloc, context.DataPointerVar);
            emittingCodec.EmitEncode(context, 1);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, context.DataPointerVar);
            il.Emit(OpCodes.Stind_I);
            il.Emit(OpCodes.Ret);
            return dynamicMethod;
        }

        private DynamicMethod EmitDecode(bool doNoCheckBounds)
        {
            var dynamicMethod = new DynamicMethod("_decode_manual_" + type.FullName + (doNoCheckBounds ? "_dncb_" : ""),
                                                  type, new[] { typeof(byte*).MakeByRefType(), typeof(int).MakeByRefType() }, Assembly.GetExecutingAssembly().ManifestModule, true);
            var il = dynamicMethod.GetILGenerator();
            var context = new EmittingContext(il);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldind_I);
            il.Emit(OpCodes.Stloc, context.DataPointerVar);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldind_I4);
            il.Emit(OpCodes.Stloc, context.RemainingBytesVar);
            emittingCodec.EmitDecode(context, doNoCheckBounds);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, context.DataPointerVar);
            il.Emit(OpCodes.Stind_I);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldloc, context.RemainingBytesVar);
            il.Emit(OpCodes.Stind_I4);
            il.Emit(OpCodes.Ret);
            return dynamicMethod;
        }
    }
}