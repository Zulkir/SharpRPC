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
    public class NativeStructArrayCodec : IEmittingCodec
    {
        private readonly Type typeOfStruct;
        private readonly int sizeOfStruct;

        public NativeStructArrayCodec(Type typeOfStruct)
        {
            this.typeOfStruct = typeOfStruct;
            sizeOfStruct = NativeStructHelper.CalculateSize(typeOfStruct);
        }

        public Type Type { get { return typeOfStruct.MakeArrayType(); } }
        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return null; } }
        public bool CanBeInlined { get { return true; } }
        public int EncodingComplexity { get { return 1; } }

        public void EmitCalculateSize(IEmittingContext context, Action<MyILGenerator> emitLoad)
        {
            var il = context.IL;

            var arrayIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            emitLoad(il);                               // if (value)
            il.Brtrue(arrayIsNotNullLabel);             //     goto arrayIsNotNullLabel

            // Array is null branch
            il.Ldc_I4(sizeof(int));                     // stack_0 = sizeof(int)
            il.Br(endOfSubmethodLabel);                 // goto endOfSubmethodLabel

            // String is not null branch
            il.MarkLabel(arrayIsNotNullLabel);
            emitLoad(il);                               // stack_0 = value.Length * sizeOfStruct + sizeof(int)
            il.Ldlen();
            il.Conv_I4();
            il.Ldc_I4(sizeOfStruct);
            il.Mul();
            il.Ldc_I4(sizeof(int));
            il.Add();
            il.MarkLabel(endOfSubmethodLabel);
        }

        public void EmitEncode(IEmittingContext context, Action<MyILGenerator> emitLoad)
        {
            var il = context.IL;

            var arrayIsNotNullLabel = il.DefineLabel();
            var arrayIsNotEmptylabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            emitLoad(il);                                               // if (value)
            il.Brtrue(arrayIsNotNullLabel);                             //     goto arrayIsNotNullLabel

            // Array is null branch
            il.Ldloc(context.DataPointerVar);                           // *(int*) data = -1
            il.Ldc_I4(-1);
            il.Stind_I4();
            il.IncreasePointer(context.DataPointerVar, sizeof(int));    // data += sizeof(int)
            il.Br(endOfSubmethodLabel);                                 // goto endOfSubmethodLabel

            il.MarkLabel(arrayIsNotNullLabel);
            emitLoad(il);                                               // if (value.Length)
            il.Ldlen();                                                 //     goto arrayIsNotEmptylabel
            il.Conv_I4();
            il.Brtrue(arrayIsNotEmptylabel);

            // Array is empty branch
            il.Ldloc(context.DataPointerVar);                           // *(int*) data = 0
            il.Ldc_I4(0);
            il.Stind_I4();
            il.IncreasePointer(context.DataPointerVar, sizeof(int));    // data += sizeof(int)
            il.Br(endOfSubmethodLabel);                                 // goto endOfSubmethodLabel

            // Array is not empty branch
            il.MarkLabel(arrayIsNotEmptylabel);
            var lengthVar = context.GetSharedVariable<int>("length");   // var length = value.Length
            emitLoad(il);                                   
            il.Ldlen();
            il.Conv_I4();
            il.Stloc(lengthVar);
            var sizeVar = context.GetSharedVariable<int>("sizeInBytes");// var sizeInBytes = length * sizeOfStruct
            il.Ldloc(lengthVar);
            il.Ldc_I4(sizeOfStruct);
            il.Mul();
            il.Stloc(sizeVar);
            il.Ldloc(context.DataPointerVar);                           // *(int*) data = length
            il.Ldloc(lengthVar);
            il.Stind_I4();
            il.IncreasePointer(context.DataPointerVar, sizeof(int));    // data += sizeof(int)
            var pointerVar = il.PinArray(typeOfStruct, emitLoad);       // var pinned arrayPointer = pin(value)
            il.Ldloc(context.DataPointerVar);                           // cpblk(data, (byte*)arrayPointer, sizeInBytes)
            il.Ldloc(pointerVar);
            il.Conv_I();
            il.Ldloc(sizeVar);
            il.Cpblk();
            il.UnpinArray(pointerVar);                                  // unpin(arrayPointer)
            il.IncreasePointer(context.DataPointerVar, sizeVar);        // data += sizeInBytes
            il.MarkLabel(endOfSubmethodLabel);
        }

        public void EmitDecode(IEmittingContext context, bool doNotCheckBounds)
        {
            var il = context.IL;

            var enoughBytesForLengthLabel = il.DefineLabel();
            var enoughBytesForDataLabel = il.DefineLabel();
            var lengthIsMinusOneLabel = il.DefineLabel();
            var lengthIsZeroLabel = il.DefineLabel();
            var labelGroup = new[] {lengthIsMinusOneLabel, lengthIsZeroLabel};
            var lengthIsPositiveLabel = il.DefineLabel();
            var endOfMethodLabel = il.DefineLabel();

            if (!doNotCheckBounds)
            {
                il.Ldloc(context.RemainingBytesVar);                    // if (remainingBytes >= sizeof(int))
                il.Ldc_I4(sizeof(int));                                 //     goto enoughBytesForLengthLabel
                il.Bge(enoughBytesForLengthLabel);

                // not enough bytes for length
                il.ThrowUnexpectedEndException();                       // throw new InvalidDataException(...)
            }
            
            // enough bytes for length
            il.MarkLabel(enoughBytesForLengthLabel);
            var lengthVar = context.GetSharedVariable<int>("length");    // var length = *(int*)data
            il.Ldloc(context.DataPointerVar);
            il.Ldind_I4();
            il.Stloc(lengthVar);
            il.IncreasePointer(context.DataPointerVar, sizeof(int));    // data += sizeof(int)
            il.DecreaseInteger(context.RemainingBytesVar, sizeof(int)); // remainingBytes -= sizeof(int)
            il.Ldloc(lengthVar);                                        // switch(length + 1)
            il.Ldc_I4(1);                                               //     case 0:  goto lengthIsMinusOneLabel
            il.Add();                                                   //     case 1:  goto lengthIsZeroLabel
            il.Switch(labelGroup);                                      //     default: goto lengthIsPositiveLabel
            il.Br(lengthIsPositiveLabel);
            
            // length is -1
            il.MarkLabel(lengthIsMinusOneLabel);
            il.Ldnull();                                                // stack_0 = null
            il.Br(endOfMethodLabel);                                    // goto endOfMethodLabel

            // length is 0
            il.MarkLabel(lengthIsZeroLabel);
            il.Ldc_I4(0);                                               // stack_0 = new T[0]
            il.Newarr(typeOfStruct);
            il.Br(endOfMethodLabel);                                    // goto endOfMethodLabel
            
            // length is positive
            il.MarkLabel(lengthIsPositiveLabel);
            var sizeVar = context.GetSharedVariable<int>("sizeInBytes");// var sizeInBytes = length * sizeOfStruct
            il.Ldloc(lengthVar);
            il.Ldc_I4(sizeOfStruct);
            il.Mul();
            il.Stloc(sizeVar);
            
            if (!doNotCheckBounds)
            {
                il.Ldloc(context.RemainingBytesVar);                    // if (remainingBytes >= sizeInBytes)
                il.Ldloc(sizeVar);                                      //     goto enoughBytesForDataLabel
                il.Bge(enoughBytesForDataLabel);
            
                // not enough bytes for data
                il.ThrowUnexpectedEndException();                       // throw new InvalidDataException(...)
            }
            
            // enough bytes for data
            il.MarkLabel(enoughBytesForDataLabel);
            var resultVar = context.GetSharedVariable(                   // var result = new T[length]
                typeOfStruct.MakeArrayType(), "arrayOf"); 
            il.Ldloc(lengthVar);
            il.Newarr(typeOfStruct);
            il.Stloc(resultVar);
            var pointerVar = il.PinArray(typeOfStruct, resultVar); // var pinned arrayPointer = pin(value)
            il.Ldloc(pointerVar);                         // cpblk((byte*)arrayPointer, data, sizeInBytes)
            il.Conv_I();
            il.Ldloc(context.DataPointerVar);
            il.Ldloc(sizeVar);
            il.Cpblk();
            il.UnpinArray(pointerVar);                                // unpin(arrayPointer)
            il.IncreasePointer(context.DataPointerVar, sizeVar);       // data += size
            il.DecreaseInteger(context.RemainingBytesVar, sizeVar);    // remainingBytes -= size
            il.Ldloc(resultVar);                          // stack_0 = result
            il.MarkLabel(endOfMethodLabel);
        }
    }
}