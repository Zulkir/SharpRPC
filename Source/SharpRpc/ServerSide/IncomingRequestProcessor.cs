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
using SharpRpc.Utility;

namespace SharpRpc.ServerSide
{
    public class IncomingRequestProcessor : IIncomingRequestProcessor
    {
        private readonly IRpcKernel kernel;
        private readonly IServiceImplementationContainer serviceImplementationContainer;
        private readonly IServiceMethodHandlerContainer serviceMethodHandlerContainer;
        private readonly IManualCodec<Exception> exceptionCodec;

        public IncomingRequestProcessor(IRpcKernel kernel, IServiceImplementationContainer serviceImplementationContainer, 
            IServiceMethodHandlerContainer serviceMethodHandlerContainer, ICodecContainer codecContainer)
        {
            this.kernel = kernel;
            this.serviceImplementationContainer = serviceImplementationContainer;
            this.serviceMethodHandlerContainer = serviceMethodHandlerContainer;
            exceptionCodec = codecContainer.GetManualCodecFor<Exception>();
        }

        public Response Process(Request request)
        {
            try
            {
                var implementationInfo = serviceImplementationContainer.GetImplementation(request.Path.ServiceName, request.ServiceScope);

                if (implementationInfo.Implementation.State == ServiceImplementationState.NotInitialized)
                    ThreadGuard.RunOnce(implementationInfo.Implementation, x => x.Initialize(kernel, request.ServiceScope));
                if (implementationInfo.Implementation.State == ServiceImplementationState.NotInitialized)
                    return Response.InvalidImplementation;

                if (implementationInfo.Implementation.State == ServiceImplementationState.NotReady)
                    return Response.NotReady;

                var methodHandler = serviceMethodHandlerContainer.GetMethodHandler(implementationInfo, request.Path);

                var responseData = methodHandler(implementationInfo.Implementation, request.Data);
                return new Response(ResponseStatus.Ok, responseData);
            }
            catch (ServiceNotFoundException)
            {
                return Response.NotFound;
            }
            catch (InvalidPathException)
            {
                return Response.BadRequest;
            }
            catch (InvalidImplementationException)
            {
                return Response.InvalidImplementation;
            }
            catch (Exception ex)
            {
                var responseData = exceptionCodec.EncodeSingle(ex);
                return new Response(ResponseStatus.Exception, responseData);
            }
        }
    }
}