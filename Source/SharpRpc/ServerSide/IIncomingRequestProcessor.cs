using SharpRpc.Interaction;

namespace SharpRpc.ServerSide
{
    public interface IIncomingRequestProcessor
    {
        Response Process(Request request);
    }
}