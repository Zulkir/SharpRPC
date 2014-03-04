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
using System.Reflection;
using System.Threading.Tasks;

namespace SharpRpc.Reflection
{
    public class MethodDescriptionBuilder : IMethodDescriptionBuilder
    {
        public MethodDescription Build(MethodInfo methodInfo)
        {
            var remotingType = GetRemotingType(methodInfo.ReturnType);
            var parameters = methodInfo.GetParameters().Select(BuildParameterDescription).ToArray();
            var genericParameters = methodInfo.IsGenericMethod
                ? methodInfo.GetGenericArguments().Select(BuildGenericParameterDescription).ToArray()
                : new GenericParameterDescription[0];
            return new MethodDescription
            {
                RemotingType = GetRemotingType(methodInfo.ReturnType),
                MethodInfo = methodInfo,
                ReturnType = methodInfo.ReturnType,
                Name = methodInfo.Name,
                Parameters = parameters,
                GenericParameters = genericParameters
            };
        }

        private static MethodRemotingType GetRemotingType(Type returnType)
        {
            if (returnType == typeof(Task))
                return MethodRemotingType.AsyncVoid;
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                return MethodRemotingType.AsyncWithRetval;
            return MethodRemotingType.Direct;
        }

        private static MethodParameterDescription BuildParameterDescription(ParameterInfo parameterInfo, int index)
        {
            var way = GetWay(parameterInfo);
            var parameterType = way == MethodParameterWay.Val 
                ? parameterInfo.ParameterType
                : parameterInfo.ParameterType.GetElementType();
            return new MethodParameterDescription(index, parameterType, parameterInfo.Name, way);
        }

        private static MethodParameterWay GetWay(ParameterInfo parameterInfo)
        {
            if (parameterInfo.IsOut)
                return MethodParameterWay.Out;
            if (parameterInfo.ParameterType.IsByRef)
                return MethodParameterWay.Ref;
            return MethodParameterWay.Val;
        }

        private static GenericParameterDescription BuildGenericParameterDescription(Type type)
        {
            return new GenericParameterDescription(type.Name);
        }
    }
}