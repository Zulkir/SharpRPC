using System;

namespace SharpRpc
{
    public class ServiceTopologyException : Exception
    {
         public ServiceTopologyException(string message) : base(message)
         {
             
         }
    }
}
