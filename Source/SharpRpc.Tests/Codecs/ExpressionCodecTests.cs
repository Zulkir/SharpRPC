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
using System.Linq.Expressions;
using NUnit.Framework;
using SharpRpc.Codecs;
using SharpRpc.Codecs.Expressions;

namespace SharpRpc.Tests.Codecs
{
    public class ExpressionCodecTests : CodecTestsBase
    {
        public class MyClass
        {
            public MyClass()
            {
                
            }

            public MyClass(int a, string b)
            {
                A = a;
                B = b;
            }

            public int A { get; set; } 
            public string B { get; set; }
            public MyClass Next { get; set; } 
        }

        private ExpressionCodec codec;

        public override void Setup()
        {
            base.Setup();
            codec = new ExpressionCodec(CodecContainer);
        }

        private void DoTest<T>(Expression<Func<T>> expression)
        {
            DoTest((Expression)expression);
        }

        private void DoTest<T1, T2>(Expression<Func<T1, T2>> expression)
        {
            DoTest((Expression)expression);
        }

        private void DoTest(Expression expression)
        {
            var data = codec.EncodeSingle(expression);
            var decoded = codec.DecodeSingle(data);
            Assert.That(decoded.ToString(), Is.EqualTo(expression.ToString()));
            Console.WriteLine(decoded);
        }

        [Test]
        public void Constant()
        {
            DoTest(() => 123);
            DoTest(() => 123.234);
            DoTest(() => "For the Horde!!!");
        }

        [Test]
        public void Unary()
        {
            DoTest<int, int>(x => -x);
            DoTest<bool, bool>(x => !x);
        }

        [Test]
        public void Binary()
        {
            DoTest<int, int>(x => x + 1);
            DoTest<double, double>(x => 123.234 * x);
        }

        [Test]
        public void MethodCall()
        {
            DoTest(() => int.Parse("123"));
            DoTest<int, string>(x => x.ToString());
            DoTest<int, bool>(x => x.Equals(123));
        }

        [Test]
        public void Conditional()
        {
            DoTest(() => double.IsNaN(123) ? "strange" : "fine");
            DoTest<int, int>(x => x >= 0 ? x : -x);
        }

        [Test]
        public void Invocation()
        {
            DoTest<Func<bool>, bool>(x => !x());
            DoTest<Func<int>, string>(x => x().ToString());
        }

        [Test]
        public void New()
        {
            DoTest(() => new object());
            DoTest(() => new Random(123));
            DoTest(() => new MyClass(123, "asdasd"));
        }

        [Test]
        public void MemberInit()
        {
            DoTest(() => new MyClass { A = 123, B = "asd" });
            DoTest(() => new MyClass { A = 123, B = "asd", Next = new MyClass { A = 234, B = "qwe"}});
        }

        [Test]
        public void MemberAccess()
        {
            DoTest(() => IntPtr.Size);
            DoTest<MyClass, int>(x => x.A);
        }

        [Test]
        public void NewArrayBounds()
        {
            DoTest(() => new int[123]);
            DoTest(() => new int[123, 234]);
        }

        [Test]
        public void NewArrayinit()
        {
            DoTest(() => new[] { 1, 2, 3 });
            DoTest(() => new byte[] { 1, 2, 3 });
            DoTest(() => new [] { new MyClass(123, "asd") });
        }
    }
}