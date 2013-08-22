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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace SharpRpc.Codecs
{
    public abstract class StructuralCodecBase<TMember> : IEmittingCodec where TMember : MemberInfo
    {
        struct DataMemberInfo
        {
            public IEmittingCodec Codec;
            public TMember Member;
        }

        private readonly Type type;
        private readonly DataMemberInfo[] memberInfos;
        private readonly int numFixedProperties;
        private readonly int fixedPartOfSize;
        private readonly int? maxSize;

        public int? FixedSize { get { return CanBeNull || !maxSize.HasValue || maxSize.Value != fixedPartOfSize ? (int?)null : fixedPartOfSize; } }
        public int? MaxSize { get { return maxSize; } }

        public StructuralCodecBase(Type type, ICodecContainer codecContainer, bool doNotCalculateMaxSize = false)
        {
            this.type = type;
            memberInfos = EnumerateMembers(type)
                .Select(x => new DataMemberInfo
                    {
                        Codec = codecContainer.GetEmittingCodecFor(GetMemberType(x)),
                        Member = x
                    })
                .OrderBy(x => x.Codec.FixedSize.HasValue ? 0 : x.Codec.MaxSize.HasValue ? 1 : 2)
                .ThenBy(x => x.Member.Name)
                .ToArray();
            numFixedProperties = memberInfos.IndexOfFirst(x => !x.Codec.FixedSize.HasValue, memberInfos.Length);
            fixedPartOfSize = memberInfos.Take(numFixedProperties).Sum(x => x.Codec.FixedSize.Value);
            if (CanBeNull)
                fixedPartOfSize += sizeof(int);
            maxSize = doNotCalculateMaxSize 
                ? null 
                : memberInfos.All(x => x.Codec.MaxSize.HasValue) ? memberInfos.Sum(x => x.Codec.MaxSize) : null;
        }

        protected abstract IEnumerable<TMember> EnumerateMembers(Type structuralType);
        protected abstract Type GetMemberType(TMember member);
        protected abstract void EmitLoadMember(ILGenerator il, Action<ILGenerator> emitLoad, TMember member);
        protected abstract void EmitSetMember(ILGenerator il, TMember member);

        private bool CanBeNull { get { return !type.IsValueType; } }

        public void EmitCalculateSize(ILGenerator il, Action<ILGenerator> emitLoad)
        {
            var contractIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            if (CanBeNull)
            {
                emitLoad(il);                                                   // if (value)
                il.Emit(OpCodes.Brtrue, contractIsNotNullLabel);                //     goto contractIsNotNullLabel

                il.Emit_Ldc_I4(sizeof(int));                                    // stack_0 = sizeof(int)
                il.Emit(OpCodes.Br, endOfSubmethodLabel);                       // goto endOfSubmethodLabel

                il.MarkLabel(contractIsNotNullLabel);                           // label contractIsNotNullLabel
            }
            
            il.Emit_Ldc_I4(fixedPartOfSize);                                    // stack_0 = fixedPartOfSize
            foreach (var memberInfo in memberInfos.Skip(numFixedProperties))    // foreach (member)
            {
                var memberVar = il.DeclareLocal(                                //     stack_0 += sizeof member
                    GetMemberType(memberInfo.Member));
                EmitLoadMember(il, emitLoad, memberInfo.Member);
                il.Emit(OpCodes.Stloc, memberVar);
                memberInfo.Codec.EmitCalculateSize(il, memberVar);          
                il.Emit(OpCodes.Add);
            }
            il.MarkLabel(endOfSubmethodLabel);                                  // label endOfSubmethodLabel
        }

        public void EmitEncode(ILGenerator il, ILocalVariableCollection locals, Action<ILGenerator> emitLoad)
        {
            var valueIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            if (CanBeNull)
            {
                emitLoad(il);                                               // if (value)
                il.Emit(OpCodes.Brtrue, valueIsNotNullLabel);               //     goto valueIsNotNullLabel

                il.Emit(OpCodes.Ldloc, locals.DataPointer);                 // *(int*) data = 0
                il.Emit_Ldc_I4(0);
                il.Emit(OpCodes.Stind_I4);
                il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));   // data += sizeof(int)
                il.Emit(OpCodes.Br, endOfSubmethodLabel);                   // goto endOfSubmethodLabel

                il.MarkLabel(valueIsNotNullLabel);                          // goto valueIsNotNullLabel
                il.Emit(OpCodes.Ldloc, locals.DataPointer);                 // *(int*) data = 1
                il.Emit_Ldc_I4(1);
                il.Emit(OpCodes.Stind_I4);
                il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));   // data += sizeof(int)
            }
            
            foreach (var memberInfo in memberInfos)                         // foreach (member)
            {
                var memberVar = il.DeclareLocal(                            //     encode(value.member)
                    GetMemberType(memberInfo.Member));
                EmitLoadMember(il, emitLoad, memberInfo.Member);
                il.Emit(OpCodes.Stloc, memberVar);
                memberInfo.Codec.EmitEncode(il, locals, memberVar);
            }
            il.MarkLabel(endOfSubmethodLabel);                              // label endOfSubmethodLabel
        }

        private static readonly MethodInfo GetTypeFromHandleMethod = typeof(Type).GetMethod("GetTypeFromHandle");
        private static readonly MethodInfo GetUninitializedObject = typeof(FormatterServices).GetMethod("GetUninitializedObject");

        public void EmitDecode(ILGenerator il, ILocalVariableCollection locals, bool doNotCheckBounds)
        {
            var resultIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            if (CanBeNull)
            {
                if (!doNotCheckBounds)
                {
                    var canReadFlagLabel = il.DefineLabel();
                    il.Emit(OpCodes.Ldloc, locals.RemainingBytes);              // if (remainingBytes >= sizeof(int))
                    il.Emit_Ldc_I4(sizeof(int));                                //     goto canReadFlagLabel
                    il.Emit(OpCodes.Bge, canReadFlagLabel);
                    il.Emit_ThrowUnexpectedEndException();                      // throw new InvalidDataException("...")
                    il.MarkLabel(canReadFlagLabel);                             // label canReadFlagLabel
                }

                var flagVar = il.DeclareLocal(typeof(int));

                il.Emit(OpCodes.Ldloc, locals.DataPointer);                     // flag = *(int*) data
                il.Emit(OpCodes.Ldind_I4);
                il.Emit(OpCodes.Stloc, flagVar);
                il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));       // data += sizeof(int)
                il.Emit_DecreaseInteger(locals.RemainingBytes, sizeof(int));    // remainingBytes -= sizeof(int)

                il.Emit(OpCodes.Ldloc, flagVar);                                // if (flag)
                il.Emit(OpCodes.Brtrue, resultIsNotNullLabel);                  //     goto resultIsNotNullLabel

                il.Emit(OpCodes.Ldnull);                                        // stack_0 = null
                il.Emit(OpCodes.Br, endOfSubmethodLabel);                       // goto endOfSubmethodLabel

                il.MarkLabel(resultIsNotNullLabel);                             // label resultIsNotNullLabel
            }

            var thisVar = il.DeclareLocal(type);

            if (CanBeNull)
            {
                il.Emit(OpCodes.Ldtoken, type);                                 // stack_0 = (T)FormatterServices.GetUninitializedObject(typeof(T))
                il.Emit(OpCodes.Call, GetTypeFromHandleMethod);
                il.Emit(OpCodes.Call, GetUninitializedObject);
                il.Emit(OpCodes.Castclass, type);
                il.Emit(OpCodes.Stloc, thisVar);
            }
            else
            {
                il.Emit(OpCodes.Ldloca, thisVar);
                il.Emit(OpCodes.Initobj, type);
            }

            foreach (var memberInfo in memberInfos)                             // foreach (member)
            {
                il.Emit(CanBeNull ? OpCodes.Ldloc : OpCodes.Ldloca, thisVar);   //     stack_0.member = decode()
                memberInfo.Codec.EmitDecode(il, locals, doNotCheckBounds);
                EmitSetMember(il, memberInfo.Member);
            }

            il.Emit(OpCodes.Ldloc, thisVar);
            il.MarkLabel(endOfSubmethodLabel);                                  // label endOfSubmethodLabel
        }
    }
}
