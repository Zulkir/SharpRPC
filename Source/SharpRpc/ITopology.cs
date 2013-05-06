namespace SharpRpc
{
    public interface ITopology
    {
        bool TryGetEndPoint(string serviceName, string scope, out ServiceEndPoint endPoint);
    }
}
