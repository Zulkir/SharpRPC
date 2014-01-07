#region License
/*
Copyright (c) 2013-2014 Daniil Rodin of Buhgalteria.Kontur team of SKB Kontur

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion

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