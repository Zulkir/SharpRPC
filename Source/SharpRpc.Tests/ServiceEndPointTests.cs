using NUnit.Framework;

namespace SharpRpc.Tests
{
    [TestFixture]
    public class ServiceEndPointTests
    {
        [Test]
        public void EndPoint()
        {
            ServiceEndPoint endPoint;
            Assert.That(ServiceEndPoint.TryParse(@"http://some-host.com:12345", out endPoint), Is.True);
            Assert.That(endPoint.Protocol, Is.EqualTo("http"));
            Assert.That(endPoint.Host, Is.EqualTo("some-host.com"));
            Assert.That(endPoint.Port, Is.EqualTo(12345));
        }
    }
}