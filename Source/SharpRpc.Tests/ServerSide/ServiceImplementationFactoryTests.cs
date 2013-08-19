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

using NSubstitute;
using NUnit.Framework;
using SharpRpc.Reflection;
using SharpRpc.ServerSide;
using SharpRpc.Settings;

namespace SharpRpc.Tests.ServerSide
{
    [TestFixture]
    public class ServiceImplementationFactoryTests
    {
        private ServiceImplementationFactory factory;
        private IRpcClientServer rpcClientServer;
        private ServiceDescriptionBuilder serviceDescriptionBuilder;

        [SetUp]
        public void Setup()
        {
            rpcClientServer = Substitute.For<IRpcClientServer>();
            serviceDescriptionBuilder = new ServiceDescriptionBuilder(new MethodDescriptionBuilder());
        }

        public interface IMyService
        {
             
        }

        public class ParameterlessService : IMyService
        {
             
        }

        [Test]
        public void CreateParameterless()
        {
            var interfaceImplementationTypePairs = new[]
                {
                    new InterfaceImplementationTypePair(typeof(IMyService), typeof(ParameterlessService))
                };
            factory = new ServiceImplementationFactory(serviceDescriptionBuilder, rpcClientServer, interfaceImplementationTypePairs);

            factory.CreateImplementation("MyService", null);
        }

        public class ScopeOnlyService : IMyService
        {
            public string Scope { get; private set; }
            
            public ScopeOnlyService(string scope)
            {
                Scope = scope;
            }
        }

        [Test]
        public void CreateScopeOnly()
        {
            var interfaceImplementationTypePairs = new[]
                {
                    new InterfaceImplementationTypePair(typeof(IMyService), typeof(ScopeOnlyService))
                };
            factory = new ServiceImplementationFactory(serviceDescriptionBuilder, rpcClientServer, interfaceImplementationTypePairs);

            var service = (ScopeOnlyService)factory.CreateImplementation("MyService", "myscope").Implementation;
            Assert.That(service.Scope, Is.EqualTo("myscope"));
        }

        public class ClientServerOnlyServer : IMyService
        {
            public IRpcClientServer ClientServer { get; private set; }

            public ClientServerOnlyServer(IRpcClientServer clientServer)
            {
                ClientServer = clientServer;
            }
        }

        [Test]
        public void CreateClientServerOnly()
        {
            var interfaceImplementationTypePairs = new[]
                {
                    new InterfaceImplementationTypePair(typeof(IMyService), typeof(ClientServerOnlyServer))
                };
            factory = new ServiceImplementationFactory(serviceDescriptionBuilder, rpcClientServer, interfaceImplementationTypePairs);

            var service = (ClientServerOnlyServer)factory.CreateImplementation("MyService", null).Implementation;
            Assert.That(service.ClientServer, Is.EqualTo(rpcClientServer));
        }

        public class CompleteService : IMyService
        {
            public IRpcClientServer ClientServer { get; private set; }
            public string Scope { get; private set; }

            public CompleteService(IRpcClientServer clientServer, string scope)
            {
                ClientServer = clientServer;
                Scope = scope;
            }
        }

        [Test]
        public void CreateComplete()
        {
            var interfaceImplementationTypePairs = new[]
                {
                    new InterfaceImplementationTypePair(typeof(IMyService), typeof(CompleteService))
                };
            factory = new ServiceImplementationFactory(serviceDescriptionBuilder, rpcClientServer, interfaceImplementationTypePairs);

            var service = (CompleteService)factory.CreateImplementation("MyService", "myscope").Implementation;
            Assert.That(service.ClientServer, Is.EqualTo(rpcClientServer));
            Assert.That(service.Scope, Is.EqualTo("myscope"));
        }

        public class ReversedCompleteService : IMyService
        {
            public IRpcClientServer ClientServer { get; private set; }
            public string Scope { get; private set; }

            public ReversedCompleteService(string scope, IRpcClientServer clientServer)
            {
                ClientServer = clientServer;
                Scope = scope;
            }
        }

        [Test]
        public void CreateCompleteReversed()
        {
            var interfaceImplementationTypePairs = new[]
                {
                    new InterfaceImplementationTypePair(typeof(IMyService), typeof(ReversedCompleteService))
                };
            factory = new ServiceImplementationFactory(serviceDescriptionBuilder, rpcClientServer, interfaceImplementationTypePairs);

            var service = (ReversedCompleteService)factory.CreateImplementation("MyService", "myscope").Implementation;
            Assert.That(service.ClientServer, Is.EqualTo(rpcClientServer));
            Assert.That(service.Scope, Is.EqualTo("myscope"));
        }
    }
}
