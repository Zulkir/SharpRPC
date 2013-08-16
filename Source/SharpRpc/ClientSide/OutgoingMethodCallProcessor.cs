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
using System.Collections.Generic;
using System.Threading;
using SharpRpc.Codecs;
using SharpRpc.Interaction;
using SharpRpc.Reflection;
using SharpRpc.Topology;

namespace SharpRpc.ClientSide
{
    public class OutgoingMethodCallProcessor : IOutgoingMethodCallProcessor
    {
        private readonly IReadOnlyDictionary<string, IServiceTopology> topology;
        private readonly IRequestSenderContainer requestSenderContainer;
        private readonly IManualCodec<Exception> exceptionCodec;

        public OutgoingMethodCallProcessor(IReadOnlyDictionary<string, IServiceTopology> topology, IRequestSenderContainer requestSenderContainer, ICodecContainer codecContainer)
        {
            this.topology = topology;
            this.requestSenderContainer = requestSenderContainer;
            exceptionCodec = codecContainer.GetManualCodecFor<Exception>();
        }

        public byte[] Process(Type serviceInterface, string pathSeparatedBySlashes, string serviceScope, byte[] data, TimeoutSettings timeoutSettings)
        {
            ServicePath path;
            if (!ServicePath.TryParse(pathSeparatedBySlashes, out path))
                throw new InvalidOperationException(string.Format("'{0}' is not a valid service path", pathSeparatedBySlashes));

            var serviceName = serviceInterface.GetServiceName();
            var endPoint = topology.GetEndPoint(serviceName, serviceScope);
            var sender = requestSenderContainer.GetSender(endPoint.Protocol);
            var request = new Request(path, serviceScope, data);
            timeoutSettings = timeoutSettings ?? TimeoutSettings.NoTimeout;

            bool hasNetworkTimeout = timeoutSettings.MaxMilliseconds != -1 && timeoutSettings.MaxMilliseconds != 0 && timeoutSettings.MaxMilliseconds != int.MaxValue;
            var startTime = DateTime.Now;
            int remainingTries = timeoutSettings.NotReadyRetryCount + 1;

            while (remainingTries > 0)
            {
                remainingTries--;

                Response response;
                try
                {
                    int? networkTimeout = hasNetworkTimeout ? (DateTime.Now - startTime).Milliseconds : (int?) null;
                    response = sender.Send(endPoint.Host, endPoint.Port, request, networkTimeout);
                }
                catch (TimeoutException ex)
                {
                    throw new ServiceTimeoutException(request, timeoutSettings.MaxMilliseconds, ex);
                }
                catch (Exception ex)
                {
                    throw new ServiceNetworkException(string.Format("Sending a '{0}' request to {1} failed", pathSeparatedBySlashes, endPoint), ex);
                }

                switch (response.Status)
                {
                    case ResponseStatus.Ok:
                        return response.Data;
                    case ResponseStatus.NotReady:
                        if (remainingTries > 0)
                            Thread.Sleep(timeoutSettings.NotReadyRetryMilliseconds);
                        break;
                    case ResponseStatus.BadRequest:
                        throw new ServiceTopologyException(string.Format("'{0}' seems to be a bad request for {1}",
                            pathSeparatedBySlashes, endPoint));
                    case ResponseStatus.ServiceNotFound:
                        throw new ServiceTopologyException(string.Format("'{0}' service was not present at {1}",
                            serviceName, endPoint));
                    case ResponseStatus.Exception:
                        {
                            Exception remoteException;
                            if (exceptionCodec.TryDecodeSingle(response.Data, out remoteException))
                                throw remoteException;
                            throw new ServiceNetworkException(
                                string.Format("'{0}' request caused {1} to return an unknown exception",
                                              pathSeparatedBySlashes, endPoint));
                        }
                    case ResponseStatus.InternalServerError:
                        throw new Exception(string.Format("'{0}' request caused {1} to encounter an internal server error",
                            pathSeparatedBySlashes, endPoint));
                    default:
                        throw new ArgumentOutOfRangeException("response.Status");
                }
            }

            throw new ServiceTimeoutException(request, timeoutSettings.NotReadyRetryCount, timeoutSettings.NotReadyRetryMilliseconds);
        }
    }
}
