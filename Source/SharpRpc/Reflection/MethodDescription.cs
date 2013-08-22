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

namespace SharpRpc.Reflection
{
    public class MethodDescription
    {
        private readonly Type returnType;
        private readonly string name;
        private readonly MethodParameterDescription[] parameters;

        public MethodDescription(Type returnType, string name, IEnumerable<MethodParameterDescription> parameters)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Method name cannot be null, empty, or consist of whitespace characters");

            this.returnType = returnType;
            this.name = name;
            this.parameters = parameters.ToArray();
        }

        public Type ReturnType { get { return returnType; } }
        public string Name { get { return name; } }
        public IReadOnlyList<MethodParameterDescription> Parameters { get { return parameters; } } 
    }
}