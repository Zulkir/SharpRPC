using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SharpRpc.Codecs
{
    public class LocalVariableCollection : ILocalVariableCollection
    {
        readonly ILGenerator il;
        readonly Dictionary<string, LocalBuilder> variables;
        readonly LocalBuilder dataPointer;
        readonly LocalBuilder remainingBytes;

        public LocalVariableCollection(ILGenerator il, bool decode)
        {
            this.il = il;
            variables = new Dictionary<string, LocalBuilder>();
            dataPointer = il.DeclareLocal(typeof(byte*));
            remainingBytes = decode ? il.DeclareLocal(typeof(int)) : null;
        }

        public LocalBuilder DataPointer
        {
            get { return dataPointer; }
        }

        public LocalBuilder RemainingBytes
        {
            get
            {
                if (remainingBytes == null) 
                    throw new InvalidOperationException("Cannot access 'remaining bytes' variable inside encode method");
                return remainingBytes;
            }
        }

        public LocalBuilder GetOrAdd(string name, Func<ILGenerator, LocalBuilder> declareVariable)
        {
            LocalBuilder variable;
            if (!variables.TryGetValue(name, out variable))
            {
                variable = declareVariable(il);
                variables.Add(name, variable);
            }
            return variable;
        }
    }
}