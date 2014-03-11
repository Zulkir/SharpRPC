﻿#region License
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
using SharpRpc.Codecs;

namespace SharpRpc.ClientSide
{
    public class ValServiceProxyMethodParameterAccessor : IServiceProxyMethodParameterAccessor
    {
        private readonly int argIndex;

        public ValServiceProxyMethodParameterAccessor(int argIndex)
        {
            this.argIndex = argIndex;
        }

        public void EmitLoad(ILGenerator il)
        {
            il.Emit_Ldarg(argIndex);
        }

        public void EmitBeginStore(ILGenerator il)
        {
            throw new InvalidOperationException("Trying to emit Store for a Val parameter");
        }

        public void EmitEndStore(ILGenerator il)
        {
            throw new InvalidOperationException("Trying to emit Store for a Val parameter");
        }
    }
}