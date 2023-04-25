using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VentiCola.UI.Internal
{
    internal sealed class WeakHashSetDebugView<T> where T : class
    {
        private readonly WeakHashSet<T> m_HashSet;

        public WeakHashSetDebugView(WeakHashSet<T> hashSet)
        {
            m_HashSet = hashSet;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                var temp = new List<T>();
                m_HashSet.ForEach(x => temp.Add(x));
                return temp.ToArray();
            }
        }
    }

    /// <summary>
    /// 可以遍历的短弱引用 HashSet。支持对 <see cref="UnityEngine.Object"/> 的短弱引用。
    /// </summary>
    /// <typeparam name="T">集合元素的类型。</typeparam>
    [DebuggerTypeProxy(typeof(WeakHashSetDebugView<>))]
    public class WeakHashSet<T> where T : class
    {
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

        // DO NOT modify collection in ForEach
        // DO NOT call ForEach in ForEach
        public void ForEach(Action<T> action)
        {
            if (action is null)
            {
                return;
            }

            int version = m_Version;
            uint length = (uint)m_Buckets.Length;

            for (uint i = 0; i < length; i++)
            {
                ref int bucket = ref m_Buckets[i];
                int entryIndex = bucket - 1;
                int prevEntryIndex = -1;

                while (entryIndex >= 0)
                {
                    ref Entry entry = ref m_Entries[entryIndex];

                    if (TryGetGCHandleTarget(entry.Handle, out T value))
                    {
                        action(value);

                        if (m_Version != version)
                        {
                            throw new InvalidOperationException("Collection was modified when being iterated!");
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
                        version++;
                        m_Version++;
                    }
                }
            }
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
    }
}