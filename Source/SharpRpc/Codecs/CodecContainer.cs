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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SharpRpc.Reflection;

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

        private IEmittingCodec CreateCodec(Type type)
        {
            if (TypeIsNativeStructure(type))
                return new NativeStructCodec(type);
            if (type == typeof (string))
                return new StringCodec();
            if (type.IsArray)
                if (TypeIsNativeStructure(type.GetElementType()))
                    return new NativeStructArrayCodec(type.GetElementType());
                else
                    return new ReferenceArrayCodec(type.GetElementType(), this);
            if (type.IsDataContract())
            {
                var members = type.EnumerateDataMembers().ToArray();
                if (SomeMembersAreIncomplete(members))
                    throw new NotSupportedException(string.Format("Data contract '{0}' is incomplete (it has members with missing getters or setters)", type.FullName));
                if (DataContractIsRecursive(type, members) || SomeMembersArePrivate(members))
                    return new IndirectCodec(type, new DataContractCodec(type, this));
                return new DataContractCodec(type, this);
            }
                
            throw new NotSupportedException(string.Format("Type '{0}' is not supported as an RPC parameter type", type.FullName));
        }

        private static bool TypeIsNativeStructure(Type type)
        {
            return (type.IsPrimitive && type != typeof (IntPtr) && type != typeof (UIntPtr)) ||
                   (type.IsValueType && type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                            .All(x => TypeIsNativeStructure(x.FieldType)));
        }

        private static bool DataContractIsRecursive(Type contractType, IEnumerable<PropertyInfo> members)
        {
            return members.Any(x =>
                x.PropertyType == contractType ||
                x.PropertyType.IsDataContract() && DataContractIsRecursive(x.PropertyType, x.PropertyType.EnumerateDataMembers()));
        }

        private static bool SomeMembersAreIncomplete(IEnumerable<PropertyInfo> members)
        {
            return members.Any(x => x.GetGetMethod(true) == null || x.GetSetMethod(true) == null);
        }

        private static bool SomeMembersArePrivate(IEnumerable<PropertyInfo> members)
        {
            return members.Any(x => x.GetGetMethod() == null || x.GetSetMethod(true) == null);
        }
    }
}