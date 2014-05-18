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
using System.Collections.Generic;
using SharpRpc.ClientSide.Proxy;
using SharpRpc.Logs;
using SharpRpc.ServerSide;
using SharpRpc.Reflection;
using SharpRpc.Settings;
using SharpRpc.Topology;

namespace SharpRpc
{
    public class RpcClientServer : IRpcClientServer
    {
        private readonly IReadOnlyDictionary<string, IServiceTopology> topology;
        private readonly TimeoutSettings defaultTimeout;
        private readonly IHostSettings settings;

        private readonly IServiceImplementationContainer serviceImplementationContainer;
        private readonly ILogger logger;
        private readonly IRequestReceiver requestReceiver;
        private readonly IProxyContainer proxyContainer;

        public RpcClientServer(ITopologyLoader topologyLoader, TimeoutSettings defaultTimeout, ISettingsLoader settingsLoader, RpcComponentOverrides componentOverrides = null)
        {
            topology = topologyLoader.Load();
            this.defaultTimeout = defaultTimeout;
            settings = settingsLoader.LoadHostSettings();
            var componentContainer = new RpcClientServerComponentContainer(this, componentOverrides ?? new RpcComponentOverrides());
            logger = componentContainer.GetLogger();
            serviceImplementationContainer = componentContainer.GetServiceImplementationContainer();
            requestReceiver = componentContainer.GetRequestReceiverContainer().GetReceiver(settings.EndPoint.Protocol);
            proxyContainer = componentContainer.GetIServiceProxyContainer();
        }

        public IReadOnlyDictionary<string, IServiceTopology> Topology { get { return topology; } }
        public TimeoutSettings DefaultTimeout { get { return defaultTimeout; } }
        public IHostSettings Settings { get { return settings; } }
        public ILogger Logger { get { return logger; }}

        public T GetService<T>(string scope, TimeoutSettings timeoutSettings) where T : class
        {
            var serviceName = typeof(T).GetServiceName();
            if (topology.GetEndPoint(serviceName, scope) == settings.EndPoint)
            {
                var implementation = serviceImplementationContainer.GetImplementation(serviceName, scope).Implementation;
                // todo: notready timeouts
                return (T)implementation;
            }
            return proxyContainer.GetProxy<T>(scope, timeoutSettings);
        }

        public void StartHost()
        {
            requestReceiver.Start(settings.EndPoint.Port, Environment.ProcessorCount);
        }

        public void StopHost()
        {
            requestReceiver.Stop();
        }

        public IEnumerable<string> GetInitializedScopesFor(string serviceName)
        {
            return serviceImplementationContainer.GetInitializedScopesFor(serviceName);
        }
    }
}
