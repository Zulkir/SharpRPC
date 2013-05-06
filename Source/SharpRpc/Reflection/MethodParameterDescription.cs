using System;

namespace SharpRpc.Reflection
{
    public class MethodParameterDescription
    {
        public Type Type { get; private set; }
        public string Name { get; private set; }
        public MethodParameterWay Way { get; private set; }

        public MethodParameterDescription(Type type, string name, MethodParameterWay way = MethodParameterWay.Val)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Method parameter name cannot be null, empty, or consist of whitespace characters");
            if (!Expressions.Name.IsMatch(name))
                throw new ArgumentException(string.Format("'{0}' is not a valid method parameter name", name), "name");
            Type = type;
            Name = name;
            Way = way;
        }
    }
}