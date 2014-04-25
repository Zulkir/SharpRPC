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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRpc.Utility
{
    public class ForeachLoopEmitter : IForeachLoopEmitter
    {
        private readonly MyILGenerator il;
        private readonly Action<MyILGenerator> emitLoadCollection;

        private readonly Type enumerableType;
        private readonly Type enumeratorType;

        private Label loopStartLabel;
        private Label loopConditionLabel;
        private LocalBuilder enumeratorVar;
        
        public ForeachLoopEmitter(MyILGenerator il, Type elementType, Action<MyILGenerator> emitLoadCollection)
        {
            this.il = il;
            this.emitLoadCollection = emitLoadCollection;
            enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
            enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);
            EmitLoopBeginning();
        }

        private MethodInfo GetEnumeratorMethod { get { return enumerableType.GetMethod("GetEnumerator"); } }
        private MethodInfo GetCurrentMethod { get { return enumeratorType.GetMethod("get_Current"); } }
        private static MethodInfo MoveNextMethod { get { return typeof(IEnumerator).GetMethod("MoveNext"); } }

        public void Dispose()
        {
            EmitLoopEnd();
        }

        public void LoadCurrent()
        {
            il.Ldloc(enumeratorVar);
            il.Callvirt(GetCurrentMethod);
        }

        private void EmitLoopBeginning()
        {
            loopStartLabel = il.DefineLabel();
            loopConditionLabel = il.DefineLabel();
            enumeratorVar = il.DeclareLocal(enumeratorType);    // IEnumerator<T> enumerator
                
            emitLoadCollection(il);                             // enumerator = value.GetEnumerator()
            il.Callvirt(GetEnumeratorMethod);
            il.Stloc(enumeratorVar);
            il.Br(loopConditionLabel);                          // goto loopConditionLabel
            il.MarkLabel(loopStartLabel);                       // label loopStartLabel
        }

        private void EmitLoopEnd()
        {
            il.MarkLabel(loopConditionLabel);                   // label loopConditionLabel
            il.Ldloc(enumeratorVar);                            // if (i < (int)value.Length)
            il.Callvirt(MoveNextMethod);                        //     goto loopStartLabel
            il.Brtrue(loopStartLabel);
        }
    }
}