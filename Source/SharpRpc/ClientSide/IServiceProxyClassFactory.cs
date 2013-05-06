using System;

namespace SharpRpc.ClientSide
{
    public interface IServiceProxyClassFactory
    {
        Func<IOutgoingMethodCallProcessor, string, T> CreateProxyClass<T>();
    }
}