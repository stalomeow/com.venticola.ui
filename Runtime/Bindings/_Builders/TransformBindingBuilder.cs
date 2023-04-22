using System;
using UnityEngine;
using VentiCola.UI.Bindings.LowLevel;

namespace VentiCola.UI.Bindings
{
    public static class TransformBindingBuilder
    {
        #region Position

        private static readonly Func<Transform, Vector3> s_PositionGetter = (Transform self) => self.position;
        private static readonly Action<Transform, Vector3> s_PositionSetter = (Transform self, Vector3 value) => self.position = value;

        public static Transform position(this Transform self, Func<Vector3> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_PositionGetter, s_PositionSetter, value);
            return self;
        }

        public static Transform position(this Transform self, Func<Vector3> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_PositionGetter, s_PositionSetter, value, in transitionConfig);
            return self;
        }

        public static Transform position(this Transform self, Func<Vector3> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_PositionGetter, s_PositionSetter, value, transitionConfig);
            return self;
        }

        #endregion

        #region PositionY

        private static readonly Func<Transform, float> s_PositionYGetter = (Transform self) => self.position.y;
        private static readonly Action<Transform, float> s_PositionYSetter = (Transform self, float value) =>
        {
            Vector3 pos = self.position;
            pos.y = value;
            self.position = pos;
        };

        public static Transform positionY(this Transform self, Func<float> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_PositionYGetter, s_PositionYSetter, value);
            return self;
        }

        public static Transform positionY(this Transform self, Func<float> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_PositionYGetter, s_PositionYSetter, value, in transitionConfig);
            return self;
        }

        public static Transform positionY(this Transform self, Func<float> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_PositionYGetter, s_PositionYSetter, value, transitionConfig);
            return self;
        }

        #endregion
    }
}