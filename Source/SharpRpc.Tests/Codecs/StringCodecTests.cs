using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    [TestFixture]
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
