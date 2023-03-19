using System;
using VentiCola.UI.Internals;

namespace VentiCola.UI
{
    /// <summary>
    /// 表示惰性求值的计算属性
    /// </summary>
    /// <typeparam name="T">属性的类型</typeparam>
    public class LazyComputedProperty<T> : IChangeObserver
    {
        private readonly Func<T> m_Getter;
        private readonly bool m_NoBranches;
        private UnityWeakHashSet<IChangeObserver> m_Observers;
        private int m_Version;
        private T m_CachedValue;
        private bool m_Dirty;
        private bool m_ExecutedAtLeastOnce;

        public LazyComputedProperty(Func<T> getter) : this(getter, false) { }

        public LazyComputedProperty(Func<T> getter, bool noBranches)
        {
            m_Getter = getter;
            m_NoBranches = noBranches;
            m_Observers = default;
            m_Version = 0;

            InitCachedValue();
        }

        private void InitCachedValue()
        {
            m_CachedValue = default;
            m_Dirty = true;
            m_ExecutedAtLeastOnce = false;
        }

        private bool ExecuteGetterSafe(out T value, out Exception exception)
        {
            ChangeUtility.BeginObservedRegion(this);

            try
            {
                value = m_Getter();
                exception = null;
                return true;
            }
            catch (Exception e)
            {
                value = default;
                exception = e;
                return false;
            }
            finally
            {
                ChangeUtility.EndObservedRegion(this);
            }
        }

        public T Value
        {
            get
            {
                if (m_Dirty)
                {
                    if (ExecuteGetterSafe(out T value, out Exception exception))
                    {
                        m_CachedValue = value;
                        m_Dirty = false;
                        m_ExecutedAtLeastOnce = true;
                    }
                    else
                    {
                        throw new Exception("Failed to execute the getter!", exception);
                    }
                }

                IChangeObserver observer = ChangeUtility.CurrentObserver;

                if (observer is { IsPassive: false })
                {
                    m_Observers ??= new UnityWeakHashSet<IChangeObserver>();
                    m_Observers.Add(observer);
                }

                return m_CachedValue;
            }
        }

        int IReusableObject.Version => m_Version;

        bool IChangeObserver.IsPassive => m_NoBranches && m_ExecutedAtLeastOnce;

        void IReusableObject.ResetObject()
        {
            m_Observers?.Clear();
            m_Version++;

            InitCachedValue();
        }

        void IChangeObserver.NotifyChanged()
        {
            m_Dirty = true;

            if (m_Observers is not null)
            {
                ChangeUtility.NotifyObservers(m_Observers);
            }
        }
    }
}