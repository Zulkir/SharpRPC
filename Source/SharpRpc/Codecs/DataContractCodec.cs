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
using SharpRpc.Reflection;

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
        private readonly int fixedPartOfSize;
        private readonly int? maxSize;

        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return maxSize; } }

        public DataContractCodec(Type type, ICodecContainer codecContainer)
        {
            this.type = type;
            memberInfos = type.EnumerateDataMembers()
                .Select(x => new DataMemberInfo
                    {
                        Codec = codecContainer.GetEmittingCodecFor(x.PropertyType),
                        Property = x
                    })
                .OrderBy(x => x.Codec.FixedSize.HasValue ? 0 : x.Codec.MaxSize.HasValue ? 1 : 2)
                .ThenBy(x => x.Property.Name)
                .ToArray();
            numFixedProperties = memberInfos.IndexOfFirst(x => !x.Codec.FixedSize.HasValue, memberInfos.Length);
            fixedPartOfSize = sizeof(int) + memberInfos.Take(numFixedProperties).Sum(x => x.Codec.FixedSize.Value);
            maxSize = memberInfos.All(x => x.Codec.MaxSize.HasValue) ? memberInfos.Sum(x => x.Codec.MaxSize) : null;
        }

        public void EmitCalculateSize(ILGenerator il, Action<ILGenerator> emitLoad)
        {
            var contractIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            emitLoad(il);                                                   // if (value)
            il.Emit(OpCodes.Brtrue, contractIsNotNullLabel);                //     goto contractIsNotNullLabel

            // Contract is null branch
            il.Emit_Ldc_I4(sizeof(int));                                    // stack_0 = sizeof(int)
            il.Emit(OpCodes.Br, endOfSubmethodLabel);                       // goto endOfSubmethodLabel

            // Contract is not null branch
            il.MarkLabel(contractIsNotNullLabel);                           // label contractIsNotNullLabel
            il.Emit_Ldc_I4(fixedPartOfSize);                                // stack_0 = fixedPartOfSize
            foreach (var memberInfo in memberInfos.Skip(numFixedProperties))
            {
                memberInfo.Codec.EmitCalculateSize(il,                      // stack_0 += sizeof member_i
                    emitLoad, memberInfo.Property.GetGetMethod(true));
                il.Emit(OpCodes.Add);
            }
            il.MarkLabel(endOfSubmethodLabel);                              // label endOfSubmethodLabel
        }

        public void EmitEncode(ILGenerator il, ILocalVariableCollection locals, Action<ILGenerator> emitLoad)
        {
            var valueIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            emitLoad(il);                                                   // if (value)
            il.Emit(OpCodes.Brtrue, valueIsNotNullLabel);                   //     goto valueIsNotNullLabel

            // Value is null branch
            il.Emit(OpCodes.Ldloc, locals.DataPointer);                     // *(int*) data = 0
            il.Emit_Ldc_I4(0);
            il.Emit(OpCodes.Stind_I4);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));       // data += sizeof(int)
            il.Emit(OpCodes.Br, endOfSubmethodLabel);                       // goto endOfSubmethodLabel

            // Value is not null branch
            il.MarkLabel(valueIsNotNullLabel);                              // goto valueIsNotNullLabel
            il.Emit(OpCodes.Ldloc, locals.DataPointer);                     // *(int*) data = 1
            il.Emit_Ldc_I4(1);
            il.Emit(OpCodes.Stind_I4);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));       // data += sizeof(int)
            foreach (var memberInfo in memberInfos)                         // foreach (Prop)
            {                                                               // {
                var propertyGetter = memberInfo.Property.GetGetMethod(true);//     encode(value.Prop)
                memberInfo.Codec.EmitEncode(
                    il, locals, emitLoad, propertyGetter);
            }                                                               // }
            il.MarkLabel(endOfSubmethodLabel);                              // label endOfSubmethodLabel
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
                il.Emit(OpCodes.Ldloc, locals.RemainingBytes);                  // if (remainingBytes >= sizeof(int))
                il.Emit_Ldc_I4(sizeof(int));                                    //     goto canReadFlagLabel
                il.Emit(OpCodes.Bge, canReadFlagLabel);
                il.Emit_ThrowUnexpectedEndException();                          // throw new InvalidDataException("...")
                il.MarkLabel(canReadFlagLabel);                                 // label canReadFlagLabel
            }

            var flagVar = locals.GetOrAdd("existanceFlag",
                lil => lil.DeclareLocal(typeof(int)));

            il.Emit(OpCodes.Ldloc, locals.DataPointer);                         // flag = *(int*) data
            il.Emit(OpCodes.Ldind_I4);
            il.Emit(OpCodes.Stloc, flagVar);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));           // data += sizeof(int)
            il.Emit_DecreaseInteger(locals.RemainingBytes, sizeof(int));        // remainingBytes -= sizeof(int)
            il.Emit(OpCodes.Ldloc, flagVar);                                    // if (flag)
            il.Emit(OpCodes.Brtrue, resultIsNotNullLabel);                      //     goto resultIsNotNullLabel

            // Result is null branch
            il.Emit(OpCodes.Ldnull);                                            // stack_0 = null
            il.Emit(OpCodes.Br, endOfSubmethodLabel);                           // goto endOfSubmethodLabel

            // Result is not null branch
            il.MarkLabel(resultIsNotNullLabel);                                 // label resultIsNotNullLabel
            il.Emit(OpCodes.Ldtoken, type);                                     // stack_0 = (T)FormatterServices.GetUninitializedObject(typeof(T))
            il.Emit(OpCodes.Call, GetTypeFromHandleMethod);
            il.Emit(OpCodes.Call, GetUninitializedObject);
            il.Emit(OpCodes.Castclass, type);

            foreach (var memberInfo in memberInfos)                             // foreach (Prop)
            {                                                                   // {
                il.Emit(OpCodes.Dup);                                           //     stack_0.Prop = decode()
                memberInfo.Codec.EmitDecode(il, locals, doNotCheckBounds);
                il.Emit(OpCodes.Call, memberInfo.Property.GetSetMethod(true));
            }                                                                   // }

            il.MarkLabel(endOfSubmethodLabel);                                  // label endOfSubmethodLabel
        }
    }
}