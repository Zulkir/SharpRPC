using System;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace SharpRpc.Codecs
{
    public class PointableStructCodec : IEmittingCodec
    {
        private readonly Type type;
        private readonly int sizeInBytes;

        public PointableStructCodec(Type type)
        {
            this.type = type;
            sizeInBytes = Marshal.SizeOf(type);
        }

        public bool HasFixedSize { get { return true; } }
        public int FixedSize { get { return sizeInBytes; } }

        // todo: special op-codes for basic types

        public void EmitCalculateSize(ILGenerator il)
        {
            il.Emit(OpCodes.Pop);
            il.Emit_Ldc_I4(sizeInBytes);
        }

        public void EmitEncode(ILGenerator il, ILocalVariableCollection locals, Action<ILGenerator> load)
        {
            il.Emit(OpCodes.Ldloc, locals.DataPointer);              // *(T*) data = val
            load(il);
            il.Emit(OpCodes.Stobj, type);
            il.Emit_IncreasePointer(locals.DataPointer, sizeInBytes);// data += sizeInBytes
        }

        public void EmitDecode(ILGenerator il, ILocalVariableCollection locals, bool doNotCheckBounds)
        {
            if (!doNotCheckBounds)
            {
                var everythingsAllrightLabel = il.DefineLabel();
                il.Emit(OpCodes.Ldloc, locals.RemainingBytes);           // if (remainingBytes >= sizeInBytes)
                il.Emit_Ldc_I4(sizeInBytes);                             //     goto everythingsAllrightLabel
                il.Emit(OpCodes.Bge, everythingsAllrightLabel);
                il.Emit_ThrowUnexpectedEndException();                   // throw new InvalidDataException("...")
                il.MarkLabel(everythingsAllrightLabel);                  // label everythingsAllrightLabel
            }
            il.Emit(OpCodes.Ldloc, locals.DataPointer);                  // stack_0 = *(T*) data
            il.Emit(OpCodes.Ldobj, type);
            il.Emit_IncreasePointer(locals.DataPointer, sizeInBytes);    // data += sizeInBytes
            il.Emit_DecreaseInteger(locals.RemainingBytes, sizeInBytes); // remainingBytes -= sizeInBytes
        }
    }
}
