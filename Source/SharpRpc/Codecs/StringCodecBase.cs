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
using System.Runtime.CompilerServices;
using SharpRpc.Utility;

namespace SharpRpc.Codecs
{
    public abstract class StringCodecBase : IEmittingCodec
    {
        private readonly bool canBeNull;

        protected StringCodecBase(bool canBeNull)
        {
            this.canBeNull = canBeNull;
        }

        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return null; } }

        public abstract Type Type { get; }
        public abstract bool CanBeInlined { get; }
        public abstract int EncodingComplexity { get; }
        protected abstract void EmitLoadAsString(MyILGenerator il, Action<MyILGenerator> emitLoad);
        protected abstract void EmitParseFromString(MyILGenerator il);

        static readonly MethodInfo GetLength = typeof(string).GetMethod("get_Length");
        static readonly MethodInfo GetOffsetToStringData = typeof(RuntimeHelpers).GetMethod("get_OffsetToStringData");
        static readonly ConstructorInfo NewString = typeof(string).GetConstructor(new[] { typeof(char*), typeof(int), typeof(int) });

        public void EmitCalculateSize(IEmittingContext context, Action<MyILGenerator> emitLoad)
        {
            var il = context.IL;

            var endOfSubmethodLabel = il.DefineLabel();

            if (canBeNull)
            {
                var stringIsNotNullLabel = il.DefineLabel();

                emitLoad(il);                               // if (value)
                il.Brtrue(stringIsNotNullLabel);            //     goto stringIsNotNullLabel

                // String is null branch
                il.Ldc_I4(sizeof(int));                     // stack_0 = sizeof(int)
                il.Br(endOfSubmethodLabel);                 // goto endOfSubmethodLabel

                // String is not null branch
                il.MarkLabel(stringIsNotNullLabel);         // label stringIsNotNullLabel
            }

            EmitLoadAsString(il, emitLoad);                 // stack_0 = (FormatToString(value).Length << 1) + sizeof(int)
            il.Call(GetLength);
            il.Ldc_I4(1);
            il.Shl();
            il.Ldc_I4(sizeof(int));
            il.Add();
            il.MarkLabel(endOfSubmethodLabel);              // label endOfSubmethodLabel
        }

        public void EmitEncode(IEmittingContext context, Action<MyILGenerator> emitLoad)
        {
            var il = context.IL;

            var strIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            if (canBeNull)
            {
                emitLoad(il);                                                       // if (val) goto strIsNotNullLabel
                il.Brtrue(strIsNotNullLabel);

                // String is null branch
                il.Ldloc(context.DataPointerVar);                                   // *(int*) data = -1
                il.Ldc_I4(-1);
                il.Stind_I4();
                il.IncreasePointer(context.DataPointerVar, sizeof(int));            // data += sizeof(int)
                il.Br(endOfSubmethodLabel);                                         // goto endOfEncodeLabel

                // String is not null branch
                il.MarkLabel(strIsNotNullLabel);                                    // label strIsNotNullLabel
            }

            var stringValueVar = context.GetSharedVariable<string>("stringValue");  // var stringValue = FormatToString(value)
            EmitLoadAsString(il, emitLoad);
            il.Stloc(stringValueVar);

            var tempIntegerVar = context.GetSharedVariable<int>("tempInteger");     // var tempInteger = stringValue.Length << 1
            il.Ldloc(stringValueVar);
            il.Call(GetLength);
            il.Ldc_I4(1);
            il.Shl();
            il.Stloc(tempIntegerVar);
            il.Ldloc(context.DataPointerVar);                                       // *(int*)data = tempInteger
            il.Ldloc(tempIntegerVar);
            il.Stind_I4();
            il.IncreasePointer(context.DataPointerVar, sizeof(int));                // data += sizeof(int)
            var pinnedString = il.DeclareLocal(typeof(string), true);               // var pinned pinnedString = stringValue
            il.Ldloc(stringValueVar);
            il.Stloc(pinnedString);
            il.Ldloc(pinnedString);                                                 // stack_0 = (byte*)pinnedString
            il.Conv_I();
            il.Call(GetOffsetToStringData);                                         // stack_0 = stack_0 + 
            il.Add();                                                               //     RuntimeHelpers.OffsetToStringData
            var charPointer = context.GetSharedVariable(typeof(char*), "charPointer");// charPointer = stack_0
            il.Stloc(charPointer);
            il.Ldloc(context.DataPointerVar);                                       // cpblk(data, charPointer, tempInteger)
            il.Ldloc(charPointer);
            il.Ldloc(tempIntegerVar);
            il.Cpblk();
            il.Ldnull();                                                            // pinnedString = null
            il.Stloc(pinnedString);
            il.IncreasePointer(context.DataPointerVar, tempIntegerVar);             // data += tempInteger
            il.MarkLabel(endOfSubmethodLabel);                                      // label endOfSubmethodLabel
        }

        public void EmitDecode(IEmittingContext context, bool doNotCheckBounds)
        {
            var il = context.IL;

            var endOfSubmethodLabel = il.DefineLabel();

            if (!doNotCheckBounds)
            {
                var canReadSizeLabel = il.DefineLabel();
                il.Ldloc(context.RemainingBytesVar);                        // if (remainingBytes >= sizeof(int))
                il.Ldc_I4(sizeof(int));                                     //     goto canReadSizeLabel
                il.Bge(canReadSizeLabel);
                il.ThrowUnexpectedEndException();                           // throw new InvalidDataException("...")
                il.MarkLabel(canReadSizeLabel);                             // label canReadSizeLabel
            }
            il.Ldloc(context.DataPointerVar);                               // stack_0 = *(int*) data
            il.Ldind_I4();
            var tempInteger = context.GetSharedVariable<int>("tempInteger");// var tempInteger = stack_0
            il.Stloc(tempInteger);
            il.IncreasePointer(context.DataPointerVar, sizeof(int));        // data += sizeof(int)
            il.DecreaseInteger(context.RemainingBytesVar, sizeof(int));     // remainingBytes -= sizeof(int)

            if (canBeNull)
            {
                var strIsNotNullLabel = il.DefineLabel();

                il.Ldloc(tempInteger);                                      // if (tempInteger != -1)
                il.Ldc_I4(-1);                                              //     goto strIsNotNullLabel
                il.Bne_Un(strIsNotNullLabel);

                // String is null branch
                il.Ldnull();                                                // stack_0 = null
                il.Br(endOfSubmethodLabel);                                 // goto endOfSubmethodLabel

                // String is not null branch
                il.MarkLabel(strIsNotNullLabel);                            // label strIsNotNullLabel
            }
            
            if (!doNotCheckBounds)
            {
                var canReadDataLabel = il.DefineLabel();
                il.Ldloc(context.RemainingBytesVar);                        // if (remainingBytes >= tempInteger)
                il.Ldloc(tempInteger);                                      //     goto canReadDataLabel
                il.Bge(canReadDataLabel);
                il.ThrowUnexpectedEndException();                           // throw new InvalidDataException("...")
                il.MarkLabel(canReadDataLabel);                             // label canReadDataLabel
            }
            il.Ldloc(context.DataPointerVar);                               // stack_0 = data
            il.Ldc_I4(0);                                                   // stack_1 = 0
            il.Ldloc(tempInteger);                                          // stack_2 = tempInteger >> 1
            il.Ldc_I4(1);
            il.Shr();
            il.Newobj(NewString);                                           // stack_0 = new string(stack_0, stack_1, stack_2)
            EmitParseFromString(il);                                        // stack_0 = Parse(stack_0)
            il.IncreasePointer(context.DataPointerVar, tempInteger);        // data += tempInteger
            il.DecreaseInteger(context.RemainingBytesVar, tempInteger);     // remainingBytes -= tempInteger
            il.MarkLabel(endOfSubmethodLabel);                              // label endOfSubmethodLabel
        }
    }
}