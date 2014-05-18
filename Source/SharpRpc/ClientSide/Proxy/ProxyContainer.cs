#region License
/*
Copyright (c) 2013-2014 Daniil Rodin of Buhgalteria.Kontur team of SKB Kontur

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

namespace SharpRpc.ClientSide.Proxy
{
    public class ProxyContainer : IProxyContainer
    {
        #region Proxy Set
        private class ProxySet<T> where T : class
        {
            private readonly IOutgoingRequestProcessor processor;
            private readonly Func<IOutgoingRequestProcessor, string, TimeoutSettings, T> constructor;
            private readonly ConcurrentDictionary<ProxyKey, T> scopedProxies;

            public ProxySet(IOutgoingRequestProcessor processor, Func<IOutgoingRequestProcessor, string, TimeoutSettings, T> constructor)
            {
                this.processor = processor;
                this.constructor = constructor;
                scopedProxies = new ConcurrentDictionary<ProxyKey, T>();
            }

            public T GetForScope(string scope, TimeoutSettings timeoutSettings)
            {
                return scopedProxies.GetOrAdd(new ProxyKey(scope, timeoutSettings), s => constructor(processor, scope, timeoutSettings));
            }
        }
        #endregion

        private readonly IOutgoingRequestProcessor processor;
        private readonly IProxyFactory factory;
        private readonly ConcurrentDictionary<Type, object> proxySets; 

        public ProxyContainer(IOutgoingRequestProcessor processor, IProxyFactory factory)
        {
            this.processor = processor;
            this.factory = factory;
            proxySets = new ConcurrentDictionary<Type, object>();
        }

        public T GetProxy<T>(string scope, TimeoutSettings timeoutSettings) where T : class
        {
            var set = (ProxySet<T>)proxySets.GetOrAdd(typeof(T), t => new ProxySet<T>(processor, factory.CreateProxy<T>()));
            return set.GetForScope(scope, timeoutSettings);
        }
    }
}
