#if PACKAGE_UGUI
using System;
using UnityEngine;
using UnityEngine.UI;
using VentiCola.UI.Bindings.LowLevel;

namespace VentiCola.UI.Bindings
{
    public static class UGUIGraphicBindingBuilder
    {
        #region Color

        private static readonly Func<Graphic, Color> s_ColorGetter = (Graphic self) => self.color;
        private static readonly Action<Graphic, Color> s_ColorSetter = (Graphic self, Color value) => self.color = value;

        public static Graphic color(this Graphic self, Func<Graphic, Color> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_ColorGetter, s_ColorSetter, value);
            return self;
        }

        public static Graphic color(this Graphic self, Func<Graphic, Color> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_ColorGetter, s_ColorSetter, value, in transitionConfig);
            return self;
        }

        public static Graphic color(this Graphic self, Func<Graphic, Color> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_ColorGetter, s_ColorSetter, value, transitionConfig);
            return self;
        }

        #endregion
    }
}
#endif