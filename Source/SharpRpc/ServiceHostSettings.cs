using System.Collections.Generic;
using System.Linq;

namespace SharpRpc
{
    public class ServiceHostSettings : IServiceHostSettings
    {
        private readonly ServiceEndPoint endPoint;
        private readonly InterfaceImplementationTypePair[] interfaceImplementationTypePairs;

        public ServiceHostSettings(ServiceEndPoint endPoint, IEnumerable<InterfaceImplementationTypePair> interfaceImplementationTypePairs)
        {
            this.endPoint = endPoint;
            this.interfaceImplementationTypePairs = interfaceImplementationTypePairs.ToArray();
        }

        public ServiceEndPoint EndPoint
        {
            get { return endPoint; }
        }

        public IEnumerable<InterfaceImplementationTypePair> GetInterfaceImplementationsPairs()
        {
            return interfaceImplementationTypePairs;
        }

        private static readonly ServiceHostSettings EmptyField = new ServiceHostSettings(new ServiceEndPoint {Protocol = "http"}, new InterfaceImplementationTypePair[0]);
        public static ServiceHostSettings Empty
        {
            get { return EmptyField; }
        }
    }
}
 