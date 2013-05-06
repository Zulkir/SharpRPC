namespace SharpRpc.ServerSide
{
    public interface IRequestReceiver
    {
        void Start(int port, int threads);
        void Stop();
    }
}
