using System;
using UnityEngine;
using VentiCola.UI.Bindings.LowLevel;

namespace VentiCola.UI.Bindings
{
    public static class RectTransformBindingBuilder
    {
        #region AnchoredPosition

        private static readonly Func<RectTransform, Vector2> s_AnchoredPositionGetter = (RectTransform self) => self.anchoredPosition;
        private static readonly Action<RectTransform, Vector2> s_AnchoredPositionSetter = (RectTransform self, Vector2 value) => self.anchoredPosition = value;


        public static RectTransform anchoredPosition(this RectTransform self, Func<RectTransform, Vector2> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_AnchoredPositionGetter, s_AnchoredPositionSetter, value);
            return self;
        }

        public static RectTransform anchoredPosition(this RectTransform self, Func<RectTransform, Vector2> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_AnchoredPositionGetter, s_AnchoredPositionSetter, value, in transitionConfig);
            return self;
        }

        public static RectTransform anchoredPosition(this RectTransform self, Func<RectTransform, Vector2> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_AnchoredPositionGetter, s_AnchoredPositionSetter, value, transitionConfig);
            return self;
        }

        #endregion

        #region AnchoredPositionY

        private static readonly Func<RectTransform, float> s_AnchoredPositionYGetter = (RectTransform self) => self.anchoredPosition.y;
        private static readonly Action<RectTransform, float> s_AnchoredPositionYSetter = (RectTransform self, float value) =>
        {
            Vector3 pos = self.anchoredPosition;
            pos.y = value;
            self.anchoredPosition = pos;
        };

        public static RectTransform anchoredPositionY(this RectTransform self, Func<RectTransform, float> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_AnchoredPositionYGetter, s_AnchoredPositionYSetter, value);
            return self;
        }

        public static RectTransform anchoredPositionY(this RectTransform self, Func<RectTransform, float> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_AnchoredPositionYGetter, s_AnchoredPositionYSetter, value, in transitionConfig);
            return self;
        }

        public static RectTransform anchoredPositionY(this RectTransform self, Func<RectTransform, float> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_AnchoredPositionYGetter, s_AnchoredPositionYSetter, value, transitionConfig);
            return self;
        }

        #endregion
    }
}