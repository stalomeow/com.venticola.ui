#if PACKAGE_UGUI
using System;
using UnityEngine.UI;
using VentiCola.UI.Bindings.LowLevel;

namespace VentiCola.UI.Bindings
{
    public static class UGUITextBindingBuilder
    {
        #region Text

        private static readonly Func<Text, string> s_TextGetter = (Text self) => self.text;
        private static readonly Action<Text, string> s_TextSetter = (Text self, string value) => self.text = value;

        public static Text text(this Text self, Func<string> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_TextGetter, s_TextSetter, value);
            return self;
        }

        public static Text text(this Text self, Func<string> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_TextGetter, s_TextSetter, value, in transitionConfig);
            return self;
        }

        public static Text text(this Text self, Func<string> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_TextGetter, s_TextSetter, value, transitionConfig);
            return self;
        }

        #endregion

        #region FontSize

        private static readonly Func<Text, int> s_FontSizeGetter = (Text self) => self.fontSize;
        private static readonly Action<Text, int> s_FontSizeSetter = (Text self, int value) => self.fontSize = value;

        public static Text fontSize(this Text self, Func<int> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_FontSizeGetter, s_FontSizeSetter, value);
            return self;
        }

        public static Text fontSize(this Text self, Func<int> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_FontSizeGetter, s_FontSizeSetter, value, in transitionConfig);
            return self;
        }

        public static Text fontSize(this Text self, Func<int> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_FontSizeGetter, s_FontSizeSetter, value, transitionConfig);
            return self;
        }

        #endregion
    }
}
#endif