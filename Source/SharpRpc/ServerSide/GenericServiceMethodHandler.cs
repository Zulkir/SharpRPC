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
using System.Collections.Concurrent;
using System.Threading.Tasks;
using SharpRpc.Codecs;
using SharpRpc.Interaction;
using SharpRpc.Reflection;

namespace SharpRpc.ServerSide
{
    public class GenericServiceMethodHandler : IServiceMethodHandler
    {
        private readonly ICodecContainer codecContainer;
        private readonly IServiceMethodDelegateFactory delegateFactory;
        private readonly ServiceDescription serviceDescription;
        private readonly ServicePath servicePath;
        private readonly int genericParameterCount;
        private readonly IManualCodec<Type> typeCodec;
        private readonly ConcurrentDictionary<TypesKey, ServiceMethodDelegate> methodDelegates;

        public GenericServiceMethodHandler(ICodecContainer codecContainer, IServiceMethodDelegateFactory delegateFactory, ServiceDescription serviceDescription, ServicePath servicePath, int genericParameterCount)
        {
            this.codecContainer = codecContainer;
            this.delegateFactory = delegateFactory;
            this.serviceDescription = serviceDescription;
            this.servicePath = servicePath;
            this.genericParameterCount = genericParameterCount;
            typeCodec = codecContainer.GetManualCodecFor<Type>();
            methodDelegates = new ConcurrentDictionary<TypesKey, ServiceMethodDelegate>();
        }

        public async Task<byte[]> Handle(object serviceImplementation, byte[] data)
        {
            int offset;
            var genericArguments = DecodeGenericArguments(data, out offset);
            var genericArgumentsKey = new TypesKey(genericArguments);
            var methodDelegate = methodDelegates.GetOrAdd(genericArgumentsKey,
                    k => delegateFactory.CreateMethodDelegate(codecContainer, serviceDescription, servicePath, k.Types));

            return await methodDelegate(codecContainer, serviceImplementation, data, offset);
        }

        private unsafe Type[] DecodeGenericArguments(byte[] data, out int offset)
        {
            var genericArguments = new Type[genericParameterCount];
            int remainingBytes = data.Length;
            fixed (byte* pData = data)
            {
                var p = pData;
                for (int i = 0; i < genericArguments.Length; i++)
                    genericArguments[i] = typeCodec.Decode(ref p, ref remainingBytes, false);
            }
            offset = data.Length - remainingBytes;
            return genericArguments;
        }
    }
}
