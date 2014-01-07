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

namespace SharpRpc.Reflection
{
    public class ServiceDescription
    {
        private readonly Type type;
        private readonly string name;
        private readonly SubserviceDescription[] subservices;
        private readonly MethodDescription[] methods;

        public ServiceDescription(Type type, string name, IEnumerable<SubserviceDescription> subservices, IEnumerable<MethodDescription> methods)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Method name cannot be null, empty, or consist of whitespace characters");

            this.type = type;
            this.name = name;
            this.subservices = subservices.ToArray();
            this.methods = methods.ToArray();
        }

        public Type Type { get { return type; } }
        public string Name { get { return name; } }
        public IReadOnlyList<SubserviceDescription> Subservices { get { return subservices; } }
        public IReadOnlyList<MethodDescription> Methods { get { return methods; } }

        public bool TryGetSubservice(string subserviceName, out SubserviceDescription description)
        {
            for (int i = 0; i < subservices.Length; i++)
            {
                if (subservices[i].Name == subserviceName)
                {
                    description = subservices[i];
                    return true;
                }
            }
            description = null;
            return false;
        }

        public bool TryGetMethod(string methodName, out MethodDescription description)
        {
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == methodName)
                {
                    description = methods[i];
                    return true;
                }
            }
            description = null;
            return false;
        }
    }
}