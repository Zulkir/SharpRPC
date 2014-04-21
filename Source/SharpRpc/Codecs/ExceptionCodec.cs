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
using System.Runtime.Serialization;
using SharpRpc.Utility;

namespace SharpRpc.Codecs
{
    public unsafe class ExceptionCodec : IManualCodec<Exception>
    {
        private readonly IManualCodec<string> stringCodec;
        private readonly Func<Type, string, string, Exception> createException; 

        public ExceptionCodec(ICodecContainer codecContainer)
        {
            stringCodec = codecContainer.GetManualCodecFor<string>();
            createException = CreateExceptionCreator();
        }

        public int CalculateSize(Exception value)
        {
            return stringCodec.CalculateSize(value.GetType().AssemblyQualifiedName) +
                   stringCodec.CalculateSize(value.Message) +
                   stringCodec.CalculateSize(value.StackTrace);
        }

        public Type Type { get { return typeof(Exception); } }
        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return null; } }

        public void Encode(ref byte* data, Exception value)
        {
            stringCodec.Encode(ref data, value.GetType().AssemblyQualifiedName);
            stringCodec.Encode(ref data, value.Message);
            stringCodec.Encode(ref data, value.StackTrace);
        }

        public Exception Decode(ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            var typeName = stringCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var message = stringCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var stackTrace = stringCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var remoteStackTrace = stackTrace + Environment.NewLine + "--- NETWORK ---" + Environment.NewLine;

            var type = Type.GetType(typeName);
            if (type == null || !typeof(Exception).IsAssignableFrom(type))
                return createException(typeof(Exception), message, remoteStackTrace);
            return createException(type, message, remoteStackTrace);
        }

        private static readonly MethodInfo GetUninitializedObject = typeof(FormatterServices).GetMethod("GetUninitializedObject");
        private static readonly MethodInfo InitMethod = typeof(Exception).GetMethod("Init", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo MessageField = typeof(Exception).GetField("_message", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo RemoteStackTraceStringField = typeof(Exception).GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);

        private static Func<Type, string, string, Exception> CreateExceptionCreator()
        {
            var dynamicMethod = new DynamicMethod(
                "__srpc__CreateException",
                typeof(Exception), new[] { typeof(Type), typeof(string), typeof(string) },
                Assembly.GetExecutingAssembly().ManifestModule, true);
            var il = new MyILGenerator(dynamicMethod.GetILGenerator());

            il.Ldarg(0);                            // stack_0 = (Exception)FormatterServices.GetUninitializedObject(typeof(T))
            il.Call(GetUninitializedObject);
            il.Castclass(typeof(Exception));

            il.Dup();                               // stack_0.Init()
            il.Call(InitMethod);
            il.Dup();                               // stack_0._message = message
            il.Ldarg(1);
            il.Stfld(MessageField);
            il.Dup();                               // stack_0._message = message
            il.Ldarg(2);
            il.Stfld(RemoteStackTraceStringField);
            il.Ret();
            return (Func<Type, string, string, Exception>)dynamicMethod.CreateDelegate(typeof(Func<Type, string, string, Exception>));
        }
    }
}
