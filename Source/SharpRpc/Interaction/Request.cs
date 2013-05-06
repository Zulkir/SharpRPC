using System;

namespace SharpRpc.Interaction
{
    public class Request
    {
        public ServicePath Path { get; private set; }
        public string ServiceScope { get; private set; }
        public byte[] Data { get; private set; }

        public Request(ServicePath servicePath, string serviceScope, byte[] data)
        {
            if (servicePath == null)
                throw new ArgumentNullException("servicePath", "Service path cannot be null");
            Path = servicePath;
            ServiceScope = serviceScope;
            Data = data;
        }
    }
}
