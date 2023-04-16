using System.Collections.Generic;
using UnityEngine.Pool;

namespace VentiCola.UI.Internals
{
    public struct ChangeObserverPlugin
    {
        private List<WeakHashSet<IChangeObserver>> m_Deps;             // 保存所有包含了当前 Observer 的 WeakHashSet
        private HashSet<WeakHashSet<IChangeObserver>> m_SurvivingDeps;
        private int m_OldDepCount;
        private uint m_StackCount;                                     // 当前 Observer 在 Stack 中的数量

        public void Reset(IChangeObserver self)
        {
            if (m_Deps is null)
            {
                return;
            }

            for (int i = 0; i < m_Deps.Count; i++)
            {
                m_Deps[i].Remove(self);
            }

            m_Deps.Clear();
        }

        internal void IncreaseStackCount()
        {
            if (m_StackCount++ > 0)
            {
                return;
            }

            m_Deps ??= new List<WeakHashSet<IChangeObserver>>();
            m_SurvivingDeps = HashSetPool<WeakHashSet<IChangeObserver>>.Get();
            m_OldDepCount = m_Deps.Count;
        }

        internal void AddSelfToHashSet(IChangeObserver self, WeakHashSet<IChangeObserver> observers)
        {
            if (observers.Add(self))
            {
                // observers 是新的，直接放最后
                m_Deps.Add(observers);
            }
            else
            {
                // 之前就存过 observers 了，之后也应该继续存着它
                m_SurvivingDeps.Add(observers);
            }
        }

        internal void DecreaseStackCount(IChangeObserver self)
        {
            // 当 Observer 结束一轮监听以后，需要更新它的 Deps，把之后不需要的移除

            if (--m_StackCount > 0)
            {
                return;
            }

            // 大多数情况下，下面的条件都是 false
            if (m_SurvivingDeps.Count != m_OldDepCount)
            {
                int removeCount = 0;
                int maxRemoveCount = m_OldDepCount - m_SurvivingDeps.Count;

                for (int i = m_OldDepCount - 1; i >= 0; i--)
                {
                    WeakHashSet<IChangeObserver> holder = m_Deps[i];

                    if (m_SurvivingDeps.Contains(holder))
                    {
                        continue;
                    }

                    holder.Remove(self); // 这一步是有开销的（GCHandle），所以尽量避免
                    ListUtility.FastRemoveAt(m_Deps, i);

                    if (++removeCount >= maxRemoveCount)
                    {
                        break;
                    }
                }
            }

            HashSetPool<WeakHashSet<IChangeObserver>>.Release(m_SurvivingDeps);
        }
    }
}