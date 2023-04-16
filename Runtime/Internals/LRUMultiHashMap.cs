using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace VentiCola.UI.Internals
{
    [DebuggerDisplay("Count = {Count}")]
    public class LRUMultiHashMap<TKey, TValue>
    {
        private struct Entry
        {
            public TKey Key;
            public TValue Value;
            public uint HashCode;
            public int Next;                                 // 下一个具有相同 HashCode 的 Entry 的 index
            public int Before;                               // 双向链表的上一个 Entry 的 index
            public int After;                                // 双向链表的下一个 Entry 的 index
        }

        private readonly int[] m_Buckets;                    // 第一个 Entry 的 index + 1
        private readonly Entry[] m_Entries;
        private readonly IEqualityComparer<TKey> m_Comparer;
        private int m_Head;                                  // 双向链表第一个 Entry 的 index
        private int m_Tail;                                  // 双向链表最后一个 Entry 的 index
        private int m_Count;
        private int m_FreeList;                              // FreeList 第一个 Entry 的 index，使用 Next 字段构成链表
        private int m_FreeCount;

        public event Action<TKey, TValue> OnEliminated;

        public LRUMultiHashMap(int capacity) : this(capacity, EqualityComparer<TKey>.Default) { }

        public LRUMultiHashMap(int capacity, IEqualityComparer<TKey> comparer)
        {
            m_Buckets = new int[HashHelpers.GetPrime(capacity)];
            m_Entries = new Entry[capacity]; // 这里就不用素数了，上面用素数是为了减少 Hash 碰撞
            m_Comparer = comparer;
            m_Head = -1;
            m_Tail = -1;
            m_Count = 0;
            m_FreeList = -1;
            m_FreeCount = 0;
        }

        public int Count => m_Count - m_FreeCount;

        public int Capacity => m_Entries.Length;

        public void Add(TKey key, TValue value)
        {
            ref int bucket = ref GetBucket(key, out uint hashCode);

            int entryIndex = GetNewEntryIndex(out bool overwriting);
            ref Entry entry = ref m_Entries[entryIndex];

            if (overwriting)
            {
                RemoveEntryFromBucket(entryIndex);
                RemoveEntryFromLinkedList(entryIndex);
                OnEliminated?.Invoke(entry.Key, entry.Value);
            }

            entry.Key = key;
            entry.Value = value;
            entry.HashCode = hashCode;
            entry.Next = bucket - 1;
            entry.Before = -1;
            entry.After = m_Head;

            // 放到双向链表和 bucket 的最前面
            m_Head = entryIndex;
            bucket = entryIndex + 1;

            if (m_Tail == -1)
            {
                m_Tail = m_Head;
            }
        }

        private int GetNewEntryIndex(out bool overwriting)
        {
            int entryIndex;
            overwriting = false;

            if (m_FreeCount > 0)
            {
                entryIndex = m_FreeList;
                m_FreeList = m_Entries[entryIndex].Next;
                m_FreeCount--;
            }
            else if (m_Count < m_Entries.Length - 1)
            {
                entryIndex = m_Count;
                m_Count++;
            }
            else
            {
                entryIndex = m_Tail;
                overwriting = true;
            }

            return entryIndex;
        }

        private void RemoveEntryFromBucket(int targetEntryIndex)
        {
            ref Entry entry = ref m_Entries[targetEntryIndex];
            ref int bucket = ref GetBucket(entry.HashCode);

            int prevEntryIndex = -1;
            int currEntryIndex = bucket - 1;

            while (currEntryIndex >= 0)
            {
                // fast path
                if (currEntryIndex == targetEntryIndex)
                {
                    RemoveEntryFromBucket(ref bucket, prevEntryIndex, in entry);
                    break;
                }

                prevEntryIndex = currEntryIndex;
                currEntryIndex = m_Entries[currEntryIndex].Next;
            }
        }

        private void RemoveEntryFromBucket(ref int bucket, int prevEntryIndex, in Entry entry)
        {
            if (prevEntryIndex >= 0)
            {
                m_Entries[prevEntryIndex].Next = entry.Next;
            }
            else
            {
                bucket = entry.Next + 1;
            }
        }

        private void RemoveEntryFromLinkedList(int targetEntryIndex)
        {
            ref Entry entry = ref m_Entries[targetEntryIndex];

            if (m_Head == targetEntryIndex)
            {
                m_Head = entry.After;
            }
            else
            {
                m_Entries[entry.Before].After = entry.After;
            }

            if (m_Tail == targetEntryIndex)
            {
                m_Tail = entry.Before;
            }
            else
            {
                m_Entries[entry.After].Before = entry.Before;
            }
        }

        public bool TryTake(TKey key, out TValue value)
        {
            ref int bucket = ref GetBucket(key, out uint hashCode);

            int prevEntryIndex = -1;
            int currEntryIndex = bucket - 1;

            while (currEntryIndex >= 0)
            {
                ref Entry entry = ref m_Entries[currEntryIndex];

                if (entry.HashCode == hashCode && m_Comparer.Equals(entry.Key, key))
                {
                    value = entry.Value;

                    RemoveEntryFromBucket(ref bucket, prevEntryIndex, in entry);
                    RemoveEntryFromLinkedList(currEntryIndex);

                    entry.Key = default;
                    entry.Value = default;
                    entry.Next = m_FreeList;
                    m_FreeList = currEntryIndex;
                    m_FreeCount++;
                    return true;
                }

                prevEntryIndex = currEntryIndex;
                currEntryIndex = entry.Next;
            }

            value = default;
            return false;
        }

        public void Clear()
        {
            Array.Clear(m_Buckets, 0, m_Buckets.Length);
            Array.Clear(m_Entries, 0, m_Count);

            m_Head = -1;
            m_Tail = -1;
            m_Count = 0;
            m_FreeList = -1;
            m_FreeCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetBucket(TKey key, out uint hashCode)
        {
            hashCode = (uint)m_Comparer.GetHashCode(key);
            return ref GetBucket(hashCode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetBucket(uint hashCode)
        {
            uint index = hashCode % (uint)m_Buckets.Length;
            return ref m_Buckets[index];
        }
    }
}