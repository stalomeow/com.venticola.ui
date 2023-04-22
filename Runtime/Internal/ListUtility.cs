using System.Collections.Generic;

namespace VentiCola.UI.Internal
{
    internal static class ListUtility
    {
        /// <summary>
        /// 对 <paramref name="list"/> 中元素顺序无要求时，可以使用该方法快速移除元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="i"></param>
        public static void FastRemoveAt<T>(this List<T> list, int i)
        {
            int lastIndex = list.Count - 1;

            if (i != lastIndex)
            {
                list[i] = list[lastIndex];
            }

            list.RemoveAt(lastIndex);
        }

        public static T PopBackUnsafe<T>(this List<T> list)
        {
            int lastIndex = list.Count - 1;
            T top = list[lastIndex];
            list.RemoveAt(lastIndex);
            return top;
        }

        public static T PeekBackOrDefault<T>(this List<T> list, T defaultValue)
        {
            int lastIndex = list.Count - 1;
            return lastIndex < 0 ? defaultValue : list[lastIndex];
        }
    }
}