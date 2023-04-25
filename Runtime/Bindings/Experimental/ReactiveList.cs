using System;
using System.Collections;
using System.Collections.Generic;
using VentiCola.UI.Internal;

namespace VentiCola.UI.Bindings.Experimental
{
    public class ReactiveList<T> : IList<T>, IReactiveCollection
    {
        private readonly List<T> m_Items;
        private WeakHashSet<IChangeObserver> m_Observers;

        public ReactiveList()
        {
            m_Items = new List<T>();
        }

        public ReactiveList(IEnumerable<T> collection)
        {
            m_Items = new List<T>(collection);
        }

        public ReactiveList(int capacity)
        {
            m_Items = new List<T>(capacity);
        }

        public void Add(T item)
        {
            m_Items.Add(item);
            ChangeUtility.TryNotify(m_Observers);
        }

        public void Clear()
        {
            m_Items.Clear();
            ChangeUtility.TryNotify(m_Observers);
        }

        public bool Contains(T item)
        {
            ChangeUtility.TryAddCurrentObserver(ref m_Observers);
            return m_Items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ChangeUtility.TryAddCurrentObserver(ref m_Observers);
            m_Items.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            if (m_Items.Remove(item))
            {
                ChangeUtility.TryNotify(m_Observers);
                return true;
            }

            return false;
        }

        public int Count
        {
            get
            {
                ChangeUtility.TryAddCurrentObserver(ref m_Observers);
                return m_Items.Count;
            }
        }

        bool ICollection<T>.IsReadOnly
        {
            get
            {
                ChangeUtility.TryAddCurrentObserver(ref m_Observers);
                return ((IList<T>)m_Items).IsReadOnly;
            }
        }

        public int IndexOf(T item)
        {
            ChangeUtility.TryAddCurrentObserver(ref m_Observers);
            return m_Items.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            m_Items.Insert(index, item);
            ChangeUtility.TryNotify(m_Observers);
        }

        public void RemoveAt(int index)
        {
            m_Items.RemoveAt(index);
            ChangeUtility.TryNotify(m_Observers);
        }

        public T this[int index]
        {
            get
            {
                ChangeUtility.TryAddCurrentObserver(ref m_Observers);
                return m_Items[index];
            }

            set
            {
                if (EqualityComparer<T>.Default.Equals(m_Items[index], value))
                {
                    return;
                }

                m_Items[index] = value;

                ChangeUtility.TryNotify(m_Observers);
            }
        }

        T1 IReactiveCollection.GetKeyAt<T1>(Index index)
        {
            throw new NotSupportedException();
        }

        T1 IReactiveCollection.GetValueAt<T1>(Index index)
        {
            return CastUtility.CastAny<T, T1>(this[index]);
        }

        public void CopyTo(ref IReactiveCollection destination, Range range)
        {
            if (destination is null || destination is not ReactiveList<T>)
            {
                destination = new ReactiveList<T>();
            }

            var list = (ReactiveList<T>)destination;
            list.m_Items.Clear();

            (int offset, int length) = range.GetOffsetAndLength(m_Items.Count);

            for (int i = 0; i < length; i++)
            {
                list.m_Items.Add(m_Items[offset + i]);
            }
        }

        List<T>.Enumerator GetEnumerator()
        {
            ChangeUtility.TryAddCurrentObserver(ref m_Observers);
            return m_Items.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}