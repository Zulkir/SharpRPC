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
using NUnit.Framework;
using SharpRpc.Reflection;

namespace SharpRpc.Tests.InterfaceReflection
{
    [TestFixture]
    public class MethodDescriptionBuilderTests
    {
        private MethodDescriptionBuilder builder;
        private readonly Type type = typeof(ITest);

        interface ITest
        {
            void Trivial();
            int GetInt();
            void ProcessBasicTypesAndStructs(int a, bool b, char c, double d);
            void ProcessClassesAndInterfaces(string c, ITest i);
            void ModifyVariables(ref int a, ref string b);
            void GetManySomething(out int a, out string b);
            void GenericMethod<TFirst, TSecond>(TFirst first, TSecond second);
        }

        [SetUp]
        public void Setup()
        {
            builder = new MethodDescriptionBuilder();
        }

        [Test]
        public void Trivial()
        {
            var desc = builder.Build(type.GetMethod("Trivial"));
            Assert.That(desc.MethodInfo, Is.EqualTo(type.GetMethod("Trivial")));
            Assert.That(desc.ReturnType, Is.EqualTo(typeof(void)));
            Assert.That(desc.Name, Is.EqualTo("Trivial"));
            Assert.That(desc.Parameters, Is.Empty);
        }

        [Test]
        public void ReturnType()
        {
            var desc = builder.Build(type.GetMethod("GetInt"));
            Assert.That(desc.ReturnType, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void SimpleParams()
        {
            var desc = builder.Build(type.GetMethod("ProcessBasicTypesAndStructs"));
            Assert.That(desc.Parameters.Count, Is.EqualTo(4));
            for (int i = 0; i < desc.Parameters.Count; i++)
                Assert.That(desc.Parameters[i].Index, Is.EqualTo(i));
            Assert.That(desc.Parameters[0].Type, Is.EqualTo(typeof(int)));
            Assert.That(desc.Parameters[1].Type, Is.EqualTo(typeof(bool)));
            Assert.That(desc.Parameters[2].Type, Is.EqualTo(typeof(char)));
            Assert.That(desc.Parameters[3].Type, Is.EqualTo(typeof(double)));
            Assert.That(desc.Parameters[0].Name, Is.EqualTo("a"));
            Assert.That(desc.Parameters[1].Name, Is.EqualTo("b"));
            Assert.That(desc.Parameters[2].Name, Is.EqualTo("c"));
            Assert.That(desc.Parameters[3].Name, Is.EqualTo("d"));
            Assert.That(desc.Parameters[0].Way, Is.EqualTo(MethodParameterWay.Val));
            Assert.That(desc.Parameters[1].Way, Is.EqualTo(MethodParameterWay.Val));
            Assert.That(desc.Parameters[2].Way, Is.EqualTo(MethodParameterWay.Val));
            Assert.That(desc.Parameters[3].Way, Is.EqualTo(MethodParameterWay.Val));
        }

        [Test]
        public void ProcessComplex()
        {
            var desc = builder.Build(type.GetMethod("ProcessClassesAndInterfaces"));
            Assert.That(desc.Parameters.Count, Is.EqualTo(2));
            Assert.That(desc.Parameters[0].Type, Is.EqualTo(typeof(string)));
            Assert.That(desc.Parameters[1].Type, Is.EqualTo(typeof(ITest)));
        }

        [Test]
        public void ProcessRefs()
        {
            var desc = builder.Build(type.GetMethod("ModifyVariables"));
            Assert.That(desc.Parameters.Count, Is.EqualTo(2));
            Assert.That(desc.Parameters[0].Type, Is.EqualTo(typeof(int)));
            Assert.That(desc.Parameters[0].Way, Is.EqualTo(MethodParameterWay.Ref));
            Assert.That(desc.Parameters[1].Type, Is.EqualTo(typeof(string)));
            Assert.That(desc.Parameters[1].Way, Is.EqualTo(MethodParameterWay.Ref));
        }

        [Test]
        public void ProcessOuts()
        {
            var desc = builder.Build(type.GetMethod("GetManySomething"));
            Assert.That(desc.Parameters.Count, Is.EqualTo(2));
            Assert.That(desc.Parameters[0].Type, Is.EqualTo(typeof(int)));
            Assert.That(desc.Parameters[0].Way, Is.EqualTo(MethodParameterWay.Out));
            Assert.That(desc.Parameters[1].Type, Is.EqualTo(typeof(string)));
            Assert.That(desc.Parameters[1].Way, Is.EqualTo(MethodParameterWay.Out));
        }

        [Test]
        public void Generic()
        {
            var desc = builder.Build(type.GetMethod("GenericMethod"));
            Assert.That(desc.GenericParameters.Count, Is.EqualTo(2));
            Assert.That(desc.GenericParameters[0].Name, Is.EqualTo("TFirst"));
            Assert.That(desc.GenericParameters[1].Name, Is.EqualTo("TSecond"));
        }
    }
}