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
using System.Collections.Generic;
using System.Reflection.Emit;
using NUnit.Framework;
using SharpRpc.Codecs;
using SharpRpc.Utility;

namespace SharpRpc.Tests.Codecs
{
    [TestFixture]
    public class IndirectCodecTests
    {
        private CodecContainer codecContainer;

        [SetUp]
        public void Setup()
        {
            codecContainer = new CodecContainer();
        }

        [Test]
        public void Simple()
        {
            var indirectCodec = new IndirectCodec(typeof(int));
            var manualCodecOverIndirect = new ManualCodec<int>(codecContainer, indirectCodec);

            var data = manualCodecOverIndirect.EncodeSingle(1231231231);
            var result = manualCodecOverIndirect.DecodeSingle(data);

            Assert.That(result, Is.EqualTo(1231231231));
        }

        private IEmittingContext SubstituteContext()
        {
            var dynamicMethod = new DynamicMethod("TestVoidMethod", typeof(void), Type.EmptyTypes);
            var il = new MyILGenerator(dynamicMethod.GetILGenerator());
            return new ManualCodecEmittingContext(il, 0);
        }

        [Test]
        public void GenericParameter()
        {
            var genericParameter = typeof(IEnumerable<>).GetGenericArguments()[0];
            var indirectCodec = new IndirectCodec(genericParameter);

            var context = SubstituteContext();

            indirectCodec.EmitCalculateSize(context, 0);
            indirectCodec.EmitEncode(context, 0);
            indirectCodec.EmitDecode(context, false);
        }
    }
}