using System;

namespace SharpRpc
{
    public struct ServiceEndPoint : IEquatable<ServiceEndPoint>
    {
        public string Protocol;
        public string Host;
        public int Port;

        public ServiceEndPoint(string protocol, string host, int port)
        {
            Protocol = protocol;
            Host = host;
            Port = port;
        }

        public static bool Equals(ServiceEndPoint ep1, ServiceEndPoint ep2)
        {
            return ep1.Protocol == ep2.Protocol && ep1.Host == ep2.Host && ep1.Port == ep2.Port;
        }

        public static bool operator ==(ServiceEndPoint ep1, ServiceEndPoint ep2)
        {
            return string.Equals(ep1, ep2) && ep1.Port == ep2.Port;
        }

        public static bool operator !=(ServiceEndPoint ep1, ServiceEndPoint ep2)
        {
            return !(ep1 == ep2);
        }

        public bool Equals(ServiceEndPoint other)
        {
            return Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            return obj is ServiceEndPoint && Equals(this, (ServiceEndPoint)obj);
        }

        public override int GetHashCode()
        {
            return (Host ?? "").GetHashCode() + Port;
        }

        public override string ToString()
        {
            return string.Format("{0}://{1}:{2}", Protocol, Host, Port);
        }
    }
}
