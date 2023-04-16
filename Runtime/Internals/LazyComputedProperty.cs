using System;

namespace VentiCola.UI.Internals
{
    /// <summary>
    /// 表示惰性求值的计算属性
    /// </summary>
    /// <typeparam name="T">属性的类型</typeparam>
    public sealed class LazyComputedProperty<T> : IChangeObserver
    {
        private ChangeObserverPlugin m_Plugin;
        private readonly Func<T> m_Getter;
        private WeakHashSet<IChangeObserver> m_Observers;
        private T m_Value;
        private bool m_IsDirty;

        public LazyComputedProperty(Func<T> getter)
        {
            m_Getter = getter;
            m_Observers = default;
            m_Value = default;
            m_IsDirty = true;
        }

        public T Value
        {
            get
            {
                if (m_IsDirty)
                {
                    ChangeUtility.BeginObservedRegion(this);

                    try
                    {
                        m_Value = m_Getter();
                    }
                    finally
                    {
                        m_IsDirty = false;
                        ChangeUtility.EndObservedRegion(this);
                    }
                }

                ChangeUtility.TryAddCurrentObserver(ref m_Observers);
                return m_Value;
            }
        }

        ref ChangeObserverPlugin IChangeObserver.Plugin => ref m_Plugin;

        bool IChangeObserver.IsPassive => false;

        void IChangeObserver.NotifyChanged()
        {
            if (m_IsDirty)
            {
                return;
            }

            m_IsDirty = true;
            ChangeUtility.TryNotify(m_Observers);
        }
    }
}