using System;
using System.Collections.Generic;
using UnityEngine;
using VentiCola.UI.Internal;

namespace VentiCola.UI.Bindings.LowLevel
{
    public class IfBinding : BaseBinding
    {
        private Func<GameObject, bool> m_ConditionFunc;
        private Action m_RenderAction;

        // 用 CanvasGroup 代替 SetActive，提高性能
        private CanvasGroup m_CanvasGroup;
        private bool m_CurrentCondition;

        public void Initialize(
            GameObject mountTarget,
            Func<GameObject, bool> conditionFunc,
            Action renderAction)
        {
            BaseInitialize(ref mountTarget);

            m_ConditionFunc = conditionFunc;
            m_RenderAction = renderAction;

            m_CanvasGroup = mountTarget.GetComponent<CanvasGroup>();
        }

        protected override void OnDetach()
        {
            m_ConditionFunc = null;
            m_RenderAction = null;
            m_CanvasGroup = null;

            base.OnDetach();
        }

        protected override void OnExecute(IAnimationUpdater animationUpdater)
        {
            m_CurrentCondition = m_ConditionFunc(MountTarget);

            if (m_CurrentCondition)
            {
                // 在 condition 第一次变为 true 时（即第一次能被看到时）执行 RenderAction
                if (m_RenderAction != null)
                {
                    using (new ContextScope(this))
                    {
                        m_RenderAction();
                    }

                    m_RenderAction = null;
                }

                m_CanvasGroup.alpha = 1.0f;
                // m_CanvasGroup.interactable = true;
                m_CanvasGroup.blocksRaycasts = true;
            }
            else
            {
                m_CanvasGroup.alpha = 0.0f;
                // m_CanvasGroup.interactable = false;
                m_CanvasGroup.blocksRaycasts = false;
            }
        }

        protected override void FilterDirtyChildren(List<WeakVObjRef<BaseBinding>> dirtyChildren, WriteOnlyList<BaseBinding> executionList)
        {
            if (!m_CurrentCondition)
            {
                return;
            }

            base.FilterDirtyChildren(dirtyChildren, executionList);
        }
    }
}