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

using SharpRpc.Logs;
using SharpRpc.ServerSide;

namespace SharpRpc
{
    public class RpcClientServerComponentContainer : RpcClientComponentContainer, IRpcClientServerComponentContainer
    {
        private readonly IRpcClientServer clientServer;
        private readonly RpcComponentOverrides overrides;

        private ILogger logger;
        private IServiceImplementationContainer serviceImplementationContainer;
        private IServiceImplementationFactory serviceImplementationFactory;
        private IServiceMethodDelegateFactory serviceMethodDelegateFactory;
        private IServiceMethodHandlerFactory serviceMethodHandlerFactory;
        private IServiceMethodHandlerContainer serviceMethodHandlerContainer;
        private IIncomingRequestProcessor incomingRequestProcessor;
        private IRequestReceiverContainer requestReceiverContainer;

        public RpcClientServerComponentContainer(IRpcClientServer clientServer, RpcComponentOverrides overrides) 
            : base(clientServer, overrides)
        {
            this.clientServer = clientServer;
            this.overrides = overrides;
        }

        public IRpcClientServer ClientServer { get { return clientServer; } }

        public ILogger GetLogger()
        {
            return logger ?? (logger = overrides.Logger != null
                ? overrides.Logger(this)
                : new ConsoleLogger());
        }

        public IServiceImplementationContainer GetServiceImplementationContainer()
        {
            return serviceImplementationContainer ?? (serviceImplementationContainer =
                                                      overrides.ServiceImplementationContainer != null
                                                          ? overrides.ServiceImplementationContainer(this)
                                                          : new ServiceImplementationContainer(GetServiceImplementationFactory()));
        }
        

        public IServiceImplementationFactory GetServiceImplementationFactory()
        {
            return serviceImplementationFactory ?? (serviceImplementationFactory =
                                                      overrides.ServiceImplementationFactory != null
                                                          ? overrides.ServiceImplementationFactory(this)
                                                          : new ServiceImplementationFactory(GetServiceDescriptionBuilder(), ClientServer, ClientServer.Settings.GetInterfaceImplementationsPairs()));
        }
        
        public IServiceMethodDelegateFactory GetServiceMethodDelegateFactory()
        {
            return serviceMethodDelegateFactory ?? (serviceMethodDelegateFactory =
                                                    overrides.ServiceMethodDelegateFactory != null
                                                        ? overrides.ServiceMethodDelegateFactory(this)
                                                        : new ServiceMethodDelegateFactory());
        }

        public IServiceMethodHandlerFactory GetServiceMethodHandlerFactory()
        {
            return serviceMethodHandlerFactory ?? (serviceMethodHandlerFactory =
                                                   overrides.ServiceMethodHandlerFactory != null
                                                       ? overrides.ServiceMethodHandlerFactory(this)
                                                       : new ServiceMethodHandlerFactory(GetCodecContainer(), GetServiceMethodDelegateFactory()));
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
                                                    : new IncomingRequestProcessor(GetLogger(), GetServiceImplementationContainer(), 
                                                                                   GetServiceMethodHandlerContainer(), GetCodecContainer()));
        }

        public IRequestReceiverContainer GetRequestReceiverContainer()
        {
            return requestReceiverContainer ?? (requestReceiverContainer =
                                                overrides.RequestReceiverContainer != null
                                                    ? overrides.RequestReceiverContainer(this)
                                                    : new RequestReceiverContainer(GetIncomingRequestProcessor(), GetLogger()));
        }
    }
}