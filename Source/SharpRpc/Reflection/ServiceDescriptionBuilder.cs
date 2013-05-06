using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpRpc.Reflection
{
    public class ServiceDescriptionBuilder : IServiceDescriptionBuilder
    {
        private readonly IMethodDescriptionBuilder methodDescriptionBuilder;

        public ServiceDescriptionBuilder(IMethodDescriptionBuilder methodDescriptionBuilder)
        {
            this.methodDescriptionBuilder = methodDescriptionBuilder;
        }

        public ServiceDescription Build(Type interfaceType)
        {
            var properties = interfaceType.GetProperties();
            var subinterfaceDescs = new List<SubserviceDescription>();
            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.SetMethod != null)
                    throw new ArgumentException(string.Format("{0} is not a valid service interface since it has a setter ({1})", interfaceType.Name, propertyInfo.Name));
                subinterfaceDescs.Add(new SubserviceDescription(propertyInfo.Name, Build(propertyInfo.PropertyType)));
            }

            var methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => !properties.Any(p => p.GetMethod == m || p.SetMethod == m));
            var methodDescs = methods.Select(methodInfo => methodDescriptionBuilder.Build(methodInfo));
            return new ServiceDescription(interfaceType, interfaceType.Name.Substring(1), subinterfaceDescs, methodDescs);
        }
    }
}