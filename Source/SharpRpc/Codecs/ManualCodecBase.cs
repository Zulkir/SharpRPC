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
using SharpRpc.Utility;

namespace SharpRpc.Codecs
{
    public unsafe class ManualCodecBase<T> : IMethodBasedManualCodec
    {
        protected delegate int CalculateSizeDelegate(ICodecContainer codecContainer, T value);
        protected delegate void EncodeDelegate(ICodecContainer codecContainer, ref byte* data, T value);
        protected delegate T DecodeDelegate(ICodecContainer codecContainer, ref byte* data, ref int remainingBytes);

        private readonly Type type;
        private readonly IEmittingCodec emittingCodec;

        private readonly DynamicMethod calculateSizeMethod;
        private readonly DynamicMethod encodeMethod;
        private readonly DynamicMethod decodeMethod;
        private readonly DynamicMethod decodeFastMethod;

        protected ManualCodecBase(Type type, IEmittingCodec emittingCodec)
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
            const int codecContainerArgIndex = 0;
            const int valueArgIndex = 1;

            var dynamicMethod = new DynamicMethod("_calculate_size_manual_" + type.FullName,
                                                  typeof(int), new[] { typeof(ICodecContainer), type }, Assembly.GetExecutingAssembly().ManifestModule, true);
            var il = new MyILGenerator(dynamicMethod.GetILGenerator());
            var context = new ManualCodecEmittingContext(il, codecContainerArgIndex);
            emittingCodec.EmitCalculateSize(context, valueArgIndex);
            il.Ret();

            return dynamicMethod;
        }

        private DynamicMethod EmitEncode()
        {
            const int codecContainerArgIndex = 0;
            const int dataArgIndex = 1;
            const int valueArgIndex = 2;

            var dynamicMethod = new DynamicMethod("_encode_manual_" + type.FullName,
                                                  typeof(void), new[] { typeof(ICodecContainer), typeof(byte*).MakeByRefType(), type }, Assembly.GetExecutingAssembly().ManifestModule, true);
            var il = new MyILGenerator(dynamicMethod.GetILGenerator());
            var context = new ManualCodecEmittingContext(il, codecContainerArgIndex);
            il.Ldarg(dataArgIndex);
            il.Ldind_I();
            il.Stloc(context.DataPointerVar);
            emittingCodec.EmitEncode(context, valueArgIndex);
            il.Ldarg(dataArgIndex);
            il.Ldloc(context.DataPointerVar);
            il.Stind_I();
            il.Ret();
            return dynamicMethod;
        }

        private DynamicMethod EmitDecode(bool doNoCheckBounds)
        {
            const int codecContainerArgIndex = 0;
            const int dataArgIndex = 1;
            const int remainingBytesArgIndex = 2;

            var dynamicMethod = new DynamicMethod("_decode_manual_" + type.FullName + (doNoCheckBounds ? "_dncb_" : ""),
                                                  type, new[] { typeof(ICodecContainer), typeof(byte*).MakeByRefType(), typeof(int).MakeByRefType() }, Assembly.GetExecutingAssembly().ManifestModule, true);
            var il = new MyILGenerator(dynamicMethod.GetILGenerator());
            var context = new ManualCodecEmittingContext(il, codecContainerArgIndex);
            il.Ldarg(dataArgIndex);
            il.Ldind_I();
            il.Stloc(context.DataPointerVar);
            il.Ldarg(remainingBytesArgIndex);
            il.Ldind_I4();
            il.Stloc(context.RemainingBytesVar);
            emittingCodec.EmitDecode(context, doNoCheckBounds);
            il.Ldarg(dataArgIndex);
            il.Ldloc(context.DataPointerVar);
            il.Stind_I();
            il.Ldarg(remainingBytesArgIndex);
            il.Ldloc(context.RemainingBytesVar);
            il.Stind_I4();
            il.Ret();
            return dynamicMethod;
        }
    }
}