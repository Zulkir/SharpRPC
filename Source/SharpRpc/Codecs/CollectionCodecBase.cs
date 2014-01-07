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

namespace SharpRpc.Codecs
{
    public abstract class CollectionCodecBase : IEmittingCodec
    {
        protected delegate void EnumerateLoopBody(Action loadCurrent);

        private readonly Type type;
        private readonly Type elementType;
        private readonly IEmittingCodec elementCodec;

        public Type Type { get { return type; } }
        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return null; } }
        public bool CanBeInlined { get { return elementCodec.CanBeInlined; } }
        public int EncodingComplexity { get { return elementCodec.EncodingComplexity; } }

        protected Type ElementType { get { return elementType; } }
        protected IEmittingCodec ElementCodec { get { return elementCodec; } }

        protected CollectionCodecBase(Type type, Type elementType, ICodecContainer codecContainer)
        {
            this.type = type;
            this.elementType = elementType;
            elementCodec = codecContainer.GetEmittingCodecFor(elementType);
        }

        protected abstract void EmitCreateCollection(ILGenerator il, LocalBuilder lengthVar);
        protected abstract void EmitLoadCount(ILGenerator il, Action<ILGenerator> emitLoad);
        protected abstract void EmitEnumerateCollection(ILGenerator il, Action<ILGenerator> emitLoad, EnumerateLoopBody loopBody);
        protected abstract void EmitDecodeAndStore(ILGenerator il, ILocalVariableCollection locals, LocalBuilder collectionVar, LocalBuilder iVar, bool doNotCheckBounds);

        public void EmitCalculateSize(ILGenerator il, Action<ILGenerator> emitLoad)
        {
            var valueIsNullOrEmptyLabel = il.DefineLabel();
            var valueHasElementsLabel = il.DefineLabel();
            var endOfMethodLabel = il.DefineLabel();

            var elemVar = il.DeclareLocal(elementType);                 // T elem

            emitLoad(il);                                               // if (!value)
            il.Emit(OpCodes.Brfalse, valueIsNullOrEmptyLabel);//               goto valueIsNullOrEmptyLabel

            EmitLoadCount(il, emitLoad);                                // if ((int)value.Length)
            il.Emit(OpCodes.Brtrue, valueHasElementsLabel);             //     goto valueHasElementsLabel

            il.MarkLabel(valueIsNullOrEmptyLabel);                      // label valueIsNullOrEmptyLabel
            il.Emit_Ldc_I4(sizeof(int));                                // stack_0 = sizeof(int)
            il.Emit(OpCodes.Br, endOfMethodLabel);                      // goto endOfMethodLabel

            il.MarkLabel(valueHasElementsLabel);                        // label valueHasElementsLabel
            il.Emit_Ldc_I4(sizeof(int));                                // sum = sizeof(int)
            EmitEnumerateCollection(il, emitLoad, emitLoadCurrent =>    // foreach (current in value) 
                {
                    emitLoadCurrent();                                  //     elem = current
                    il.Emit(OpCodes.Stloc, elemVar);
                    elementCodec.EmitCalculateSize(il, elemVar);        //     stack_1 = CalculateSize(elem)
                    il.Emit(OpCodes.Add);                               //     stack_0 = stack_0 + stack_1
                });
            il.MarkLabel(endOfMethodLabel);                             // label endOfMethodLabel
        }

        public void EmitEncode(ILGenerator il, ILocalVariableCollection locals, Action<ILGenerator> emitLoad)
        {
            var valueIsNotNullLabel = il.DefineLabel();
            var endOfMethodLabel = il.DefineLabel();

            var elemVar = il.DeclareLocal(elementType);                 // TElement elem

            emitLoad(il);                                               // if (value)
            il.Emit(OpCodes.Brtrue, valueIsNotNullLabel);               //     goto valueIsNotNullLabel

            // value is null branch
            il.Emit(OpCodes.Ldloc, locals.DataPointer);                 // *(int*) data = -1
            il.Emit_Ldc_I4(-1);
            il.Emit(OpCodes.Stind_I4);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));   // data += sizeof(int)
            il.Emit(OpCodes.Br, endOfMethodLabel);                      // goto endOfMethodLabel

            // value is not null branch
            il.MarkLabel(valueIsNotNullLabel);                          // label valueIsNotNullLabel
            il.Emit(OpCodes.Ldloc, locals.DataPointer);                 // *(int*) data = (int)value.Count
            EmitLoadCount(il, emitLoad);
            il.Emit(OpCodes.Stind_I4);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));   // data += sizeof(int)
            EmitEnumerateCollection(il, emitLoad, emitLoadCurrent =>    // foreach (current in value)
                {
                    emitLoadCurrent();                                  //     elem = current
                    il.Emit(OpCodes.Stloc, elemVar);
                    elementCodec.EmitEncode(il, locals, elemVar);       //     encode(data, elem)
                });
            il.MarkLabel(endOfMethodLabel);                             // label endOfMethodLabel
        }

        public void EmitDecode(ILGenerator il, ILocalVariableCollection locals, bool doNotCheckBounds)
        {
            var valueIsNotNullLabel = il.DefineLabel();
            var endOfMethodLabel = il.DefineLabel();

            var resultVar = il.DeclareLocal(type);                      // TCollection result

            if (!doNotCheckBounds)
            {
                var canReadLengthLabel = il.DefineLabel();
                il.Emit(OpCodes.Ldloc, locals.RemainingBytes);          // if (remainingBytes >= sizeof(int))
                il.Emit_Ldc_I4(sizeof(int));                            //     goto canReadLengthLabel
                il.Emit(OpCodes.Bge, canReadLengthLabel);
                il.Emit_ThrowUnexpectedEndException();                  // throw new InvalidDataException("...")
                il.MarkLabel(canReadLengthLabel);                       // label canReadLengthLabel
            }

            var lengthVar = il.DeclareLocal(typeof(int));               // var length = *(int*) data
            il.Emit(OpCodes.Ldloc, locals.DataPointer);
            il.Emit(OpCodes.Ldind_I4);
            il.Emit(OpCodes.Stloc, lengthVar);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));   // data += sizeof(int)
            il.Emit_DecreaseInteger(locals.RemainingBytes, sizeof(int));// remainingBytes -= sizeof(int)
            il.Emit(OpCodes.Ldloc, lengthVar);                          // if (length != -1)
            il.Emit_Ldc_I4(-1);                                         //     goto valueIsNotNullLabel
            il.Emit(OpCodes.Bne_Un, valueIsNotNullLabel);

            il.Emit(OpCodes.Ldnull);                                    // stack_0 = null
            il.Emit(OpCodes.Br, endOfMethodLabel);                      // goto endOfMethodLabel

            il.MarkLabel(valueIsNotNullLabel);                          // label valueIsNotNullLabel
            EmitCreateCollection(il, lengthVar);                        // result = new TCollection()
            il.Emit(OpCodes.Stloc, resultVar);
            il.EmitForLoop(lengthVar, (lil, iVar) =>                    // for (int i = 0; i < length; i++)
                EmitDecodeAndStore(                                     //     result.Add(decode(data, remainingBytes))
                lil, locals, resultVar, iVar, doNotCheckBounds));
            il.Emit(OpCodes.Ldloc, resultVar);                          // stack_0 = result
            il.MarkLabel(endOfMethodLabel);                             // label endOfMethodLabel
        }
    }
}