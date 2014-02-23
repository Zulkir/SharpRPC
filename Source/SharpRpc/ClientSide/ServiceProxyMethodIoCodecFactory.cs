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

namespace SharpRpc.ClientSide
{
    public class ServiceProxyMethodIoCodecFactory : IServiceProxyMethodIoCodecFactory
    {
        private const int MaxInlinableComplexity = 16;

        private readonly ICodecContainer codecContainer;

        public ServiceProxyMethodIoCodecFactory(ICodecContainer codecContainer)
        {
            this.codecContainer = codecContainer;
        }

        public IServiceProxyMethodGenericTypeParameterCodec CreateGenericTypeParameterCodec(GenericTypeParameterBuilder typeParameterBuilder)
        {
            return new ServiceProxyMethodGenericTypeParameterCodec(typeParameterBuilder, codecContainer.GetEmittingCodecFor(typeof(Type)));
        }

        public IServiceProxyMethodParameterCodec CreateParameterCodec(MethodParameterDescription substitutedDescription)
        {
            return CreateCodec(substitutedDescription.Type, CraeteParameterAccessor(substitutedDescription));
        }

        private static IServiceProxyMethodParameterAccessor CraeteParameterAccessor(MethodParameterDescription substitutedDescription)
        {
            int argIndex = substitutedDescription.Index + 1;
            switch (substitutedDescription.Way)
            {
                case MethodParameterWay.Val: return new ValServiceProxyMethodParameterAccessor(argIndex);
                case MethodParameterWay.Ref: return new RefServiceProxyMethodParameterAccessor(argIndex, substitutedDescription.Type);
                case MethodParameterWay.Out: return new OutServiceProxyMethodParameterAccessor(argIndex, substitutedDescription.Type);
                default: throw new ArgumentOutOfRangeException("way");
            }
        }

        public IServiceProxyMethodRetvalCodec CreateRetvalCodec(Type substitutedType)
        {
            return CreateCodec(substitutedType, new RetvalServiceProxyMethodParameterAccessor());
        }

        private IServiceProxyMethodIoCodec CreateCodec(Type type, IServiceProxyMethodParameterAccessor accessor)
        {
            if (type.ContainsGenericParameters)
                return new GenericServiceProxyMethodIoCodec(type, accessor);

            var emittingCodec = codecContainer.GetEmittingCodecFor(type);
            if (emittingCodec.CanBeInlined && emittingCodec.EncodingComplexity <= MaxInlinableComplexity)
                return new InliningServiceProxyMethodIoCodec(emittingCodec, accessor);

            return new ManualServiceProxyMethodIoCodec(type, accessor);
        }
    }
}