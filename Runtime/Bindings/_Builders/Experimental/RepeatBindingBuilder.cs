using System;
using System.Collections.Generic;
using UnityEngine;
using VentiCola.UI.Bindings.LowLevel;
using VentiCola.UI.Bindings.LowLevel.Experimental;

namespace VentiCola.UI.Bindings.Experimental
{
    public static class RepeatBindingBuilder
    {
        public static GameObject RepeatForEachOf<T>(
            this GameObject self,
            Func<GameObject, ReactiveList<T>> collection,
            Action<ForEachItem<T>> renderItemAction = null,
            IEqualityComparer<T> customEqualityComparer = null)
        {
            BaseBinding
                .Allocate<ForEachBinding<T>>()
                .Initalize(self, collection, renderItemAction, customEqualityComparer);
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