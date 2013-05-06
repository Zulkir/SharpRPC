namespace SharpRpc.ServerSide
{
    public delegate byte[] ServiceMethodHandler(IServiceImplementation serviceImplementation, byte[] data);
}