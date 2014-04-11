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

namespace SharpRpc.Reflection
{
    // This is a hack taken from the GroBuf library
    // todo: refactor this magic out
    public static class DynamicMethodPointerExtractor
    {
        private static readonly DynamicMethod extractPointerMethod;
        private static readonly Func<DynamicMethod, IntPtr> extractPointerDelegate;

        static DynamicMethodPointerExtractor()
        {
            extractPointerMethod = EmitExtractMethod();
            extractPointerDelegate = (Func<DynamicMethod, IntPtr>) extractPointerMethod.CreateDelegate(typeof(Func<DynamicMethod, IntPtr>));
        }

        //public static DynamicMethod ExtractPointerMethod { get { return extractPointerMethod; } }

        public static IntPtr ExtractPointer(DynamicMethod method)
        {
            return extractPointerDelegate(method);
        }

        private static DynamicMethod EmitExtractMethod()
        {
            var extractorMethod = new DynamicMethod("DynamicMethodPointerExtractor", typeof(IntPtr), new[] { typeof(DynamicMethod) }, Assembly.GetExecutingAssembly().ManifestModule, true);
            var il = extractorMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            var getMethodDescriptorMethod = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
            if (getMethodDescriptorMethod == null)
                throw new MissingMethodException(typeof(DynamicMethod).Name, "GetMethodDescriptor");
            il.Emit(OpCodes.Call, getMethodDescriptorMethod);
            var runtimeMethodHandle = il.DeclareLocal(typeof(RuntimeMethodHandle));
            il.Emit(OpCodes.Stloc, runtimeMethodHandle);
            il.Emit(OpCodes.Ldloc, runtimeMethodHandle);
            var prepareMethodMethod = typeof(RuntimeHelpers).GetMethod("PrepareMethod", new[] { typeof(RuntimeMethodHandle) });
            if (prepareMethodMethod == null)
                throw new MissingMethodException(typeof(RuntimeHelpers).Name, "PrepareMethod");
            il.Emit(OpCodes.Call, prepareMethodMethod);
            var getFunctionPointerMethod = typeof(RuntimeMethodHandle).GetMethod("GetFunctionPointer", BindingFlags.Instance | BindingFlags.Public);
            if (getFunctionPointerMethod == null)
                throw new MissingMethodException(typeof(RuntimeMethodHandle).Name, "GetFunctionPointer");
            il.Emit(OpCodes.Ldloca, runtimeMethodHandle);
            il.Emit(OpCodes.Call, getFunctionPointerMethod);
            il.Emit(OpCodes.Ret);
            return extractorMethod;
        }
    }
}
