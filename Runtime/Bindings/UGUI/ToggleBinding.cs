using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// TODO: 当使用 ToggleGroup 时，点击一个已经被选上的 Toggle 也会触发 OnValueChanged。具体看 Toggle 源码 269 行。

namespace VentiCola.UI.Bindings
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Toggle))]
    [AddComponentMenu("UI Bindings/UGUI/Toggle > Is On")]
    public class ToggleBinding : BindingBaseTwoWay<bool>
    {
        [SerializeField]
        [PropertyTypeConstraints(typeof(bool))]
        private PropertyLikeProxy m_Value;

        [NonSerialized] private Toggle m_Toggle;
        [NonSerialized] private bool m_IsOn;
        [NonSerialized] private UnityAction<bool> m_OnValueChanged;

        public override void InitializeObject(ILayoutBinding parent, TemplateObject? templateObject = null)
        {
            base.InitializeObject(parent, templateObject);

            m_Toggle = GetComponent<Toggle>();

            m_OnValueChanged ??= OnValueChanged;
            m_Toggle.onValueChanged.AddListener(m_OnValueChanged);
        }

        public override void ResetObject()
        {
            m_Toggle.onValueChanged.RemoveListener(m_OnValueChanged);
            m_Toggle = null;

            // 没必要重置 m_OnValueChanged

            base.ResetObject();
        }

        protected override void SetPropertyValueDirect(bool value)
        {
            m_Value.SetValue<bool>(value);
        }

        public override void CalculateValues(out bool changed)
        {
            var isOn = m_Value.GetValue<bool>();
            changed = (isOn != m_IsOn);
            m_IsOn = isOn;
        }

        public override void RenderSelf()
        {
            m_Toggle.isOn = m_IsOn;

            // 不要使用下面的方式设置。如果有其他代码监听 Toggle，下面的方式会吃掉 Notification。
            // m_Toggle.SetIsOnWithoutNotify(m_IsOn);
        }

        public override bool HasCoveredAllBranchesSinceFirstRendering()
        {
            return m_Value.IsRealProperty;
        }
    }
}