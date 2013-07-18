#region License
/*
Copyright (c) 2013 Daniil Rodin of Buhgalteria.Kontur team of SKB Kontur

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

using SharpRpc.ClientSide;
using SharpRpc.Codecs;
using SharpRpc.Logs;
using SharpRpc.Reflection;
using SharpRpc.ServerSide;

namespace SharpRpc
{
    public class RpcComponentContainer : IRpcComponentContainer
    {
        private readonly IRpcKernel kernel;
        private readonly RpcComponentOverrides overrides;

        private ILogger logger;
        private IMethodDescriptionBuilder methodDescriptionBuilder;
        private IServiceDescriptionBuilder serviceDescriptionBuilder;
        private IServiceImplementationContainer serviceImplementationContainer;
        private ICodecContainer codecContainer;
        private IServiceMethodHandlerFactory serviceMethodHandlerFactory;
        private IServiceMethodHandlerContainer serviceMethodHandlerContainer;
        private IIncomingRequestProcessor incomingRequestProcessor;
        private IRequestReceiverContainer requestReceiverContainer;
        private IRequestSenderContainer requestSenderContainer;
        private IOutgoingMethodCallProcessor outgoingMethodCallProcessor;
        private IServiceProxyClassFactory serviceProxyClassFactory;
        private IServiceProxyContainer serviceProxyContainer;

        public RpcComponentContainer(IRpcKernel kernel, RpcComponentOverrides overrides)
        {
            this.kernel = kernel;
            this.overrides = overrides;
        }

        public IRpcKernel Kernel { get { return kernel; } }

        public ILogger GetLogger()
        {
            return logger ?? (logger = overrides.Logger != null
                ? overrides.Logger(this)
                : new ConsoleLogger());
        }

        public IMethodDescriptionBuilder GetMethodDescriptionBuilder()
        {
            return methodDescriptionBuilder ?? (methodDescriptionBuilder =
                                                (overrides.MethodDescriptionBuilder != null 
                                                     ? overrides.MethodDescriptionBuilder(this)
                                                     : new MethodDescriptionBuilder()));
        }

        public IServiceDescriptionBuilder GetServiceDescriptionBuilder()
        {
            return serviceDescriptionBuilder ?? (serviceDescriptionBuilder = 
                                                 overrides.ServiceDescriptionBuilder != null
                                                     ? overrides.ServiceDescriptionBuilder(this) 
                                                     : new ServiceDescriptionBuilder(GetMethodDescriptionBuilder()));
        }

        public IServiceImplementationContainer GetServiceImplementationContainer()
        {
            return serviceImplementationContainer ?? (serviceImplementationContainer =
                                                      overrides.ServiceImplementationContainer != null
                                                          ? overrides.ServiceImplementationContainer(this)
                                                          : new ServiceImplementationContainer(GetServiceDescriptionBuilder()));
        }

        public ICodecContainer GetCodecContainer()
        {
            return codecContainer ?? (codecContainer =
                                      overrides.CodecContainer != null
                                          ? overrides.CodecContainer(this)
                                          : new CodecContainer());
        }

        public IServiceMethodHandlerFactory GetServiceMethodHandlerFactory()
        {
            return serviceMethodHandlerFactory ?? (serviceMethodHandlerFactory =
                                                   overrides.ServiceMethodHandlerFactory != null
                                                       ? overrides.ServiceMethodHandlerFactory(this)
                                                       : new ServiceMethodHandlerFactory(GetCodecContainer()));
        }

        public IServiceMethodHandlerContainer GetServiceMethodHandlerContainer()
        {
            return serviceMethodHandlerContainer ?? (serviceMethodHandlerContainer =
                                                     overrides.ServiceMethodHandlerContainer != null
                                                         ? overrides.ServiceMethodHandlerContainer(this)
                                                         : new ServiceMethodHandlerContainer(GetServiceMethodHandlerFactory()));
        }

        public IIncomingRequestProcessor GetIncomingRequestProcessor()
        {
            return incomingRequestProcessor ?? (incomingRequestProcessor =
                                                overrides.IncomingRequestProcessor != null
                                                    ? overrides.IncomingRequestProcessor(this)
                                                    : new IncomingRequestProcessor(kernel, GetServiceImplementationContainer(), 
                                                                                   GetServiceMethodHandlerContainer(), GetCodecContainer()));
        }

        public IRequestReceiverContainer GetRequestReceiverContainer()
        {
            return requestReceiverContainer ?? (requestReceiverContainer =
                                                overrides.RequestReceiverContainer != null
                                                    ? overrides.RequestReceiverContainer(this)
                                                    : new RequestReceiverContainer(GetIncomingRequestProcessor()));
        }

        public IRequestSenderContainer GetRequestSenderContainer()
        {
            return requestSenderContainer ?? (requestSenderContainer =
                                              overrides.RequestSenderContainer != null
                                                  ? overrides.RequestSenderContainer(this)
                                                  : new RequestSenderContainer());
        }

        public IOutgoingMethodCallProcessor GetOutgoingMethodCallProcessor()
        {
            return outgoingMethodCallProcessor ?? (outgoingMethodCallProcessor =
                                                   overrides.OutgoingMethodCallProcessor != null
                                                       ? overrides.OutgoingMethodCallProcessor(this)
                                                       : new OutgoingMethodCallProcessor(kernel.Topology, GetRequestSenderContainer(), GetCodecContainer()));
        }

        public IServiceProxyClassFactory GetServiceProxyClassFactory()
        {
            return serviceProxyClassFactory ?? (serviceProxyClassFactory =
                                                overrides.ServiceProxyClassFactory != null
                                                    ? overrides.ServiceProxyClassFactory(this)
                                                    : new ServiceProxyClassFactory(GetServiceDescriptionBuilder(), GetCodecContainer()));
        }

        public IServiceProxyContainer GetIServiceProxyContainer()
        {
            return serviceProxyContainer ?? (serviceProxyContainer =
                                             overrides.ServiceProxyContainer != null
                                                 ? overrides.ServiceProxyContainer(this)
                                                 : new ServiceProxyContainer(GetOutgoingMethodCallProcessor(), GetServiceProxyClassFactory()));
        }
    }
}