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

namespace SharpRpc.Utility
{
    public class MethodHelpers
    {
        private static readonly Func<DynamicMethod, IntPtr> ExtractDynamicMethodPointerMethod = EmitDynamicMethodPointerExtractor();

        // Slightly modified code from the GroBuf library
        private static Func<DynamicMethod, IntPtr> EmitDynamicMethodPointerExtractor()
        {
            var method = new DynamicMethod("DynamicMethodPointerExtractor", typeof(IntPtr), new[] { typeof(DynamicMethod) }, Assembly.GetExecutingAssembly().ManifestModule, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // stack: [dynamicMethod]
            MethodInfo getMethodDescriptorMethod = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
            if (getMethodDescriptorMethod == null)
                throw new MissingMethodException(typeof(DynamicMethod).Name, "GetMethodDescriptor");
            il.Emit(OpCodes.Call, getMethodDescriptorMethod); // stack: [dynamicMethod.GetMethodDescriptor()]
            var runtimeMethodHandle = il.DeclareLocal(typeof(RuntimeMethodHandle));
            il.Emit(OpCodes.Stloc, runtimeMethodHandle); // runtimeMethodHandle = dynamicMethod.GetMethodDescriptor(); stack: []
            il.Emit(OpCodes.Ldloc, runtimeMethodHandle); // stack: [runtimeMethodHandle]
            MethodInfo prepareMethodMethod = typeof(RuntimeHelpers).GetMethod("PrepareMethod", new[] { typeof(RuntimeMethodHandle) });
            if (prepareMethodMethod == null)
                throw new MissingMethodException(typeof(RuntimeHelpers).Name, "PrepareMethod");
            il.Emit(OpCodes.Call, prepareMethodMethod); // RuntimeHelpers.PrepareMethod(runtimeMethodHandle)
            MethodInfo getFunctionPointerMethod = typeof(RuntimeMethodHandle).GetMethod("GetFunctionPointer", BindingFlags.Instance | BindingFlags.Public);
            if (getFunctionPointerMethod == null)
                throw new MissingMethodException(typeof(RuntimeMethodHandle).Name, "GetFunctionPointer");
            il.Emit(OpCodes.Ldloca, runtimeMethodHandle); // stack: [&runtimeMethodHandle]
            il.Emit(OpCodes.Call, getFunctionPointerMethod); // stack: [runtimeMethodHandle.GetFunctionPointer()]
            il.Emit(OpCodes.Ret); // return runtimeMethodHandle.GetFunctionPointer()
            return (Func<DynamicMethod, IntPtr>)method.CreateDelegate(typeof(Func<DynamicMethod, IntPtr>));
        }

        public static IntPtr ExtractDynamicMethodPointer(DynamicMethod dynamicMethod)
        {
            return ExtractDynamicMethodPointerMethod(dynamicMethod);
        }
    }
}