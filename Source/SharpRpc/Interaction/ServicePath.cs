using System;
using System.Linq;

namespace SharpRpc.Interaction
{
    public class ServicePath : IEquatable<ServicePath>
    {
        private readonly string[] elements;

        public string this[int index] { get { return elements[index]; } }
        public int Length { get { return elements.Length; } }
        public string ServiceName { get { return elements[0]; } }
        public string MethodName { get { return elements[elements.Length - 1]; } }

        public ServicePath(params string[] elements)
        {
            if (elements == null)
                throw new ArgumentNullException("elements");
            if (elements.Length < 2)
                throw new ArgumentException("Service path must have at least two elements: service name and a method name.");
            if (elements.Any(string.IsNullOrEmpty))
                throw new ArgumentException("All elements of a service path must be non-empty.");
            this.elements = elements;
        }

        public string Format()
        {
            return string.Join("/", elements);
        }

        private static readonly char[] Separators = new[] {'/'};

        public static bool TryParse(string text, out ServicePath path)
        {
            if (text == null)
            {
                path = null;
                return false;
            }
            var elements = text.Split(Separators);
            if (elements.Length < 2)
            {
                path = null;
                return false;
            }
            if (elements.Any(string.IsNullOrEmpty))
            {
                path = null;
                return false;
            }
            path = new ServicePath(elements);
            return true;
        }

        public bool Equals(ServicePath other)
        {
            if (ReferenceEquals(other, null))
                return false;
            if (elements.Length != other.Length)
                return false;
            for (int i = 0; i < elements.Length; i++)
                if (elements[i] != other.elements[i])
                    return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ServicePath);
        }

        public static bool operator ==(ServicePath p1, ServicePath p2)
        {
            if (ReferenceEquals(p1, p2))
                return true;
            if (ReferenceEquals(p1, null))
                return false;
            return p1.Equals(p2);
        }

        public static bool operator !=(ServicePath p1, ServicePath p2)
        {
            return !(p1 == p2);
        }

        public override int GetHashCode()
        {
            int result = 0;
            for (int i = 0; i < elements.Length; i++)
                result += elements[i].GetHashCode();
            return result;
        }

        public override string ToString()
        {
            return Format();
        }
    }
}