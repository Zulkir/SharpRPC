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
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

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
        protected abstract void EmitLoadAsString(ILGenerator il, Action<ILGenerator> emitLoad);
        protected abstract void EmitParseFromString(ILGenerator il);

        static readonly MethodInfo GetLength = typeof(string).GetMethod("get_Length");
        static readonly MethodInfo GetOffsetToStringData = typeof(RuntimeHelpers).GetMethod("get_OffsetToStringData");
        static readonly ConstructorInfo NewString = typeof(string).GetConstructor(new[] { typeof(char*), typeof(int), typeof(int) });

        public void EmitCalculateSize(ILGenerator il, Action<ILGenerator> emitLoad)
        {
            var endOfSubmethodLabel = il.DefineLabel();

            if (canBeNull)
            {
                var stringIsNotNullLabel = il.DefineLabel();

                emitLoad(il);                                 // if (value)
                il.Emit(OpCodes.Brtrue, stringIsNotNullLabel);//     goto stringIsNotNullLabel

                // String is null branch
                il.Emit_Ldc_I4(sizeof(int));                  // stack_0 = sizeof(int)
                il.Emit(OpCodes.Br, endOfSubmethodLabel);     // goto endOfSubmethodLabel

                // String is not null branch
                il.MarkLabel(stringIsNotNullLabel);           // label stringIsNotNullLabel
            }

            EmitLoadAsString(il, emitLoad);                   // stack_0 = (FormatToString(value).Length << 1) + sizeof(int)
            il.Emit(OpCodes.Call, GetLength);
            il.Emit_Ldc_I4(1);
            il.Emit(OpCodes.Shl);
            il.Emit_Ldc_I4(sizeof(int));
            il.Emit(OpCodes.Add);
            il.MarkLabel(endOfSubmethodLabel);                // label endOfSubmethodLabel
        }

        public void EmitEncode(ILGenerator il, ILocalVariableCollection locals, Action<ILGenerator> emitLoad)
        {
            var strIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            if (canBeNull)
            {
                emitLoad(il);                                            // if (val) goto strIsNotNullLabel
                il.Emit(OpCodes.Brtrue, strIsNotNullLabel);

                // String is null branch
                il.Emit(OpCodes.Ldloc, locals.DataPointer);              // *(int*) data = -1
                il.Emit_Ldc_I4(-1);
                il.Emit(OpCodes.Stind_I4);
                il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));// data += sizeof(int)
                il.Emit(OpCodes.Br, endOfSubmethodLabel);                // goto endOfEncodeLabel

                // String is not null branch
                il.MarkLabel(strIsNotNullLabel);                         // label strIsNotNullLabel
            }

            var stringValueVar = locals.GetOrAdd("stringValue",         // var stringValue = FormatToString(value)
                g => g.DeclareLocal(typeof(string)));
            EmitLoadAsString(il, emitLoad);
            il.Emit(OpCodes.Stloc, stringValueVar);

            var tempIntegerVar = locals.GetOrAdd("tempInteger",         // var tempInteger = stringValue.Length << 1
                g => g.DeclareLocal(typeof(int)));
            il.Emit(OpCodes.Ldloc, stringValueVar);
            il.Emit(OpCodes.Call, GetLength);
            il.Emit_Ldc_I4(1);
            il.Emit(OpCodes.Shl);
            il.Emit(OpCodes.Stloc, tempIntegerVar);
            il.Emit(OpCodes.Ldloc, locals.DataPointer);                 // *(int*)data = tempInteger
            il.Emit(OpCodes.Ldloc, tempIntegerVar);
            il.Emit(OpCodes.Stind_I4);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));   // data += sizeof(int)
            var pinnedString = il.DeclareLocal(typeof(string), true);   // var pinned pinnedString = stringValue
            il.Emit(OpCodes.Ldloc, stringValueVar);
            il.Emit(OpCodes.Stloc, pinnedString);
            il.Emit(OpCodes.Ldloc, pinnedString);                       // stack_0 = (byte*)pinnedString
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Call, GetOffsetToStringData);               // stack_0 = stack_0 + 
            il.Emit(OpCodes.Add);                                       //     RuntimeHelpers.OffsetToStringData
            var charPointer = locals.GetOrAdd("charPointer",            // charPointer = stack_0
                g => g.DeclareLocal(typeof(char*)));
            il.Emit(OpCodes.Stloc, charPointer);
            il.Emit(OpCodes.Ldloc, locals.DataPointer);                 // cpblk(data, charPointer, tempInteger)
            il.Emit(OpCodes.Ldloc, charPointer);
            il.Emit(OpCodes.Ldloc, tempIntegerVar);
            il.Emit(OpCodes.Cpblk);
            il.Emit(OpCodes.Ldnull);                                    // pinnedString = null
            il.Emit(OpCodes.Stloc, pinnedString);
            il.Emit_IncreasePointer(locals.DataPointer, tempIntegerVar);// data += tempInteger
            il.MarkLabel(endOfSubmethodLabel);                          // label endOfSubmethodLabel
        }

        public void EmitDecode(ILGenerator il, ILocalVariableCollection locals, bool doNotCheckBounds)
        {
            var endOfSubmethodLabel = il.DefineLabel();

            if (!doNotCheckBounds)
            {
                var canReadSizeLabel = il.DefineLabel();
                il.Emit(OpCodes.Ldloc, locals.RemainingBytes);          // if (remainingBytes >= sizeof(int))
                il.Emit_Ldc_I4(sizeof(int));                            //     goto canReadSizeLabel
                il.Emit(OpCodes.Bge, canReadSizeLabel);
                il.Emit_ThrowUnexpectedEndException();                  // throw new InvalidDataException("...")
                il.MarkLabel(canReadSizeLabel);                         // label canReadSizeLabel
            }
            il.Emit(OpCodes.Ldloc, locals.DataPointer);                 // stack_0 = *(int*) data
            il.Emit(OpCodes.Ldind_I4);
            var tempInteger = locals.GetOrAdd("tempInteger",            // var tempInteger = stack_0
                g => g.DeclareLocal(typeof(int)));
            il.Emit(OpCodes.Stloc, tempInteger);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));   // data += sizeof(int)
            il.Emit_DecreaseInteger(locals.RemainingBytes, sizeof(int));// remainingBytes -= sizeof(int)

            if (canBeNull)
            {
                var strIsNotNullLabel = il.DefineLabel();

                il.Emit(OpCodes.Ldloc, tempInteger);                    // if (tempInteger != -1)
                il.Emit_Ldc_I4(-1);                                     //     goto strIsNotNullLabel
                il.Emit(OpCodes.Bne_Un, strIsNotNullLabel);

                // String is null branch
                il.Emit(OpCodes.Ldnull);                                // stack_0 = null
                il.Emit(OpCodes.Br, endOfSubmethodLabel);               // goto endOfSubmethodLabel

                // String is not null branch
                il.MarkLabel(strIsNotNullLabel);                        // label strIsNotNullLabel
            }
            
            if (!doNotCheckBounds)
            {
                var canReadDataLabel = il.DefineLabel();
                il.Emit(OpCodes.Ldloc, locals.RemainingBytes);          // if (remainingBytes >= tempInteger)
                il.Emit(OpCodes.Ldloc, tempInteger);                    //     goto canReadDataLabel
                il.Emit(OpCodes.Bge, canReadDataLabel);
                il.Emit_ThrowUnexpectedEndException();                  // throw new InvalidDataException("...")
                il.MarkLabel(canReadDataLabel);                         // label canReadDataLabel
            }
            il.Emit(OpCodes.Ldloc, locals.DataPointer);                 // stack_0 = data
            il.Emit_Ldc_I4(0);                                          // stack_1 = 0
            il.Emit(OpCodes.Ldloc, tempInteger);                        // stack_2 = tempInteger >> 1
            il.Emit_Ldc_I4(1);
            il.Emit(OpCodes.Shr);
            il.Emit(OpCodes.Newobj, NewString);                         // stack_0 = new string(stack_0, stack_1, stack_2)
            EmitParseFromString(il);                                    // stack_0 = Parse(stack_0)
            il.Emit_IncreasePointer(locals.DataPointer, tempInteger);   // data += tempInteger
            il.Emit_DecreaseInteger(locals.RemainingBytes, tempInteger);// remainingBytes -= tempInteger
            il.MarkLabel(endOfSubmethodLabel);                          // label endOfSubmethodLabel
        }
    }
}