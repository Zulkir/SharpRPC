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

using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    public class StringCodecTests : CodecTestsBase
    {
        private void DoTest(string value)
        {
            DoTest(new StringCodec(), value);
        }

        [Test]
        public void Null()
        {
            DoTest(null);
        }

        [Test]
        public void Special()
        {
            DoTest("");
            DoTest("\r\n");
            DoTest("\n");
            DoTest("\t");
            DoTest("    ");
        }

        [Test]
        public void Ascii()
        {
            DoTest("simple");
            DoTest("CamelCase");
            DoTest("This is a simple sentence.");
            DoTest("testStr = ImagineSomething() + 'asd123' + __underscore + \"we love quotes\";");
        }

        [Test]
        public void Cyrillic()
        {
            DoTest("Помоги мне, Юникод!");
            DoTest("Мы не забудем букву 'ё'.");
        }

        [Test]
        public void Eastern()
        {
            DoTest("テスト");
            DoTest("サンタクロースをいつまで信じていたかなんてことはたわいのない世間話にもならないくらいのどうでもいい話だが、" +
                   "それでも俺がいつまでサンタなどという想像上の赤服ジイさんを信じていたかというと" +
                   "俺は確信をもって言えるが最初から信じてなどいなかった。");
            DoTest("キュウべえ　「僕と契約して魔法少女になってよ！　／人◕ ‿‿ ◕人＼　」");
        }

        [Test]
        public void RightToLeft()
        {
            DoTest("اللغة العربية هي لغة جيدة");
            DoTest("עברית היא שפה טובה");
        }

        [Test]
        public void Mixed()
        {
            DoTest("These tests contain text in different languages, including 日本語, עברית, русский, اللغة, and English.");
        }
    }
}
