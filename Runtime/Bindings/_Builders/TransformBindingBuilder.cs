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

        public static Transform position(this Transform self, Func<Transform, Vector3> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_PositionGetter, s_PositionSetter, value);
            return self;
        }

        public static Transform position(this Transform self, Func<Transform, Vector3> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_PositionGetter, s_PositionSetter, value, in transitionConfig);
            return self;
        }

        public static Transform position(this Transform self, Func<Transform, Vector3> value, SharedValue<TransitionConfig> transitionConfig)
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

        public static Transform positionY(this Transform self, Func<Transform, float> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_PositionYGetter, s_PositionYSetter, value);
            return self;
        }

        public static Transform positionY(this Transform self, Func<Transform, float> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_PositionYGetter, s_PositionYSetter, value, in transitionConfig);
            return self;
        }

        public static Transform positionY(this Transform self, Func<Transform, float> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_PositionYGetter, s_PositionYSetter, value, transitionConfig);
            return self;
        }

        #endregion

        #region Rotation

        private static readonly Func<Transform, Quaternion> s_RotationGetter = (Transform self) => self.rotation;
        private static readonly Action<Transform, Quaternion> s_RotationSetter = (Transform self, Quaternion value) => self.rotation = value;

        public static Transform rotation(this Transform self, Func<Transform, Quaternion> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_RotationGetter, s_RotationSetter, value);
            return self;
        }

        public static Transform rotation(this Transform self, Func<Transform, Quaternion> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_RotationGetter, s_RotationSetter, value, in transitionConfig);
            return self;
        }

        public static Transform rotation(this Transform self, Func<Transform, Quaternion> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_RotationGetter, s_RotationSetter, value, transitionConfig);
            return self;
        }

        #endregion

        #region LocalPosition

        private static readonly Func<Transform, Vector3> s_LocalPositionGetter = (Transform self) => self.localPosition;
        private static readonly Action<Transform, Vector3> s_LocalPositionSetter = (Transform self, Vector3 value) => self.localPosition = value;

        public static Transform localPosition(this Transform self, Func<Transform, Vector3> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_LocalPositionGetter, s_LocalPositionSetter, value);
            return self;
        }

        public static Transform localPosition(this Transform self, Func<Transform, Vector3> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_LocalPositionGetter, s_LocalPositionSetter, value, in transitionConfig);
            return self;
        }

        public static Transform localPosition(this Transform self, Func<Transform, Vector3> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_LocalPositionGetter, s_LocalPositionSetter, value, transitionConfig);
            return self;
        }

        #endregion

        #region LocalRotation

        private static readonly Func<Transform, Quaternion> s_LocalRotationGetter = (Transform self) => self.localRotation;
        private static readonly Action<Transform, Quaternion> s_LocalRotationSetter = (Transform self, Quaternion value) => self.localRotation = value;

        public static Transform localRotation(this Transform self, Func<Transform, Quaternion> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_LocalRotationGetter, s_LocalRotationSetter, value);
            return self;
        }

        public static Transform localRotation(this Transform self, Func<Transform, Quaternion> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_LocalRotationGetter, s_LocalRotationSetter, value, in transitionConfig);
            return self;
        }

        public static Transform localRotation(this Transform self, Func<Transform, Quaternion> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_LocalRotationGetter, s_LocalRotationSetter, value, transitionConfig);
            return self;
        }

        #endregion

        #region LocalScale

        private static readonly Func<Transform, Vector3> s_LocalScaleGetter = (Transform self) => self.localScale;
        private static readonly Action<Transform, Vector3> s_LocalScaleSetter = (Transform self, Vector3 value) => self.localScale = value;

        public static Transform localScale(this Transform self, Func<Transform, Vector3> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_LocalScaleGetter, s_LocalScaleSetter, value);
            return self;
        }

        public static Transform localScale(this Transform self, Func<Transform, Vector3> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_LocalScaleGetter, s_LocalScaleSetter, value, in transitionConfig);
            return self;
        }

        public static Transform localScale(this Transform self, Func<Transform, Vector3> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_LocalScaleGetter, s_LocalScaleSetter, value, transitionConfig);
            return self;
        }

        #endregion
    }
}