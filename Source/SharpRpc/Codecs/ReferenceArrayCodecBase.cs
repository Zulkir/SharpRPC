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
    public abstract class ReferenceArrayCodecBase : CollectionCodecBase
    {
        protected ReferenceArrayCodecBase(Type elementType, ICodecContainer codecContainer) 
            : base(elementType.MakeArrayType(), elementType, codecContainer)
        {
        }

        protected abstract void EmitLoadElement(ILGenerator il);

        protected override void EmitCreateCollection(ILGenerator il, LocalBuilder lengthVar)
        {
            il.Emit(OpCodes.Ldloc, lengthVar);
            il.Emit(OpCodes.Newarr, ElementType);
        }

        protected override void EmitLoadCount(ILGenerator il, Action<ILGenerator> emitLoad)
        {
            emitLoad(il);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
        }

        protected override void EmitEnumerateCollection(ILGenerator il, Action<ILGenerator> emitLoad, EnumerateLoopBody loopBody)
        {
            var lengthVar = il.DeclareLocal(typeof(int));
            emitLoad(il);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Stloc, lengthVar);
            il.EmitForLoop(lengthVar, (lil, iVar) => loopBody(() =>
                {
                    emitLoad(lil);
                    lil.Emit(OpCodes.Ldloc, iVar);
                    EmitLoadElement(lil);
                }));
        }
    }
}