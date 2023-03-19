using System;
using UnityEngine;

namespace VentiCola.UI.Bindings
{
    [Serializable]
    public class PropertyLikeProxy : PropertyProxy
    {
        [SerializeField] protected bool m_IsMethod;
        [SerializeField] protected DynamicArgument[] m_Arguments;

        protected internal PropertyLikeProxy() { }

        public bool IsRealProperty
        {
            get => !m_IsMethod;
        }

        public override T GetValue<T>()
        {
            if (m_IsMethod)
            {
                UIPage page = (UIPage)m_SourceObj;
                return page.InvokeMethod<T>(m_PropertyPath[0], m_Arguments);
            }

            return base.GetValue<T>();
        }

        public override void SetValue<T>(T value)
        {
            if (m_IsMethod)
            {
                throw new InvalidOperationException("Readonly");
            }

            base.SetValue<T>(value);
        }
    }
}