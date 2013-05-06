using System;

namespace SharpRpc.Reflection
{
    public static class TypeExtensions
    {
        public static string GetServiceName(this Type serviceInterface)
        {
            return serviceInterface.Name.Substring(1);
        }
    }
}
