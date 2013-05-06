using System;
using System.Collections.Concurrent;

namespace SharpRpc.Utility
{
    public static class ThreadGuard
    {
        private static readonly ConcurrentDictionary<object, object> lockObjects = new ConcurrentDictionary<object, object>();

        public static void RunOnce<T>(T obj, Action<T> action)
        {
            if (ReferenceEquals(obj, null))
                throw new ArgumentNullException();

            var lockObject = lockObjects.GetOrAdd(obj, x => new object());

            lock (lockObject)
            {
                action(obj);
            }
        }
    }
}