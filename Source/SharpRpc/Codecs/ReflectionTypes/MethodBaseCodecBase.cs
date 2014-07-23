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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SharpRpc.Codecs.ReflectionTypes
{
    public abstract unsafe class MethodBaseCodecBase<T> : MemberInfoCodecBase<T> where T : MethodBase
    {
        private readonly IManualCodec<Type[]> typeArrayCodec;

        protected MethodBaseCodecBase(ICodecContainer codecContainer) 
            : base(codecContainer)
        {
            typeArrayCodec = codecContainer.GetManualCodecFor<Type[]>();
        }

        protected abstract IEnumerable<T> AdjustForGenerics(IEnumerable<T> candidates, Type[] genericArguments); 

        protected override void Validate(T value)
        {
            if (value.ContainsGenericParameters)
                throw new NotSupportedException("Methods with unassigned generic parameters are not supported by MethodInfoCodec");
        }

        protected override int CalculateAdditionalSize(T value)
        {
            int result = 0;
            result += typeArrayCodec.CalculateSize(value.IsGenericMethod ? value.GetGenericArguments() : null);
            result += typeArrayCodec.CalculateSize(value.GetParameters().Select(x => x.ParameterType).ToArray());
            return result;
        }

        protected override void EncodeAdditional(ref byte* data, T value)
        {
            typeArrayCodec.Encode(ref data, value.IsGenericMethod ? value.GetGenericArguments() : null);
            typeArrayCodec.Encode(ref data, value.GetParameters().Select(x => x.ParameterType).ToArray());
        }

        protected override T FinishDecoding(Type reflectedType, string name, IEnumerable<T> candidates, ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            var genericArguments = typeArrayCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var parameterTypes = typeArrayCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);

            var method = AdjustForGenerics(candidates, genericArguments).FirstOrDefault(x => ParameterAreMatching(x.GetParameters(), parameterTypes));

            if (method == null)
                throw new InvalidDataException(string.Format("Method {0}.{1}{2}({3}) was not found",
                reflectedType.Name,
                name,
                genericArguments == null ? "" : "<" + string.Join(", ", genericArguments.Select(x => x.Name)) + ">",
                string.Join(", ", parameterTypes.Select(x => x.Name))));

            return method;
        }

        private static bool ParameterAreMatching(ParameterInfo[] parameterInfos, Type[] parameterTypes)
        {
            if (parameterInfos.Length != parameterTypes.Length)
                return false;
            for (int i = 0; i < parameterInfos.Length; i++)
                if (parameterInfos[i].ParameterType != parameterTypes[i])
                    return false;
            return true;
        } 
    }
}