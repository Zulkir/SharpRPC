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

using System.Reflection.Emit;

namespace SharpRpc.Codecs
{
    public class ForLoopEmitter : IForLoopEmittingContext
    {
        private readonly ILGenerator il;
        private readonly LocalBuilder lengthVar;
        private LocalBuilder iVar;
        private Label loopStartLabel;
        private Label loopConditionLabel;

        public ForLoopEmitter(ILGenerator il, LocalBuilder lengthVar)
        {
            this.il = il;
            this.lengthVar = lengthVar;
            EmitLoopBeginning();
        }

        public void Dispose()
        {
            EmitLoopEnd();
        }

        public void LoadIndex()
        {
            il.Emit(OpCodes.Ldloc, iVar);
        }

        private void EmitLoopBeginning()
        {
            loopStartLabel = il.DefineLabel();
            loopConditionLabel = il.DefineLabel();

            iVar = il.DeclareLocal(typeof(int));        // int i

            il.Emit_Ldc_I4(0);                          // i = 0
            il.Emit(OpCodes.Stloc, iVar);
            il.Emit(OpCodes.Br, loopConditionLabel);    // goto loopConditionLabel

            il.MarkLabel(loopStartLabel);               // label loopStartLabel
        }

        private void EmitLoopEnd()
        {
            il.Emit(OpCodes.Ldloc, iVar);               // i++
            il.Emit_Ldc_I4(1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, iVar);

            il.MarkLabel(loopConditionLabel);           // label loopConditionLabel
            il.Emit(OpCodes.Ldloc, iVar);               // if (i < (int)value.Length)
            il.Emit(OpCodes.Ldloc, lengthVar);          //     goto loopStartLabel
            il.Emit(OpCodes.Blt, loopStartLabel);
        }
    }
}