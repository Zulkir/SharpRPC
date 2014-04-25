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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Linq;

namespace SharpRpc.Utility
{
    public class MyILGenerator
    {
        private readonly ILGenerator il;
        private readonly StringBuilder stringBuilder;

        public MyILGenerator(ILGenerator il)
        {
            this.il = il;
            stringBuilder = new StringBuilder();
        }

        public string Text { get { return stringBuilder.ToString(); } }

        private static string FormatOpCode(OpCode opCode)
        {
            return opCode.ToString().ToLower().Replace('_', '.');
        }

        private void Emit(OpCode opCode)
        {
            il.Emit(opCode);
            stringBuilder.AppendLine(FormatOpCode(opCode));
        }

        private void Emit(OpCode opCode, ConstructorInfo constructor)
        {
            il.Emit(opCode, constructor);
            stringBuilder.AppendLine(FormatOpCode(opCode) + " " + constructor.Name);
        }

        private void Emit(OpCode opCode, FieldInfo field)
        {
            il.Emit(opCode, field);
            stringBuilder.AppendLine(FormatOpCode(opCode) + " " + field.Name);
        }

        private void Emit(OpCode opCode, int c)
        {
            il.Emit(opCode, c);
            stringBuilder.AppendLine(FormatOpCode(opCode) + " " + c);
        }

        private void Emit(OpCode opCode, Label label)
        {
            il.Emit(opCode, label);
            stringBuilder.AppendLine(FormatOpCode(opCode) + " " + label.GetHashCode());
        }

        private void Emit(OpCode opCode, Label[] labelGroup)
        {
            il.Emit(opCode, labelGroup);
            var labelsString = string.Join(", ", labelGroup.Select(x => x.GetHashCode().ToString(CultureInfo.InvariantCulture)));
            stringBuilder.AppendLine(string.Format("{0} [{1}]", FormatOpCode(opCode), labelsString));
        }

        private void Emit(OpCode opCode, LocalBuilder localVar)
        {
            il.Emit(opCode, localVar);
            stringBuilder.AppendLine(string.Format("{0} var{1} ({2})", FormatOpCode(opCode), localVar.LocalIndex, localVar.LocalType));
        }

        private void Emit(OpCode opCode, long c)
        {
            il.Emit(opCode, c);
            stringBuilder.AppendLine(FormatOpCode(opCode) + " " + c);
        }

        private void Emit(OpCode opCode, MethodInfo methodInfo)
        {
            il.Emit(opCode, methodInfo);
            stringBuilder.AppendLine(FormatOpCode(opCode) + " " + methodInfo.Name);
        }

        private void Emit(OpCode opCode, string str)
        {
            il.Emit(opCode, str);
            stringBuilder.AppendLine(string.Format("{0} \"{1}\"", FormatOpCode(opCode), str));
        }

        private void Emit(OpCode opCode, Type type)
        {
            il.Emit(opCode, type);
            stringBuilder.AppendLine(FormatOpCode(opCode) + " " + type.FullName);
        }

        private static bool IsShort(int constant)
        {
            return sbyte.MinValue <= constant && constant <= sbyte.MaxValue;
        }

        #region Real Instructions

        public void Add()
        {
            Emit(OpCodes.Add);
        }

        public void Bge(Label label)
        {
            Emit(OpCodes.Bge, label);
        }

        public void Blt(Label label)
        {
            Emit(OpCodes.Blt, label);
        }

        public void Bne_Un(Label label)
        {
            Emit(OpCodes.Bne_Un, label);
        }

        public void Br(Label label)
        {
            Emit(OpCodes.Br, label);
        }

        public void Brfalse(Label label)
        {
            Emit(OpCodes.Brfalse, label);
        }

        public void Brtrue(Label label)
        {
            Emit(OpCodes.Brtrue, label);
        }

        public void Call(ConstructorInfo constructor)
        {
            Emit(OpCodes.Call, constructor);
        }

        public void Call(MethodInfo method)
        {
            Emit(OpCodes.Call, method);
        }

        public void Callvirt(MethodInfo method)
        {
            Emit(OpCodes.Callvirt, method);
        }

        public void Castclass(Type type)
        {
            Emit(OpCodes.Castclass, type);
        }

        public void Conv_I()
        {
            Emit(OpCodes.Conv_I);
        }

        public void Conv_I4()
        {
            Emit(OpCodes.Conv_I4);
        }

        public void Conv_U()
        {
            Emit(OpCodes.Conv_U);
        }

        public void Cpblk()
        {
            Emit(OpCodes.Cpblk);
        }

        public void Dup()
        {
            Emit(OpCodes.Dup);
        }

        public void Initobj(Type type)
        {
            Emit(OpCodes.Initobj, type);
        }

        public void Isinst(Type type)
        {
            Emit(OpCodes.Isinst, type);
        }

        public void Ldarg(int argIndex)
        {
            switch (argIndex)
            {
                case 0: Emit(OpCodes.Ldarg_0); return;
                case 1: Emit(OpCodes.Ldarg_1); return;
                case 2: Emit(OpCodes.Ldarg_2); return;
                case 3: Emit(OpCodes.Ldarg_3); return;
            }
            Emit(IsShort(argIndex) ? OpCodes.Ldarg_S : OpCodes.Ldarg, argIndex);
        }

        public void Ldc_I4(int constant)
        {
            switch (constant)
            {
                case -1: Emit(OpCodes.Ldc_I4_M1); return;
                case 0: Emit(OpCodes.Ldc_I4_0); return;
                case 1: Emit(OpCodes.Ldc_I4_1); return;
                case 2: Emit(OpCodes.Ldc_I4_2); return;
                case 3: Emit(OpCodes.Ldc_I4_3); return;
                case 4: Emit(OpCodes.Ldc_I4_4); return;
                case 5: Emit(OpCodes.Ldc_I4_5); return;
                case 6: Emit(OpCodes.Ldc_I4_6); return;
                case 7: Emit(OpCodes.Ldc_I4_7); return;
                case 8: Emit(OpCodes.Ldc_I4_8); return;
            }

            Emit(IsShort(constant) ? OpCodes.Ldc_I4_S : OpCodes.Ldc_I4, constant);
        }

        public void Ldc_I8(long constant)
        {
            Emit(OpCodes.Ldc_I8, constant);
        }

        public void Ldelem_Ref()
        {
            Emit(OpCodes.Ldelem_Ref);
        }

        public void Ldelema(Type type)
        {
            Emit(OpCodes.Ldelema, type);
        }

        public void Ldfld(FieldInfo field)
        {
            Emit(OpCodes.Ldfld, field);
        }

        public void Ldftn(MethodInfo method)
        {
            Emit(OpCodes.Ldftn, method);
        }

        public void Ldind_I()
        {
            Emit(OpCodes.Ldind_I);
        }

        public void Ldind_I4()
        {
            Emit(OpCodes.Ldind_I4);
        }

        public void Ldlen()
        {
            Emit(OpCodes.Ldlen);
        }

        public void Ldloc(LocalBuilder local)
        {
            switch (local.LocalIndex)
            {
                case 0: Emit(OpCodes.Ldloc_0, local); return;
                case 1: Emit(OpCodes.Ldloc_1, local); return;
                case 2: Emit(OpCodes.Ldloc_2, local); return;
                case 3: Emit(OpCodes.Ldloc_3, local); return;
            }
            Emit(IsShort(local.LocalIndex) ? OpCodes.Ldloc_S : OpCodes.Ldloc, local);
        }

        public void Ldloca(LocalBuilder local)
        {
            Emit(IsShort(local.LocalIndex) ? OpCodes.Ldloca_S : OpCodes.Ldloca, local);
        }

        public void Ldnull()
        {
            Emit(OpCodes.Ldnull);
        }

        public void Ldobj(Type type)
        {
            Emit(OpCodes.Ldobj, type);
        }

        public void Ldstr(string str)
        {
            Emit(OpCodes.Ldstr, str);
        }

        public void Ldtoken(Type type)
        {
            Emit(OpCodes.Ldtoken, type);
        }

        public void Mul()
        {
            Emit(OpCodes.Mul);
        }

        public void Newarr(Type type)
        {
            Emit(OpCodes.Newarr, type);
        }

        public void Newobj(ConstructorInfo constructor)
        {
            Emit(OpCodes.Newobj, constructor);
        }

        public void Pop()
        {
            Emit(OpCodes.Pop);
        }

        public void Ret()
        {
            Emit(OpCodes.Ret);
        }

        public void Shl()
        {
            Emit(OpCodes.Shl);
        }

        public void Shr()
        {
            Emit(OpCodes.Shr);
        }

        public void Stelem_Ref()
        {
            Emit(OpCodes.Stelem_Ref);
        }

        public void Stfld(FieldInfo field)
        {
            Emit(OpCodes.Stfld, field);
        }

        public void Stind_I()
        {
            Emit(OpCodes.Stind_I);
        }

        public void Stind_I4()
        {
            Emit(OpCodes.Stind_I4);
        }

        public void Stind_Ref()
        {
            Emit(OpCodes.Stind_Ref);
        }

        public void Stloc(LocalBuilder localVar)
        {
            switch (localVar.LocalIndex)
            {
                case 0: Emit(OpCodes.Stloc_0, localVar); return;
                case 1: Emit(OpCodes.Stloc_1, localVar); return;
                case 2: Emit(OpCodes.Stloc_2, localVar); return;
                case 3: Emit(OpCodes.Stloc_3, localVar); return;
            }
            Emit(IsShort(localVar.LocalIndex) ? OpCodes.Stloc_S : OpCodes.Stloc, localVar);
        }

        public void Stobj(Type type)
        {
            Emit(OpCodes.Stobj, type);
        }

        public void Sub()
        {
            Emit(OpCodes.Sub);
        }

        public void Switch(Label[] labelGroup)
        {
            Emit(OpCodes.Switch, labelGroup);
        }

        public void Throw()
        {
            Emit(OpCodes.Throw);
        }

        #endregion

        #region Real Metaactions

        public LocalBuilder DeclareLocal(Type localType, bool pinned = false)
        {
            return il.DeclareLocal(localType, pinned);
        }

        public Label DefineLabel()
        {
            return il.DefineLabel();
        }

        public void MarkLabel(Label label)
        {
            il.MarkLabel(label);
            stringBuilder.AppendLine("label " + label.GetHashCode());
        }

        #endregion

        #region Helpers

        public void IncreasePointer(LocalBuilder dataPointerVar, int distance)
        {
            Ldloc(dataPointerVar);
            Ldc_I4(distance);
            Add();
            Stloc(dataPointerVar);
        }

        public void IncreasePointer(LocalBuilder dataPointerVar, LocalBuilder distanceVar)
        {
            Ldloc(dataPointerVar);
            Ldloc(distanceVar);
            Add();
            Stloc(dataPointerVar);
        }

        public void DecreaseInteger(LocalBuilder remainingBytesVal, int distance)
        {
            Ldloc(remainingBytesVal);
            Ldc_I4(distance);
            Sub();
            Stloc(remainingBytesVal);
        }

        public void DecreaseInteger(LocalBuilder remainingBytesVal, LocalBuilder distanceVar)
        {
            Ldloc(remainingBytesVal);
            Ldloc(distanceVar);
            Sub();
            Stloc(remainingBytesVal);
        }

        public LocalBuilder PinArray(Type elementType, Action<MyILGenerator> load)
        {
            var pointerVar = DeclareLocal(elementType.MakeByRefType(), true);

            load(this);
            Ldc_I4(0);
            Ldelema(elementType);
            Stloc(pointerVar);

            return pointerVar;
        }

        public LocalBuilder PinArray(Type elementType, LocalBuilder localVar)
        {
            return PinArray(elementType, lil => lil.Ldloc(localVar));
        }

        public LocalBuilder PinArray(Type elementType, int argIndex)
        {
            return PinArray(elementType, lil => lil.Ldarg(argIndex));
        }

        public void UnpinArray(LocalBuilder pointerVar)
        {
            Ldc_I4(0);
            Conv_U();
            Stloc(pointerVar);
        }

        private static readonly ConstructorInfo ExceptionConstructor = typeof(InvalidDataException).GetConstructor(new[] { typeof(string) });
        public void ThrowUnexpectedEndException()
        {
            Ldstr("Unexpected end of request data");// throw new InvalidDataException(
            Newobj(ExceptionConstructor);           //     "Unexpected end of request data")
            Throw();
        }

        public IForLoopEmitter EmitForLoop(LocalBuilder lengthVar)
        {
            return new ForLoopEmitter(this, lengthVar);
        }

        public IForeachLoopEmitter EmitForeachLoop(Type elementType, Action<MyILGenerator> emitLoadCollection)
        {
            return new ForeachLoopEmitter(this, elementType, emitLoadCollection);
        }

        #endregion
    }
}