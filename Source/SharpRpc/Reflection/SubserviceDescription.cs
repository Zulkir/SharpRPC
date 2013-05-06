using System;

namespace SharpRpc.Reflection
{
    public class SubserviceDescription
    {
        public string Name { get; private set; }
        public ServiceDescription Service { get; private set; }

        public SubserviceDescription(string name, ServiceDescription serviceDesc)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Subservice name cannot be null, empty, or consist of whitespace characters");
            if (!Expressions.Name.IsMatch(name))
                throw new ArgumentException(string.Format("'{0}' is not a valid subservice", name), "name");
            if (serviceDesc == null)
                throw new ArgumentNullException("serviceDesc");

            Name = name;
            Service = serviceDesc;
        }
    }
}