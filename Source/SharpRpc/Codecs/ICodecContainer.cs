using System;

namespace SharpRpc.Codecs
{
    public interface ICodecContainer
    {
        IEmittingCodec GetEmittingCodecFor(Type type);
        IManualCodec<T> GetManualCodecFor<T>();
    }
}
