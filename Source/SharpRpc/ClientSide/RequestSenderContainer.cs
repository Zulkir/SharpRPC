using System;

namespace SharpRpc.ClientSide
{
    public class RequestSenderContainer : IRequestSenderContainer
    {
        private HttpRequestSender httpSender;

        public IRequestSender GetSender(string protocol)
        {
            switch (protocol)
            {
                case "http": return httpSender ?? (httpSender = new HttpRequestSender());
                default: throw new NotSupportedException(string.Format("Protocol '{0}' is not supported", protocol));
            }
        }
    }
}
