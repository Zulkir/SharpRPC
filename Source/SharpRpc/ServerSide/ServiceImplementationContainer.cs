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
using System.Collections.Generic;
using System.Linq;
using SharpRpc.Utility;

namespace SharpRpc.ServerSide
{
    public class ServiceImplementationContainer : IServiceImplementationContainer
    {
        class ImplementationSet
        {
            private readonly string serviceName;
            private readonly IServiceImplementationFactory serviceImplementationFactory;
            private readonly ConcurrentDictionary<ScopeKey, Lazy<ServiceImplementationInfo>> scopedImplementations;

            public ImplementationSet(string serviceName, IServiceImplementationFactory serviceImplementationFactory)
            {
                this.serviceName = serviceName;
                this.serviceImplementationFactory = serviceImplementationFactory;
                scopedImplementations = new ConcurrentDictionary<ScopeKey, Lazy<ServiceImplementationInfo>>();
            }

            private ServiceImplementationInfo CreateNew(string scope)
            {
                return serviceImplementationFactory.CreateImplementation(serviceName, scope);
            }

            public ServiceImplementationInfo GetForScope(string scope)
            {
                return scopedImplementations.GetOrAdd(new ScopeKey(scope), s => new Lazy<ServiceImplementationInfo>(() => CreateNew(s.Scope), true)).Value;
            }

            public IEnumerable<string> GetInitializedScopes()
            {
                return scopedImplementations.Keys.Select(x => x.Scope);
            }
        }

        private readonly IServiceImplementationFactory serviceImplementationFactory;
        private readonly ConcurrentDictionary<string, ImplementationSet> implementations;

        public ServiceImplementationContainer(IServiceImplementationFactory serviceImplementationFactory)
        {
            this.serviceImplementationFactory = serviceImplementationFactory;
            implementations = new ConcurrentDictionary<string, ImplementationSet>();
        }

        public ServiceImplementationInfo GetImplementation(string serviceName, string scope)
        {
            if (serviceName == null)
                throw new InvalidPathException();

            var set = implementations.GetOrAdd(serviceName, x => new ImplementationSet(x, serviceImplementationFactory));
            return set.GetForScope(scope);
        }

        public IEnumerable<string> GetInitializedScopesFor(string serviceName)
        {
            if (serviceName == null)
                throw new InvalidPathException();

            ImplementationSet set;
            if (!implementations.TryGetValue(serviceName, out set))
            {
                if (!serviceImplementationFactory.CanCreate(serviceName))
                    throw new ServiceNotFoundException();
                return Enumerable.Empty<string>();
            }

            return set.GetInitializedScopes();
        }
    }
}