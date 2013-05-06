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