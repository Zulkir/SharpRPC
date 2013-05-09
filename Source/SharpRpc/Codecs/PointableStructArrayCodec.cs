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
        private readonly Type typeOfStruct;
        private readonly int sizeOfStruct;

        public PointableStructArrayCodec(Type typeOfStruct)
        {
            this.typeOfStruct = typeOfStruct;
            sizeOfStruct = Marshal.SizeOf(typeOfStruct);
        }

        public bool HasFixedSize { get { return false; } }
        public int FixedSize { get { throw new InvalidOperationException(); } }

        public void EmitCalculateSize(ILGenerator il)
        {
            var arrayIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            il.Emit(OpCodes.Dup);                        // stack_1 = stack_0
            il.Emit(OpCodes.Brtrue, arrayIsNotNullLabel);// if (stack_1) goto arrayIsNotNullLabel

            // Array is null branch
            il.Emit(OpCodes.Pop);                        // pop(stack_0)
            il.Emit_Ldc_I4(sizeof(int));                 // stack_0 = sizeof(int)
            il.Emit(OpCodes.Br, endOfSubmethodLabel);    // goto endOfSubmethodLabel

            // String is not null branch
            il.MarkLabel(arrayIsNotNullLabel);
            il.Emit(OpCodes.Ldlen);                      // stack_0 = stack_0.Length * sizeOfStruct + sizeof(int)
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

            load(il);                                                // if (value)
            il.Emit(OpCodes.Brtrue, arrayIsNotNullLabel);            //     goto arrayIsNotNullLabel

            // Array is null branch
            il.Emit(OpCodes.Ldloc, locals.DataPointer);              // *(int*) data = -1
            il.Emit_Ldc_I4(-1);
            il.Emit(OpCodes.Stind_I4);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));// data += sizeof(int)
            il.Emit(OpCodes.Br, endOfSubmethodLabel);                // goto endOfSubmethodLabel

            il.MarkLabel(arrayIsNotNullLabel);
            load(il);                                                // if (value.Length)
            il.Emit(OpCodes.Ldlen);                                  //     goto arrayIsNotEmptylabel
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Brtrue, arrayIsNotEmptylabel);

            // Array is empty branch
            il.Emit(OpCodes.Ldloc, locals.DataPointer);              // *(int*) data = 0
            il.Emit_Ldc_I4(0);
            il.Emit(OpCodes.Stind_I4);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));// data += sizeof(int)
                
            il.Emit(OpCodes.Br, endOfSubmethodLabel);                // goto endOfSubmethodLabel

            // Array is not empty branch
            il.MarkLabel(arrayIsNotEmptylabel);
            var lengthVar = locals.GetOrAdd("length",                // var length = value.Length
                lil => lil.DeclareLocal(typeof(int)));
            load(il);                                   
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Stloc, lengthVar);
            var sizeVar = locals.GetOrAdd("sizeInBytes",             // var sizeInBytes = length * sizeOfStruct
                lil => lil.DeclareLocal(typeof(int)));
            il.Emit(OpCodes.Ldloc, lengthVar);
            il.Emit_Ldc_I4(sizeOfStruct);
            il.Emit(OpCodes.Mul);
            il.Emit(OpCodes.Stloc, sizeVar);
            il.Emit(OpCodes.Ldloc, locals.DataPointer);              // *(int*) data = length
            il.Emit(OpCodes.Ldloc, lengthVar);
            il.Emit(OpCodes.Stind_I4);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));// data += sizeof(int)
            var arrayPointerVar = il.Emit_PinArray(                  // var pinned arrayPointer = pin(value)
                typeOfStruct, locals, load);
            il.Emit(OpCodes.Ldloc, locals.DataPointer);              // cpblk(data, (byte*)arrayPointer, sizeInBytes)
            il.Emit(OpCodes.Ldloc, arrayPointerVar);
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Ldloc, sizeVar);
            il.Emit(OpCodes.Cpblk);
            il.Emit_UnpinArray(arrayPointerVar);                     // unpin(arrayPointer)
            il.Emit_IncreasePointer(locals.DataPointer, sizeVar);    // data += sizeInBytes
            il.MarkLabel(endOfSubmethodLabel);
        }

        public void EmitDecode(ILGenerator il, ILocalVariableCollection locals, bool doNotCheckBounds)
        {
            var enoughBytesForLengthLabel = il.DefineLabel();
            var enoughBytesForDataLabel = il.DefineLabel();
            var lengthIsMinusOneLabel = il.DefineLabel();
            var lengthIsZeroLabel = il.DefineLabel();
            var labelGroup = new[] {lengthIsMinusOneLabel, lengthIsZeroLabel};
            var lengthIsPositiveLabel = il.DefineLabel();
            var endOfMethodLabel = il.DefineLabel();

            if (!doNotCheckBounds)
            {
                il.Emit(OpCodes.Ldloc, locals.RemainingBytes);          // if (remainingBytes >= sizeof(int))
                il.Emit_Ldc_I4(sizeof(int));                            //     goto enoughBytesForLengthLabel
                il.Emit(OpCodes.Bge, enoughBytesForLengthLabel);

                // not enough bytes for length
                il.Emit_ThrowUnexpectedEndException();                  // throw new InvalidDataException(...)
            }
            
            // enough bytes for length
            il.MarkLabel(enoughBytesForLengthLabel);
            var lengthVar = locals.GetOrAdd("length",                   // var length = *(int*)data
                lil => lil.DeclareLocal(typeof(int)));
            il.Emit(OpCodes.Ldloc, locals.DataPointer);
            il.Emit(OpCodes.Ldind_I4);
            il.Emit(OpCodes.Stloc, lengthVar);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));   // data += sizeof(int)
            il.Emit_DecreaseInteger(locals.RemainingBytes, sizeof(int));// remainingBytes -= sizeof(int)
            il.Emit(OpCodes.Ldloc, lengthVar);                          // switch(length + 1)
            il.Emit_Ldc_I4(1);                                          //     case 0:  goto lengthIsMinusOneLabel
            il.Emit(OpCodes.Add);                                       //     case 1:  goto lengthIsZeroLabel
            il.Emit(OpCodes.Switch, labelGroup);                        //     default: goto lengthIsPositiveLabel
            il.Emit(OpCodes.Br, lengthIsPositiveLabel);
            
            // length is -1
            il.MarkLabel(lengthIsMinusOneLabel);
            il.Emit(OpCodes.Ldnull);                                    // stack_0 = null
            il.Emit(OpCodes.Br, endOfMethodLabel);                      // goto endOfMethodLabel

            // length is 0
            il.MarkLabel(lengthIsZeroLabel);
            il.Emit_Ldc_I4(0);                                          // stack_0 = new T[0]
            il.Emit(OpCodes.Newarr, typeOfStruct);
            il.Emit(OpCodes.Br, endOfMethodLabel);                      // goto endOfMethodLabel
            
            // length is positive
            il.MarkLabel(lengthIsPositiveLabel);
            var sizeVar = locals.GetOrAdd("sizeInBytes",                // var sizeInBytes = length * sizeOfStruct
                lil => lil.DeclareLocal(typeof(int)));
            il.Emit(OpCodes.Ldloc, lengthVar);
            il.Emit_Ldc_I4(sizeOfStruct);
            il.Emit(OpCodes.Mul);
            il.Emit(OpCodes.Stloc, sizeVar);
            
            if (!doNotCheckBounds)
            {
                il.Emit(OpCodes.Ldloc, locals.RemainingBytes);          // if (remainingBytes >= sizeInBytes)
                il.Emit(OpCodes.Ldloc, sizeVar);                        //     goto enoughBytesForDataLabel
                il.Emit(OpCodes.Bge, enoughBytesForDataLabel);
            
                // not enough bytes for data
                il.Emit_ThrowUnexpectedEndException();                  // throw new InvalidDataException(...)
            }
            
            // enough bytes for data
            il.MarkLabel(enoughBytesForDataLabel);
            var resultVar = locals.GetOrAdd(                            // var result = new T[length]
                "arrayOf" + typeOfStruct.FullName,
                lil => lil.DeclareLocal(typeOfStruct.MakeArrayType()));
            il.Emit(OpCodes.Ldloc, lengthVar);
            il.Emit(OpCodes.Newarr, typeOfStruct);
            il.Emit(OpCodes.Stloc, resultVar);
            var arrayPointerVar = il.Emit_PinArray(                     // var pinned arrayPointer = pin(value)
                typeOfStruct, locals, resultVar);
            il.Emit(OpCodes.Ldloc, arrayPointerVar);                    // cpblk((byte*)arrayPointer, data, sizeInBytes)
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Ldloc, locals.DataPointer);
            il.Emit(OpCodes.Ldloc, sizeVar);
            il.Emit(OpCodes.Cpblk);
            il.Emit_UnpinArray(arrayPointerVar);                        // unpin(arrayPointer)
            il.Emit_IncreasePointer(locals.DataPointer, sizeVar);       // data += size
            il.Emit_DecreaseInteger(locals.RemainingBytes, sizeVar);    // remainingBytes -= size
            il.Emit(OpCodes.Ldloc, resultVar);                          // stack_0 = result
            il.MarkLabel(endOfMethodLabel);
        }
    }
}