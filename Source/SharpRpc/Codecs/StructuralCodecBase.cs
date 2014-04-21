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
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using SharpRpc.Utility;

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
        private readonly bool canBeInlined;
        private readonly int encodingComplexity;

        public Type Type { get { return type; } }
        public int? FixedSize { get { return CanBeNull || !maxSize.HasValue || maxSize.Value != fixedPartOfSize ? (int?)null : fixedPartOfSize; } }
        public int? MaxSize { get { return maxSize; } }
        public bool CanBeInlined { get { return canBeInlined; } }
        public int EncodingComplexity { get { return encodingComplexity; } }

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
            canBeInlined = memberInfos.All(x => IsMemberPublic(x.Member) && x.Codec.CanBeInlined);
            encodingComplexity = memberInfos.Sum(x => x.Codec.EncodingComplexity);
        }

        protected abstract bool IsMemberPublic(TMember member);
        protected abstract IEnumerable<TMember> EnumerateMembers(Type structuralType);
        protected abstract Type GetMemberType(TMember member);
        protected abstract void EmitLoadMember(MyILGenerator il, Action<MyILGenerator> emitLoad, TMember member);
        protected abstract void EmitSetMember(MyILGenerator il, TMember member);

        private bool CanBeNull { get { return !type.IsValueType; } }

        public void EmitCalculateSize(IEmittingContext context, Action<MyILGenerator> emitLoad)
        {
            var il = context.IL;

            var contractIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            if (CanBeNull)
            {
                emitLoad(il);                                                   // if (value)
                il.Brtrue(contractIsNotNullLabel);                              //     goto contractIsNotNullLabel

                il.Ldc_I4(sizeof(int));                                         // stack_0 = sizeof(int)
                il.Br(endOfSubmethodLabel);                                     // goto endOfSubmethodLabel

                il.MarkLabel(contractIsNotNullLabel);                           // label contractIsNotNullLabel
            }
            
            il.Ldc_I4(fixedPartOfSize);                                         // stack_0 = fixedPartOfSize
            foreach (var memberInfo in memberInfos.Skip(numFixedProperties))    // foreach (member)
            {
                var memberVar = il.DeclareLocal(                                //     stack_0 += sizeof member
                    GetMemberType(memberInfo.Member));
                EmitLoadMember(il, emitLoad, memberInfo.Member);
                il.Stloc(memberVar);
                memberInfo.Codec.EmitCalculateSize(context, memberVar);          
                il.Add();
            }
            il.MarkLabel(endOfSubmethodLabel);                                  // label endOfSubmethodLabel
        }

        public void EmitEncode(IEmittingContext context, Action<MyILGenerator> emitLoad)
        {
            var il = context.IL;

            var valueIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            if (CanBeNull)
            {
                emitLoad(il);                                               // if (value)
                il.Brtrue(valueIsNotNullLabel);                             //     goto valueIsNotNullLabel

                il.Ldloc(context.DataPointerVar);                           // *(int*) data = 0
                il.Ldc_I4(0);
                il.Stind_I4();
                il.IncreasePointer(context.DataPointerVar, sizeof(int));    // data += sizeof(int)
                il.Br(endOfSubmethodLabel);                                 // goto endOfSubmethodLabel

                il.MarkLabel(valueIsNotNullLabel);                          // goto valueIsNotNullLabel
                il.Ldloc(context.DataPointerVar);                           // *(int*) data = 1
                il.Ldc_I4(1);
                il.Stind_I4();
                il.IncreasePointer(context.DataPointerVar, sizeof(int));    // data += sizeof(int)
            }
            
            foreach (var memberInfo in memberInfos)                         // foreach (member)
            {
                var memberVar = il.DeclareLocal(                            //     encode(value.member)
                    GetMemberType(memberInfo.Member));
                EmitLoadMember(il, emitLoad, memberInfo.Member);
                il.Stloc(memberVar);
                memberInfo.Codec.EmitEncode(context, memberVar);
            }
            il.MarkLabel(endOfSubmethodLabel);                              // label endOfSubmethodLabel
        }

        private static readonly MethodInfo GetTypeFromHandleMethod = typeof(Type).GetMethod("GetTypeFromHandle");
        private static readonly MethodInfo GetUninitializedObject = typeof(FormatterServices).GetMethod("GetUninitializedObject");

        public void EmitDecode(IEmittingContext context, bool doNotCheckBounds)
        {
            var il = context.IL;

            var resultIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            if (CanBeNull)
            {
                if (!doNotCheckBounds)
                {
                    var canReadFlagLabel = il.DefineLabel();
                    il.Ldloc(context.RemainingBytesVar);                    // if (remainingBytes >= sizeof(int))
                    il.Ldc_I4(sizeof(int));                                 //     goto canReadFlagLabel
                    il.Bge(canReadFlagLabel);
                    il.ThrowUnexpectedEndException();                       // throw new InvalidDataException("...")
                    il.MarkLabel(canReadFlagLabel);                         // label canReadFlagLabel
                }

                var flagVar = il.DeclareLocal(typeof(int));

                il.Ldloc(context.DataPointerVar);                           // flag = *(int*) data
                il.Ldind_I4();
                il.Stloc(flagVar);
                il.IncreasePointer(context.DataPointerVar, sizeof(int));    // data += sizeof(int)
                il.DecreaseInteger(context.RemainingBytesVar, sizeof(int)); // remainingBytes -= sizeof(int)

                il.Ldloc(flagVar);                                          // if (flag)
                il.Brtrue(resultIsNotNullLabel);                            //     goto resultIsNotNullLabel

                il.Ldnull();                                                // stack_0 = null
                il.Br(endOfSubmethodLabel);                                 // goto endOfSubmethodLabel

                il.MarkLabel(resultIsNotNullLabel);                         // label resultIsNotNullLabel
            }

            var thisVar = il.DeclareLocal(type);

            if (CanBeNull)
            {
                il.Ldtoken(type);                                           // stack_0 = (T)FormatterServices.GetUninitializedObject(typeof(T))
                il.Call(GetTypeFromHandleMethod);
                il.Call(GetUninitializedObject);
                il.Castclass(type);
                il.Stloc(thisVar);
            }
            else
            {
                il.Ldloca(thisVar);
                il.Initobj(type);
            }

            foreach (var memberInfo in memberInfos)                         // foreach (member)
            {
                if (CanBeNull)                                              //     stack_0.member = decode()
                    il.Ldloc(thisVar);
                else
                    il.Ldloca(thisVar);
                memberInfo.Codec.EmitDecode(context, doNotCheckBounds);
                EmitSetMember(il, memberInfo.Member);
            }

            il.Ldloc(thisVar);
            il.MarkLabel(endOfSubmethodLabel);                              // label endOfSubmethodLabel
        }
    }
}
