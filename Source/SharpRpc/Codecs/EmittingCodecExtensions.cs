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
using SharpRpc.Utility;

namespace SharpRpc.Codecs
{
    public static class EmittingCodecExtensions
    {
        public static void EmitCalculateSize(this IEmittingCodec codec, IEmittingContext context, int argIndex)
        {
            codec.EmitCalculateSize(context, il => il.Ldarg(argIndex));
        }

        public static void EmitCalculateSizeIndirect(this IEmittingCodec codec, IEmittingContext context, int argIndex, Type type)
        {
            codec.EmitCalculateSize(context, il => { il.Ldarg(argIndex); il.Ldobj(type); });
        }

        public static void EmitCalculateSize(this IEmittingCodec codec, IEmittingContext context, LocalBuilder localVar)
        {
            codec.EmitCalculateSize(context, il => il.Ldloc(localVar));
        }

        public static void EmitCalculateSize(this IEmittingCodec codec, IEmittingContext context, Action<MyILGenerator> emitLoadParent, MethodInfo propertyGetter)
        {
            codec.EmitCalculateSize(context, il => { emitLoadParent(il); il.Call(propertyGetter); });
        }

        public static void EmitEncode(this IEmittingCodec codec, IEmittingContext context, int argIndex)
        {
            codec.EmitEncode(context, il => il.Ldarg(argIndex));
        }

        public static void EmitEncodeIndirect(this IEmittingCodec codec, IEmittingContext context, int argIndex, Type type)
        {
            codec.EmitEncode(context, il => { il.Ldarg(argIndex); il.Ldobj(type); });
        }

        public static void EmitEncode(this IEmittingCodec codec, IEmittingContext context, LocalBuilder localVar)
        {
            codec.EmitEncode(context, il => il.Ldloc(localVar));
        }

        public static void EmitEncode(this IEmittingCodec codec, IEmittingContext context, Action<MyILGenerator> emitLoadParent, MethodInfo propertyGetter)
        {
            codec.EmitEncode(context, il => { emitLoadParent(il); il.Call(propertyGetter); });
        }
    }
}
