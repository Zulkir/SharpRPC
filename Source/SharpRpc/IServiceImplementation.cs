using System;

namespace SharpRpc
{
    public interface IServiceImplementation : IDisposable
    {
        ServiceImplementationState State { get; }
        void Initialize(string scope);
    }
}
