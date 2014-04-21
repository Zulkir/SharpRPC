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
using System.Reflection;
using SharpRpc.Reflection;
using SharpRpc.Utility;

namespace SharpRpc.Codecs
{
    public class DataContractCodec : StructuralCodecBase<PropertyInfo>
    {
        public DataContractCodec(Type type, ICodecContainer codecContainer, bool doNotCalculateMaxSize = false) 
            : base(type, codecContainer, doNotCalculateMaxSize)
        {
        }

        protected override bool IsMemberPublic(PropertyInfo member)
        {
            return member.GetGetMethod(true).IsPublic && member.GetSetMethod(true).IsPublic;
        }

        protected override IEnumerable<PropertyInfo> EnumerateMembers(Type structuralType)
        {
            return structuralType.EnumerateDataMembers();
        }

        protected override Type GetMemberType(PropertyInfo member)
        {
            return member.PropertyType;
        }

        protected override void EmitLoadMember(MyILGenerator il, Action<MyILGenerator> emitLoad, PropertyInfo member)
        {
            var propertyGetter = member.GetGetMethod(true);
            emitLoad(il);
            il.Call(propertyGetter);
        }

        protected override void EmitSetMember(MyILGenerator il, PropertyInfo member)
        {
            var propertySetter = member.GetSetMethod(true);
            il.Call(propertySetter);
        }
    }
}