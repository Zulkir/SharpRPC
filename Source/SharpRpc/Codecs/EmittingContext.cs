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

namespace SharpRpc.Codecs
{
    public class EmittingContext : IEmittingContext
    {
        private readonly ILGenerator il;
        private readonly LocalBuilder dataPointerVar;
        private readonly LocalBuilder remainingBytesVar;
        private readonly Dictionary<string, LocalBuilder> variables;

        public EmittingContext(ILGenerator il, bool decode)
        {
            this.il = il;
            dataPointerVar = il.DeclareLocal(typeof(byte*));
            remainingBytesVar = decode ? il.DeclareLocal(typeof(int)) : null;
            variables = new Dictionary<string, LocalBuilder>();
        }

        public ILGenerator IL { get { return il; } }
        public LocalBuilder DataPointerVar { get { return dataPointerVar; } }
        
        public LocalBuilder RemainingBytesVar
        {
            get
            {
                if (remainingBytesVar == null) 
                    throw new InvalidOperationException("Cannot access 'remaining bytes' variable inside encode method");
                return remainingBytesVar;
            }
        }

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
    }
}