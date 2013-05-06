using System.Reflection.Emit;

namespace SharpRpc.Codecs
{
    public static class EmittingCodecExtensions
    {
         public static void EmitEncode(this IEmittingCodec codec, ILGenerator il, LocalVariableCollection locals, int argIndex)
         {
             codec.EmitEncode(il, locals, lil => lil.Emit_Ldarg(argIndex));
         }

         public static void EmitEncode(this IEmittingCodec codec, ILGenerator il, LocalVariableCollection locals, LocalBuilder localVar)
         {
             codec.EmitEncode(il, locals, lil => lil.Emit(OpCodes.Ldloc, localVar));
         }
    }
}