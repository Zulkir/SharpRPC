using SharpRpc.Interaction;

namespace SharpRpc.ClientSide
{
    public interface IRequestSender
    {
        string Protocol { get; }
        Response Send(string host, int port, Request request);
    }
}
