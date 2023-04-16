using System;
using UnityEngine;

namespace VentiCola.UI.Bindings
{
    public static class CanvasGroupBindingBuilder
    {
        #region Alpha

        private static readonly Func<CanvasGroup, float> s_AlphaGetter = (CanvasGroup self) => self.alpha;
        private static readonly Action<CanvasGroup, float> s_AlphaSetter = (CanvasGroup self, float value) => self.alpha = value;

        public static CanvasGroup alpha(this CanvasGroup self, Func<float> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_AlphaGetter, s_AlphaSetter, value);
            return self;
        }

        public static CanvasGroup alpha(this CanvasGroup self, Func<float> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_AlphaGetter, s_AlphaSetter, value, in transitionConfig);
            return self;
        }

        public static CanvasGroup alpha(this CanvasGroup self, Func<float> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_AlphaGetter, s_AlphaSetter, value, transitionConfig);
            return self;
        }

        #endregion
    }
}