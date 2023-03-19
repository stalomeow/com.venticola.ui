using System;
using UnityEngine;
using VentiCola.UI.Internals;

namespace VentiCola.UI.Bindings
{
    internal enum BindingMode
    {
        [InspectorName("⇄ Two Way (Auto)")] TwoWayAuto = 0,
        [InspectorName("⇄ Two Way (Callback)")] TwoWayCallback = 1,
        [InspectorName("→ One Way")] OneWay = 2
    }

    public abstract class BindingBaseTwoWay<T> : BindingBase
    {
        [SerializeField] private BindingMode m_Mode;
        [SerializeField] private VoidMethodProxy<T> m_Changed;
        [NonSerialized] private bool m_ForcePassive = false;


        public override bool IsPassive
        {
            get => base.IsPassive || m_ForcePassive;
        }

        protected void OnValueChanged(T value)
        {
            if (m_Mode == BindingMode.OneWay)
            {
                return;
            }

            // 排除更新界面导致的变化
            if (ReferenceEquals(ChangeUtility.CurrentObserver, this))
            {
                return;
            }

            m_ForcePassive = true;
            ChangeUtility.BeginObservedRegion(this);

            try
            {
                switch (m_Mode)
                {
                    case BindingMode.TwoWayAuto:
                        SetPropertyValueDirect(value);
                        break;

                    case BindingMode.TwoWayCallback:
                        m_Changed.Invoke(value);
                        break;

                    default:
                        Debug.LogError($"Unexpected binding mode {m_Mode}!", this);
                        break;
                }
            }
            finally
            {
                ChangeUtility.EndObservedRegion(this);
                m_ForcePassive = false;
            }
        }

        protected abstract void SetPropertyValueDirect(T value);
    }
}