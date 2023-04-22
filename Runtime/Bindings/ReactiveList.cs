using System;
using System.Collections;
using System.Collections.Generic;
using VentiCola.UI.Internal;

namespace VentiCola.UI.Bindings
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
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get => m_Items.Count;
        }

        public bool IsReadOnly { get; }

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
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

        int IReactiveCollection.Count => m_Items.Count;

        // bool IReactiveCollection.HasKey => false;
        //
        // Type IReactiveCollection.KeyType => throw new NotSupportedException();
        //
        // Type IReactiveCollection.ValueType => typeof(T);

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

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}