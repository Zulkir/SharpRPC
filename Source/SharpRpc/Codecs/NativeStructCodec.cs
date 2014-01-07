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
using System.Reflection.Emit;
using SharpRpc.Utility;

namespace SharpRpc.Codecs
{
    public class NativeStructCodec : IEmittingCodec
    {
        private readonly Type type;
        private readonly int sizeInBytes;

        public NativeStructCodec(Type type)
        {
            this.type = type;
            sizeInBytes = NativeStructHelper.CalculateSize(type);
        }

        public Type Type { get { return type; } }
        public int? FixedSize { get { return sizeInBytes; } }
        public int? MaxSize { get { return sizeInBytes; } }
        public bool CanBeInlined { get { return true; } }
        public int EncodingComplexity { get { return 1; } }

        // todo: special op-codes for basic types

        public void EmitCalculateSize(ILGenerator il, Action<ILGenerator> emitLoad)
        {
            il.Emit_Ldc_I4(sizeInBytes);
        }

        public void EmitEncode(ILGenerator il, ILocalVariableCollection locals, Action<ILGenerator> emitLoad)
        {
            il.Emit(OpCodes.Ldloc, locals.DataPointer);              // *(T*) data = val
            emitLoad(il);
            il.Emit(OpCodes.Stobj, type);
            il.Emit_IncreasePointer(locals.DataPointer, sizeInBytes);// data += sizeInBytes
        }

        public void EmitDecode(ILGenerator il, ILocalVariableCollection locals, bool doNotCheckBounds)
        {
            if (!doNotCheckBounds)
            {
                var everythingsAllrightLabel = il.DefineLabel();
                il.Emit(OpCodes.Ldloc, locals.RemainingBytes);           // if (remainingBytes >= sizeInBytes)
                il.Emit_Ldc_I4(sizeInBytes);                             //     goto everythingsAllrightLabel
                il.Emit(OpCodes.Bge, everythingsAllrightLabel);
                il.Emit_ThrowUnexpectedEndException();                   // throw new InvalidDataException("...")
                il.MarkLabel(everythingsAllrightLabel);                  // label everythingsAllrightLabel
            }
            il.Emit(OpCodes.Ldloc, locals.DataPointer);                  // stack_0 = *(T*) data
            il.Emit(OpCodes.Ldobj, type);
            il.Emit_IncreasePointer(locals.DataPointer, sizeInBytes);    // data += sizeInBytes
            il.Emit_DecreaseInteger(locals.RemainingBytes, sizeInBytes); // remainingBytes -= sizeInBytes
        }
    }
}
