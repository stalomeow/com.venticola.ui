using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VentiCola.UI.Bindings
{
    public class ForEachBinding<T> : BaseBinding
    {
        private Func<IReactiveCollection> m_CollectionFunc;

        private IEqualityComparer<T> m_EqualityComparer;
        private Action<int, T> m_RenderItemAction;

        private IReactiveCollection m_LastCollection;
        private Stack<GameObject> m_ItemPool;
        private List<GameObject> m_ItemList;

        public void Initalize(
            GameObject mountTarget,
            Func<IReactiveCollection> collectionFunc,
            Action<int, T> renderItemAction)
        {
            BaseInitialize(ref mountTarget);

            m_CollectionFunc = collectionFunc;

            m_EqualityComparer = EqualityComparer<T>.Default;
            m_RenderItemAction = renderItemAction;

            mountTarget.SetActive(false); // 隐藏 template
        }

        protected override void OnDetach()
        {
            m_CollectionFunc = null;

            int childCount = ChildCount;
            for (int i = 0; i < childCount; i++)
            {
                Object.Destroy(GetChild(i).MountTarget);
            }

            base.OnDetach();
        }

        protected override void OnExecute(IAnimationUpdater animationUpdater)
        {
            // clear old items
            DetachChildren(Range.All, child => Object.Destroy(child.MountTarget));

            IReactiveCollection collection = m_CollectionFunc();

            for (int i = 0; i < collection.Count; i++)
            {
                // use this.MountTarget as template
                GameObject go = Object.Instantiate(MountTarget, MountTarget.transform.parent);

                using (new ContextScope(this))
                {
                    // create a wrapper
                    var itemBindingWrapper = Allocate<ForEachItemBinding>();
                    itemBindingWrapper.Initialize(go, RawMountTarget);

                    using (new ContextScope(itemBindingWrapper))
                    {
                        // render the item
                        m_RenderItemAction?.Invoke(i, collection.GetValueAt<T>(i));
                    }
                }
            }
        }

        private void Diff(IReactiveCollection collection, ref Range range)
        {

        }
    }
}