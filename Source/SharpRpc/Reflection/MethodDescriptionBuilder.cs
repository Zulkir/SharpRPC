using System.Linq;
using System.Reflection;

namespace SharpRpc.Reflection
{
    public class MethodDescriptionBuilder : IMethodDescriptionBuilder
    {
        public MethodDescription Build(MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            var parameterDescs = parameters.Select(parameterInfo => new MethodParameterDescription(parameterInfo.ParameterType, parameterInfo.Name, GetWay(parameterInfo)));
            return new MethodDescription(methodInfo.ReturnType, methodInfo.Name, parameterDescs);
        }

        private static MethodParameterWay GetWay(ParameterInfo parameterInfo)
        {
            if (parameterInfo.IsOut)
                return MethodParameterWay.Out;
            if (parameterInfo.ParameterType.IsByRef)
                return MethodParameterWay.Ref;
            return MethodParameterWay.Val;
        }
    }
}