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
using System.Collections.Generic;
using System.Threading.Tasks;
using SharpRpc.Codecs;
using SharpRpc.Interaction;
using SharpRpc.Reflection;

namespace SharpRpc.ServerSide.Handler
{
    public class GenericHandler : IHandler
    {
        private readonly ConcurrentDictionary<TypesKey, IHandler> rawHandlers;
        private readonly Func<Type[], IHandler> createRawHandler;
        private readonly int genericParameterCount;
        private readonly IManualCodec<Type> typeCodec;

        public GenericHandler(ICodecContainer codecContainer, IRawHandlerFactory delegateFactory, IReadOnlyList<ServiceDescription> serviceDescriptionChain, MethodDescription methodDescription, ServicePath servicePath)
        {
            rawHandlers = new ConcurrentDictionary<TypesKey, IHandler>();
            createRawHandler = delegateFactory.CreateGenericClass(serviceDescriptionChain, methodDescription, servicePath);
            genericParameterCount = methodDescription.GenericParameters.Count;
            typeCodec = codecContainer.GetManualCodecFor<Type>();
        }

        public async Task<byte[]> Handle(object serviceImplementation, byte[] data, int offset)
        {
            var genericArguments = DecodeGenericArguments(data, ref offset);
            var genericArgumentsKey = new TypesKey(genericArguments);
            var rawHandler = rawHandlers.GetOrAdd(genericArgumentsKey, k => createRawHandler(k.Types));

            return await rawHandler.Handle(serviceImplementation, data, offset);
        }

        private unsafe Type[] DecodeGenericArguments(byte[] data, ref int offset)
        {
            var genericArguments = new Type[genericParameterCount];
            int remainingBytes = data.Length;
            fixed (byte* pData = data)
            {
                var p = pData + offset;
                for (int i = 0; i < genericArguments.Length; i++)
                    genericArguments[i] = typeCodec.Decode(ref p, ref remainingBytes, false);
            }
            offset = data.Length - remainingBytes;
            return genericArguments;
        }
    }
}
