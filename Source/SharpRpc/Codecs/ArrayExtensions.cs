using System;
using System.Collections.Generic;

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

         public static int IndexOfFirst<T>(this IReadOnlyList<T> list, Func<T, bool> condition, int indexOfNone = -1)
         {
             for (int i = 0; i < list.Count; i++)
                 if (condition(list[i]))
                     return i;
             return indexOfNone;
         }
    }
}
