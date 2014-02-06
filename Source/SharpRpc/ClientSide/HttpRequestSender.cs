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

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using SharpRpc.Interaction;

namespace SharpRpc.ClientSide
{
    public class HttpRequestSender : IRequestSender
    {
        private readonly ConcurrentDictionary<int, Lazy<HttpClient>> httpClients;

        public HttpRequestSender()
        {
            httpClients = new ConcurrentDictionary<int, Lazy<HttpClient>>();
        }

        private const int NoTimeout = -1;

        public async Task<Response> SendAsync(string host, int port, Request request, int? timeoutMilliseconds)
        {
            var uri = string.Format("http://{0}:{1}/{2}?scope={3}", host, port, request.Path, request.ServiceScope);
            var content = new ByteArrayContent(request.Data ?? new byte[0]);
            //content.Headers.Add("scope", request.ServiceScope);

            var httpClient = httpClients.GetOrAdd(timeoutMilliseconds ?? NoTimeout, CreateLazyHttpClient).Value;

            var httpResponseMessage = await httpClient.PostAsync(uri, content);
            var responseData = await httpResponseMessage.Content.ReadAsByteArrayAsync();

            return new Response((ResponseStatus)httpResponseMessage.StatusCode, responseData);
        }

        private static Lazy<HttpClient> CreateLazyHttpClient(int timeoutMilliseconds)
        {
            return new Lazy<HttpClient>(() => CreateHttpClient(timeoutMilliseconds));
        } 

        private static HttpClient CreateHttpClient(int timeoutMilliseconds)
        {
            var client = new HttpClient();
            if (timeoutMilliseconds != NoTimeout)
                client.Timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
            return client;
        }
    }
}
