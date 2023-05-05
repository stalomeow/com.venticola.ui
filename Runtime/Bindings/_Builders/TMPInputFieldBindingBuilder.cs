#if PACKAGE_TMPRO
using System;
using UnityEngine.Events;
using VentiCola.UI.Bindings.LowLevel;
using InputField = TMPro.TMP_InputField;

namespace VentiCola.UI.Bindings
{
    public static class TMPInputFieldBindingBuilder
    {
        #region Text

        private static readonly Func<InputField, string> s_TextGetter = (InputField self) => self.text;
        private static readonly Action<InputField, string> s_TextSetter = (InputField self, string value) => self.text = value;

        public static InputField text(this InputField self, Func<InputField, string> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_TextGetter, s_TextSetter, value);
            return self;
        }

        public static InputField text(this InputField self, Func<InputField, string> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_TextGetter, s_TextSetter, value, in transitionConfig);
            return self;
        }

        public static InputField text(this InputField self, Func<InputField, string> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_TextGetter, s_TextSetter, value, transitionConfig);
            return self;
        }

        #endregion

        #region OnValidateInput

        private static readonly Func<InputField, InputField.OnValidateInput> s_OnValidateInputGetter = (InputField self) => self.onValidateInput;
        private static readonly Action<InputField, InputField.OnValidateInput> s_OnValidateInputSetter = (InputField self, InputField.OnValidateInput value) => self.onValidateInput = value;

        public static InputField onValidateInput(this InputField self, Func<InputField, InputField.OnValidateInput> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_OnValidateInputGetter, s_OnValidateInputSetter, value);
            return self;
        }

        public static InputField onValidateInput(this InputField self, Func<InputField, InputField.OnValidateInput> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_OnValidateInputGetter, s_OnValidateInputSetter, value, in transitionConfig);
            return self;
        }

        public static InputField onValidateInput(this InputField self, Func<InputField, InputField.OnValidateInput> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_OnValidateInputGetter, s_OnValidateInputSetter, value, transitionConfig);
            return self;
        }

        #endregion

        #region OnValueChanged

        private static readonly Func<InputField, UnityEvent<string>> s_OnValueChangedGetter = (InputField self) => self.onValueChanged;

        public static InputField onValueChanged(this InputField self, Action<InputField, string> handler)
        {
            BindingUtility.BindComponentEvent(self.gameObject, s_OnValueChangedGetter, handler);
            return self;
        }

        #endregion

        #region OnEndEdit

        private static readonly Func<InputField, UnityEvent<string>> s_OnEndEditGetter = (InputField self) => self.onEndEdit;

        public static InputField onEndEdit(this InputField self, Action<InputField, string> handler)
        {
            BindingUtility.BindComponentEvent(self.gameObject, s_OnEndEditGetter, handler);
            return self;
        }

        #endregion

        #region OnSubmit

        private static readonly Func<InputField, UnityEvent<string>> s_OnSubmitGetter = (InputField self) => self.onSubmit;

        public static InputField onSubmit(this InputField self, Action<InputField, string> handler)
        {
            BindingUtility.BindComponentEvent(self.gameObject, s_OnSubmitGetter, handler);
            return self;
        }

        #endregion
    }
}
#endif