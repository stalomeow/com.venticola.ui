using System;
using UnityEngine;

namespace VentiCola.UI.Bindings
{
    [Serializable]
    public class VoidMethodProxy<T>
    {
        [SerializeField] private UIPage m_Target;
        [SerializeField] private string m_MethodName;
        [SerializeField] private DynamicArgument[] m_Arguments;

        protected internal VoidMethodProxy() { }

        public void Invoke(T arg)
        {
            if (m_Target == null || string.IsNullOrEmpty(m_MethodName))
            {
                return;
            }

            m_Target.InvokeMethod(m_MethodName, arg, m_Arguments);
        }
    }
}