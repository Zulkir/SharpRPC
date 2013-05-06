namespace SharpRpc.Codecs
{
    public interface IManualCodec : ICodec
    {
        
    }

    public unsafe interface IManualCodec<T> : IManualCodec
    {
        int CalculateSize(T value);
        void Encode(ref byte* data, T value);
        T Decode(ref byte* data, ref int remainingBytes, bool doNotCheckBounds);
    }
}
