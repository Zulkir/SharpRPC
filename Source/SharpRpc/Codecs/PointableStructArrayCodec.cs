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
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace SharpRpc.Codecs
{
    public class PointableStructArrayCodec : IEmittingCodec
    {
        private readonly int sizeOfStruct;

        public PointableStructArrayCodec(Type typeOfStruct)
        {
            sizeOfStruct = Marshal.SizeOf(typeOfStruct);
        }

        public bool HasFixedSize { get { return false; } }
        public int FixedSize { get { throw new InvalidOperationException(); } }

        public void EmitCalculateSize(ILGenerator il)
        {
            var arrayIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brtrue, arrayIsNotNullLabel);

            // Array is null branch
            il.Emit(OpCodes.Pop);
            il.Emit_Ldc_I4(sizeof(int));
            il.Emit(OpCodes.Br, endOfSubmethodLabel);

            // String is not null branch
            il.MarkLabel(arrayIsNotNullLabel);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
            il.Emit_Ldc_I4(sizeOfStruct);
            il.Emit(OpCodes.Mul);
            il.Emit_Ldc_I4(sizeof(int));
            il.Emit(OpCodes.Add);
            il.MarkLabel(endOfSubmethodLabel);
        }

        public void EmitEncode(ILGenerator il, ILocalVariableCollection locals, Action<ILGenerator> load)
        {
            var arrayIsNotNullLabel = il.DefineLabel();
            var arrayIsNotEmptylabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            load(il);
            il.Emit(OpCodes.Brtrue, arrayIsNotNullLabel);

            // Array is null branch
            il.Emit(OpCodes.Ldloc, locals.DataPointer);
            il.Emit_Ldc_I4(-1);
            il.Emit(OpCodes.Stind_I4);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));
            il.Emit(OpCodes.Br, endOfSubmethodLabel);

            il.MarkLabel(arrayIsNotNullLabel);
            load(il);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Brtrue, arrayIsNotEmptylabel);

            // Array is empty branch
            il.Emit(OpCodes.Ldloc, locals.DataPointer);
            il.Emit_Ldc_I4(0);
            il.Emit(OpCodes.Stind_I4);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));
            il.Emit(OpCodes.Br, endOfSubmethodLabel);

            // Array is not empty branch
            load(il);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);

        }

        public void EmitDecode(ILGenerator il, ILocalVariableCollection locals, bool doNotCheckBounds)
        {
            throw new NotImplementedException();
        }
    }
}