using System.Collections.Generic;

namespace SharpRpc
{
    public interface IServiceHostSettings
    {
        ServiceEndPoint EndPoint { get; }
        IEnumerable<InterfaceImplementationTypePair> GetInterfaceImplementationsPairs();
    }
}
