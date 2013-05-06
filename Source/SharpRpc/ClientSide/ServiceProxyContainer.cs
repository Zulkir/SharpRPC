using System;
using System.Collections.Concurrent;

namespace SharpRpc.ClientSide
{
    public class ServiceProxyContainer : IServiceProxyContainer
    {
        #region Proxy Set
        private class ProxySet<T> where T : class
        {
            private readonly IOutgoingMethodCallProcessor processor;
            private readonly Func<IOutgoingMethodCallProcessor, string, T> constructor;
            private readonly ConcurrentDictionary<string, T> scopedProxies;
            private T nullScopeProxy;

            public ProxySet(IOutgoingMethodCallProcessor processor, Func<IOutgoingMethodCallProcessor, string, T> constructor)
            {
                this.processor = processor;
                this.constructor = constructor;
                scopedProxies = new ConcurrentDictionary<string, T>();
            }

            public T GetUnscoped()
            {
                return nullScopeProxy ?? (nullScopeProxy = constructor(processor, null));
            }

            public T GetForScope(string scope)
            {
                return scopedProxies.GetOrAdd(scope, s => constructor(processor, scope));
            }
        }
        #endregion

        private readonly IOutgoingMethodCallProcessor processor;
        private readonly IServiceProxyClassFactory factory;
        private readonly ConcurrentDictionary<Type, object> proxySets; 

        public ServiceProxyContainer(IOutgoingMethodCallProcessor processor, IServiceProxyClassFactory factory)
        {
            this.processor = processor;
            this.factory = factory;
            proxySets = new ConcurrentDictionary<Type, object>();
        }

        public T GetProxy<T>(string scope) where T : class
        {
            var set = (ProxySet<T>)proxySets.GetOrAdd(typeof(T), t => new ProxySet<T>(processor, factory.CreateProxyClass<T>()));
            return scope == null ? set.GetUnscoped() : set.GetForScope(scope);
        }
    }
}
