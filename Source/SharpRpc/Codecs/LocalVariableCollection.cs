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
    public class LocalVariableCollection : ILocalVariableCollection
    {
        readonly ILGenerator il;
        readonly Dictionary<string, LocalBuilder> variables;
        readonly LocalBuilder dataPointer;
        readonly LocalBuilder remainingBytes;

        public LocalVariableCollection(ILGenerator il, bool decode)
        {
            this.il = il;
            variables = new Dictionary<string, LocalBuilder>();
            dataPointer = il.DeclareLocal(typeof(byte*));
            remainingBytes = decode ? il.DeclareLocal(typeof(int)) : null;
        }

        public LocalBuilder DataPointer
        {
            get { return dataPointer; }
        }

        public LocalBuilder RemainingBytes
        {
            get
            {
                if (remainingBytes == null) 
                    throw new InvalidOperationException("Cannot access 'remaining bytes' variable inside encode method");
                return remainingBytes;
            }
        }

        public LocalBuilder GetOrAdd(string name, Func<ILGenerator, LocalBuilder> declareVariable)
        {
            LocalBuilder variable;
            if (!variables.TryGetValue(name, out variable))
            {
                variable = declareVariable(il);
                variables.Add(name, variable);
            }
            return variable;
        }
    }
}