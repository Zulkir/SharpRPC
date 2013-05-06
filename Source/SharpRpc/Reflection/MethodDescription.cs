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
            if (!Expressions.Name.IsMatch(name))
                throw new ArgumentException(string.Format("'{0}' is not a valid method name", name), "name");

            this.returnType = returnType;
            this.name = name;
            this.parameters = parameters.ToArray();
        }

        public Type ReturnType { get { return returnType; } }
        public string Name { get { return name; } }
        public IReadOnlyList<MethodParameterDescription> Parameters { get { return parameters; } } 
    }
}