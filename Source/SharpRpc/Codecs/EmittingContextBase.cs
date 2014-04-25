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
using System.Collections.Generic;
using System.Reflection.Emit;
using SharpRpc.Utility;

namespace SharpRpc.Codecs
{
    public abstract class EmittingContextBase : IEmittingContext
    {
        private readonly MyILGenerator il;
        private readonly Dictionary<string, LocalBuilder> variables;
        private LocalBuilder dataPointerVar;
        private LocalBuilder remainingBytesVar;

        protected EmittingContextBase(MyILGenerator il)
        {
            this.il = il;
            variables = new Dictionary<string, LocalBuilder>();
        }

        public MyILGenerator IL { get { return il; } }
        public LocalBuilder DataPointerVar { get { return dataPointerVar ?? (dataPointerVar = il.DeclareLocal(typeof(byte*))); } }
        public LocalBuilder RemainingBytesVar { get { return remainingBytesVar ?? (remainingBytesVar = il.DeclareLocal(typeof(int))); } }

        public LocalBuilder GetSharedVariable(Type type, string name)
        {
            LocalBuilder variable;
            if (!variables.TryGetValue(name, out variable))
            {
                variable = il.DeclareLocal(type);
                variables.Add(name, variable);
            }
            return variable;
        }

        public abstract void EmitLoadManualCodecFor(Type type);
    }
}