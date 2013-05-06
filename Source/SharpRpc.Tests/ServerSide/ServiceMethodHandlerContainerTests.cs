using NSubstitute;
using NUnit.Framework;
using SharpRpc.Interaction;
using SharpRpc.ServerSide;

namespace SharpRpc.Tests.ServerSide
{
    [TestFixture]
    public class ServiceMethodHandlerContainerTests
    {
        private IServiceMethodHandlerFactory factory;
        private IServiceMethodHandlerContainer container;

        [SetUp]
        public void Setup()
        {
            factory = Substitute.For<IServiceMethodHandlerFactory>();
            container = new ServiceMethodHandlerContainer(factory);
        }

        [Test]
        public void Trivial()
        {
            var implementationInfo = new ServiceImplementationInfo();
            var path = new ServicePath("MyService", "MyMethod");
            var handler = (ServiceMethodHandler)((i, d) => new byte[0]);
            factory.CreateMethodHandler(implementationInfo, path).Returns(handler);

            var handler1 = container.GetMethodHandler(implementationInfo, path);
            var handler2 = container.GetMethodHandler(implementationInfo, path);

            Assert.That(handler1, Is.EqualTo(handler2));
        }
    }
}