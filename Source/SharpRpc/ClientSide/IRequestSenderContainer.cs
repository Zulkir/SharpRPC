namespace SharpRpc.ClientSide
{
    public interface IRequestSenderContainer
    {
        IRequestSender GetSender(string protocol);
    }
}
