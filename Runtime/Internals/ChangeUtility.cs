using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace VentiCola.UI.Internals
{
    public static class ChangeUtility
    {
        [ThreadStatic] private static Stack<IChangeObserver> s_Observers;
        private static volatile int s_DisableNotification;

        /// <summary>
        /// 获取当前的观察者。如果没有，则返回 null。
        /// </summary>
        public static IChangeObserver CurrentObserver
        {
            get
            {
                if (s_Observers is null or { Count: 0 })
                {
                    return null;
                }

                return s_Observers.Peek();
            }
        }

        public static void BeginObservedRegion(IChangeObserver observer)
        {
            s_Observers ??= new Stack<IChangeObserver>();
            s_Observers.Push(observer);
        }

        public static void EndObservedRegion(IChangeObserver observer)
        {
            bool broken = false;

            if (s_Observers is null or { Count: 0 })
            {
                broken = true;
            }
            else
            {
                IChangeObserver ob = s_Observers.Pop();

                if (!ReferenceEquals(observer, ob))
                {
                    broken = true;
                }
            }

            if (broken)
            {
                Debug.LogError("Observer Stack is broken!");
            }
        }

        public static void BeginNoNotifyRegion()
        {
            Interlocked.Increment(ref s_DisableNotification);
        }

        public static void EndNoNotifyRegion()
        {
            Interlocked.Decrement(ref s_DisableNotification);
        }

        public static void NotifyObservers(UnityWeakHashSet<IChangeObserver> observers)
        {
            if (s_DisableNotification > 0)
            {
                return;
            }

            var currentOb = CurrentObserver;
            var it = observers.GetEnumerator();

            try
            {
                while (it.MoveNext())
                {
                    IChangeObserver observer = it.Current;

                    if (!ReferenceEquals(observer, currentOb))
                    {
                        observer.NotifyChanged();
                    }
                }
            }
            finally
            {
                it.Dispose();
            }
        }
    }
}