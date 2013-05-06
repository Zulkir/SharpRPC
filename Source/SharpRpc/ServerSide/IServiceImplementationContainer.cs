using System;
using System.Collections.Generic;

namespace SharpRpc.ServerSide
{
    public interface IServiceImplementationContainer
    {
        void RegisterImplementation(Type serviceInterface, Type implementationType);
        ServiceImplementationInfo GetImplementation(string serviceName, string scope);
        IEnumerable<string> GetInitializedScopesFor(string serviceName);
    }
}
