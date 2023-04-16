using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VentiCola.UI.Internals
{
    internal sealed class WeakHashSetDebugView<T> where T : class, IVersionable
    {
        private readonly WeakHashSet<T> m_HashSet;

        public WeakHashSetDebugView(WeakHashSet<T> hashSet)
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
    [DebuggerTypeProxy(typeof(WeakHashSetDebugView<>))]
    public class WeakHashSet<T> : IEnumerable<T> where T : class
    {
        public struct Enumerator : IEnumerator<T>
        {
            private readonly int m_Version;
            private WeakHashSet<T> m_HashSet;
            private int m_BucketIndex;
            private int m_EntryIndex;
            private int m_PrevEntryIndex;
            private T m_Current; // hold a strong reference here
            private bool m_IsEnd;

            public Enumerator(WeakHashSet<T> hashSet)
            {
                m_Version = hashSet.m_Version;
                m_HashSet = hashSet;
                m_BucketIndex = -2; // -2 means not started
                m_EntryIndex = -2; // -2 means not started
                m_PrevEntryIndex = -2;
                m_Current = null;
                m_IsEnd = false;
            }

            public T Current
            {
                get
                {
                    DoCollectionCheck();

                    if (m_BucketIndex < 0)
                    {
                        throw new InvalidOperationException();
                    }

                    if (m_IsEnd)
                    {
                        throw new InvalidOperationException();
                    }

                    return m_Current;
                }
            }

            public bool MoveNext()
            {
                DoCollectionCheck();

                if (m_IsEnd)
                {
                    throw new InvalidOperationException();
                }

                if (m_BucketIndex < 0)
                {
                    m_BucketIndex = 0;
                }

                while (m_BucketIndex < m_HashSet.m_Buckets.Length)
                {
                    ref int bucket = ref m_HashSet.m_Buckets[m_BucketIndex];

                    if (m_EntryIndex < -1)
                    {
                        m_EntryIndex = bucket - 1;
                        m_PrevEntryIndex = -1;
                    }

                    while (m_EntryIndex >= 0)
                    {
                        ref Entry entry = ref m_HashSet.m_Entries[m_EntryIndex];

                        if (TryGetGCHandleTarget(entry.Handle, out m_Current))
                        {
                            m_PrevEntryIndex = m_EntryIndex;
                            m_EntryIndex = entry.Next;
                            return true;
                        }
                        else
                        {
                            int nextEntryIndex = entry.Next;

                            m_HashSet.FreeEntry(ref bucket, m_EntryIndex, m_PrevEntryIndex);

                            // shouldn't change m_PrevEntryIndex here
                            m_EntryIndex = nextEntryIndex;
                        }
                    }

                    m_BucketIndex++;
                    m_EntryIndex = -2;
                }

                m_IsEnd = true;
                return false;
            }

            public void Dispose()
            {
                DoCollectionCheck();

                m_HashSet = null;
                m_Current = null; // remove strong reference
            }

            private void DoCollectionCheck()
            {
                if (m_HashSet is null)
                {
                    throw new ObjectDisposedException(nameof(WeakHashSet<T>));
                }

                if (m_HashSet.m_Version != m_Version)
                {
                    throw new InvalidOperationException("Modify collection while enumerating it.");
                }
            }

            object IEnumerator.Current => Current;

            public void Reset() => throw new NotSupportedException();
        }

        private struct Entry
        {
            public GCHandle Handle; // 这里不直接用 WeakReference，它有额外的开销
            public uint HashCode;
            public int Next;
        }

        private int[] m_Buckets;
        private Entry[] m_Entries;
        private int m_Count;
        private int m_FreeList;
        private int m_Version;

        public WeakHashSet() : this(0) { }

        public WeakHashSet(int capacity)
        {
            int size = HashHelpers.GetPrime(capacity);

            m_Buckets = new int[size];
            m_Entries = new Entry[size];
            m_Count = 0;
            m_FreeList = -1;
            m_Version = 0;
        }

        ~WeakHashSet() => Clear();

        public int Capacity => m_Entries.Length;

        public bool Add(T item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            ref int bucket = ref GetBucket(item, out uint hashCode, out uint bucketIndex);

            if (FindItemInBucket(ref bucket, item, hashCode, out _, out _))
            {
                return false;
            }

            ScavengeDeadEntriesIfNeeded(excludeBucketIndex: bucketIndex); // exclude the bucket which has been gone through above
            int entryIndex = GetNewEntryIndex(hashCode, ref bucket, ref bucketIndex);
            ref Entry entry = ref m_Entries[entryIndex];

            entry.Handle = GCHandle.Alloc(item, GCHandleType.Weak);
            entry.HashCode = hashCode;
            entry.Next = bucket - 1;
            bucket = entryIndex + 1;
            m_Version++;
            return true;
        }

        public bool Remove(T item)
        {
            ref int bucket = ref GetBucket(item, out uint hashCode, out _);

            if (FindItemInBucket(ref bucket, item, hashCode, out int entryIndex, out int prevEntryIndex))
            {
                FreeEntry(ref bucket, entryIndex, prevEntryIndex);
                m_Version++;
                return true;
            }

            return false;
        }

        public void Clear()
        {
            if (m_Count <= 0)
            {
                return;
            }

            // free all handles
            for (int i = 0; i < m_Count; i++)
            {
                GCHandle handle = m_Entries[i].Handle;

                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }

            Array.Clear(m_Buckets, 0, m_Buckets.Length);
            Array.Clear(m_Entries, 0, m_Count);

            m_Count = 0;
            m_FreeList = -1;
            m_Version++;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        private int GetNewEntryIndex(uint hashCode, ref int bucket, ref uint bucketIndex)
        {
            if (m_FreeList >= 0)
            {
                int index = m_FreeList;
                m_FreeList = m_Entries[index].Next;
                return index;
            }

            if (m_Count >= m_Entries.Length)
            {
                Resize();

                // recalculate
                bucket = ref GetBucket(hashCode, out bucketIndex);
            }

            return m_Count++;
        }

        private bool FindItemInBucket(ref int bucket, T item, uint hashCode, out int itemEntryIndex, out int itemPrevEntryIndex)
        {
            int entryIndex = bucket - 1;
            int prevEntryIndex = -1;

            while (entryIndex >= 0)
            {
                ref Entry entry = ref m_Entries[entryIndex];

                if (TryGetGCHandleTarget(entry.Handle, out T value))
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

                    FreeEntry(ref bucket, entryIndex, prevEntryIndex);

                    // shouldn't change prevEntryIndex here
                    entryIndex = nextEntryIndex;
                }
            }

            itemEntryIndex = -1;
            itemPrevEntryIndex = -1;
            return false;
        }

        private void FreeEntry(ref int bucket, int entryIndex, int prevEntryIndex)
        {
            ref Entry entry = ref m_Entries[entryIndex];

            if (prevEntryIndex >= 0)
            {
                m_Entries[prevEntryIndex].Next = entry.Next;
            }
            else
            {
                bucket = entry.Next + 1;
            }

            entry.Handle.Free();
            entry.Handle = default;
            entry.Next = m_FreeList;
            m_FreeList = entryIndex;
        }

        private void ScavengeDeadEntriesIfNeeded(uint excludeBucketIndex)
        {
            if (m_FreeList >= 0 || m_Count < m_Entries.Length)
            {
                return;
            }

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
            m_Buckets = new int[newSize];

            Array.Resize(ref m_Entries, newSize);

            for (int i = 0; i < m_Count; i++)
            {
                ref Entry entry = ref m_Entries[i];

                if (!entry.Handle.IsAllocated)
                {
                    continue;
                }

                ref int bucket = ref GetBucket(entry.HashCode, out _);
                entry.Next = bucket - 1;
                bucket = i + 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetBucket(T item, out uint hashCode, out uint bucketIndex)
        {
            hashCode = (uint)RuntimeHelpers.GetHashCode(item);
            return ref GetBucket(hashCode, out bucketIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetBucket(uint hashCode, out uint bucketIndex)
        {
            bucketIndex = hashCode % (uint)m_Buckets.Length;
            return ref m_Buckets[bucketIndex];
        }

        private static bool TryGetGCHandleTarget(GCHandle handle, out T value)
        {
            if (!handle.IsAllocated)
            {
                value = null;
                return false;
            }

            value = (T)handle.Target;

            if (value is UnityEngine.Object unityObj && unityObj == null)
            {
                value = null;
                return false;
            }

            return (value is not null);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}