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

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Runtime.Serialization;

namespace SharpRpc.Codecs
{
    public class DataContractCodec : IEmittingCodec
    {
        struct DataMemberInfo
        {
            public IEmittingCodec Codec;
            public PropertyInfo Property;
        }

        private readonly Type type;
        private readonly DataMemberInfo[] memberInfos;
        private readonly int numFixedProperties;
        private readonly int numLimitedProperties;
        private readonly int fixedPartOfSize;

        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return memberInfos.Length == numFixedProperties ? (int?)fixedPartOfSize : null; } }

        public DataContractCodec(Type type, ICodecContainer codecContainer)
        {
            this.type = type;
            memberInfos = type.GetProperties()
                .Where(x => x.GetCustomAttributes(typeof(DataMemberAttribute), true).Any())
                .Select(x => new DataMemberInfo
                    {
                        Codec = codecContainer.GetEmittingCodecFor(x.PropertyType),
                        Property = x
                    })
                .OrderBy(x => x.Codec.FixedSize.HasValue ? 0 : x.Codec.MaxSize.HasValue ? 1 : 2)
                .ThenBy(x => x.Property.Name)
                .ToArray();
            numFixedProperties = memberInfos.IndexOfFirst(x => !x.Codec.FixedSize.HasValue, memberInfos.Length);
            numLimitedProperties = memberInfos.IndexOfFirst(x => !x.Codec.MaxSize.HasValue, memberInfos.Length) - numFixedProperties;
            fixedPartOfSize = sizeof(int) + memberInfos.Take(numFixedProperties).Sum(x => x.Codec.FixedSize.Value);
        }

        public void EmitCalculateSize(ILGenerator il, Action<ILGenerator> emitLoad)
        {
            var contractIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            emitLoad(il);                                           // if (value)
            il.Emit(OpCodes.Brtrue, contractIsNotNullLabel);        //     goto stringIsNotNullLabel

            // Contract is null branch
            il.Emit_Ldc_I4(sizeof(int));                            // stack_0 = sizeof(int)
            il.Emit(OpCodes.Br, endOfSubmethodLabel);               // goto endOfSubmethodLabel

            // Contract is not null branch
            il.MarkLabel(contractIsNotNullLabel);                   // label stringIsNotNullLabel
            il.Emit_Ldc_I4(fixedPartOfSize);                        // stack_0 = fixedPartOfSize
            foreach (var memberInfo in memberInfos.Skip(numFixedProperties))
            {
                memberInfo.Codec.EmitCalculateSize(il,              // stack_0 += sizeof member_i
                    emitLoad, memberInfo.Property.GetGetMethod());
                il.Emit(OpCodes.Add);
            }
            il.MarkLabel(endOfSubmethodLabel);
        }

        public void EmitEncode(ILGenerator il, ILocalVariableCollection locals, Action<ILGenerator> emitLoad)
        {
            foreach (var memberInfo in memberInfos)
            {
                var propertyGetter = memberInfo.Property.GetGetMethod();
                emitLoad(il);
                il.Emit(OpCodes.Call, propertyGetter);
                memberInfo.Codec.EmitEncode(il, locals, emitLoad, propertyGetter);
            }
        }

        public void EmitDecode(ILGenerator il, ILocalVariableCollection locals, bool doNotCheckBounds)
        {
            throw new NotImplementedException();
        }
    }
}