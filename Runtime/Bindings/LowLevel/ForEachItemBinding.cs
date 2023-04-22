using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VentiCola.UI.Internal;

namespace VentiCola.UI.Bindings.LowLevel
{
    public sealed class ForEachItemBinding : BaseBinding
    {
        private readonly Dictionary<GameObject, GameObject> m_MountTargetCache = new();
        private Transform m_RawForEachTemplateTransform;

        public void Initialize(GameObject exactMountTarget, GameObject rawTemplate)
        {
            BaseInitialize(ref exactMountTarget, mountTargetIsUnreliable: false, setDirty: false);

            m_RawForEachTemplateTransform = rawTemplate.transform;

            exactMountTarget.SetActive(true);
        }

        protected override void OnDetach()
        {
            m_MountTargetCache.Clear();
            m_RawForEachTemplateTransform = null;

            base.OnDetach();
        }

        protected override bool FixDescendantMountTarget(ref GameObject descendantMountTarget)
        {
            if (m_MountTargetCache.TryGetValue(descendantMountTarget, out GameObject realMountTarget))
            {
                descendantMountTarget = realMountTarget;
                return true;
            }

            // ForEachItemBinding 和 ForEachBinding 绑定在**同深度的不同物体**上
            // ForEachItemBinding 的 (Raw)MountTarget 是当前 Item 的根节点
            // ForEachBinding 的 MountTarget 是 ForEach 用的模板物体
            // ForEachBinding 的 RawMountTarget 是代码中显式绑定的物体，是未修正的 MountTarget
            // （为了支持嵌套 ForEach，所以很复杂 233

            List<int> path = ListPool<int>.Get();
            Transform transform = descendantMountTarget.transform;

            // 从下往上记录路径
            while (transform != m_RawForEachTemplateTransform)
            {
                path.Add(transform.GetSiblingIndex());
                transform = transform.parent;
            }

            transform = MountTarget.transform;

            // 从上往下找到新的 mount target
            for (int i = path.Count - 1; i >= 0; i--)
            {
                transform = transform.GetChild(path[i]);
            }

            realMountTarget = transform.gameObject;
            m_MountTargetCache.Add(descendantMountTarget, realMountTarget);
            descendantMountTarget = realMountTarget;

            ListPool<int>.Release(path);
            return true;
        }

        protected override void OnExecute(IAnimationUpdater animationUpdater)
        {
            throw new NotImplementedException($"The {nameof(OnExecute)}() method of {TypeUtility.GetFriendlyTypeName(GetType(), false)} should never be executed!");
        }
    }
}