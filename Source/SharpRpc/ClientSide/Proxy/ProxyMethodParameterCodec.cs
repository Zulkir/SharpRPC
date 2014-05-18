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
using SharpRpc.Codecs;
using SharpRpc.Reflection;
using SharpRpc.Utility;

namespace SharpRpc.ClientSide.Proxy
{
    public class ProxyMethodParameterCodec
    {
        private readonly Type type;
        private readonly MethodParameterWay way;
        private readonly int argIndex;
        private readonly IEmittingCodec codec;
        private readonly Action<MyILGenerator> emitLoad;

        public ProxyMethodParameterCodec(MethodParameterDescription description)
        {
            type = description.Type;
            way = description.Way;
            argIndex = description.Index + 1;
            codec = new IndirectCodec(type);
            emitLoad = description.Way == MethodParameterWay.Val
                ? Loaders.Argument(argIndex)
                : Loaders.ArgumentRef(argIndex, description.Type);
        }

        public bool IsRequestParameter { get { return way == MethodParameterWay.Val || way == MethodParameterWay.Ref; } }
        public bool IsResponseParameter { get { return way == MethodParameterWay.Ref || way == MethodParameterWay.Out; } }

        public void EmitCalculateSize(IEmittingContext emittingContext)
        {
            codec.EmitCalculateSize(emittingContext, emitLoad);
        }

        public void EmitEncode(IEmittingContext emittingContext)
        {
            codec.EmitEncode(emittingContext, emitLoad);
        }

        public void EmitDecodeAndStore(IEmittingContext emittingContext)
        {
            var il = emittingContext.IL;
            il.Ldarg(argIndex);
            codec.EmitDecode(emittingContext, false);
            if (type.IsValueType)
                il.Stobj(type);
            else
                il.Stind_Ref();
        }
    }
}