namespace SharpRpc.Codecs
{
    public unsafe static class ManualCodecExtensions
    {
         public static byte[] EncodeSingle<T>(this IManualCodec<T> codec, T value)
         {
             var data = new byte[codec.CalculateSize(value)];
             fixed (byte* pData = data)
             {
                 var p = pData;
                 codec.Encode(ref p, value);
             }
             return data;
         }

        public static T DecodeSingle<T>(this IManualCodec<T> codec, byte[] data)
        {
            T result;
            fixed (byte* pData = data)
            {
                var p = pData;
                int remainingBytes = data.Length;
                result = codec.Decode(ref p, ref remainingBytes, false);
            }
            return result;
        }

        public static bool TryDecodeSingle<T>(this IManualCodec<T> codec, byte[] data, out T value)
        {
            try
            {
                value = codec.DecodeSingle(data);
                return true;
            }
            catch
            {
                value = default(T);
                return false;
            }
        }
    }
}