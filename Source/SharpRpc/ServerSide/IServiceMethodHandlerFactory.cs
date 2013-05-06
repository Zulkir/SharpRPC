using SharpRpc.Interaction;

namespace SharpRpc.ServerSide
{
    public interface IServiceMethodHandlerFactory
    {
        ServiceMethodHandler CreateMethodHandler(ServiceImplementationInfo serviceImplementationInfo, ServicePath servicePath); 
    }
}
