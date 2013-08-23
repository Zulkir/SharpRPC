#region License
/*
Copyright (c) 2013 Daniil Rodin of Buhgalteria.Kontur team of SKB Kontur

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

using System;
using System.IO;
using System.Net;
using SharpRpc.Interaction;
using System.Linq;

namespace SharpRpc.ClientSide
{
    public class HttpRequestSender : IRequestSender
    {
        public string Protocol { get { return "http"; } }

        public Response Send(string host, int port, Request request, int? timeoutMilliseconds)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("Host name cannot be null, emtry, or whitespace");

            var httpWebRequest = WebRequest.CreateHttp(string.Format("http://{0}:{1}/{2}", host, port, request.Path));

            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/octet-stream";
            if (!string.IsNullOrEmpty(request.ServiceScope))
                httpWebRequest.Headers["scope"] = request.ServiceScope;

            var requestData = request.Data ?? new byte[0];
            using (var stream = httpWebRequest.GetRequestStream())
                stream.Write(requestData, 0, requestData.Length);
            httpWebRequest.Timeout = timeoutMilliseconds.HasValue ? timeoutMilliseconds.Value : -1;

            Response response;
            using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                int dataLength;
                if (!httpWebResponse.Headers.AllKeys.Contains("data-length") || !int.TryParse(httpWebResponse.Headers["data-length"], out dataLength))
                    dataLength = 0;

                var responseData = new byte[dataLength];
                using (var stream = httpWebResponse.GetResponseStream())
                {
                    if (stream != null)
                    {
                        int offset = 0;
                        while (offset < dataLength)
                        {
                            int read = stream.Read(responseData, offset, responseData.Length - offset);
                            if (read == 0)
                                throw new InvalidDataException("Unexpected end of response stream");
                            offset += read;
                        }   
                    }
                }
                int status;
                if (!httpWebResponse.Headers.AllKeys.Contains("status") || !int.TryParse(httpWebResponse.Headers["status"], out status))
                    status = (int)ResponseStatus.Unknown;
                response = new Response((ResponseStatus)status, responseData);
            }
            return response;
        }
    }
}
