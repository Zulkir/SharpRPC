using System;

namespace SharpRpc.Utility
{
    public struct ScopeKey : IEquatable<ScopeKey>
    {
        public string Scope;

        public ScopeKey(string scope)
        {
            Scope = scope;
        }

        public bool Equals(ScopeKey other)
        {
            return Scope == other.Scope;
        }

        public override bool Equals(object obj)
        {
            return obj is ScopeKey && Equals((ScopeKey)obj);
        }

        public override int GetHashCode()
        {
            return Scope == null ? 0 : Scope.GetHashCode();
        }

        public override string ToString()
        {
            return Scope;
        }
    }
}