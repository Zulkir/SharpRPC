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

using System.IO;

namespace SharpRpc.Utility
{
    public static class StreamExtensions
    {
         public static byte[] ReadToEnd(this Stream stream)
         {
             var buffer = new byte[16 * 1024];
             using (var memoryStream = new MemoryStream())
             {
                 int bytesRead;
                 while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                 {
                     memoryStream.Write(buffer, 0, bytesRead);
                 }
                 return memoryStream.ToArray();
             }
         }

         public static byte[] ReadToEnd(this Stream stream, long bytesToRead)
         {
             var data = new byte[bytesToRead];
             int offset = 0;
             while (offset < data.Length)
             {
                 int read = stream.Read(data, offset, data.Length - offset);
                 if (read == 0)
                     throw new InvalidDataException("Unexpected end of response stream");
                 offset += read;
             }
             return data;
         }
    }
}