using System;
using SharpRpc.Codecs;
using SharpRpc.Interaction;
using SharpRpc.Utility;

namespace SharpRpc.ServerSide
{
    public class IncomingRequestProcessor : IIncomingRequestProcessor
    {
        private readonly IServiceImplementationContainer serviceImplementationContainer;
        private readonly IServiceMethodHandlerContainer serviceMethodHandlerContainer;
        private readonly IManualCodec<Exception> exceptionCodec;

        public IncomingRequestProcessor(IServiceImplementationContainer serviceImplementationContainer, 
            IServiceMethodHandlerContainer serviceMethodHandlerContainer, ICodecContainer codecContainer)
        {
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
                    ThreadGuard.RunOnce(implementationInfo.Implementation, x => x.Initialize(request.ServiceScope));
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