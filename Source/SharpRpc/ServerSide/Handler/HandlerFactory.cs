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

using System.Collections.Generic;
using SharpRpc.Codecs;
using SharpRpc.Interaction;
using SharpRpc.Reflection;
using System.Linq;

namespace SharpRpc.ServerSide.Handler
{
    public class HandlerFactory : IHandlerFactory
    {
        private readonly ICodecContainer codecContainer;
        private readonly IRawHandlerFactory rawHandlerFactory;

        public HandlerFactory(ICodecContainer codecContainer, IRawHandlerFactory rawHandlerFactory)
        {
            this.codecContainer = codecContainer;
            this.rawHandlerFactory = rawHandlerFactory;
        }

        public IHandler CreateHandler(ServiceDescription rootDescription, ServicePath path)
        {
            var serviceDescriptionChain = CreateServiceDescriptionChain(rootDescription, path);
            var methodDescription = GetMethodDescription(serviceDescriptionChain, path);
            return methodDescription.GenericParameters.Any() 
                ? new GenericHandler(codecContainer, rawHandlerFactory, serviceDescriptionChain, methodDescription, path) 
                : rawHandlerFactory.CreateGenericClass(serviceDescriptionChain, methodDescription, path)(null);
        }

        private static List<ServiceDescription> CreateServiceDescriptionChain(ServiceDescription rootDescription, ServicePath path)
        {
            var chain = new List<ServiceDescription>(path.Length - 1);
            var currentDescription = rootDescription;
            chain.Add(currentDescription);
            for (int i = 1; i < path.Length - 1; i++)
            {
                if (!currentDescription.TryGetSubservice(path[i], out currentDescription))
                    throw new InvalidPathException();
                chain.Add(currentDescription);
            }
            return chain;
        }

        private static MethodDescription GetMethodDescription(IEnumerable<ServiceDescription> serviceDescriptionChain, ServicePath path)
        {
            MethodDescription methodDescription;
            if (!serviceDescriptionChain.Last().TryGetMethod(path.MethodName, out methodDescription))
                throw new InvalidPathException();
            return methodDescription;
        }
    }
}