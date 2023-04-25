using System;
using System.Collections.Generic;
using UnityEngine;

namespace VentiCola.UI.Internal
{
    public static class ChangeUtility
    {
        private static int s_DisableNotification = 0;
        private static readonly Stack<IChangeObserver> s_ObserverStack = new();
        private static readonly Action<IChangeObserver> s_NotifyAction = (IChangeObserver ob) =>
        {
            if (ReferenceEquals(ob, CurrentObserver))
            {
                return;
            }

            ob.NotifyChanged();
        };

        /// <summary>
        /// 获取当前的观察者。如果没有，则返回 null。
        /// </summary>
        public static IChangeObserver CurrentObserver => s_ObserverStack.TryPeek(out var ob) ? ob : null;

        public static void TryAddCurrentObserver(ref WeakHashSet<IChangeObserver> observers)
        {
            IChangeObserver observer = CurrentObserver;

            if (observer is { IsPassive: false })
            {
                observers ??= new WeakHashSet<IChangeObserver>();
                observer.Plugin.AddSelfToHashSet(observer, observers);
            }
        }

        public static void SetWithNotify<T>(ref T location, T value, WeakHashSet<IChangeObserver> observers)
        {
            SetWithNotify(ref location, value, observers, EqualityComparer<T>.Default);
        }

        public static void SetWithNotify<T>(ref T location, T value, WeakHashSet<IChangeObserver> observers, IEqualityComparer<T> comparer)
        {
            if (comparer.Equals(location, value))
            {
                return;
            }

            location = value;
            TryNotify(observers);
        }

        public static void SetWithNotify<T, TComparer>(ref T location, T value,
            WeakHashSet<IChangeObserver> observers, ref TComparer comparer) where TComparer : class, IEqualityComparer<T>, new()
        {
            SetWithNotify(ref location, value, observers, comparer ??= new TComparer());
        }

        public static void BeginObservedRegion(IChangeObserver observer)
        {
            s_ObserverStack.Push(observer);
            observer.Plugin.IncreaseStackCount();
        }

        public static void EndObservedRegion(IChangeObserver observer)
        {
            bool popped = s_ObserverStack.TryPop(out IChangeObserver topObserver);

            if (!popped || !ReferenceEquals(observer, topObserver))
            {
                Debug.LogError("Observer Stack is broken!");
                return;
            }

            observer.Plugin.DecreaseStackCount(observer);
        }

        public static void BeginNoNotifyRegion()
        {
            s_DisableNotification++;
        }

        public static void EndNoNotifyRegion()
        {
            s_DisableNotification--;
        }

        /// <summary>
        /// 通知所有观察者
        /// </summary>
        /// <param name="observers">该值可以为 null</param>
        public static void TryNotify(WeakHashSet<IChangeObserver> observers)
        {
            if (observers is null || s_DisableNotification > 0)
            {
                return;
            }

            observers.ForEach(s_NotifyAction);
        }
    }
}