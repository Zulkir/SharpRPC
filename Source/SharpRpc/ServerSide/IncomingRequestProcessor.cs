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
using SharpRpc.Codecs;
using SharpRpc.Interaction;
using SharpRpc.Logs;
using SharpRpc.Utility;

namespace SharpRpc.ServerSide
{
    public class IncomingRequestProcessor : IIncomingRequestProcessor
    {
        private readonly IRpcClientServer clientServer;
        private readonly IServiceImplementationContainer serviceImplementationContainer;
        private readonly IServiceMethodHandlerContainer serviceMethodHandlerContainer;
        private readonly IManualCodec<Exception> exceptionCodec;
        private readonly ILogger logger;

        public IncomingRequestProcessor(IRpcClientServer clientServer, IServiceImplementationContainer serviceImplementationContainer, 
            IServiceMethodHandlerContainer serviceMethodHandlerContainer, ICodecContainer codecContainer)
        {
            this.clientServer = clientServer;
            this.logger = clientServer.Logger;
            this.serviceImplementationContainer = serviceImplementationContainer;
            this.serviceMethodHandlerContainer = serviceMethodHandlerContainer;
            exceptionCodec = codecContainer.GetManualCodecFor<Exception>();
        }

        public Response Process(Request request)
        {
            try
            {
                logger.IncomingRequest(request);
                var startTime = DateTime.Now;

                var implementationInfo = serviceImplementationContainer.GetImplementation(request.Path.ServiceName, request.ServiceScope);

                if (implementationInfo.Implementation.State == ServiceImplementationState.NotInitialized)
                    ThreadGuard.RunOnce(implementationInfo.Implementation, x =>
                        x.Initialize(clientServer, clientServer.Settings.GetServiceSettings(request.Path.ServiceName), request.ServiceScope));
                if (implementationInfo.Implementation.State == ServiceImplementationState.NotInitialized)
                    return Response.InvalidImplementation;

                if (implementationInfo.Implementation.State == ServiceImplementationState.NotReady)
                    return Response.NotReady;

                var methodHandler = serviceMethodHandlerContainer.GetMethodHandler(implementationInfo, request.Path);

                var responseData = methodHandler(implementationInfo.Implementation, request.Data);

                var executionTime = DateTime.Now - startTime;
                logger.ProcessedRequestSuccessfully(request, executionTime);

                return new Response(ResponseStatus.Ok, responseData);
            }
            catch (ServiceNotFoundException)
            {
                logger.ProcessedRequestWithBadStatus(request, ResponseStatus.ServiceNotFound);
                return Response.NotFound;
            }
            catch (InvalidPathException)
            {
                logger.ProcessedRequestWithBadStatus(request, ResponseStatus.BadRequest);
                return Response.BadRequest;
            }
            catch (InvalidImplementationException)
            {
                logger.ProcessedRequestWithBadStatus(request, ResponseStatus.InvalidImplementation);
                return Response.InvalidImplementation;
            }
            catch (Exception ex)
            {
                logger.ProcessedRequestWithException(request, ex);
                var responseData = exceptionCodec.EncodeSingle(ex);
                return new Response(ResponseStatus.Exception, responseData);
            }
        }
    }
}