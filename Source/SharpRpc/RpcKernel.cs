using System;
using System.Collections.Generic;
using SharpRpc.ClientSide;
using SharpRpc.Codecs;
using SharpRpc.ServerSide;
using SharpRpc.Reflection;

namespace SharpRpc
{
    public class RpcKernel : IRpcKernel
    {
        private readonly ITopology topology;
        private readonly IServiceHostSettings serviceHostSettings;
        
        private readonly IServiceImplementationContainer serviceImplementationContainer;
        private readonly IRequestReceiver requestReceiver;
        private readonly IServiceProxyContainer serviceProxyContainer;

        public RpcKernel(ITopology topology, IServiceHostSettings serviceHostSettings)
        {
            this.topology = topology;
            this.serviceHostSettings = serviceHostSettings;
            var serviceDescriptionBuilder = new ServiceDescriptionBuilder(new MethodDescriptionBuilder());
            serviceImplementationContainer = new ServiceImplementationContainer(serviceDescriptionBuilder);
            foreach (var pair in serviceHostSettings.GetInterfaceImplementationsPairs())
                serviceImplementationContainer.RegisterImplementation(pair.Interface, pair.ImplementationType);
            var codecContainer = new CodecContainer();
            var serviceMethodHandlerFactory = new ServiceMethodHandlerFactory(codecContainer);
            var serviceMethodHandlerContainer = new ServiceMethodHandlerContainer(serviceMethodHandlerFactory);
            var incomingRequestProcessor = new IncomingRequestProcessor(serviceImplementationContainer, serviceMethodHandlerContainer, codecContainer);
            var requestReceiverContainer = new RequestReceiverContainer(incomingRequestProcessor);
            requestReceiver = requestReceiverContainer.GetReceiver(serviceHostSettings.EndPoint.Protocol);
            var requestSenderContainer = new RequestSenderContainer();
            var outgoingMethodCallProcessor = new OutgoingMethodCallProcessor(topology, requestSenderContainer, codecContainer);
            var serviceProxyClassFactory = new ServiceProxyClassFactory(serviceDescriptionBuilder, codecContainer);
            serviceProxyContainer = new ServiceProxyContainer(outgoingMethodCallProcessor, serviceProxyClassFactory);
        }

        public RpcKernel(ITopology topology, IServiceHostSettings serviceHostSettings, 
            IServiceImplementationContainer serviceImplementationContainer,
            IRequestReceiver requestReceiver,
            IServiceProxyContainer serviceProxyContainer)
        {
            this.topology = topology;
            this.serviceHostSettings = serviceHostSettings;
            this.serviceImplementationContainer = serviceImplementationContainer;
            foreach (var pair in serviceHostSettings.GetInterfaceImplementationsPairs())
                serviceImplementationContainer.RegisterImplementation(pair.Interface, pair.ImplementationType);
            this.requestReceiver = requestReceiver;
            this.serviceProxyContainer = serviceProxyContainer;
        }

        public T GetService<T>(string scope) where T : class
        {
            var serviceName = typeof(T).GetServiceName();

            ServiceEndPoint serviceEndPoint;
            if (!topology.TryGetEndPoint(serviceName, scope, out serviceEndPoint))
                throw new ServiceTopologyException(string.Format(
                    "Service '{0}' with scope '{1}' was not found in the topology", serviceName, scope));

            if (serviceEndPoint == serviceHostSettings.EndPoint)
                return (T)serviceImplementationContainer.GetImplementation(serviceName, scope).Implementation;

            return serviceProxyContainer.GetProxy<T>(scope);
        }

        public void StartHost()
        {
            requestReceiver.Start(serviceHostSettings.EndPoint.Port, Environment.ProcessorCount);
        }

        public void StopHost()
        {
            requestReceiver.Stop();
        }

        public IEnumerable<string> GetInitializedScopesFor<T>()
        {
            var serviceName = typeof(T).GetServiceName();
            return serviceImplementationContainer.GetInitializedScopesFor(serviceName);
        }
    }
}
