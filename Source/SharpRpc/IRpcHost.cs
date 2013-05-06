using System.Collections.Generic;

namespace SharpRpc
{
    public interface IRpcHost
    {
        void StartHost();
        void StopHost();
        IEnumerable<string> GetInitializedScopesFor<T>(); 
    }
}
