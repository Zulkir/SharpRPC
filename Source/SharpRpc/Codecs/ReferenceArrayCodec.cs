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

            var iVar = il.DeclareLocal(typeof(int));
            var sumVar = il.DeclareLocal(typeof(int));
            var elemVar = il.DeclareLocal(type);

            emitLoad(il);
            il.Emit(OpCodes.Brfalse, valueIsNullOrEmptyLabel);

            emitLoad(il);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Brtrue, valueHasElementsLabel);

            il.MarkLabel(valueIsNullOrEmptyLabel);
            il.Emit_Ldc_I4(sizeof(int));
            il.Emit(OpCodes.Br, endOfMethodLabel);

            il.MarkLabel(valueHasElementsLabel);
            il.Emit_Ldc_I4(0);
            il.Emit(OpCodes.Stloc, iVar);
            il.Emit_Ldc_I4(0);
            il.Emit(OpCodes.Stloc, sumVar);
            il.Emit(OpCodes.Br, loopConditionLabel);

            il.MarkLabel(loopStartLabel);
            il.Emit(OpCodes.Ldloc, sumVar);
            emitLoad(il);
            il.Emit(OpCodes.Ldloc, iVar);
            EmitLdelem(il);
            il.Emit(OpCodes.Stloc, elemVar);
            elementCodec.EmitCalculateSize(il, elemVar);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, sumVar);
            il.Emit(OpCodes.Ldloc, iVar);
            il.Emit_Ldc_I4(1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, iVar);

            il.MarkLabel(loopConditionLabel);
            il.Emit(OpCodes.Ldloc, iVar);
            emitLoad(il);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Blt, loopStartLabel);

            il.Emit(OpCodes.Ldloc, sumVar);
        }

        public void EmitEncode(ILGenerator il, ILocalVariableCollection locals, Action<ILGenerator> emitLoad)
        {
            throw new NotImplementedException();
        }

        public void EmitDecode(ILGenerator il, ILocalVariableCollection locals, bool doNotCheckBounds)
        {
            throw new NotImplementedException();
        }
    }
}