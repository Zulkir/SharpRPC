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

namespace SharpRpc.Codecs
{
    public unsafe class ExceptionCodec : IManualCodec<Exception>
    {
        private readonly IManualCodec<string> stringCodec; 

        public ExceptionCodec(ICodecContainer codecContainer)
        {
            stringCodec = codecContainer.GetManualCodecFor<string>();
        }

        public int CalculateSize(Exception value)
        {
            return stringCodec.CalculateSize(value.GetType().AssemblyQualifiedName) +
                   stringCodec.CalculateSize(value.Message) +
                   stringCodec.CalculateSize(ComposStackTraceToEncode(value));
        }

        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return null; } }

        public void Encode(ref byte* data, Exception value)
        {
            stringCodec.Encode(ref data, value.GetType().AssemblyQualifiedName);
            stringCodec.Encode(ref data, value.Message);
            stringCodec.Encode(ref data, ComposStackTraceToEncode(value));
        }

        private static readonly Type[] ExceptionConstructorParameterTypes = new[] {typeof(string), typeof(Exception)};

        public Exception Decode(ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            var typeName = stringCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var message = stringCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var stackTrace = stringCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);

            var type = Type.GetType(typeName);
            if (type == null || !typeof(Exception).IsAssignableFrom(type))
                return new Exception(message, new Exception(stackTrace));
            var constructor = type.GetConstructor(ExceptionConstructorParameterTypes);
            if (constructor == null)
                return new Exception(message, new Exception(stackTrace));
            return (Exception)constructor.Invoke(new object[] {message, new Exception(stackTrace)});
        }

        private static string ComposStackTraceToEncode(Exception exception)
        {
            return exception.InnerException != null
                ? exception.InnerException.Message + "--- NETWORK ---\r\n" + exception.StackTrace
                : exception.StackTrace;
        }
    }
}
