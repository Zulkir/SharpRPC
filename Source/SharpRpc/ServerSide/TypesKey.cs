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
using System.Linq;

namespace SharpRpc.ServerSide
{
    public struct TypesKey : IEquatable<TypesKey>
    {
        public readonly Type[] Types;

        public TypesKey(Type[] types) : this()
        {
            Types = types ?? Type.EmptyTypes;
        }

        public bool Equals(TypesKey other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is TypesKey && this == (TypesKey)obj;
        }

        public override int GetHashCode()
        {
            return Types.Sum(x => x.GetHashCode());
        }

        public override string ToString()
        {
            return "[" + string.Join(", ", Types.Select(x => x.Name)) + "]";
        }

        public static bool operator ==(TypesKey k1, TypesKey k2)
        {
            if (k1.Types.Length != k2.Types.Length)
                return false;
            for (int i = 0; i < k1.Types.Length; i++)
                if (k1.Types[i] != k2.Types[i])
                    return false;
            return true;
        }

        public static bool operator !=(TypesKey k1, TypesKey k2)
        {
            return !(k1 == k2);
        }
    }
}