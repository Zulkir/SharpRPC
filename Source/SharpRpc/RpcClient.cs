
#region License
/*
Copyright (c) 2013 Daniil Rodin, Maxim Sannikov of Buhgalteria.Kontur team of SKB Kontur

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
using SharpRpc.Reflection;
using SharpRpc.Topology;

namespace SharpRpc
{
    public class RpcClient : IRpcClient
    {
        private readonly ITopology topology;
        private readonly IServiceProxyContainer serviceProxyContainer;

        public RpcClient(ITopologyLoader topologyLoader, RpcClientComponentOverrides componentOverrides = null)
        {
            topology = topologyLoader.Load();
            var componentContainer = new RpcClientComponentContainer(this, componentOverrides ?? new RpcClientComponentOverrides());
            serviceProxyContainer = componentContainer.GetIServiceProxyContainer();
        }

        public ITopology Topology { get { return topology; } }

        public T GetService<T>(string scope) where T : class
        {
            var serviceName = typeof(T).GetServiceName();

            ServiceEndPoint serviceEndPoint;
            if (!topology.TryGetEndPoint(serviceName, scope, out serviceEndPoint))
                throw new ServiceTopologyException(string.Format(
                    "Service '{0}' with scope '{1}' was not found in the topology", serviceName, scope));

            return serviceProxyContainer.GetProxy<T>(scope);
        }
    }
}