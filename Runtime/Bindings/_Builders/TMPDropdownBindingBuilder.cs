#if PACKAGE_TMPRO
using System;
using System.Collections.Generic;
using UnityEngine.Events;
using VentiCola.UI.Bindings.LowLevel;
using Dropdown = TMPro.TMP_Dropdown;

namespace VentiCola.UI.Bindings
{
    public static class TMPDropdownBindingBuilder
    {
        #region Value

        private static readonly Func<Dropdown, int> s_ValueGetter = (Dropdown self) => self.value;
        private static readonly Action<Dropdown, int> s_ValueSetter = (Dropdown self, int value) => self.value = value;

        public static Dropdown value(this Dropdown self, Func<int> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_ValueGetter, s_ValueSetter, value);
            return self;
        }

        public static Dropdown value(this Dropdown self, Func<int> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_ValueGetter, s_ValueSetter, value, in transitionConfig);
            return self;
        }

        public static Dropdown value(this Dropdown self, Func<int> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_ValueGetter, s_ValueSetter, value, transitionConfig);
            return self;
        }

        #endregion

        #region Options

        private static readonly Func<Dropdown, List<Dropdown.OptionData>> s_OptionsGetter = (Dropdown self) => self.options;
        private static readonly Action<Dropdown, List<Dropdown.OptionData>> s_OptionsSetter = (Dropdown self, List<Dropdown.OptionData> value) => self.options = value;

        public static Dropdown options(this Dropdown self, Func<List<Dropdown.OptionData>> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_OptionsGetter, s_OptionsSetter, value);
            return self;
        }

        public static Dropdown options(this Dropdown self, Func<List<Dropdown.OptionData>> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_OptionsGetter, s_OptionsSetter, value, in transitionConfig);
            return self;
        }

        public static Dropdown options(this Dropdown self, Func<List<Dropdown.OptionData>> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_OptionsGetter, s_OptionsSetter, value, transitionConfig);
            return self;
        }

        #endregion

        #region OnValueChanged

        private static readonly Func<Dropdown, UnityEvent<int>> s_OnValueChangedGetter = (Dropdown self) => self.onValueChanged;

        public static Dropdown onValueChanged(this Dropdown self, UnityAction<int> handler)
        {
            BindingUtility.BindComponentEvent(self.gameObject, s_OnValueChangedGetter, handler);
            return self;
        }

        #endregion
    }
}
#endif