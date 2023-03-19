using System;
using System.Collections;
using System.Collections.Generic;
using VentiCola.UI.Internals;

namespace VentiCola.UI
{
    public class ReactiveArray<T> : IReadOnlyList<T>, IOrderedCollection
    {
        private readonly T[] m_Array;
        private HashSet<int> m_DirtyIndices;
        private UnityWeakHashSet<IChangeObserver> m_Observers;

        public ReactiveArray(int length)
        {
            m_Array = new T[length];
        }

        public int Length
        {
            get => m_Array.Length;
        }

        public IEnumerable<int> DirtyIndices
        {
            get => (IEnumerable<int>)m_DirtyIndices ?? Array.Empty<int>();
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= m_Array.Length)
                {
                    throw new IndexOutOfRangeException();
                }

                IChangeObserver observer = ChangeUtility.CurrentObserver;

                if (observer is { IsPassive: false })
                {
                    m_Observers ??= new UnityWeakHashSet<IChangeObserver>();
                    m_Observers.Add(observer);
                }

                return m_Array[index];
            }

            set
            {
                if (index < 0 || index >= m_Array.Length)
                {
                    throw new IndexOutOfRangeException();
                }

                ref T location = ref m_Array[index];

                if (EqualityComparer<T>.Default.Equals(location, value))
                {
                    return;
                }

                location = value;
                m_DirtyIndices ??= new HashSet<int>();
                m_DirtyIndices.Add(index);

                if (m_Observers is not null)
                {
                    ChangeUtility.NotifyObservers(m_Observers);
                }
            }
        }

        int IReadOnlyCollection<T>.Count
        {
            get => m_Array.Length;
        }

        int IOrderedCollection.Count => m_Array.Length;

        bool IOrderedCollection.HasKey => false;

        Type IOrderedCollection.KeyType => throw new NotSupportedException();

        Type IOrderedCollection.ValueType => typeof(T);

        public IEnumerator<T> GetEnumerator()
        {

            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        T1 IOrderedCollection.CastAndGetKeyAt<T1>(int index)
        {
            throw new NotSupportedException();
        }

        T1 IOrderedCollection.CastAndGetValueAt<T1>(int index)
        {
            return CastUtility.CastAny<T, T1>(this[index]);
        }
    }
}