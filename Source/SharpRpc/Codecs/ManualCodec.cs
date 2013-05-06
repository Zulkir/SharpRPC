#region License
/*
Copyright (c) 2013 Daniil Rodin of Buhgalteria.Kontur team of SKB Kontur

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
    public unsafe class ManualCodec<T> : IManualCodec<T>
    {
        private delegate int CalculateSizeMethod(T value);
        private delegate void EncodeMethod(ref byte* data, T value);
        private delegate T DecodeMethod(ref byte* data, ref int remainingBytes);

        private readonly IEmittingCodec emittingCodec;
        private readonly CalculateSizeMethod calculateSizeMethod;
        private readonly EncodeMethod encodeMethod;
        private readonly DecodeMethod decodeMethod;
        private readonly DecodeMethod decodeFastMethod;

        public ManualCodec(IEmittingCodec emittingCodec)
        {
            this.emittingCodec = emittingCodec;
            calculateSizeMethod = EmitCalculateSize(emittingCodec);
            encodeMethod = EmitEncode(emittingCodec);
            decodeMethod = EmitDecode(emittingCodec, false);
            decodeFastMethod = EmitDecode(emittingCodec, true);
        }

        public bool HasFixedSize { get { return emittingCodec.HasFixedSize; } }
        public int FixedSize { get { return emittingCodec.FixedSize; } }

        public int CalculateSize(T value)
        {
            return calculateSizeMethod(value);
        }

        public void Encode(ref byte* data, T value)
        {
            encodeMethod(ref data, value);
        }

        public T Decode(ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            return doNotCheckBounds
                ? decodeFastMethod(ref data, ref remainingBytes)
                : decodeMethod(ref data, ref remainingBytes);
        }

        private static readonly Type[] CalculateSizeParameterTypes = new[] { typeof(T) };
        private static readonly Type[] EncodeParameterTypes = new[] { typeof(byte*).MakeByRefType(), typeof(T) };
        private static readonly Type[] DecodeParameterTypes = new[] { typeof(byte*).MakeByRefType(), typeof(int).MakeByRefType() };

        private static CalculateSizeMethod EmitCalculateSize(IEmittingCodec emittingCodec)
        {
            var dynamicMethod = new DynamicMethod("_calculate_size_manual_" + typeof(T).FullName,
                typeof(int), CalculateSizeParameterTypes, Assembly.GetExecutingAssembly().ManifestModule);
            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            emittingCodec.EmitCalculateSize(il);
            il.Emit(OpCodes.Ret);
            return (CalculateSizeMethod)dynamicMethod.CreateDelegate(typeof(CalculateSizeMethod));
        }

        private static EncodeMethod EmitEncode(IEmittingCodec emittingCodec)
        {
            var dynamicMethod = new DynamicMethod("_encode_manual_" + typeof(T).FullName, 
                typeof(void), EncodeParameterTypes, Assembly.GetExecutingAssembly().ManifestModule);
            var il = dynamicMethod.GetILGenerator();
            var locals = new LocalVariableCollection(il, false);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldind_I);
            il.Emit(OpCodes.Stloc, locals.DataPointer);
            emittingCodec.EmitEncode(il, locals, 1);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, locals.DataPointer);
            il.Emit(OpCodes.Stind_I);
            il.Emit(OpCodes.Ret);
            return (EncodeMethod)dynamicMethod.CreateDelegate(typeof(EncodeMethod));
        }

        private static DecodeMethod EmitDecode(IEmittingCodec emittingCodec, bool doNoCheckBounds)
        {
            var dynamicMethod = new DynamicMethod("_decode_manual_" + typeof(T).FullName + (doNoCheckBounds ? "_dncb_" : ""),
                typeof(T), DecodeParameterTypes, Assembly.GetExecutingAssembly().ManifestModule);
            var il = dynamicMethod.GetILGenerator();
            var locals = new LocalVariableCollection(il, true);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldind_I);
            il.Emit(OpCodes.Stloc, locals.DataPointer);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldind_I4);
            il.Emit(OpCodes.Stloc, locals.RemainingBytes);
            emittingCodec.EmitDecode(il, locals, doNoCheckBounds);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, locals.DataPointer);
            il.Emit(OpCodes.Stind_I);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldloc, locals.RemainingBytes);
            il.Emit(OpCodes.Stind_I4);
            il.Emit(OpCodes.Ret);
            return (DecodeMethod)dynamicMethod.CreateDelegate(typeof(DecodeMethod));
        }
    }
}