using System;
using System.Reflection.Emit;

namespace SharpRpc.Codecs
{
    public interface IEmittingCodec : ICodec
    {
        void EmitCalculateSize(ILGenerator il);
        void EmitEncode(ILGenerator il, ILocalVariableCollection locals, Action<ILGenerator> load);
        void EmitDecode(ILGenerator il, ILocalVariableCollection locals, bool doNotCheckBounds);
    }
}
