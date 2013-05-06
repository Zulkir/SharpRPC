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
using System.IO;
using NSubstitute;
using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    [TestFixture]
    public unsafe class ExceptionCodecTests
    {
        private IManualCodec<string> stringCodec;
        private ExceptionCodec exceptionCodec;

        [SetUp]
        public void Setup()
        {
            stringCodec = new ManualCodec<string>(new StringCodec());
            var codecContainer = Substitute.For<ICodecContainer>();
            codecContainer.GetManualCodecFor<string>().Returns(stringCodec);
            exceptionCodec = new ExceptionCodec(codecContainer);
        }

        private void DoTest(Exception exception)
        {
            var exceptionWithStackTrace = CreateExceptionWithStackTrace(exception);
            Exception decodedException;
            var data = new byte[exceptionCodec.CalculateSize(exceptionWithStackTrace)];
            fixed (byte* pData = data)
            {
                var p = pData;
                exceptionCodec.Encode(ref p, exceptionWithStackTrace);
                p = pData;
                int remainingBytes = data.Length;
                decodedException = exceptionCodec.Decode(ref p, ref remainingBytes, false);
            }
            AssertExceptions(decodedException, exceptionWithStackTrace);
        }

        private static T CreateExceptionWithStackTrace<T>(T exception) where T : Exception
        {
            T exceptionWithStackTrace;
            try
            {
                throw exception;
            }
            catch (T catched)
            {
                exceptionWithStackTrace = catched;
            }
            return exceptionWithStackTrace;
        }

        private void AssertExceptions(Exception decoded, Exception original)
        {
            Assert.That(decoded.GetType(), Is.EqualTo(original.GetType()));
            Assert.That(decoded.Message, Is.EqualTo(original.Message));
            Assert.That(decoded.InnerException, Is.Not.Null);
            if (original.InnerException != null)
                Assert.That(decoded.InnerException.Message,
                            Is.EqualTo(original.InnerException.Message + "--- NETWORK ---\r\n" + original.StackTrace));
            else
                Assert.That(decoded.InnerException.Message, Is.EqualTo(original.StackTrace));
        }

        [Test]
        public void Trivial()
        {
            DoTest(new Exception());
        }

        [Test]
        public void Simple()
        {
            DoTest(new Exception("Some message."));
        }

        [Test]
        public void UnicodeMessage()
        {
            DoTest(new Exception("Неожиданный символ \"愛\"."));
        }

        [Test]
        public void StandartExceptions()
        {
            DoTest(new NullReferenceException("This thing is null!"));
            DoTest(new InvalidOperationException("Doing wrong things is bad."));
            DoTest(new InvalidDataException("Too bad data."));
            DoTest(new DivideByZeroException("Does this ever happen."));
        }

        public class MyCustomException : Exception
        {
            public MyCustomException(string message) : base(message) { }
            public MyCustomException(string message, Exception innerException) : base(message, innerException) { }
        }

        [Test]
        public void CustomType()
        {
            DoTest(new MyCustomException("My awesome exception."));
        }

        [Test]
        public void Indirect()
        {
            DoTest(new InvalidOperationException("asdasd", new InvalidOperationException("qweqwe")));
        }
    }
}
