using System;

namespace SharpRpc.Codecs
{
    public class IndirectDataContractCodec : IndirectCodec
    {
        public IndirectDataContractCodec(Type type, ICodecContainer codecContainer)
            : base(type, new DirectDataContractCodec(type, codecContainer))
        {
        }
    }
}
