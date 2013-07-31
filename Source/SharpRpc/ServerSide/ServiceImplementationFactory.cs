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
using SharpRpc.Reflection;
using SharpRpc.Settings;
using System.Linq;

namespace SharpRpc.ServerSide
{
    public class ServiceImplementationFactory : IServiceImplementationFactory
    {
        private readonly IServiceDescriptionBuilder serviceDescriptionBuilder;
        private readonly ConcurrentDictionary<string, InterfaceImplementationTypePair> implementationTypes;

        public ServiceImplementationFactory(IServiceDescriptionBuilder serviceDescriptionBuilder, IEnumerable<InterfaceImplementationTypePair> interfaceImplementationTypePairs)
        {
            this.serviceDescriptionBuilder = serviceDescriptionBuilder;
            implementationTypes = new ConcurrentDictionary<string, InterfaceImplementationTypePair>(interfaceImplementationTypePairs.Select(ConvertPair));
        }

        private KeyValuePair<string, InterfaceImplementationTypePair> ConvertPair(InterfaceImplementationTypePair pair)
        {
            CheckImplementationPair(pair);
            return new KeyValuePair<string, InterfaceImplementationTypePair>(pair.Interface.GetServiceName(), pair);
        }

        private void CheckImplementationPair(InterfaceImplementationTypePair pair)
        {
            var serviceName = pair.Interface.GetServiceName();
            if (!typeof(IServiceImplementation).IsAssignableFrom(pair.ImplementationType))
                throw new ArgumentException(string.Format("Given implementation for a '{0}' service ({1}) does not implement an IServiceImplementation interface", serviceName, pair.ImplementationType.FullName));
            if (!pair.Interface.IsAssignableFrom(pair.ImplementationType))
                throw new ArgumentException(string.Format("Given implementation for a '{0}' service ({1}) does not implement its interface", serviceName, pair.ImplementationType.FullName));
            if (pair.ImplementationType.GetConstructor(Type.EmptyTypes) == null)
                throw new ArgumentException(string.Format("Given implementation of the {0} service does not have a parameterless constructor", serviceName));
        }

        public bool CanCreate(string serviceName)
        {
            return implementationTypes.ContainsKey(serviceName);
        }

        public ServiceImplementationInfo CreateImplementation(string serviceName)
        {
            InterfaceImplementationTypePair pair;
            if (!implementationTypes.TryGetValue(serviceName, out pair))
                throw new ArgumentOutOfRangeException("serviceName", string.Format("Implementation for service '{0}' was not found", serviceName));
            var implementation = (IServiceImplementation)Activator.CreateInstance(pair.ImplementationType);
            return new ServiceImplementationInfo(serviceDescriptionBuilder.Build(pair.Interface), implementation);
        }
    }
}