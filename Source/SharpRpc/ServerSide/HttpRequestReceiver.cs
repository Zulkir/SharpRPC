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
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using SharpRpc.Interaction;

namespace SharpRpc.ServerSide
{
    public class HttpRequestReceiver : IRequestReceiver
    {
        private readonly IIncomingRequestProcessor requestProcessor;
        private readonly ConcurrentQueue<HttpListenerContext> requestQueue;
        private HttpListener listener;
        private Thread listenerThread;
        private Thread[] workerThreads;

        public HttpRequestReceiver(IIncomingRequestProcessor requestProcessor)
        {
            this.requestProcessor = requestProcessor;
            requestQueue = new ConcurrentQueue<HttpListenerContext>();
        }

        private void DoListen()
        {
            listener.Start();
            while (listener.IsListening)
            {
                var context = listener.GetContext();
                requestQueue.Enqueue(context);
            }
        }

        private void DoWork()
        {
            HttpListenerContext context;
            bool hasRequest = requestQueue.TryDequeue(out context);
            while (hasRequest || listener.IsListening)
            {
                while (hasRequest)
                {
                    context.Response.StatusCode = 200;
                    try
                    {
                        Request request;
                        if (TryDecodeRequest(context.Request, out request))
                        {
                            var response = requestProcessor.Process(request);
                            context.Response.Headers["status"] = ((int)response.Status).ToString(CultureInfo.InvariantCulture);
                            context.Response.Headers["data-length"] = response.Data.Length.ToString(CultureInfo.InvariantCulture);
                            context.Response.OutputStream.Write(response.Data, 0, response.Data.Length);
                        }
                        else
                        {
                            context.Response.Headers["status"] = ((int)ResponseStatus.BadRequest).ToString(CultureInfo.InvariantCulture);
                            context.Response.Headers["data-length"] = "0";
                        }
                    }
                    catch (Exception)
                    {
                        context.Response.Headers["status"] = ((int)ResponseStatus.InternalServerError).ToString(CultureInfo.InvariantCulture);
                        context.Response.Headers["data-length"] = "0";
                    }
                    finally
                    {
                        try
                        {
                            context.Response.Close();
                        }
// ReSharper disable EmptyGeneralCatchClause
                        catch {}
// ReSharper restore EmptyGeneralCatchClause
                    }

                    hasRequest = requestQueue.TryDequeue(out context);
                }

                Thread.Sleep(1);
                hasRequest = requestQueue.TryDequeue(out context);
            }
        }


        private static readonly Regex UrlEx = new Regex(@"^http://[\w\.]+:\d+/(.+)$");
        private static bool TryDecodeRequest(HttpListenerRequest httpWebRequest, out Request request)
        {
            var urlMatch = UrlEx.Match(httpWebRequest.Url.ToString());
            if (!urlMatch.Success)
            {
                request = null;
                return false;
            }

            ServicePath servicePath;
            if (!ServicePath.TryParse(urlMatch.Groups[1].Value, out servicePath))
            {
                request = null;
                return false;
            }

            var scope = httpWebRequest.Headers["scope"];
            if (string.IsNullOrWhiteSpace(scope))
                scope = null;

            var data = new byte[httpWebRequest.ContentLength64];
            using (var stream = httpWebRequest.InputStream)
            {
                stream.Read(data, 0, data.Length);
            }

            request = new Request(servicePath, scope, data);
            return true;
        }

        public void Start(int port, int threads)
        {
            if (listener != null)
                throw new InvalidOperationException("Trying to start a receiver that is already running");

            listener = new HttpListener();
            listener.Prefixes.Add("http://*:" + port + "/");
            listenerThread = new Thread(DoListen);
            listenerThread.Start();

            workerThreads = Enumerable.Range(0, threads).Select(i => new Thread(DoWork)).ToArray();
            foreach (var workerThread in workerThreads)
                workerThread.Start();
        }

        public void Stop()
        {
            listener.Stop();
            listenerThread.Join();
            listener.Close();
            listener = null;

            foreach (var workerThread in workerThreads)
                workerThread.Join();
        }
    }
}