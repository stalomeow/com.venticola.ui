using System;
using System.Collections.Generic;
using UnityEngine;
using VentiCola.UI.Bindings.Experimental;
using Object = UnityEngine.Object;

namespace VentiCola.UI.Bindings.LowLevel.Experimental
{
    public class ForEachBinding<T> : BaseBinding
    {
        private Func<GameObject, ReactiveList<T>> m_CollectionFunc;
        private Action<ForEachItem<T>> m_RenderItemAction;
        private IEqualityComparer<T> m_EqualityComparer;

        private int m_ItemIndexOffset;
        private List<ForEachItem<T>> m_Items;
        private Stack<GameObject> m_ItemGOPool;

        public void Initalize(
            GameObject mountTarget,
            Func<GameObject, ReactiveList<T>> collectionFunc,
            Action<ForEachItem<T>> renderItemAction,
            IEqualityComparer<T> customEqualityComparer)
        {
            BaseInitialize(ref mountTarget);

            m_CollectionFunc = collectionFunc;
            m_RenderItemAction = renderItemAction;
            m_EqualityComparer = customEqualityComparer ?? EqualityComparer<T>.Default;

            m_ItemIndexOffset = 0;
            m_Items = new List<ForEachItem<T>>();
            m_ItemGOPool = new Stack<GameObject>();

            mountTarget.SetActive(false); // 隐藏 template
        }

        protected override void OnDetach()
        {
            int childCount = ChildCount;
            for (int i = 0; i < childCount; i++)
            {
                // Object.Destroy 是在当前帧最后才实际执行的，所以这里 destroy 没关系
                Object.Destroy(GetChild(i).MountTarget);
            }

            while (m_ItemGOPool.Count > 0)
            {
                Object.Destroy(m_ItemGOPool.Pop());
            }

            m_CollectionFunc = null;
            m_RenderItemAction = null;
            m_EqualityComparer = null;

            m_Items = null;
            m_ItemGOPool = null;

            base.OnDetach();
        }

        protected override void OnExecute(IAnimationUpdater animationUpdater)
        {
            ReactiveList<T> list = m_CollectionFunc(MountTarget);
            Patch(list, Range.All);
        }

        private void Patch(ReactiveList<T> list, Range range)
        {
            (int offset, int length) = range.GetOffsetAndLength(list.Count);

            int startIndex1 = offset;
            int startIndex2 = 0;

            int endIndex1 = offset + length - 1;
            int endIndex2 = m_Items.Count - 1;

            while (startIndex1 <= endIndex1 && startIndex2 <= endIndex2)
            {
                if (m_EqualityComparer.Equals(list[startIndex1], m_Items[startIndex2].Value))
                {
                    startIndex1++;
                    startIndex2++;
                }
                else
                {
                    break;
                }
            }

            while (startIndex1 <= endIndex1 && startIndex2 <= endIndex2)
            {
                if (m_EqualityComparer.Equals(list[endIndex1], m_Items[endIndex2].Value))
                {
                    endIndex1--;
                    endIndex2--;
                }
                else
                {
                    break;
                }
            }

            if (startIndex1 <= endIndex1 && startIndex2 > endIndex2)
            {
                // 只要增加新元素即可
                for (int i = startIndex1; i <= endIndex1; i++)
                {
                    CreateNewItem(list, i);
                }
            }
            else if (startIndex1 > endIndex1 && startIndex2 <= endIndex2)
            {
                // 只要移除旧元素即可
                for (int i = endIndex2; i >= startIndex2; i--)
                {
                    RecycleItem(i);
                }
            }
            else
            {
                while (startIndex1 <= endIndex1 && startIndex2 <= endIndex2)
                {
                    ForEachItem<T> item = m_Items[startIndex2];

                    item.Index = startIndex1;
                    item.Value = list[startIndex1];

                    startIndex1++;
                    startIndex2++;
                }

                while (startIndex2 <= endIndex2)
                {
                    RecycleItem(startIndex2);
                }

                while (startIndex1 <= endIndex1)
                {
                    CreateNewItem(list, startIndex1);
                    startIndex1++;
                }
            }
        }

        private void CreateNewItem(ReactiveList<T> list, int index, GameObject go = null, bool forceAppend = false)
        {
            var item = new ForEachItem<T>()
            {
                Index = index,
                Value = list[index]
            };

            int itemIndex = forceAppend ? (m_Items.Count - 1) : (index - m_ItemIndexOffset);
            m_Items.Insert(itemIndex, item);

            if (go == null)
            {
                if (!m_ItemGOPool.TryPop(out go))
                {
                    // use this.MountTarget as template
                    go = Object.Instantiate(MountTarget, MountTarget.transform.parent);
                }

                go.SetActive(true);
            }

            go.transform.SetSiblingIndex(itemIndex + 1); // the first child is always the template

            using (new ContextScope(this))
            {
                var itemBinding = Allocate<ForEachItemBinding>();
                itemBinding.Initialize(go, RawMountTarget);
                MoveChild(^1, itemIndex);

                if (m_RenderItemAction is not null)
                {
                    using (new ContextScope(itemBinding))
                    {
                        m_RenderItemAction(item);
                    }
                }
            }
        }

        private void RecycleItem(int itemIndex)
        {
            m_Items.RemoveAt(itemIndex);

            DetachChildren(itemIndex..(itemIndex + 1), child =>
            {
                GameObject go = child.MountTarget;
                go.SetActive(false);
                m_ItemGOPool.Push(go);
            });
        }
    }
}