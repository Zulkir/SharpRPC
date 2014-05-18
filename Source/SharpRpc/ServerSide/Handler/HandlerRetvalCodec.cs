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
using SharpRpc.Codecs;

namespace SharpRpc.ServerSide.Handler
{
    public class HandlerRetvalCodec
    {
        private readonly IEmittingContext emittingContext;
        private readonly IEmittingCodec codec;
        private readonly LocalBuilder local;

        public HandlerRetvalCodec(IEmittingContext emittingContext, Type type)
        {
            this.emittingContext = emittingContext;
            codec = new IndirectCodec(type);
            local = emittingContext.IL.DeclareLocal(type);
        }

        public void EmitStore()
        {
            emittingContext.IL.Stloc(local);
        }

        public void EmitCalculateSize()
        {
            codec.EmitCalculateSize(emittingContext, Loaders.Local(local));
        }

        public void EmitEncode()
        {
            codec.EmitEncode(emittingContext, Loaders.Local(local));
        }
    }
}