#if PACKAGE_UGUI
using System;
using UnityEngine.Events;
using UnityEngine.UI;
using VentiCola.UI.Bindings.LowLevel;

namespace VentiCola.UI.Bindings
{
    public static class UGUIToggleBindingBuilder
    {
        #region IsOn

        private static readonly Func<Toggle, bool> s_IsOnGetter = (Toggle self) => self.isOn;
        private static readonly Action<Toggle, bool> s_IsOnSetter = (Toggle self, bool value) => self.isOn = value;

        public static Toggle isOn(this Toggle self, Func<bool> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_IsOnGetter, s_IsOnSetter, value);
            return self;
        }

        public static Toggle isOn(this Toggle self, Func<bool> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_IsOnGetter, s_IsOnSetter, value, in transitionConfig);
            return self;
        }

        public static Toggle isOn(this Toggle self, Func<bool> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_IsOnGetter, s_IsOnSetter, value, transitionConfig);
            return self;
        }

        #endregion

        #region OnValueChanged

        private static readonly Func<Toggle, UnityEvent<bool>> s_OnValueChangedGetter = (Toggle self) => self.onValueChanged;

        public static Toggle onValueChanged(this Toggle self, UnityAction<bool> handler)
        {
            BindingUtility.BindComponentEvent(self.gameObject, s_OnValueChangedGetter, handler);
            return self;
        }

        #endregion
    }
}
#endif