using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VentiCola.UI.Internals;
using VentiCola.UI.Transitions;

namespace VentiCola.UI.Bindings
{
    [RequireComponent(typeof(Canvas))]
    [AddComponentMenu("UI Bindings/Show Iff <Condition> is True")]
    public sealed class IfBinding : LayoutBindingAuthoring, ILayoutBinding
    {
        [SerializeField]
        [PropertyTypeConstraints(typeof(bool))]
        private PropertyLikeProxy m_Condition;

        [SerializeField]
        private bool m_NegateCondition;

        [SerializeField]
        private VoidMethodProxy<bool> m_Callback;

        [SerializeField]
        [HideInInspector]
        private TransitionBase[] m_Transitions;

        [NonSerialized] private int m_Version = 0;
        [NonSerialized] private bool m_IsPassive;
        [NonSerialized] private bool m_IsDirty;
        [NonSerialized] private bool m_IsFirstRendering;
        [NonSerialized] private bool m_ConditionValue;
        [NonSerialized] private TemplateObject m_TemplateObject;
        [NonSerialized] private ILayoutBinding m_Parent;
        [NonSerialized] private HashSet<IBinding> m_DirtyChildren;
        [NonSerialized] private TemplateInstance? m_Instance;
        [NonSerialized] private Canvas m_SubCanvas;

        public override ILayoutBinding ProvideBinding()
        {
            return this;
        }

        public override TemplateObject ConvertToTemplate()
        {
            return new TemplateObject(gameObject, true);
        }

        public void InitializeObject(ILayoutBinding parent, TemplateObject? templateObject = null)
        {
            m_IsPassive = false;
            m_IsDirty = true;
            m_IsFirstRendering = true;

            m_TemplateObject = templateObject ?? throw new ArgumentNullException(nameof(templateObject));
            m_Parent = parent;
            m_DirtyChildren = HashSetPool<IBinding>.Get();
            m_Instance = null;
        }

        public void ResetObject()
        {
            StopAllCoroutines();

            m_Version++;

            HashSetPool<IBinding>.Release(m_DirtyChildren);

            m_TemplateObject.Dispose();
            m_TemplateObject = default;
            m_Parent = null;
            m_DirtyChildren = null;
            m_SubCanvas = null;

            if (m_Instance.HasValue)
            {
                m_Instance.Value.Dispose(false);
                m_Instance = null;
            }
        }

        public void CalculateValues(out bool changed)
        {
            var condition = m_Condition.GetValue<bool>();

            if (m_NegateCondition)
            {
                condition = !condition;
            }

            changed = (m_ConditionValue != condition);
            m_ConditionValue = condition;
        }

        public void RenderSelf()
        {
            if (m_Instance == null)
            {
                m_Instance = m_TemplateObject.Instantiate();
                m_Instance.Value.InitializeBindingsAndRender(this);
                m_SubCanvas = GetComponent<Canvas>();
            }

            m_SubCanvas.enabled = true;

            TransitionType transitionType = m_ConditionValue ? TransitionType.Enter : TransitionType.Exit;

            StopAllCoroutines();

            m_Callback.Invoke(m_ConditionValue);

            for (int i = 0; i < m_Transitions.Length; i++)
            {
                m_Transitions[i].BeginTransition(transitionType, false, m_IsFirstRendering);
            }

            if (m_IsFirstRendering)
            {
                if (!m_ConditionValue)
                {
                    m_SubCanvas.enabled = false;
                }
            }
            else
            {
                StartCoroutine(UpdateTransitions(!m_ConditionValue));
            }
        }

        public bool HasCoveredAllBranchesSinceFirstRendering()
        {
            return m_Condition.IsRealProperty;
        }

        private IEnumerator UpdateTransitions(bool deactivateGameObject)
        {
            bool needUpdate = true;

            while (needUpdate)
            {
                needUpdate = false;

                for (int i = 0; i < m_Transitions.Length; i++)
                {
                    TransitionBase transition = m_Transitions[i];

                    if (!transition.IsFinished)
                    {
                        needUpdate = true;
                        transition.UpdateTransition();
                    }
                }

                yield return null;
            }

            if (deactivateGameObject)
            {
                m_SubCanvas.enabled = false;
            }
        }

        int IReusableObject.Version => m_Version;

        bool IChangeObserver.IsPassive => m_IsPassive;

        ILayoutBinding IBinding.ParentBinding => m_Parent;

        bool IBinding.IsSelfDirty
        {
            get => m_IsDirty;
            set => m_IsDirty = value;
        }

        bool IBinding.IsFirstRendering
        {
            get => m_IsFirstRendering;
            set => m_IsFirstRendering = value;
        }

        bool ILayoutBinding.EnableChildBindingRendering => m_ConditionValue;

        HashSet<IBinding> ILayoutBinding.DirtyChildBindings => m_DirtyChildren;

        void IChangeObserver.NotifyChanged()
        {
            BindingUtility.SetDirty(this);
        }

        void ILayoutBinding.GetChildBindings(List<IBinding> results)
        {
            if (m_Instance == null)
            {
                m_Instance = m_TemplateObject.Instantiate();
                m_Instance.Value.InitializeBindingsAndRender(this);
                m_SubCanvas = GetComponent<Canvas>();
            }

            results.AddRange(m_Instance.Value.Bindings);
        }

        GameObject IBinding.GetOwnerGameObject()
        {
            return gameObject;
        }

        void IBinding.SetIsPassive(bool value)
        {
            m_IsPassive = value;
        }
    }
}