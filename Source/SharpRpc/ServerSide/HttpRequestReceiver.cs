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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SharpRpc.Interaction;
using SharpRpc.Logs;
using SharpRpc.Utility;

namespace SharpRpc.ServerSide
{
    public class HttpRequestReceiver : IRequestReceiver
    {
        private readonly IIncomingRequestProcessor requestProcessor;
        private readonly ILogger logger;
        private HttpListener listener;
        private Thread listenerThread;

        public HttpRequestReceiver(IIncomingRequestProcessor requestProcessor, ILogger logger)
        {
            this.requestProcessor = requestProcessor;
            this.logger = logger;
        }

        private void DoListen()
        {
            try
            {
                listener.Start();
                logger.Info("Listener has started");
                while (listener.IsListening)
                {
                    try
                    {
                        var context = listener.GetContext();
                        Task.Run(() => DoWork(context));
                    }
                    catch (Exception ex)
                    {
                        if (listener != null && !listener.IsListening)
                            logger.Info("Listener was stopped while getting a context");
                        else
                            logger.NetworkingException("Listener failed to get a context", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Fatal("Listener thread was killed by an exception", ex);
            }
            logger.Info("Listener has finished working");
        }

        private async Task DoWork(HttpListenerContext context)
        {
            try
            {
                Request request;
                if (TryDecodeRequest(context.Request, out request))
                {
                    var response = await requestProcessor.Process(request);
                    context.Response.StatusCode = (int)response.Status;
                    context.Response.ContentLength64 = response.Data.Length;
                    context.Response.OutputStream.Write(response.Data, 0, response.Data.Length);
                }
                else
                {
                    logger.Error(string.Format("Failed to decode request '{0}'", context.Request.Url));
                    context.Response.StatusCode = (int)ResponseStatus.BadRequest;
                    context.Response.ContentLength64 = 0;
                }
            }
            catch (Exception ex)
            {
                logger.NetworkingException("Processing a request failed unexpectedly", ex);
                context.Response.StatusCode = (int)ResponseStatus.InternalServerError;
                context.Response.ContentLength64 = 0;
            }
            finally
            {
                try
                {
                    context.Response.Close();
                }
                catch (Exception ex)
                {
                    logger.NetworkingException("Closing a response stream failed", ex);
                }
            }
        }

        private static bool TryDecodeRequest(HttpListenerRequest httpWebRequest, out Request request)
        {
            using (var inputStream = httpWebRequest.InputStream)
            {
                ServicePath servicePath;
                if (!ServicePath.TryParse(httpWebRequest.Url.LocalPath, out servicePath))
                {
                    request = null;
                    return false;
                }

                var scope = httpWebRequest.QueryString["scope"];
                var data = inputStream.ReadToEnd(httpWebRequest.ContentLength64);

                request = new Request(servicePath, scope, data);
                return true;
            }
        }

        public void Start(int port, int threads)
        {
            if (listener != null)
                throw new InvalidOperationException("Trying to start a receiver that is already running");

            listener = new HttpListener();
            listener.Prefixes.Add("http://*:" + port + "/");
            listenerThread = new Thread(DoListen);
            listenerThread.Start();
        }

        public void Stop()
        {
            listener.Stop();
            listenerThread.Join();
            listener.Close();
            listener = null;
        }
    }
}