using System;
using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    [TestFixture]
    public class CodecContainerTests
    {
        private CodecContainer codecContainer;

        [SetUp]
        public void Setup()
        {
            codecContainer = new CodecContainer();
        }

        [Test]
        public void Emitting()
        {
            var codec1 = codecContainer.GetEmittingCodecFor(typeof(decimal));
            var codec2 = codecContainer.GetEmittingCodecFor(typeof(decimal));
            Assert.That(codec1, Is.EqualTo(codec2));
        }

        [Test]
        public void Manual()
        {
            var codec1 = codecContainer.GetManualCodecFor<decimal>();
            var codec2 = codecContainer.GetManualCodecFor<decimal>();
            Assert.That(codec1, Is.EqualTo(codec2));
        }

        [Test]
        public void ManualExceptions()
        {
            var codec1 = codecContainer.GetManualCodecFor<Exception>();
            var codec2 = codecContainer.GetManualCodecFor<Exception>();
            Assert.That(codec1, Is.EqualTo(codec2));
        }
    }
}