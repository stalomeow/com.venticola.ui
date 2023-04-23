#if PACKAGE_UGUI
using System;
using UnityEngine.Events;
using UnityEngine.UI;
using VentiCola.UI.Bindings.LowLevel;

namespace VentiCola.UI.Bindings
{
    public static class UGUISliderBindingBuilder
    {
        #region Value

        private static readonly Func<Slider, float> s_ValueGetter = (Slider self) => self.value;
        private static readonly Action<Slider, float> s_ValueSetter = (Slider self, float value) => self.value = value;

        public static Slider value(this Slider self, Func<float> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_ValueGetter, s_ValueSetter, value);
            return self;
        }

        public static Slider value(this Slider self, Func<float> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_ValueGetter, s_ValueSetter, value, in transitionConfig);
            return self;
        }

        public static Slider value(this Slider self, Func<float> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_ValueGetter, s_ValueSetter, value, transitionConfig);
            return self;
        }

        #endregion

        #region OnValueChanged

        private static readonly Func<Slider, UnityEvent<float>> s_OnValueChangedGetter = (Slider self) => self.onValueChanged;

        public static Slider onValueChanged(this Slider self, UnityAction<float> handler)
        {
            BindingUtility.BindComponentEvent(self.gameObject, s_OnValueChangedGetter, handler);
            return self;
        }

        #endregion
    }
}
#endif