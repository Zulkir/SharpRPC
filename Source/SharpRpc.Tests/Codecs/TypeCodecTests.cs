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
using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    public class TypeCodecTests : CodecTestsBase
    {
        private void DoTest(Type value)
        {
            DoTest(new TypeCodec(), value);
        }

        private void DoTest<T>()
        {
            DoTest(typeof(T));
        }

        [Test]
        public void Null()
        {
            DoTest(null);
        }

        [Test]
        public void Void()
        {
            DoTest(typeof(void));
        }

        [Test]
        public void System()
        {
            DoTest<int>();
            DoTest<string>();
            DoTest<DateTime>();
            DoTest<ArgumentOutOfRangeException>();
        }

        public class MyCustomType { public int A { get; set; } public string B { get; set; } }

        [Test]
        public void Custom()
        {
            DoTest<MyCustomType>();
        }

        [Test]
        public void Generic()
        {
            DoTest(typeof(Dictionary<,>));
            DoTest(typeof(Dictionary<int, string>));
            DoTest(typeof(Dictionary<MyCustomType, MyCustomType>));
        }
    }
}