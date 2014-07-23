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
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    public abstract class MemberInfoCodecTestsBase<TMemberInfo> : CodecTestsBase where TMemberInfo : MemberInfo
    {
        private IManualCodec<TMemberInfo> codec;

        protected abstract IManualCodec<TMemberInfo> CreateCodec();
        protected abstract IEnumerable<TMemberInfo> GetMembers(Type type);

        public override void Setup()
        {
            base.Setup();
            codec = CreateCodec();
        }

        private void DoTest<T>()
        {
            foreach (var info in GetMembers(typeof(T)))
                DoTest(info);
        }

        protected void DoTest(TMemberInfo info)
        {
            var data = codec.EncodeSingle(info);
            var decoded = codec.DecodeSingle(data);
            Assert.That(decoded, Is.EqualTo(info));
        }

        [Test]
        public void Null()
        {
            DoTest(null);
        }

        [Test]
        public void Omni()
        {
            DoTest<object>();
            DoTest<int>();
            DoTest<string>();
            DoTest<Expression<Func<int, string>>>();
            DoTest<Dictionary<string, int>>();
            DoTest<Task<string>>();
        }
    }
}