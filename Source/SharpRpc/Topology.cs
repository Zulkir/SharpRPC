using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SharpRpc
{
    public class Topology : ITopology
    {
        private readonly ConcurrentDictionary<string, List<ServiceEndPoint>> endPoints; 

        public Topology()
        {
            endPoints = new ConcurrentDictionary<string, List<ServiceEndPoint>>();
        }

        public void AddEndPoint(string serviceName, string scope, ServiceEndPoint endPoint)
        {
            var list = endPoints.GetOrAdd(serviceName, n => new List<ServiceEndPoint>());
            list.Add(endPoint);
        }

        public bool TryGetEndPoint(string serviceName, string scope, out ServiceEndPoint endPoint)
        {
            List<ServiceEndPoint> list;
            if (!endPoints.TryGetValue(serviceName, out list) || list.Count == 0)
            {
                endPoint = default(ServiceEndPoint);
                return false;
            }
            endPoint = scope == null ? list[0] : list[scope[scope.Length - 1] % list.Count];
            return true;
        }
    }
}
