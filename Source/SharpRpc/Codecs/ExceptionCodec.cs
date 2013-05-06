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

        public bool HasFixedSize { get { return false; } }
        public int FixedSize { get { throw new InvalidOperationException(); } }

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
