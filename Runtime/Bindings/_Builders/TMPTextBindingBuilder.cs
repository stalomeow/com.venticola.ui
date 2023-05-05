#if PACKAGE_TMPRO
using System;
using VentiCola.UI.Bindings.LowLevel;
using Text = TMPro.TMP_Text;

namespace VentiCola.UI.Bindings
{
    public static class TMPTextBindingBuilder
    {
        #region Text

        private static readonly Func<Text, string> s_TextGetter = (Text self) => self.text;
        private static readonly Action<Text, string> s_TextSetter = (Text self, string value) => self.text = value;

        public static Text text(this Text self, Func<Text, string> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_TextGetter, s_TextSetter, value);
            return self;
        }

        public static Text text(this Text self, Func<Text, string> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_TextGetter, s_TextSetter, value, in transitionConfig);
            return self;
        }

        public static Text text(this Text self, Func<Text, string> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_TextGetter, s_TextSetter, value, transitionConfig);
            return self;
        }

        #endregion

        #region FontSize

        private static readonly Func<Text, float> s_FontSizeGetter = (Text self) => self.fontSize;
        private static readonly Action<Text, float> s_FontSizeSetter = (Text self, float value) => self.fontSize = value;

        public static Text fontSize(this Text self, Func<Text, float> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_FontSizeGetter, s_FontSizeSetter, value);
            return self;
        }

        public static Text fontSize(this Text self, Func<Text, float> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_FontSizeGetter, s_FontSizeSetter, value, in transitionConfig);
            return self;
        }

        public static Text fontSize(this Text self, Func<Text, float> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_FontSizeGetter, s_FontSizeSetter, value, transitionConfig);
            return self;
        }

        #endregion
    }
}
#endif