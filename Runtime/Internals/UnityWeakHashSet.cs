using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VentiCola.UI.Internals
{
    internal sealed class UnityWeakHashSetDebugView<T> where T : class, IReusableObject
    {
        private readonly UnityWeakHashSet<T> m_HashSet;

        public UnityWeakHashSetDebugView(UnityWeakHashSet<T> hashSet)
        {
            m_HashSet = hashSet;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get => System.Linq.Enumerable.ToArray(m_HashSet);
        }
    }

    /// <summary>
    /// 可以遍历的短弱引用 HashSet。支持对 <see cref="UnityEngine.Object"/> 的短弱引用。
    /// </summary>
    /// <typeparam name="T">集合元素的类型。</typeparam>
    [DebuggerTypeProxy(typeof(UnityWeakHashSetDebugView<>))]
    public class UnityWeakHashSet<T> : IEnumerable<T>, IDisposable where T : class, IReusableObject
    {
        public struct Enumerator : IEnumerator<T>
        {
            private UnityWeakHashSet<T> m_HashSet;
            private int m_BucketIndex;
            private int m_EntryIndex;
            private int m_PrevEntryIndex;
            private T m_Current;

            internal Enumerator(UnityWeakHashSet<T> hashSet)
            {
                hashSet.m_DisableModification = true;

                m_HashSet = hashSet;
                m_BucketIndex = -2; // -2 means not started
                m_EntryIndex = -2; // -2 means not started
                m_PrevEntryIndex = -2;
                m_Current = null;
            }

            public T Current
            {
                get
                {
                    if (m_BucketIndex < 0 || m_HashSet is null)
                    {
                        throw new InvalidOperationException();
                    }

                    return m_Current;
                }
            }

            public bool MoveNext()
            {
                if (m_HashSet is null)
                {
                    throw new InvalidOperationException();
                }

                if (m_BucketIndex < 0)
                {
                    m_BucketIndex = 0;
                }

                while (m_BucketIndex < m_HashSet.m_Buckets.Length)
                {
                    ref int firstEntryIndexPlusOne = ref m_HashSet.m_Buckets[m_BucketIndex];

                    if (m_EntryIndex < -1)
                    {
                        m_EntryIndex = firstEntryIndexPlusOne - 1;
                        m_PrevEntryIndex = -1;
                    }

                    while (m_EntryIndex >= 0)
                    {
                        ref Entry entry = ref m_HashSet.m_Entries[m_EntryIndex];

                        if (m_HashSet.TryExtractEntryValueIfAlive(in entry, out m_Current))
                        {
                            m_PrevEntryIndex = m_EntryIndex;
                            m_EntryIndex = entry.Next;
                            return true;
                        }
                        else
                        {
                            int nextEntryIndex = entry.Next;

                            m_HashSet.FreeEntry(ref firstEntryIndexPlusOne, m_EntryIndex, m_PrevEntryIndex);

                            // shouldn't change m_PrevEntryIndex here
                            m_EntryIndex = nextEntryIndex;
                        }
                    }

                    m_BucketIndex++;
                    m_EntryIndex = -2;
                }

                Dispose();
                return false;
            }

            public void Dispose()
            {
                if (m_HashSet is not null)
                {
                    m_HashSet.m_DisableModification = false;
                    m_HashSet = null;
                    m_Current = null; // remove strong reference
                }
            }

            object IEnumerator.Current => Current;

            public void Reset() => throw new NotSupportedException();
        }

        private struct Entry
        {
            public int Next;
            public uint HashCode;
            public int Version;
            public GCHandle Value; // 这里不直接用 WeakReference，它有额外的开销
        }

        private int[] m_Buckets; // element stores the first entry index plus one, so zero is invalid
        private Entry[] m_Entries;
        private int m_Count; // not the exact count of element. Actually it is the sum of HashSet-Element-Count and FreeList-Element-Count
        private int m_FreeList; // -1 means no elements
        private bool m_DisableModification;
        private bool m_Disposed;

        public UnityWeakHashSet() { }

        ~UnityWeakHashSet() => DisposeImpl();

        public void Dispose()
        {
            if (m_DisableModification)
            {
                throw new InvalidOperationException("Can not modify WeakHashSet when enumerating it.");
            }

            DisposeImpl();
            GC.SuppressFinalize(this);
        }

        protected virtual void DisposeImpl()
        {
            if (!m_Disposed && m_Buckets is not null)
            {
                FreeAllHandles();
                m_Buckets = null;
                m_Entries = null;
                m_Disposed = true;
            }
        }

        private void FreeAllHandles()
        {
            for (int i = 0; i < m_Count; i++)
            {
                GCHandle handle = m_Entries[i].Value;

                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }

        public int Capacity
        {
            get
            {
                if (m_Disposed)
                {
                    throw new ObjectDisposedException(nameof(UnityWeakHashSet<T>));
                }

                if (m_Entries is null)
                {
                    return 0;
                }

                return m_Entries.Length;
            }
        }

        public bool Add(T item)
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(nameof(UnityWeakHashSet<T>));
            }

            if (m_DisableModification)
            {
                throw new InvalidOperationException("Can not modify WeakHashSet when enumerating it.");
            }

            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (m_Buckets is null)
            {
                Initialize(0);
            }

            // GetHashCode() 返回值可能是负的
            uint hashCode = (uint)item.GetHashCode(); // must use object.GetHashCode() here.
            uint bucketIndex = hashCode % (uint)m_Buckets.Length;
            ref int firstEntryIndexPlusOne = ref m_Buckets[bucketIndex];

            if (FindItemInBucket(ref firstEntryIndexPlusOne, item, hashCode, out _, out _))
            {
                return false;
            }

            // find a new entry and insert the item

            if (m_FreeList < 0 && m_Count == m_Entries.Length)
            {
                // exclude the bucket which has been gone through above
                ScavengeDeadEntries(excludeBucketIndex: bucketIndex);
            }

            int index;

            if (m_FreeList >= 0)
            {
                index = m_FreeList;
                m_FreeList = m_Entries[index].Next;
            }
            else
            {
                if (m_Count == m_Entries.Length)
                {
                    Resize();

                    bucketIndex = hashCode % (uint)m_Buckets.Length;
                    firstEntryIndexPlusOne = ref m_Buckets[bucketIndex];
                }

                index = m_Count;
                m_Count += 1;
            }

            ref Entry entry = ref m_Entries[index];
            entry.HashCode = hashCode;
            entry.Version = item.Version;
            entry.Value = GCHandle.Alloc(item, GCHandleType.Weak);
            entry.Next = firstEntryIndexPlusOne - 1;
            firstEntryIndexPlusOne = index + 1;

            return true;
        }

        public bool Remove(T item)
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(nameof(UnityWeakHashSet<T>));
            }

            if (m_DisableModification)
            {
                throw new InvalidOperationException("Can not modify WeakHashSet when enumerating it.");
            }

            if (m_Buckets is null)
            {
                return false;
            }

            uint hashCode = (uint)item.GetHashCode(); // must use object.GetHashCode() here.
            uint bucketIndex = hashCode % (uint)m_Buckets.Length;
            ref int firstEntryIndexPlusOne = ref m_Buckets[bucketIndex];

            if (FindItemInBucket(ref firstEntryIndexPlusOne, item, hashCode, out int entryIndex, out int prevEntryIndex))
            {
                FreeEntry(ref firstEntryIndexPlusOne, entryIndex, prevEntryIndex);
                return true;
            }

            return false;
        }

        public void Clear()
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(nameof(UnityWeakHashSet<T>));
            }

            if (m_DisableModification)
            {
                throw new InvalidOperationException("Can not modify WeakHashSet when enumerating it.");
            }

            if (m_Buckets is not null && m_Count > 0)
            {
                FreeAllHandles();
                Array.Clear(m_Buckets, 0, m_Buckets.Length);
                Array.Clear(m_Entries, 0, m_Count);

                m_Count = 0;
                m_FreeList = -1;
            }
        }

        public Enumerator GetEnumerator()
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(nameof(UnityWeakHashSet<T>));
            }

            if (m_DisableModification)
            {
                throw new InvalidOperationException();
            }

            if (m_Buckets is null)
            {
                Initialize(0);
            }

            return new Enumerator(this);
        }

        private bool TryExtractEntryValueIfAlive(in Entry entry, out T value)
        {
            GCHandle handle = entry.Value;

            if (!handle.IsAllocated)
            {
                value = null;
                return false;
            }

            object target = handle.Target;

            if (target is UnityEngine.Object unityObj)
            {
                value = (unityObj == null) ? null : (T)target;
            }
            else
            {
                value = (T)target;
            }

            if (value is null || value.Version != entry.Version)
            {
                value = null;
                return false;
            }

            return true;
        }

        private bool FindItemInBucket(ref int firstEntryIndexPlusOne, T item, uint hashCode, out int itemEntryIndex, out int itemPrevEntryIndex)
        {
            int entryIndex = firstEntryIndexPlusOne - 1;
            int prevEntryIndex = -1;

            while (entryIndex >= 0)
            {
                ref Entry entry = ref m_Entries[entryIndex];

                if (TryExtractEntryValueIfAlive(in entry, out T value))
                {
                    // if item is null, we will just go through all the entries in the bucket.
                    if (item is not null)
                    {
                        if (entry.HashCode == hashCode && ReferenceEquals(value, item))
                        {
                            itemEntryIndex = entryIndex;
                            itemPrevEntryIndex = prevEntryIndex;
                            return true;
                        }
                    }

                    prevEntryIndex = entryIndex;
                    entryIndex = entry.Next;
                }
                else
                {
                    int nextEntryIndex = entry.Next;

                    FreeEntry(ref firstEntryIndexPlusOne, entryIndex, prevEntryIndex);

                    // shouldn't change prevEntryIndex here
                    entryIndex = nextEntryIndex;
                }
            }

            itemEntryIndex = -1;
            itemPrevEntryIndex = -1;
            return false;
        }

        private void FreeEntry(ref int firstEntryIndexPlusOne, int entryIndex, int prevEntryIndex)
        {
            ref Entry entry = ref m_Entries[entryIndex];

            if (prevEntryIndex >= 0)
            {
                m_Entries[prevEntryIndex].Next = entry.Next;
            }
            else
            {
                firstEntryIndexPlusOne = entry.Next + 1;
            }

            entry.Value.Free();
            entry.Value = default;
            entry.Next = m_FreeList;
            m_FreeList = entryIndex;
        }

        private void ScavengeDeadEntries(uint excludeBucketIndex)
        {
            uint length = (uint)m_Buckets.Length;

            for (uint i = 0; i < length; i++)
            {
                if (i != excludeBucketIndex)
                {
                    FindItemInBucket(ref m_Buckets[i], null, 0, out _, out _);
                }
            }
        }

        private void Resize()
        {
            int newSize = HashHelpers.ExpandPrime(m_Count);
            var buckets = new int[newSize];
            var entries = new Entry[newSize];

            Array.Copy(m_Entries, entries, m_Count);

            for (int i = 0; i < m_Count; i++)
            {
                ref Entry entry = ref entries[i];

                if (entry.Value.IsAllocated)
                {
                    uint newBucketIndex = entry.HashCode % (uint)newSize;
                    ref int firstEntryIndexPlusOne = ref buckets[newBucketIndex];
                    entry.Next = firstEntryIndexPlusOne - 1;
                    firstEntryIndexPlusOne = i + 1;
                }
            }

            // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
            m_Buckets = buckets;
            m_Entries = entries;
        }

        private void Initialize(int capacity)
        {
            int size = HashHelpers.GetPrime(capacity);
            var buckets = new int[size];
            var entries = new Entry[size];

            // Assign member variables after both arrays are allocated to guard against corruption from OOM if second fails.
            m_Buckets = buckets;
            m_Entries = entries;
            m_Count = 0;
            m_FreeList = -1;
            m_DisableModification = false;
            m_Disposed = false;
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