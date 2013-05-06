using System;

namespace SharpRpc.ClientSide
{
    public interface IOutgoingMethodCallProcessor
    {
        byte[] Process(Type serviceInterface, string pathSeparatedBySlashes, string serviceScope, byte[] data);
    }
}
