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
using System.Reflection;
using SharpRpc.Reflection;
using SharpRpc.Settings;
using System.Linq;

namespace SharpRpc.ServerSide
{
    public class ServiceImplementationFactory : IServiceImplementationFactory
    {
        #region Nester Structs
        private struct ImplementationCreationInfo
        {
            public ServiceDescription Description;
            public ConstructorInfo Constructor;
        }
        #endregion

        private readonly IServiceDescriptionBuilder serviceDescriptionBuilder;
        private readonly IRpcClientServer clientServer;
        private readonly ConcurrentDictionary<string, ImplementationCreationInfo> constructors;

        public ServiceImplementationFactory(IServiceDescriptionBuilder serviceDescriptionBuilder, IRpcClientServer clientServer, 
            IEnumerable<InterfaceImplementationTypePair> interfaceImplementationTypePairs)
        {
            this.serviceDescriptionBuilder = serviceDescriptionBuilder;
            this.clientServer = clientServer;
            constructors = new ConcurrentDictionary<string, ImplementationCreationInfo>(interfaceImplementationTypePairs.Select(ConvertPair));
        }

        private KeyValuePair<string, ImplementationCreationInfo> ConvertPair(InterfaceImplementationTypePair pair)
        {
            var serviceName = pair.Interface.GetServiceName();
            if (!pair.Interface.IsAssignableFrom(pair.ImplementationType))
                throw new ArgumentException(string.Format("Given implementation for a '{0}' service ({1}) does not implement its interface", serviceName, pair.ImplementationType.FullName));
            var constructor = FindLargestAppropriateConstructor(pair.ImplementationType);
            if (constructor == null)
                throw new ArgumentException(string.Format("No appropriate constructor found for {0}", pair.ImplementationType.FullName));
            var creationInfo = new ImplementationCreationInfo
                {
                    Constructor = constructor,
                    Description = serviceDescriptionBuilder.Build(pair.Interface)
                };
            return new KeyValuePair<string, ImplementationCreationInfo>(pair.Interface.GetServiceName(), creationInfo);
        }

        private static ConstructorInfo FindLargestAppropriateConstructor(Type type)
        {
            return type.GetConstructors()
                       .Where(x => x.GetParameters().All(ParameterIsInjectable))
                       .OrderBy(x => x.GetParameters().Length)
                       .FirstOrDefault();
        }

        private static bool ParameterIsInjectable(ParameterInfo parameterInfo)
        {
            var type = parameterInfo.ParameterType;
            return type == typeof(string) || type == typeof(IRpcClient) || type == typeof(IRpcClientServer);
        }

        public bool CanCreate(string serviceName)
        {
            return constructors.ContainsKey(serviceName);
        }

        public ServiceImplementationInfo CreateImplementation(string serviceName, string scope)
        {
            ImplementationCreationInfo creationInfo;
            if (!constructors.TryGetValue(serviceName, out creationInfo))
                throw new ArgumentOutOfRangeException("serviceName", string.Format("Implementation for service '{0}' was not found", serviceName));
            return new ServiceImplementationInfo(creationInfo.Description, InvokeConstructor(creationInfo.Constructor, clientServer, scope));
        }

        private static object InvokeConstructor(ConstructorInfo constructor, IRpcClientServer clientServer, string scope)
        {
            var arguments = constructor.GetParameters().Select(x =>
                {
                    var paramType = x.ParameterType;
                    if (paramType == typeof(string))
                        return (object)scope;
                    if (paramType == typeof(IRpcClient) || paramType == typeof(IRpcClientServer))
                        return (object)clientServer;
                    throw new InvalidOperationException("Should never happen");
                }).ToArray();
            return constructor.Invoke(arguments);
        }
    }
}