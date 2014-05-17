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
using System.Reflection;
using System.Threading.Tasks;
using SharpRpc.Reflection;

namespace SharpRpc.Codecs
{
    public static class TaskMethods
    {
        public static MethodInfo GetResult(Type resultType) { return typeof(Task<>).MakeGenericType(resultType).GetMethodSmart("get_Result"); }
        public static MethodInfo FromResult(Type resultType) { return typeof(Task).GetMethod("FromResult").MakeGenericMethod(resultType); }

        public static MethodInfo ContinueWith(Type originalResultType, Type continuationResultType)
        {
            return typeof(Task<>).MakeGenericType(originalResultType).GetMethodSmart(IsCorrectContinueWith).MakeGenericMethod(continuationResultType);
        }

        private static bool IsCorrectContinueWith(MethodInfo methodInfo)
        {
            if (methodInfo.Name != "ContinueWith")
                return false;

            var parameters = methodInfo.GetParameters();
            if (parameters.Length != 1)
                return false;

            var parameterType = parameters[0].ParameterType;
            if (parameterType.GetGenericTypeDefinition() != typeof(Func<,>))
                return false;

            if (!parameterType.GetGenericArguments()[0].IsGenericType)
                return false;

            return true;
        }
    }
}