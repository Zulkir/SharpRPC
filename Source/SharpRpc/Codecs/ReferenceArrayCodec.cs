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

namespace SharpRpc.Codecs
{
    public class ReferenceArrayCodec : IEmittingCodec
    {
        private readonly Type type;
        private readonly IEmittingCodec elementCodec;
        private readonly bool isStruct;

        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return null; } }

        public ReferenceArrayCodec(Type type, ICodecContainer codecContainer)
        {
            this.type = type;
            elementCodec = codecContainer.GetEmittingCodecFor(type);
            isStruct = type.IsValueType;
        }

        private void EmitLdelem(ILGenerator il)
        {
            if (isStruct)
            {
                il.Emit(OpCodes.Ldelema, type);
                il.Emit(OpCodes.Ldobj, type);
            }
            else
            {
                il.Emit(OpCodes.Ldelem_Ref);
            }
        }

        public void EmitCalculateSize(ILGenerator il, Action<ILGenerator> emitLoad)
        {
            var valueIsNullOrEmptyLabel = il.DefineLabel();
            var valueHasElementsLabel = il.DefineLabel();
            var loopStartLabel = il.DefineLabel();
            var loopConditionLabel = il.DefineLabel();
            var endOfMethodLabel = il.DefineLabel();

            var iVar = il.DeclareLocal(typeof(int));          // int i
            var sumVar = il.DeclareLocal(typeof(int));        // int sum
            var elemVar = il.DeclareLocal(type);              // Type elem

            emitLoad(il);                                     // if (!value)
            il.Emit(OpCodes.Brfalse, valueIsNullOrEmptyLabel);//     goto valueIsNullOrEmptyLabel

            emitLoad(il);                                     // if ((int)value.Length)
            il.Emit(OpCodes.Ldlen);                           //     goto valueHasElementsLabel
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Brtrue, valueHasElementsLabel);

            il.MarkLabel(valueIsNullOrEmptyLabel);            // label valueIsNullOrEmptyLabel
            il.Emit_Ldc_I4(sizeof(int));                      // stack_0 = sizeof(int)
            il.Emit(OpCodes.Br, endOfMethodLabel);            // goto endOfMethodLabel

            il.MarkLabel(valueHasElementsLabel);              // label valueHasElementsLabel
            il.Emit_Ldc_I4(0);                                // i = 0
            il.Emit(OpCodes.Stloc, iVar);
            il.Emit_Ldc_I4(0);                                // sum = 0
            il.Emit(OpCodes.Stloc, sumVar);
            il.Emit(OpCodes.Br, loopConditionLabel);          // goto loopConditionLabel

            il.MarkLabel(loopStartLabel);                     // label loopStartLabel
            il.Emit(OpCodes.Ldloc, sumVar);                   // stack_0 = sum
            emitLoad(il);                                     // elem = value[i]
            il.Emit(OpCodes.Ldloc, iVar);
            EmitLdelem(il);
            il.Emit(OpCodes.Stloc, elemVar);
            elementCodec.EmitCalculateSize(il, elemVar);      // stack_1 = CalculateSize(elem)
            il.Emit(OpCodes.Add);                             // sum = stack_0 + stack_1
            il.Emit(OpCodes.Stloc, sumVar);
            il.Emit(OpCodes.Ldloc, iVar);                     // i++
            il.Emit_Ldc_I4(1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, iVar);

            il.MarkLabel(loopConditionLabel);                 // label loopConditionLabel
            il.Emit(OpCodes.Ldloc, iVar);                     // if (i < (int)value.Length)
            emitLoad(il);                                     //     goto loopStartLabel
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Blt, loopStartLabel);

            il.Emit(OpCodes.Ldloc, sumVar);                   // stack_0 = sum
            il.MarkLabel(endOfMethodLabel);                   // label endOfMethodLabel
        }

        public void EmitEncode(ILGenerator il, ILocalVariableCollection locals, Action<ILGenerator> emitLoad)
        {
            var valueIsNotNullLabel = il.DefineLabel();
            var loopStartLabel = il.DefineLabel();
            var loopConditionLabel = il.DefineLabel();
            var endOfMethodLabel = il.DefineLabel();

            var iVar = il.DeclareLocal(typeof(int));                 // int i
            var elemVar = il.DeclareLocal(type);                     // Type elem

            emitLoad(il);                                            // if (value)
            il.Emit(OpCodes.Brtrue, valueIsNotNullLabel);            //     goto valueIsNotNullLabel

            // Value is null branch
            il.Emit(OpCodes.Ldloc, locals.DataPointer);              // *(int*) data = -1
            il.Emit_Ldc_I4(-1);
            il.Emit(OpCodes.Stind_I4);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));// data += sizeof(int)
            il.Emit(OpCodes.Br, endOfMethodLabel);                   // goto endOfMethodLabel

            // Value is not null branch
            il.MarkLabel(valueIsNotNullLabel);                       // label valueIsNotNullLabel
            il.Emit(OpCodes.Ldloc, locals.DataPointer);              // *(int*) data = (int)value.Length
            emitLoad(il);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Stind_I4);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));// data += sizeof(int)
            il.Emit_Ldc_I4(0);                                       // i = 0
            il.Emit(OpCodes.Stloc, iVar);
            il.Emit(OpCodes.Br, loopConditionLabel);                 // goto loopConditionLabel

            // loop start
            il.MarkLabel(loopStartLabel);                            // loopStartLabel
            emitLoad(il);                                            // elem = value[i]
            il.Emit(OpCodes.Ldloc, iVar);
            EmitLdelem(il);
            il.Emit(OpCodes.Stloc, elemVar);
            elementCodec.EmitEncode(il, locals, elemVar);            // encode(data, elem)
            il.Emit(OpCodes.Ldloc, iVar);                            // i++
            il.Emit_Ldc_I4(1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, iVar);

            // loop condition
            il.MarkLabel(loopConditionLabel);                        // label loopConditionLabel
            il.Emit(OpCodes.Ldloc, iVar);                            // if (i < (int)value.Length)
            emitLoad(il);                                            //     goto loopStartLabel
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Blt, loopStartLabel);

            il.MarkLabel(endOfMethodLabel);                          // endOfMethodLabel
        }

        public void EmitDecode(ILGenerator il, ILocalVariableCollection locals, bool doNotCheckBounds)
        {
            throw new NotImplementedException();
        }
    }
}