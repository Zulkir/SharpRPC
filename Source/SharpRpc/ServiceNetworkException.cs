using System;

namespace SharpRpc
{
    public class ServiceNetworkException : Exception
    {
        public ServiceNetworkException(string message) : base(message)
        {
            
        }

        public ServiceNetworkException(string message, Exception innerException) : base(message, innerException)
        {
            
        }
    }
}
