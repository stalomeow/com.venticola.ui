using System.Runtime.CompilerServices;
using VentiCola.UI.Internal;

namespace VentiCola.UI.Bindings.Experimental
{
    public class ForEachItem<T>
    {
        private int m_Index;
        private WeakHashSet<IChangeObserver> m_IndexObservers;
        private T m_Value;
        private WeakHashSet<IChangeObserver> m_ValueObservers;

        protected internal ForEachItem() { }

        public int Index
        {
            get
            {
                ChangeUtility.TryAddCurrentObserver(ref m_IndexObservers);
                return m_Index;
            }

            protected internal set
            {
                ChangeUtility.SetWithNotify(ref m_Index, value, m_IndexObservers);
            }
        }

        public T Value
        {
            get
            {
                ChangeUtility.TryAddCurrentObserver(ref m_ValueObservers);
                return m_Value;
            }

            protected internal set
            {
                ChangeUtility.SetWithNotify(ref m_Value, value, m_ValueObservers);
            }
        }

        protected internal int GetIndexDirect()
        {
            return m_Index;
        }

        protected internal T GetValueDirect()
        {
            return m_Value;
        }

        protected internal virtual void Reset()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                m_Value = default;
            }

            m_IndexObservers?.Clear();
            m_ValueObservers?.Clear();
        }
    }

    public class ForEachItem<TKey, TValue> : ForEachItem<TValue>
    {
        private TKey m_Key;
        private WeakHashSet<IChangeObserver> m_KeyObservers;

        protected internal ForEachItem() { }

        public TKey Key
        {
            get
            {
                ChangeUtility.TryAddCurrentObserver(ref m_KeyObservers);
                return m_Key;
            }

            protected internal set
            {
                ChangeUtility.SetWithNotify(ref m_Key, value, m_KeyObservers);
            }
        }

        protected internal override void Reset()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
            {
                m_Key = default;
            }

            m_KeyObservers?.Clear();

            base.Reset();
        }
    }
}