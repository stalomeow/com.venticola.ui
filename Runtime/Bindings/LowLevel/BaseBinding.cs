using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VentiCola.UI.Internal;

namespace VentiCola.UI.Bindings.LowLevel
{
    // 一个 Binding 不应该关心自己的父节点，不应该修改自己的父节点以及同级节点，只能修改自己和子节点

    public abstract class BaseBinding : IChangeObserver, IVersionable
    {
        private static readonly int s_ObjectPoolStackMaxCount = 1000;
        private static readonly Dictionary<Type, Stack<BaseBinding>> s_ObjectPool = new();
        private static readonly Stack<BaseBinding> s_ContextStack = new();

        public ref struct ContextScope
        {
            private BaseBinding m_Binding;

            public ContextScope(BaseBinding binding)
            {
                m_Binding = binding;

                if (binding != null)
                {
                    s_ContextStack.Push(binding);
                }
            }

            public void Dispose()
            {
                if (m_Binding == null)
                {
                    return;
                }

                m_Binding = null;
                s_ContextStack.Pop();
            }
        }

        protected readonly ref struct WriteOnlyList<T>
        {
            private readonly List<T> m_List;

            internal WriteOnlyList(List<T> list)
            {
                m_List = list;
            }

            public void Add(T item)
            {
                m_List.Add(item);
            }
        }

        private ChangeObserverPlugin m_Plugin;
        private int m_Version = 0;
        private bool m_IsDirty;
        private bool m_HasNotifiedUpwards;
        private bool m_IsPassivelyObservingChanges;
        private GameObject m_MountTarget;
        private GameObject m_RawMountTarget;
        private WeakVObjRef<BaseBinding> m_Parent;
        private List<BaseBinding> m_AllChildren;                // lazy initialize and use object pool (because not all bindings have children)
        private List<WeakVObjRef<BaseBinding>> m_DirtyChildren; // lazy initialize and use object pool (because not all bindings have children)

        protected BaseBinding() { }

        public bool IsPassivelyObservingChanges
        {
            get => m_IsPassivelyObservingChanges;
            set => m_IsPassivelyObservingChanges = value;
        }

        public GameObject MountTarget => m_MountTarget;

        public GameObject RawMountTarget => m_RawMountTarget;

        public int ChildCount => (m_AllChildren == null) ? 0 : m_AllChildren.Count;

        public int DirtyChildCount => (m_DirtyChildren == null) ? 0 : m_DirtyChildren.Count;

        protected void BaseInitialize(
            ref GameObject mountTarget,
            bool mountTargetIsUnreliable = true,
            bool setDirty = true)
        {
            // set the current context as parent
            if (s_ContextStack.TryPeek(out BaseBinding parent))
            {
                parent.m_AllChildren ??= ListPool<BaseBinding>.Get();
                parent.m_AllChildren.Add(this);
            }

            m_Parent = new WeakVObjRef<BaseBinding>(parent);
            m_RawMountTarget = mountTarget;

            if (mountTargetIsUnreliable && parent != null)
            {
                // change the template mount target to the real one (if necessary)
                // must fix the local variable 'mountTarget' !!! someone may use it outside this method!
                FixMountTarget(parent, ref mountTarget);
            }

            m_MountTarget = mountTarget;
            m_IsPassivelyObservingChanges = false;

            m_IsDirty = false;
            m_HasNotifiedUpwards = false;

            // 如果不 setDirty，那么 OnExecute 将永远不会被执行
            if (setDirty)
            {
                SetDirtyAndNotifyUpwards();
            }
        }

        protected virtual void OnDetach()
        {
            m_Plugin.Reset(this);

            m_Version++;
            m_MountTarget = null;
            m_RawMountTarget = null;
            m_Parent = default;

            if (m_AllChildren != null)
            {
                ListPool<BaseBinding>.Release(m_AllChildren);
                m_AllChildren = null;
            }

            if (m_DirtyChildren != null)
            {
                ListPool<WeakVObjRef<BaseBinding>>.Release(m_DirtyChildren);
                m_DirtyChildren = null;
            }

            Recycle(this);
        }

        public BaseBinding GetChild(Index index)
        {
            int count = ChildCount;
            int offset = index.GetOffset(count);

            if (offset < 0 || offset >= count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return m_AllChildren[offset];
        }

        protected void DetachChildren(Range range, Action<BaseBinding> preDetachCallback = null)
        {
            (int offset, int length) = range.GetOffsetAndLength(ChildCount);

            if (length <= 0)
            {
                return;
            }

            List<BaseBinding> stack = ListPool<BaseBinding>.Get(); // 统一都用 List，方便重用

            for (int i = 0; i < length; i++)
            {
                BaseBinding child = m_AllChildren[offset + i];

                // 只对子节点调用，子节点的子节点不调用
                preDetachCallback?.Invoke(child);

                // 回收子节点以及所有后代节点。使用先序遍历的原因如下：
                // 1. Binding 不应该关心自己的祖先节点和兄弟节点，但是它会修改子孙节点
                // 2. 先序遍历的非递归实现更简单 233
                stack.Add(child);

                while (stack.Count > 0)
                {
                    BaseBinding top = stack.PopBackUnsafe();

                    if (top.m_AllChildren != null)
                    {
                        stack.AddRange(top.m_AllChildren);
                    }

                    // 清理方法会重置几乎所有字段，所以放在最后
                    // 使用 try-catch，避免一个 binding 出问题，后面全部无法执行的情况
                    try
                    {
                        top.OnDetach();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        Debug.LogError($"Failed to execute {nameof(OnDetach)}() method on binding({TypeUtility.GetFriendlyTypeName(top.GetType(), false)})!", top.MountTarget);
                    }
                }
            }

            m_AllChildren.RemoveRange(offset, length);
            ListPool<BaseBinding>.Release(stack);
        }

        public static T Allocate<T>() where T : BaseBinding, new()
        {
            if (s_ObjectPool.TryGetValue(typeof(T), out Stack<BaseBinding> pool))
            {
                if (pool.TryPop(out BaseBinding binding))
                {
                    return (T)binding;
                }
            }

            return new T();
        }

        private static void Recycle(BaseBinding binding)
        {
            Type bindingType = binding.GetType();

            if (!s_ObjectPool.TryGetValue(bindingType, out Stack<BaseBinding> pool))
            {
                pool = new Stack<BaseBinding>();
                s_ObjectPool.Add(bindingType, pool);
            }

            if (pool.Count > s_ObjectPoolStackMaxCount)
            {
                return;
            }

            pool.Push(binding);
        }

        private static void FixMountTarget(BaseBinding ancestor, ref GameObject mountTarget)
        {
            while (ancestor != null)
            {
                if (ancestor.FixDescendantMountTarget(ref mountTarget))
                {
                    // 因为祖先节点也会被它的祖先节点修正，所以这里让离自己最近的祖先节点修正一次即可
                    break;
                }

                ancestor = ancestor.m_Parent.GetTargetOrDefault(null);
            }
        }

        protected virtual bool FixDescendantMountTarget(ref GameObject descendantMountTarget)
        {
            return false;
        }

        public void Execute(IAnimationUpdater animationUpdater)
        {
            // short path
            if (!m_IsDirty && (m_DirtyChildren == null || m_DirtyChildren.Count == 0))
            {
                return;
            }

            List<BaseBinding> stack = ListPool<BaseBinding>.Get(); // 统一都用 List，方便重用
            var writeOnlyStack = new WriteOnlyList<BaseBinding>(stack);
            var topIsNotThis = false;

            stack.Add(this);

            while (stack.Count > 0)
            {
                BaseBinding top = stack.PopBackUnsafe();

                // 使用 NoThrow 方法，避免一个 binding 出问题，后面全部无法执行的情况
                top.ExecuteNoThrow(animationUpdater);
                top.PushDirtyChildrenNoThrow(writeOnlyStack, topIsNotThis);

                topIsNotThis = true;
            }

            ListPool<BaseBinding>.Release(stack);
        }

        private void ExecuteNoThrow(IAnimationUpdater animationUpdater)
        {
            if (!m_IsDirty)
            {
                return;
            }

            ChangeUtility.BeginObservedRegion(this);

            try
            {
                OnExecute(animationUpdater);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"Failed to execute binding({TypeUtility.GetFriendlyTypeName(GetType(), false)})!", MountTarget);
            }
            finally
            {
                m_IsDirty = false; // 不管 execute 成不成功都清除 dirty 标记
                ChangeUtility.EndObservedRegion(this);
            }
        }

        protected abstract void OnExecute(IAnimationUpdater animationUpdater);

        private void PushDirtyChildrenNoThrow(WriteOnlyList<BaseBinding> stack, bool resetNotifyFlag)
        {
            try
            {
                if (m_DirtyChildren != null && m_DirtyChildren.Count > 0)
                {
                    FilterDirtyChildren(m_DirtyChildren, stack);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"Failed to filter dirty children of binding({TypeUtility.GetFriendlyTypeName(GetType(), false)})!", MountTarget);
            }
            finally
            {
                if (resetNotifyFlag)
                {
                    m_HasNotifiedUpwards = false;
                }
            }
        }

        protected virtual void FilterDirtyChildren(List<WeakVObjRef<BaseBinding>> dirtyChildren, WriteOnlyList<BaseBinding> executionList)
        {
            for (int i = 0; i < dirtyChildren.Count; i++)
            {
                if (dirtyChildren[i].TryGetTarget(out BaseBinding child))
                {
                    executionList.Add(child);
                }
            }

            dirtyChildren.Clear();
        }

        protected void SetDirtyAndNotifyUpwards()
        {
            if (m_IsDirty)
            {
                return;
            }

            m_IsDirty = true; // 注：m_IsDirty 仅指自己是否 Dirty，不包括子节点

            BaseBinding parent;
            BaseBinding binding = this;

            // 向上逐层通知
            while ((parent = binding.m_Parent.GetTargetOrDefault(null)) != null)
            {
                if (binding.m_HasNotifiedUpwards)
                {
                    break;
                }

                parent.m_DirtyChildren ??= ListPool<WeakVObjRef<BaseBinding>>.Get();
                parent.m_DirtyChildren.Add(binding);
                binding.m_HasNotifiedUpwards = true;
                binding = parent;
            }
        }

        int IVersionable.Version => m_Version;

        ref ChangeObserverPlugin IChangeObserver.Plugin => ref m_Plugin;

        bool IChangeObserver.IsPassive => m_IsPassivelyObservingChanges;

        void IChangeObserver.NotifyChanged() => SetDirtyAndNotifyUpwards();
    }
}