using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace SharpRpc.Codecs
{
    public class CodecContainer : ICodecContainer
    {
        private readonly ConcurrentDictionary<Type, IEmittingCodec> emittingCodecs = new ConcurrentDictionary<Type, IEmittingCodec>();
        private readonly ConcurrentDictionary<Type, IManualCodec> manualCodecs = new ConcurrentDictionary<Type, IManualCodec>();

        public IEmittingCodec GetEmittingCodecFor(Type type)
        {
            return emittingCodecs.GetOrAdd(type, x => CreateCodec(type));
        }

        public IManualCodec<T> GetManualCodecFor<T>()
        {
            return (IManualCodec<T>)manualCodecs.GetOrAdd(typeof(T), x => CreateManualCodec<T>());
        }

        private IManualCodec<T> CreateManualCodec<T>()
        {
            if (typeof(T) == typeof(Exception))
                return (IManualCodec<T>)new ExceptionCodec(this);
            return new ManualCodec<T>(GetEmittingCodecFor(typeof(T)));
        }

        private static IEmittingCodec CreateCodec(Type type)
        {
            if (TypeIsPointableStructure(type))
                return new PointableStructCodec(type);
            if (type == typeof (string))
                return new StringCodec();
            throw new NotSupportedException(string.Format("Type '{0}' is not supported as an RPC parameter type", type.FullName));
        }

        private static bool TypeIsPointableStructure(Type type)
        {
            return (type.IsPrimitive && type != typeof (IntPtr) && type != typeof (UIntPtr)) ||
                   (type.IsValueType && type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                            .All(x => TypeIsPointableStructure(x.FieldType)));
        }
    }
}