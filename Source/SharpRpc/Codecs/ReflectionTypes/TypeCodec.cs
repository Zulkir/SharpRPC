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
using System.Reflection;
using SharpRpc.Utility;

namespace SharpRpc.Codecs.ReflectionTypes
{
    public class TypeCodec : StringCodecBase
    {
        public TypeCodec() : base(true)
        {
            
        }

        private static readonly MethodInfo GetTypeMethod = typeof(Type).GetMethod("GetType", new[] { typeof(string) });
        private static readonly MethodInfo GetAssemblyQualifiedNameMethod = typeof(Type).GetMethod("get_AssemblyQualifiedName");

        public override Type Type { get { return typeof(Type); } }
        public override bool CanBeInlined { get { return true; } }
        public override int EncodingComplexity { get { return 1; } }

        protected override void EmitLoadAsString(MyILGenerator il, Action<MyILGenerator> emitLoad)
        {
            emitLoad(il);
            il.Callvirt(GetAssemblyQualifiedNameMethod);
        }

        protected override void EmitParseFromString(MyILGenerator il)
        {
            il.Call(GetTypeMethod);
        }
    }
}