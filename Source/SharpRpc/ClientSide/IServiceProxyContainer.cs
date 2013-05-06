namespace SharpRpc.ClientSide
{
    public interface IServiceProxyContainer
    {
        T GetProxy<T>(string scope) where T : class;
    }
}
