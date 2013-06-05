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
            il.Emit(OpCodes.Brtrue, contractIsNotNullLabel);        //     goto contractIsNotNullLabel

            // Contract is null branch
            il.Emit_Ldc_I4(sizeof(int));                            // stack_0 = sizeof(int)
            il.Emit(OpCodes.Br, endOfSubmethodLabel);               // goto endOfSubmethodLabel

            // Contract is not null branch
            il.MarkLabel(contractIsNotNullLabel);                   // label contractIsNotNullLabel
            il.Emit_Ldc_I4(fixedPartOfSize);                        // stack_0 = fixedPartOfSize
            foreach (var memberInfo in memberInfos.Skip(numFixedProperties))
            {
                memberInfo.Codec.EmitCalculateSize(il,              // stack_0 += sizeof member_i
                    emitLoad, memberInfo.Property.GetGetMethod());
                il.Emit(OpCodes.Add);
            }
            il.MarkLabel(endOfSubmethodLabel);                      // label endOfSubmethodLabel
        }

        public void EmitEncode(ILGenerator il, ILocalVariableCollection locals, Action<ILGenerator> emitLoad)
        {
            var valueIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            emitLoad(il);
            il.Emit(OpCodes.Brtrue, valueIsNotNullLabel);

            // Value is null branch
            il.Emit(OpCodes.Ldloc, locals.DataPointer);              // *(int*) data = -1
            il.Emit_Ldc_I4(0);
            il.Emit(OpCodes.Stind_I4);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));
            il.Emit(OpCodes.Br, endOfSubmethodLabel);

            // Value is not null branch
            il.MarkLabel(valueIsNotNullLabel);
            il.Emit(OpCodes.Ldloc, locals.DataPointer);              // *(int*) data = 1
            il.Emit_Ldc_I4(1);
            il.Emit(OpCodes.Stind_I4);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));
            foreach (var memberInfo in memberInfos)
            {
                var propertyGetter = memberInfo.Property.GetGetMethod();
                memberInfo.Codec.EmitEncode(il, locals, emitLoad, propertyGetter);
            }
            il.MarkLabel(endOfSubmethodLabel);
        }

        private static readonly MethodInfo GetTypeFromHandleMethod = typeof(Type).GetMethod("GetTypeFromHandle");
        private static readonly MethodInfo GetUninitializedObject = typeof(FormatterServices).GetMethod("GetUninitializedObject");

        public void EmitDecode(ILGenerator il, ILocalVariableCollection locals, bool doNotCheckBounds)
        {
            var resultIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            if (!doNotCheckBounds)
            {
                var canReadFlagLabel = il.DefineLabel();
                il.Emit(OpCodes.Ldloc, locals.RemainingBytes);          // if (remainingBytes >= sizeof(int))
                il.Emit_Ldc_I4(sizeof(int));                            //     goto canReadSizeLabel
                il.Emit(OpCodes.Bge, canReadFlagLabel);
                il.Emit_ThrowUnexpectedEndException();                  // throw new InvalidDataException("...")
                il.MarkLabel(canReadFlagLabel);                         // label canReadSizeLabel
            }

            var flagVar = locals.GetOrAdd("existanceFlag",
                lil => lil.DeclareLocal(typeof(int)));

            il.Emit(OpCodes.Ldloc, locals.DataPointer);                 // if (*(int*) data)
            il.Emit(OpCodes.Ldind_I4);                                  //     goto resultIsNotNullLabel
            il.Emit(OpCodes.Stloc, flagVar);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));
            il.Emit_DecreaseInteger(locals.RemainingBytes, sizeof(int));
            il.Emit(OpCodes.Ldloc, flagVar);
            il.Emit(OpCodes.Brtrue, resultIsNotNullLabel);

            // Result is null branch
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Br, endOfSubmethodLabel);

            // Result is not null branch
            il.MarkLabel(resultIsNotNullLabel);
            il.Emit(OpCodes.Ldtoken, type);
            il.Emit(OpCodes.Call, GetTypeFromHandleMethod);
            il.Emit(OpCodes.Call, GetUninitializedObject);
            il.Emit(OpCodes.Castclass, type);

            foreach (var memberInfo in memberInfos)
            {
                il.Emit(OpCodes.Dup);
                memberInfo.Codec.EmitDecode(il, locals, doNotCheckBounds);
                il.Emit(OpCodes.Call, memberInfo.Property.GetSetMethod());
            }

            il.MarkLabel(endOfSubmethodLabel);
        }
    }
}