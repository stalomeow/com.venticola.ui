#if PACKAGE_UGUI
using System;
using UnityEngine.Events;
using UnityEngine.UI;
using VentiCola.UI.Bindings.LowLevel;

namespace VentiCola.UI.Bindings
{
    public static class UGUIScrollbarBindingBuilder
    {
        #region Value

        private static readonly Func<Scrollbar, float> s_ValueGetter = (Scrollbar self) => self.value;
        private static readonly Action<Scrollbar, float> s_ValueSetter = (Scrollbar self, float value) => self.value = value;

        public static Scrollbar value(this Scrollbar self, Func<Scrollbar, float> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_ValueGetter, s_ValueSetter, value);
            return self;
        }

        public static Scrollbar value(this Scrollbar self, Func<Scrollbar, float> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_ValueGetter, s_ValueSetter, value, in transitionConfig);
            return self;
        }

        public static Scrollbar value(this Scrollbar self, Func<Scrollbar, float> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_ValueGetter, s_ValueSetter, value, transitionConfig);
            return self;
        }

        #endregion

        #region OnValueChanged

        private static readonly Func<Scrollbar, UnityEvent<float>> s_OnValueChangedGetter = (Scrollbar self) => self.onValueChanged;

        public static Scrollbar onValueChanged(this Scrollbar self, Action<Scrollbar, float> handler)
        {
            BindingUtility.BindComponentEvent(self.gameObject, s_OnValueChangedGetter, handler);
            return self;
        }

        #endregion
    }
}
#endif