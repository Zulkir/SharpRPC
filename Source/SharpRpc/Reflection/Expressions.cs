using System.Text.RegularExpressions;

namespace SharpRpc.Reflection
{
    public static class Expressions
    {
        private static readonly Regex name = new Regex(@"^([_a-zA-Z]\w*)$");

        public static Regex Name { get { return name; } }
    }
}