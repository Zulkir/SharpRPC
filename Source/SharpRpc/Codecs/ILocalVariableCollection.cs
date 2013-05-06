using System;
using System.Reflection.Emit;

namespace SharpRpc.Codecs
{
    public interface ILocalVariableCollection
    {
        LocalBuilder DataPointer { get; }
        LocalBuilder RemainingBytes { get; }
        LocalBuilder GetOrAdd(string name, Func<ILGenerator, LocalBuilder> declareVariable);
    }
}
