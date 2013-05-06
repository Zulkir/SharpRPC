using System;

namespace SharpRpc.Reflection
{
    public interface IServiceDescriptionBuilder
    {
        ServiceDescription Build(Type interfaceType);
    }
}