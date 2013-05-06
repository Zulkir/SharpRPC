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
            if (!Expressions.Name.IsMatch(name))
                throw new ArgumentException(string.Format("'{0}' is not a valid method name", name), "name");

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