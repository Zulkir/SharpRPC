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
using System.IO;
using NUnit.Framework;
using SharpRpc.Settings;

namespace SharpRpc.Tests
{
    [TestFixture]
    public class HostSettingsParserTests
    {
        #region Nested Classes
        public interface IMyService1 { }
        public interface IMyService2 { }
        public interface IMyService3 { }
        public interface IMyService4 { }
        public class MyService1 { }
        public class MyService2 { }
        public class MyService3 { }
        public class MyService4 { }
        #endregion

        private HostSettingsParser parser;

        [SetUp]
        public void Setup()
        {
            parser = new HostSettingsParser();
        }

        [Test]
        public void InvalidText()
        {
            Assert.Throws<ArgumentNullException>(() => parser.Parse(null));
            Assert.Throws<InvalidDataException>(() => parser.Parse(""));
        }

        [Test]
        public void Pairs()
        {
            var text = string.Format(@"qwe://asd:123
                            
                         {0} {1}     {2}

                         {0}  {3} {0}       {4}
                {0}     {5}              {6}
                    {0}     {7}      {0} {8}

                            ",
                 "SharpRpc.Tests", 
                 typeof(IMyService1).FullName, typeof(MyService1).FullName,
                 typeof(IMyService2).FullName, typeof(MyService2).FullName,
                 typeof(IMyService3).FullName, typeof(MyService3).FullName,
                 typeof(IMyService4).FullName, typeof(MyService4).FullName);

            var hostSettings = parser.Parse(text);
            Assert.That(hostSettings.GetInterfaceImplementationsPairs(), Is.EquivalentTo(new []
                {
                    new InterfaceImplementationTypePair(typeof(IMyService1), typeof(MyService1)),
                    new InterfaceImplementationTypePair(typeof(IMyService2), typeof(MyService2)),
                    new InterfaceImplementationTypePair(typeof(IMyService3), typeof(MyService3)),
                    new InterfaceImplementationTypePair(typeof(IMyService4), typeof(MyService4))
                }));
        }

        [Test]
        public void UnknownTypes()
        {
            const string text = @"qwe://asd:123
                         BADASSEMBLY BADINTERFACE BADTYPE";
            Assert.Throws<InvalidDataException>(() => parser.Parse(text));
        }
    }
}
