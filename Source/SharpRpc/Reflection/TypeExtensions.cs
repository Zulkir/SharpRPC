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
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace SharpRpc.Reflection
{
    public static class TypeExtensions
    {
        public static string GetServiceName(this Type serviceInterface)
        {
            return serviceInterface.Name.Substring(1);
        }

        public static bool IsDataContract(this Type type)
        {
            return type.GetCustomAttributes<DataContractAttribute>().Any();
        }

        public static IEnumerable<PropertyInfo> EnumerateDataMembers(this Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(x => x.GetCustomAttributes(typeof(DataMemberAttribute), true).Any());
        }

        public static Type DeepSubstituteGenerics(this Type type, IReadOnlyDictionary<string, Type> genericArgumentMap)
        {
            if (type.IsGenericParameter)
                return genericArgumentMap[type.Name];
            if (type.IsGenericType)
                return type.GetGenericTypeDefinition().MakeGenericType(type.GetGenericArguments().Select(x => DeepSubstituteGenerics(x, genericArgumentMap)).ToArray());
            if (type.HasElementType)
                return DeepSubstituteGenerics(type.GetElementType(), genericArgumentMap).MakeArrayType();
            return type;
        }

        public static bool ContainsTypeBuilders(this Type type)
        {
            if (type is GenericTypeParameterBuilder)
                return true;
            if (type.IsGenericType)
                return type.GenericTypeArguments.Any(ContainsTypeBuilders);
            if (type.HasElementType)
                return ContainsTypeBuilders(type.GetElementType());
            return false;
        }

        public static MethodInfo GetMethodSmart(this Type type, string methodName)
        {
            if (type.IsGenericType && type.ContainsTypeBuilders())
            {
                var genericDefinition = type.GetGenericTypeDefinition();
                var methodInfo = genericDefinition.GetMethod(methodName);
                return TypeBuilder.GetMethod(type, methodInfo);
            }
            return type.GetMethod(methodName);
        }

        public static MethodInfo GetMethodSmart(this Type type, Func<MethodInfo, bool> isCorrectMethod)
        {
            if (type.IsGenericType && type.ContainsTypeBuilders())
            {
                var genericDefinition = type.GetGenericTypeDefinition();
                var methodInfo = genericDefinition.GetMethods().Single(isCorrectMethod);
                return TypeBuilder.GetMethod(type, methodInfo);
            }
            return type.GetMethods().Single(isCorrectMethod);
        }

        public static ConstructorInfo GetConstructorSmart(this Type type, Type[] parameterTypes)
        {
            if (type.IsGenericType && type.ContainsTypeBuilders())
            {
                var genericDefinition = type.GetGenericTypeDefinition();
                var constructorInfo = genericDefinition.GetConstructor(parameterTypes);
                return TypeBuilder.GetConstructor(type, constructorInfo);
            }
            return type.GetConstructor(parameterTypes);
        }
    }
}

