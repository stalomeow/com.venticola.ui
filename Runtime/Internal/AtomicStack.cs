using System.Threading;

namespace VentiCola.UI.Internal
{
    /// <summary>
    /// 无锁线程安全 Stack
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks><see cref="System.Collections.Concurrent.ConcurrentStack{T}"/> GC 比较高，所以自己实现了一个</remarks>
    internal class AtomicStack<T>
    {
        private class Node
        {
            public T Value;
            public Node Next;
        }

        private volatile Node m_Head;
        private volatile Node m_FreeList;

        public void Push(T value)
        {
            Node node = m_FreeList;

            // 只尝试一次（节省时间），如果不行就 new 一个
            if (node is null || Interlocked.CompareExchange(ref m_FreeList, node.Next, node) != node)
            {
                node = new Node();
            }

            node.Value = value;

            // 不断尝试 CAS m_Head，直到成功
            var spinWait = new SpinWait();

            while (true)
            {
                node.Next = m_Head;

                if (Interlocked.CompareExchange(ref m_Head, node, node.Next) == node.Next)
                {
                    break;
                }

                spinWait.SpinOnce();
            }
        }

        public bool TryPeek(out T value)
        {
            if (m_Head is not null)
            {
                value = m_Head.Value;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryPop(out T value)
        {
            Node node;

            // 不断尝试 CAS m_Head，直到成功
            var spinWait = new SpinWait();

            while (true)
            {
                if (m_Head is null)
                {
                    value = default;
                    return false;
                }

                node = m_Head;

                if (Interlocked.CompareExchange(ref m_Head, node.Next, node) == node)
                {
                    value = node.Value;
                    break;
                }

                spinWait.SpinOnce();
            }

            // 回收 Node（只尝试一次）
            node.Value = default; // 清除引用（如果有），因为后面可能会回收 Node
            node.Next = m_FreeList;
            Interlocked.CompareExchange(ref m_FreeList, node, node.Next);
            return true;
        }
    }
}