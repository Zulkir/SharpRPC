namespace SharpRpc.Utility
{
    public static class CommonImmuables
    {
        private static readonly byte[] EmptyBytesField = new byte[0];
        public static byte[] EmptyBytes { get { return EmptyBytesField; } }

        private static readonly object[] EmptyObjectsField = new object[0];
        public static object[] EmptyObjects { get { return EmptyObjectsField; } }
    }
}
