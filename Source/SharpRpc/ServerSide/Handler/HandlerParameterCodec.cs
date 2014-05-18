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
using SharpRpc.Reflection;

namespace SharpRpc.ServerSide.Handler
{
    public class HandlerParameterCodec
    {
        private readonly IEmittingContext emittingContext;
        private readonly Type type;
        private readonly MethodParameterWay way;
        private readonly IEmittingCodec codec;
        private readonly LocalBuilder local;

        public HandlerParameterCodec(IEmittingContext emittingContext, MethodParameterDescription description)
        {
            this.emittingContext = emittingContext;
            type = description.Type;
            way = description.Way;
            codec = new IndirectCodec(type);
            if (IsResponseParameter)
                local = emittingContext.IL.DeclareLocal(type);
        }

        public bool IsRequestParameter { get { return way == MethodParameterWay.Val || way == MethodParameterWay.Ref; } }
        public bool IsResponseParameter { get { return way == MethodParameterWay.Ref || way == MethodParameterWay.Out; } }

        public void EmitCalculateSize()
        {
            codec.EmitCalculateSize(emittingContext, Loaders.Local(local));
        }

        public void EmitEncode()
        {
            codec.EmitEncode(emittingContext, Loaders.Local(local));
        }

        public void EmitDecodeAndPrepare()
        {
            var il = emittingContext.IL;
            switch (way)
            {
                case MethodParameterWay.Val:
                    codec.EmitDecode(emittingContext, false);
                    break;
                case MethodParameterWay.Ref:
                    codec.EmitDecode(emittingContext, false);
                    il.Stloc(local);
                    il.Ldloca(local);
                    break;
                case MethodParameterWay.Out:
                    il.Ldloca(local);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}