using System;
using UnityEngine;
using VentiCola.UI.Bindings.LowLevel;
using VentiCola.UI.Bindings.LowLevel.Experimental;

namespace VentiCola.UI.Bindings.Experimental
{
    public static class RepeatBindingBuilder
    {
        public static GameObject RepeatForEachOf<T>(this GameObject self, Func<ReactiveList<T>> collection, Action<int, T> renderItemAction = null)
        {
            BaseBinding
                .Allocate<ForEachBinding<T>>()
                .Initalize(self, collection, renderItemAction);
            return self;
        }

        //public static GameObject Count(this GameObject self, Func<int> count, Action<int> renderItemAction)
        //{
        //    //BaseBinding
        //    //    .Allocate<ForEachBinding<int>>()
        //    //    .InitalizeObject(go, m_CollectionFunc, renderItemAction);
        //    return self;
        //}
    }
}