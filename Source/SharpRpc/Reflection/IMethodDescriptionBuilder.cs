using System.Reflection;

namespace SharpRpc.Reflection
{
    public interface IMethodDescriptionBuilder
    {
        MethodDescription Build(MethodInfo methodInfo);
    }
}