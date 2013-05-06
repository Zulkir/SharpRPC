#region License
/*
Copyright (c) 2013 Daniil Rodin of Buhgalteria.Kontur team of SKB Kontur

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion

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
