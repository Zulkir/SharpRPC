namespace SharpRpc.Codecs
{
    public interface ICodec
    {
        bool HasFixedSize { get; }
        int FixedSize { get; } 
    }
}
