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
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using SharpRpc.Reflection;

namespace SharpRpc.ServerSide
{
    public class ServiceImplementationContainer : IServiceImplementationContainer
    {
        class ImplementationSet
        {
            private readonly Type serviceInterface;
            private readonly ServiceDescription description;
            private readonly ConstructorInfo constructor;
            private IServiceImplementation nullScopeImplementation;
            private ConcurrentDictionary<string, IServiceImplementation> scopedImplementations;

            public Type Interface { get { return serviceInterface; } }
            public ServiceDescription Description { get { return description; } }

            public ImplementationSet(Type serviceInterface, ServiceDescription description, ConstructorInfo constructor)
            {
                this.serviceInterface = serviceInterface;
                this.description = description;
                this.constructor = constructor;
            }

            private static readonly object[] EmptyParams = new object[0];

            private IServiceImplementation CreateNew()
            {
                return (IServiceImplementation)constructor.Invoke(EmptyParams);
            }

            public IServiceImplementation GetForScope(string scope)
            {
                if (scope == null)
                    return nullScopeImplementation ?? (nullScopeImplementation = CreateNew());
                if (scopedImplementations == null)
                    scopedImplementations = new ConcurrentDictionary<string, IServiceImplementation>();
                return scopedImplementations.GetOrAdd(scope, s => CreateNew());
            }

            public IEnumerable<string> GetInitializedScopes()
            {
                if (nullScopeImplementation != null)
                    yield return null;
                if (scopedImplementations != null)
                    foreach (var key in scopedImplementations.Keys)
                        yield return key;
            }
        }

        private readonly IServiceDescriptionBuilder serviceDescriptionBuilder;
        private readonly ConcurrentDictionary<string, ImplementationSet> implementations;

        public ServiceImplementationContainer(IServiceDescriptionBuilder serviceDescriptionBuilder)
        {
            this.serviceDescriptionBuilder = serviceDescriptionBuilder;
            implementations = new ConcurrentDictionary<string, ImplementationSet>();
        }

        public void RegisterImplementation(Type serviceInterface, Type implementationType)
        {
            if (serviceInterface == null)
                throw new ArgumentNullException("serviceInterface");

            var serviceName = serviceInterface.Name.Substring(1);

            if (implementationType == null)
                throw new ArgumentNullException("implementationType");
            if (!typeof(IServiceImplementation).IsAssignableFrom(implementationType))
                throw new ArgumentException(string.Format("Given implementation for a '{0}' service ({1}) does not implement an IServiceImplementation interface", serviceName, implementationType.FullName));
            if (!serviceInterface.IsAssignableFrom(implementationType))
                throw new ArgumentException(string.Format("Given implementation for a '{0}' service ({1}) does not implement its interface", serviceName, implementationType.FullName));
            var constructor = implementationType.GetConstructors().FirstOrDefault(x => x.GetParameters().Length == 0);
            if (constructor == null)
                throw new ArgumentException(string.Format("Given implementation of the {0} service does not have a parameterless constructor", serviceName));

            var interfaceDescription = serviceDescriptionBuilder.Build(serviceInterface);
            implementations.AddOrUpdate(serviceName, n => new ImplementationSet(serviceInterface, interfaceDescription, constructor), (n, p) => p);
        }

        public ServiceImplementationInfo GetImplementation(string serviceName, string scope)
        {
            if (serviceName == null)
                throw new InvalidPathException();

            ImplementationSet set;
            if (!implementations.TryGetValue(serviceName, out set))
                throw new ServiceNotFoundException();

            var implementation = set.GetForScope(scope);
            return new ServiceImplementationInfo(set.Interface, set.Description, implementation);
        }

        public IEnumerable<string> GetInitializedScopesFor(string serviceName)
        {
            if (serviceName == null)
                throw new InvalidPathException();

            ImplementationSet set;
            if (!implementations.TryGetValue(serviceName, out set))
                throw new ServiceNotFoundException();

            return set.GetInitializedScopes();
        }
    }
}