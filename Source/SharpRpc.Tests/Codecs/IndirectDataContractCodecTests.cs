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

using System.Runtime.Serialization;
using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    public class IndirectDataContractCodecTests : CodecTestsBase
    {
        #region Contracts

        [DataContract]
        public class ContractWithPrivateFields
        {
            [DataMember] public int A { get; set; }
            [DataMember] public int B { private get; set; }
            [DataMember] public int C { get; private set; }
            [DataMember] private int D { get; set; }

            public ContractWithPrivateFields(int a, int b, int c, int d)
            {
                A = a; B = b; C = c; D = d;
            }

            public bool Equals(ContractWithPrivateFields other) { return A == other.A && B == other.B && C == other.C && D == other.D; }
            public override bool Equals(object obj) { return obj is ContractWithPrivateFields && Equals((ContractWithPrivateFields)obj); }
            public override int GetHashCode() { return 0; }
        }

        #endregion

        private ICodecContainer codecContainer;

        [SetUp]
        public void Setup()
        {
            codecContainer = new CodecContainer();
        }

        private void DoTest<T>(T value) where T : class
        {
            DoTest(new IndirectDataContractCodec(typeof(T), codecContainer), value, (o1, o2) =>
            {
                if (ReferenceEquals(o1, null))
                    Assert.That(o2, Is.Null);
                else
                    Assert.That(o2, Is.EqualTo(o1));
            });
        }

        [Test]
        public void PrivateFields()
        {
            DoTest((ContractWithPrivateFields)null);
            DoTest(new ContractWithPrivateFields(123, 234, 345, 456));
        }
    }
}