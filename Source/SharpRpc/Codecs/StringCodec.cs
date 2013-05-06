using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace SharpRpc.Codecs
{
    public class StringCodec : IEmittingCodec
    {
        public bool HasFixedSize { get { return false; } }
        public int FixedSize { get { throw new InvalidOperationException(); } }

        public int CalculateSize(string item)
        {
            return sizeof(int) + (item == null ? 0 : (item.Length << 1));
        }

        static readonly MethodInfo GetOffsetToStringData = typeof(RuntimeHelpers).GetMethod("get_OffsetToStringData");
        static readonly MethodInfo GetLength = typeof(string).GetMethod("get_Length");
        static readonly ConstructorInfo NewString = typeof(string).GetConstructor(new[] {typeof(char*), typeof(int), typeof(int)});

        public void EmitCalculateSize(ILGenerator il)
        {
            var stringIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brtrue, stringIsNotNullLabel);
            
            // String is null branch
            il.Emit(OpCodes.Pop);
            il.Emit_Ldc_I4(sizeof(int));
            il.Emit(OpCodes.Br, endOfSubmethodLabel);

            // String is not null branch
            il.MarkLabel(stringIsNotNullLabel);
            il.Emit(OpCodes.Call, GetLength);
            il.Emit_Ldc_I4(1);
            il.Emit(OpCodes.Shl);
            il.Emit_Ldc_I4(sizeof(int));
            il.Emit(OpCodes.Add);
            il.MarkLabel(endOfSubmethodLabel);
        }

        public void EmitEncode(ILGenerator il, ILocalVariableCollection locals, Action<ILGenerator> load)
        {
            //var strPointerLabel = il.DefineLabel();
            var strIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            load(il);                                                       // if (val) goto strIsNotNullLabel
            il.Emit(OpCodes.Brtrue, strIsNotNullLabel);

            // String is null branch
            il.Emit(OpCodes.Ldloc, locals.DataPointer);                     // *(int*) data = -1
            il.Emit_Ldc_I4(-1);
            il.Emit(OpCodes.Stind_I4);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));       // data += sizeof(int)
            il.Emit(OpCodes.Br, endOfSubmethodLabel);                          // goto endOfEncodeLabel

            // String is not null branch
            il.MarkLabel(strIsNotNullLabel);                                // label strIsNotNullLabel
            var tempInteger = locals.GetOrAdd("tempInteger",                // var tempInteger = val.Length << 1
                g => g.DeclareLocal(typeof(int)));
            load(il);
            il.Emit(OpCodes.Call, GetLength);
            il.Emit_Ldc_I4(1);
            il.Emit(OpCodes.Shl);
            il.Emit(OpCodes.Stloc, tempInteger);
            il.Emit(OpCodes.Ldloc, locals.DataPointer);                     // *(int*)data = tempInteger
            il.Emit(OpCodes.Ldloc, tempInteger);
            il.Emit(OpCodes.Stind_I4);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));       // data += sizeof(int)
            var pinnedString = locals.GetOrAdd("pinnedString",              // var pinned pinnedString = val
                g => g.DeclareLocal(typeof(string), true));
            load(il);
            il.Emit(OpCodes.Stloc, pinnedString);
            il.Emit(OpCodes.Ldloc, pinnedString);                           // stack_0 = (byte*)pinnedString
            il.Emit(OpCodes.Conv_I);
            //il.Emit(OpCodes.Dup);                                         // stack_1 = stack_0
            //il.Emit(OpCodes.Brfalse, strPointerLabel);                    // if (!stack_1) goto strPointerLabel
            il.Emit(OpCodes.Call, GetOffsetToStringData);                   // stack_0 = stack_0 + 
            il.Emit(OpCodes.Add);                                           //     RuntimeHelpers.OffsetToStringData
            //il.MarkLabel(strPointerLabel);
            var charPointer = locals.GetOrAdd("charPointer",                // charPointer = stack_0
                g => g.DeclareLocal(typeof(char*)));
            il.Emit(OpCodes.Stloc, charPointer);
            il.Emit(OpCodes.Ldloc, locals.DataPointer);                     // cpblk(data, charPointer, tempInteger)
            il.Emit(OpCodes.Ldloc, charPointer);
            il.Emit(OpCodes.Ldloc, tempInteger);
            il.Emit(OpCodes.Cpblk);
            il.Emit(OpCodes.Ldnull);                                        // pinnedString = null
            il.Emit(OpCodes.Stloc, pinnedString);
            il.Emit_IncreasePointerDynamic(locals.DataPointer, tempInteger);// data += tempInteger
            il.MarkLabel(endOfSubmethodLabel);
        }

        public void EmitDecode(ILGenerator il, ILocalVariableCollection locals, bool doNotCheckBounds)
        {
            var strIsNotNullLabel = il.DefineLabel();
            var endOfSubmethodLabel = il.DefineLabel();

            if (!doNotCheckBounds)
            {
                var canReadSizeLabel = il.DefineLabel();
                il.Emit(OpCodes.Ldloc, locals.RemainingBytes);                 // if (remainingBytes >= sizeof(int))
                il.Emit_Ldc_I4(sizeof(int));                                   //     goto canReadSizeLabel
                il.Emit(OpCodes.Bge, canReadSizeLabel);
                il.Emit_ThrowUnexpectedEndException();                         // throw new InvalidDataException("...")
                il.MarkLabel(canReadSizeLabel);                                // label canReadSizeLabel
            }
            il.Emit(OpCodes.Ldloc, locals.DataPointer);                        // stack_0 = *(int*) data
            il.Emit(OpCodes.Ldind_I4);
            var tempInteger = locals.GetOrAdd("tempInteger",                   // var tempInteger = stack_0
                g => g.DeclareLocal(typeof(int)));
            il.Emit(OpCodes.Stloc, tempInteger);
            il.Emit_IncreasePointer(locals.DataPointer, sizeof(int));          // data += sizeof(int)
            il.Emit_DecreaseInteger(locals.RemainingBytes, sizeof(int));       // remainingBytes -= sizeof(int)
            il.Emit(OpCodes.Ldloc, tempInteger);
            il.Emit_Ldc_I4(-1);
            il.Emit(OpCodes.Bne_Un, strIsNotNullLabel);

            // String is null branch
            il.Emit(OpCodes.Ldnull);                                           // stack_0 = null
            il.Emit(OpCodes.Br, endOfSubmethodLabel);                          // goto endOfSubmethodLabel

            // String is not null branch
            il.MarkLabel(strIsNotNullLabel);
            if (!doNotCheckBounds)
            {
                var canReadDataLabel = il.DefineLabel();
                il.Emit(OpCodes.Ldloc, locals.RemainingBytes);                 // if (remainingBytes >= tempInteger)
                il.Emit(OpCodes.Ldloc, tempInteger);                           //     goto canReadDataLabel
                il.Emit(OpCodes.Bge, canReadDataLabel);
                il.Emit_ThrowUnexpectedEndException();                         // throw new InvalidDataException("...")
                il.MarkLabel(canReadDataLabel);                                // label canReadDataLabel
            }
            il.Emit(OpCodes.Ldloc, locals.DataPointer);                        // stack_0 = data
            il.Emit_Ldc_I4(0);                                                 // stack_1 = 0
            il.Emit(OpCodes.Ldloc, tempInteger);                               // stack_2 = tempInteger >> 1
            il.Emit_Ldc_I4(1);
            il.Emit(OpCodes.Shr);
            il.Emit(OpCodes.Newobj, NewString);                                // stack_0 = new string(stack_0, stack_1, stack_2)
            il.Emit_IncreasePointerDynamic(locals.DataPointer, tempInteger);   // data += tempInteger
            il.Emit_DecreaseIntegerDynamic(locals.RemainingBytes, tempInteger);// remainingBytes -= tempInteger
            il.MarkLabel(endOfSubmethodLabel);
        }
    }
}