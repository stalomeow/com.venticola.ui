using System.Collections.Generic;

namespace VentiCola.UI.Internals
{
    internal static class ListUtility
    {
        /// <summary>
        /// 对 <paramref name="list"/> 中元素顺序无要求时，可以使用该方法快速移除元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="i"></param>
        public static void FastRemoveAt<T>(List<T> list, int i)
        {
            int lastIndex = list.Count - 1;

            if (i != lastIndex)
            {
                list[i] = list[lastIndex];
            }

            list.RemoveAt(lastIndex);
        }
    }
}