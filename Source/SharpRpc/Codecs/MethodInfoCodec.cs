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
using System.IO;
using System.Linq;
using System.Reflection;

namespace SharpRpc.Codecs
{
    public unsafe class MethodInfoCodec : IManualCodec<MethodInfo>
    {
        private readonly IManualCodec<string> stringCodec;
        private readonly IManualCodec<Type> typeCodec;
        private readonly IManualCodec<Type[]> typeArrayCodec;

        public MethodInfoCodec(ICodecContainer codecContainer)
        {
            stringCodec = codecContainer.GetManualCodecFor<string>();
            typeCodec = codecContainer.GetManualCodecFor<Type>();
            typeArrayCodec = codecContainer.GetManualCodecFor<Type[]>();
        }

        public Type Type { get { return typeof(MethodInfo); } }
        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return null; } }

        public int CalculateSize(MethodInfo value)
        {
            if (value == null)
                return typeCodec.CalculateSize(null);
            if (value.ContainsGenericParameters)
                throw new NotSupportedException("Methods with unassigned generic parameters are not supported by MaethodInfoCodec");

            int result = 0;
            result += typeCodec.CalculateSize(value.ReflectedType);
            result += typeCodec.CalculateSize(value.DeclaringType);
            result += stringCodec.CalculateSize(value.Name);
            result += typeArrayCodec.CalculateSize(value.IsGenericMethod ? value.GetGenericArguments() : null);
            result += typeArrayCodec.CalculateSize(value.GetParameters().Select(x => x.ParameterType).ToArray());
            return result;
        }

        public void Encode(ref byte* data, MethodInfo value)
        {
            if (value == null)
            {
                typeCodec.Encode(ref data, null);
                return;
            }
            if (value.ContainsGenericParameters)
                throw new NotSupportedException("Methods with unassigned generic parameters are not supported by MaethodInfoCodec");

            typeCodec.Encode(ref data, value.ReflectedType);
            typeCodec.Encode(ref data, value.DeclaringType);
            stringCodec.Encode(ref data, value.Name);
            typeArrayCodec.Encode(ref data, value.IsGenericMethod ? value.GetGenericArguments() : null);
            typeArrayCodec.Encode(ref data, value.GetParameters().Select(x => x.ParameterType).ToArray());
        }

        public MethodInfo Decode(ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            var reflectedType = typeCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            if (reflectedType == null)
                return null;
            var declareingType = typeCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var methodName = stringCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var genericArguments = typeArrayCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var parameterTypes = typeArrayCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);

            foreach (var method in reflectedType.GetMethods())
            {
                var methodToReturn = method;

                if (method.Name != methodName)
                    continue;
                if (method.DeclaringType != declareingType)
                    continue;

                if (genericArguments == null)
                {
                    if (method.IsGenericMethod)
                        continue;
                }
                else
                {
                    if (!method.IsGenericMethod)
                        continue;
                    if (method.GetGenericArguments().Length != genericArguments.Length)
                        continue;

                    methodToReturn = method.MakeGenericMethod(genericArguments);
                }

                if (ParameterMismatch(methodToReturn.GetParameters(), parameterTypes))
                    continue;

                return methodToReturn;
            }

            throw new InvalidDataException(string.Format("Method {0}.{1}{2}({3}) was not found", 
                reflectedType.Name, 
                methodName, 
                genericArguments == null ? "" : "<" + string.Join(", ", genericArguments.Select(x => x.Name))  + ">",
                string.Join(", ", parameterTypes.Select(x => x.Name))));
        }

        private static bool ParameterMismatch(ParameterInfo[] parameterInfos, Type[] parameterTypes)
        {
            if (parameterInfos.Length != parameterTypes.Length)
                return true;
            for (int i = 0; i < parameterInfos.Length; i++)
                if (parameterInfos[i].ParameterType != parameterTypes[i])
                    return true;
            return false;
        }
    }
}