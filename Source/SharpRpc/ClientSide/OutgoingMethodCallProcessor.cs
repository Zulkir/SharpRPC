using System;
using SharpRpc.Codecs;
using SharpRpc.Interaction;
using SharpRpc.Reflection;

namespace SharpRpc.ClientSide
{
    public class OutgoingMethodCallProcessor : IOutgoingMethodCallProcessor
    {
        private readonly ITopology topology;
        private readonly IRequestSenderContainer requestSenderContainer;
        private readonly IManualCodec<Exception> exceptionCodec;

        public OutgoingMethodCallProcessor(ITopology topology, IRequestSenderContainer requestSenderContainer, ICodecContainer codecContainer)
        {
            this.topology = topology;
            this.requestSenderContainer = requestSenderContainer;
            exceptionCodec = codecContainer.GetManualCodecFor<Exception>();
        }

        public byte[] Process(Type serviceInterface, string pathSeparatedBySlashes, string serviceScope, byte[] data)
        {
            ServicePath path;
            if (!ServicePath.TryParse(pathSeparatedBySlashes, out path))
                throw new InvalidOperationException(string.Format("'{0}' is not a valid service path", pathSeparatedBySlashes));

            var serviceName = serviceInterface.GetServiceName();

            ServiceEndPoint endPoint;
            if (!topology.TryGetEndPoint(serviceName, serviceScope, out endPoint))
                throw new ServiceTopologyException(string.Format("End point for {0} scope of {1} was not found in the topology",
                    serviceName, serviceScope));

            var sender = requestSenderContainer.GetSender(endPoint.Protocol);
            var request = new Request(path, serviceScope, data);

            Response response;
            try
            {
                response = sender.Send(endPoint.Host, endPoint.Port, request);
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
                    throw new NotImplementedException();
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
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
