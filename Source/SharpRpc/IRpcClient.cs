namespace SharpRpc
{
    public interface IRpcClient
    {
        T GetService<T>(string scope) where T : class; 
    }
}
