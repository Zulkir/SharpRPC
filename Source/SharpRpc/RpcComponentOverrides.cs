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

using System;
using SharpRpc.ClientSide;
using SharpRpc.Codecs;
using SharpRpc.Logs;
using SharpRpc.Reflection;
using SharpRpc.ServerSide;
using SharpRpc.Settings;
using SharpRpc.Topology;

namespace SharpRpc
{
    public class RpcComponentOverrides
    {
        public Func<IRpcComponentContainer, ITopologyLoader> TopologyLoader { get; set; }
        public Func<IRpcComponentContainer, ISettingsLoader> SettingsLoader { get; set; }
        public Func<IRpcComponentContainer, ILogger> Logger { get; set; }
        public Func<IRpcComponentContainer, IServiceDescriptionBuilder> ServiceDescriptionBuilder { get; set; }
        public Func<IRpcComponentContainer, IMethodDescriptionBuilder> MethodDescriptionBuilder { get; set; }
        public Func<IRpcComponentContainer, IServiceImplementationContainer> ServiceImplementationContainer { get; set; }
        public Func<IRpcComponentContainer, ICodecContainer> CodecContainer { get; set; }
        public Func<IRpcComponentContainer, IServiceMethodHandlerFactory> ServiceMethodHandlerFactory { get; set; }
        public Func<IRpcComponentContainer, IServiceMethodHandlerContainer> ServiceMethodHandlerContainer { get; set; }
        public Func<IRpcComponentContainer, IIncomingRequestProcessor> IncomingRequestProcessor { get; set; }
        public Func<IRpcComponentContainer, IRequestReceiverContainer> RequestReceiverContainer { get; set; }
        public Func<IRpcComponentContainer, IRequestSenderContainer> RequestSenderContainer { get; set; }
        public Func<IRpcComponentContainer, IOutgoingMethodCallProcessor> OutgoingMethodCallProcessor { get; set; }
        public Func<IRpcComponentContainer, IServiceProxyClassFactory> ServiceProxyClassFactory { get; set; }
        public Func<IRpcComponentContainer, IServiceProxyContainer> ServiceProxyContainer { get; set; }
    }
}