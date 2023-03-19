using System;
using UnityEngine;
using VentiCola.UI.Internals;

namespace VentiCola.UI.Bindings
{
    public abstract class BindingBase : MonoBehaviour, IBinding
    {
        [NonSerialized] private int m_Version = 0;
        [NonSerialized] private bool m_IsDirty;
        [NonSerialized] private bool m_IsPassive;
        [NonSerialized] private bool m_IsFirstExecution;
        [NonSerialized] private ILayoutBinding m_Parent;

        public bool IsSelfDirty
        {
            get => m_IsDirty;
            set => m_IsDirty = value;
        }

        public virtual bool IsPassive
        {
            get => m_IsPassive;
        }

        public bool IsFirstRendering
        {
            get => m_IsFirstExecution;
            set => m_IsFirstExecution = value;
        }

        public ILayoutBinding ParentBinding
        {
            get => m_Parent;
        }

        public virtual void InitializeObject(ILayoutBinding parent, TemplateObject? templateObject = null)
        {
            m_IsPassive = false;
            m_IsFirstExecution = true;
            m_IsDirty = true;
            m_Parent = parent;
        }

        public virtual void ResetObject()
        {
            m_Parent = null;

            m_Version++;
        }

        public abstract void CalculateValues(out bool changed);

        public abstract void RenderSelf();

        public abstract bool HasCoveredAllBranchesSinceFirstRendering();


        int IReusableObject.Version => m_Version;

        void IChangeObserver.NotifyChanged()
        {
            BindingUtility.SetDirty(this);
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