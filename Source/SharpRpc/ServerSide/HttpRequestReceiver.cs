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
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using SharpRpc.Interaction;
using SharpRpc.Logs;

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
                while (listener.IsListening)
                {
                    try
                    {
                        var context = listener.GetContext();
                        ThreadPool.QueueUserWorkItem(x => DoWork((HttpListenerContext)x), context);
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

        private void DoWork(HttpListenerContext context)
        {
            try
            {
                context.Response.StatusCode = 200;
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
                    logger.Error(string.Format("Failed to decode request '{0}'", context.Request.Url));
                    context.Response.Headers["status"] = ((int)ResponseStatus.BadRequest).ToString(CultureInfo.InvariantCulture);
                    context.Response.Headers["data-length"] = "0";
                }
            }
            catch (Exception ex)
            {
                logger.NetworkingException("Processing a request failed unexpectedly", ex);
                context.Response.Headers["status"] = ((int)ResponseStatus.InternalServerError).ToString(CultureInfo.InvariantCulture);
                context.Response.Headers["data-length"] = "0";
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


        private static readonly Regex UrlEx = new Regex(@"^http://[\w\.\-]+:\d+/(.+)$");
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
                int offset = 0;
                while (offset < data.Length)
                {
                    int read = stream.Read(data, offset, data.Length - offset);
                    if (read == 0)
                        throw new InvalidDataException("Unexpected end of response stream");
                    offset += read;
                }
                
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