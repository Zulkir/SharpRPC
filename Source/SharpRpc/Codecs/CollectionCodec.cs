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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRpc.Codecs
{
    public class CollectionCodec : CollectionCodecBase
    {
        private readonly ConstructorInfo collectionConstructor;

        private readonly MethodInfo getCountMethod;
        private readonly MethodInfo addMethod;
        private readonly MethodInfo getEnumeratorMethod;
        private readonly MethodInfo moveNextMethod;
        private readonly MethodInfo getCurrentMethod;

        public CollectionCodec(Type type, Type elementType, ICodecContainer codecContainer)
            : base(type, elementType, codecContainer)
        {
            collectionConstructor = type.GetConstructor(Type.EmptyTypes);
            getCountMethod = typeof(ICollection<>).MakeGenericType(elementType).GetMethod("get_Count");
            addMethod = typeof(ICollection<>).MakeGenericType(elementType).GetMethod("Add");
            getEnumeratorMethod = typeof(IEnumerable<>).MakeGenericType(elementType).GetMethod("GetEnumerator");
            moveNextMethod = typeof(IEnumerator).GetMethod("MoveNext");
            getCurrentMethod = typeof(IEnumerator<>).MakeGenericType(elementType).GetMethod("get_Current");
        }

        protected override void EmitCreateCollection(ILGenerator il, LocalBuilder lengthVar)
        {
            il.Emit(OpCodes.Newobj, collectionConstructor);
        }

        protected override void EmitLoadCount(ILGenerator il, Action<ILGenerator> emitLoad)
        {
            emitLoad(il);
            il.Emit(OpCodes.Callvirt, getCountMethod);
        }

        protected override void EmitEnumerateCollection(ILGenerator il, Action<ILGenerator> emitLoad, EnumerateLoopBody emitLoopBody)
        {
            var loopStartLabel = il.DefineLabel();
            var loopConditionLabel = il.DefineLabel();

            var enumeratorVar = il.DeclareLocal(              // IEnumerator<T> enumerator
                typeof(IEnumerator<>).MakeGenericType(ElementType));

            emitLoad(il);                                     // enumerator = value.GetEnumerator()
            il.Emit(OpCodes.Callvirt, getEnumeratorMethod);
            il.Emit(OpCodes.Stloc, enumeratorVar);
            il.Emit(OpCodes.Br, loopConditionLabel);          // goto loopConditionLabel

            il.MarkLabel(loopStartLabel);                     // label loopStartLabel
            emitLoopBody(() =>
            {
                il.Emit(OpCodes.Ldloc, enumeratorVar);
                il.Emit(OpCodes.Callvirt, getCurrentMethod);
            });

            il.MarkLabel(loopConditionLabel);                 // label loopConditionLabel
            il.Emit(OpCodes.Ldloc, enumeratorVar);            // if (i < (int)value.Length)
            il.Emit(OpCodes.Callvirt, moveNextMethod);        //     goto loopStartLabel
            il.Emit(OpCodes.Brtrue, loopStartLabel);
        }

        protected override void EmitDecodeAndStore(ILGenerator il, ILocalVariableCollection locals, LocalBuilder collectionVar, LocalBuilder iVar, bool doNotCheckBounds)
        {
            il.Emit(OpCodes.Ldloc, collectionVar);
            ElementCodec.EmitDecode(il, locals, doNotCheckBounds);
            il.Emit(OpCodes.Callvirt, addMethod);
        }
    }
}
