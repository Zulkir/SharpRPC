namespace SharpRpc.ServerSide
{
    public interface IRequestReceiverContainer
    {
        IRequestReceiver GetReceiver(string protocol);
    }
}