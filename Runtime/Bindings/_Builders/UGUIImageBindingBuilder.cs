#if PACKAGE_UGUI
using System;
using UnityEngine;
using UnityEngine.UI;
using VentiCola.UI.Bindings.LowLevel;

namespace VentiCola.UI.Bindings
{
    public static class UGUIImageBindingBuilder
    {
        #region Sprite

        private static readonly Func<Image, Sprite> s_SpriteGetter = (Image self) => self.sprite;
        private static readonly Action<Image, Sprite> s_SpriteSetter = (Image self, Sprite value) => self.sprite = value;

        public static Image sprite(this Image self, Func<Sprite> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_SpriteGetter, s_SpriteSetter, value);
            return self;
        }

        public static Image sprite(this Image self, Func<Sprite> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_SpriteGetter, s_SpriteSetter, value, in transitionConfig);
            return self;
        }

        public static Image sprite(this Image self, Func<Sprite> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_SpriteGetter, s_SpriteSetter, value, transitionConfig);
            return self;
        }

        #endregion

        #region FillAmount

        private static readonly Func<Image, float> s_FillAmountGetter = (Image self) => self.fillAmount;
        private static readonly Action<Image, float> s_FillAmountSetter = (Image self, float value) => self.fillAmount = value;

        public static Image fillAmount(this Image self, Func<float> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_FillAmountGetter, s_FillAmountSetter, value);
            return self;
        }

        public static Image fillAmount(this Image self, Func<float> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_FillAmountGetter, s_FillAmountSetter, value, in transitionConfig);
            return self;
        }

        public static Image fillAmount(this Image self, Func<float> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_FillAmountGetter, s_FillAmountSetter, value, transitionConfig);
            return self;
        }

        #endregion
    }
}
#endif