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
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace SharpRpc.Codecs
{
    public static class ILGeneratorExtensions
    {
        public static void Emit_Ldc_I4(this ILGenerator il, int c)
        {
            switch (c)
            {
                case -1: il.Emit(OpCodes.Ldc_I4_M1); return;
                case 0: il.Emit(OpCodes.Ldc_I4_0); return;
                case 1: il.Emit(OpCodes.Ldc_I4_1); return;
                case 2: il.Emit(OpCodes.Ldc_I4_2); return;
                case 3: il.Emit(OpCodes.Ldc_I4_3); return;
                case 4: il.Emit(OpCodes.Ldc_I4_4); return;
                case 5: il.Emit(OpCodes.Ldc_I4_5); return;
                case 6: il.Emit(OpCodes.Ldc_I4_6); return;
                case 7: il.Emit(OpCodes.Ldc_I4_7); return;
                case 8: il.Emit(OpCodes.Ldc_I4_8); return;
            }
            
            if (sbyte.MinValue <= c && c <= sbyte.MaxValue)
            {
                il.Emit(OpCodes.Ldc_I4_S, (sbyte)c);
                return;
            }
            
            il.Emit(OpCodes.Ldc_I4, c);
        }

        public static void Emit_Ldarg(this ILGenerator il, int index)
        {
            switch (index)
            {
                case 0: il.Emit(OpCodes.Ldarg_0); return;
                case 1: il.Emit(OpCodes.Ldarg_1); return;
                case 2: il.Emit(OpCodes.Ldarg_2); return;
                case 3: il.Emit(OpCodes.Ldarg_3); return;
            }

            if (sbyte.MinValue <= index && index <= sbyte.MaxValue)
            {
                il.Emit(OpCodes.Ldc_I4_S, (sbyte)index);
                return;
            }

            il.Emit(OpCodes.Ldarg, index);
        }

        public static void Emit_IncreasePointer(this ILGenerator il, LocalBuilder dataPointerVar, int distance)
        {
            il.Emit(OpCodes.Ldloc, dataPointerVar);
            il.Emit_Ldc_I4(distance);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, dataPointerVar);
        }

        public static void Emit_IncreasePointer(this ILGenerator il, LocalBuilder dataPointerVar, LocalBuilder distanceVar)
        {
            il.Emit(OpCodes.Ldloc, dataPointerVar);
            il.Emit(OpCodes.Ldloc, distanceVar);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, dataPointerVar);
        }

        public static void Emit_DecreaseInteger(this ILGenerator il, LocalBuilder remainingBytesVal, int distance)
        {
            il.Emit(OpCodes.Ldloc, remainingBytesVal);
            il.Emit_Ldc_I4(distance);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Stloc, remainingBytesVal);
        }

        public static void Emit_DecreaseInteger(this ILGenerator il, LocalBuilder remainingBytesVal, LocalBuilder distanceVar)
        {
            il.Emit(OpCodes.Ldloc, remainingBytesVal);
            il.Emit(OpCodes.Ldloc, distanceVar);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Stloc, remainingBytesVal);
        }

        private static readonly ConstructorInfo ExceptionConstructor = typeof(InvalidDataException).GetConstructor(new[] { typeof(string) });
        public static void Emit_ThrowUnexpectedEndException(this ILGenerator il)
        {
            il.Emit(OpCodes.Ldstr, "Unexpected end of request data");// throw new InvalidDataException(
            il.Emit(OpCodes.Newobj, ExceptionConstructor);           //     "Unexpected end of request data")
            il.Emit(OpCodes.Throw);
        }
        /*
        public static void Emit_LoadSize(this ILGenerator il, IEmittingCodec codec)
        {
            if (codec.HasFixedSize)
            {
                il.Emit(OpCodes.Pop);
                il.Emit_Ldc_I4(codec.FixedSize);
            }
            else
            {
                codec.EmitCalculateSize(il);
            }
        }

        public static void Emit_LoadSize(this ILGenerator il, IEmittingCodec codec, LocalBuilder localVar)
        {
            Emit_LoadSize(il, codec, lil => lil.Emit(OpCodes.Ldloc, localVar));
        }*/

        public static LocalBuilder Emit_PinArray(this ILGenerator il, Type elementType, ILocalVariableCollection locals, Action<ILGenerator> load)
        {
            var argsDataPointerVar = locals.GetOrAdd("pinnedArray" + elementType.FullName,
                lil => lil.DeclareLocal(elementType.MakePointerType(), true));
            load(il);
            il.Emit_Ldc_I4(0);
            il.Emit(OpCodes.Ldelema, elementType);
            il.Emit(OpCodes.Stloc, argsDataPointerVar);
            return argsDataPointerVar;
        }

        public static LocalBuilder Emit_PinArray(this ILGenerator il, Type elementType, ILocalVariableCollection locals)
        {
            return Emit_PinArray(il, elementType, locals, lil => { });
        }

        public static LocalBuilder Emit_PinArray(this ILGenerator il, Type elementType, ILocalVariableCollection locals, LocalBuilder localVar)
        {
            return Emit_PinArray(il, elementType, locals, lil => lil.Emit(OpCodes.Ldloc, localVar));
        }

        public static LocalBuilder Emit_PinArray(this ILGenerator il, Type elementType, ILocalVariableCollection locals, int argIndex)
        {
            return Emit_PinArray(il, elementType, locals, lil => lil.Emit_Ldarg(argIndex));
        }

        public static void Emit_UnpinArray(this ILGenerator il, LocalBuilder pinnedPointerVar)
        {
            il.Emit_Ldc_I4(0);
            il.Emit(OpCodes.Conv_U);
            il.Emit(OpCodes.Stloc, pinnedPointerVar);
        }
    }
}