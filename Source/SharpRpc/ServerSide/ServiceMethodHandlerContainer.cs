using System.Collections.Concurrent;
using SharpRpc.Interaction;

namespace SharpRpc.ServerSide
{
    public class ServiceMethodHandlerContainer : IServiceMethodHandlerContainer 
    {
        private readonly IServiceMethodHandlerFactory factory;
        private readonly ConcurrentDictionary<ServicePath, ServiceMethodHandler> handlers; 

        public ServiceMethodHandlerContainer(IServiceMethodHandlerFactory factory)
        {
            this.factory = factory;
            handlers = new ConcurrentDictionary<ServicePath, ServiceMethodHandler>();
        }

        public ServiceMethodHandler GetMethodHandler(ServiceImplementationInfo serviceImplementationInfo, ServicePath servicePath)
        {
            return handlers.GetOrAdd(servicePath, p => factory.CreateMethodHandler(serviceImplementationInfo, p));
        }
    }
}
