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

using SharpRpc.ClientSide;
using SharpRpc.Codecs;
using SharpRpc.Reflection;

namespace SharpRpc
{
    public class RpcClientComponentContainer : IRpcClientComponentContainer
    {
        private readonly IRpcClient client;
        private readonly RpcClientComponentOverrides overrides;

        private IMethodDescriptionBuilder methodDescriptionBuilder;
        private IServiceDescriptionBuilder serviceDescriptionBuilder;
        private ICodecContainer codecContainer;
        private IRequestSenderContainer requestSenderContainer;
        private IOutgoingMethodCallProcessor outgoingMethodCallProcessor;
        private IServiceProxyClassFactory serviceProxyClassFactory;
        private IServiceProxyContainer serviceProxyContainer;

        public RpcClientComponentContainer(IRpcClient client, RpcClientComponentOverrides overrides)
        {
            this.client = client;
            this.overrides = overrides;
        }

        public IRpcClient Client { get { return client; } }

        public IServiceDescriptionBuilder GetServiceDescriptionBuilder()
        {
            return serviceDescriptionBuilder ?? (serviceDescriptionBuilder = new ServiceDescriptionBuilder(new MethodDescriptionBuilder()));
        }

        public ICodecContainer GetCodecContainer()
        {
            return codecContainer ?? (codecContainer =
                                      overrides.CodecContainer != null
                                          ? overrides.CodecContainer(this)
                                          : new CodecContainer());
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
                                                       : new OutgoingMethodCallProcessor(client.Topology, GetRequestSenderContainer(), GetCodecContainer()));
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