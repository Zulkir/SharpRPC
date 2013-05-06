using System;

namespace SharpRpc.ServerSide
{
    public class RequestReceiverContainer : IRequestReceiverContainer 
    {
        private readonly IIncomingRequestProcessor requestProcessor;
        private HttpRequestReceiver httpReceiver;

        public RequestReceiverContainer(IIncomingRequestProcessor requestProcessor)
        {
            this.requestProcessor = requestProcessor;
        }

        public IRequestReceiver GetReceiver(string protocol)
        {
            switch (protocol)
            {
                case "http": return httpReceiver ?? (httpReceiver = new HttpRequestReceiver(requestProcessor));
                default: throw new NotSupportedException(string.Format("Protocol '{0}' is not supported", protocol));
            }
        }
    }
}