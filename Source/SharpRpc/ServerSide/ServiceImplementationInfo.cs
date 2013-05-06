using System;
using SharpRpc.Reflection;

namespace SharpRpc.ServerSide
{
    public struct ServiceImplementationInfo
    {
        public readonly Type Interface;
        public readonly ServiceDescription Description;
        public readonly IServiceImplementation Implementation;

        public ServiceImplementationInfo(Type serviceInterface, ServiceDescription description, IServiceImplementation implementation)
        {
            Interface = serviceInterface;
            Description = description;
            Implementation = implementation;
        }
    }
}
