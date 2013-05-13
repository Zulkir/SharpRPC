using System;

namespace SharpRpc.Codecs
{
    public static class ArrayExtensions
    {
         public static int IndexOfFirst<T>(this T[] array, Func<T, bool> condition, int indexOfNone = -1)
         {
             for (int i = 0; i < array.Length; i++)
                 if (condition(array[i]))
                     return i;
             return indexOfNone;
         }
    }
}
