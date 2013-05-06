using SharpRpc.Interaction;

namespace SharpRpc.ServerSide
{
    public interface IServiceMethodHandlerContainer
    {
        ServiceMethodHandler GetMethodHandler(ServiceImplementationInfo serviceImplementationInfo, ServicePath servicePath);
    }
}
