using System;
using UnityEngine;

namespace VentiCola.UI.Bindings
{
    /// <summary>
    /// 过渡动画设置
    /// </summary>
    [Serializable]
    public struct TransitionConfig
    {
        /// <summary>
        /// 延迟（单位：秒）
        /// </summary>
        [Min(0)] public float Delay;

        /// <summary>
        /// 时长（单位：秒）
        /// </summary>
        [Min(0)] public float Duration;

        /// <summary>
        /// 缓动曲线
        /// </summary>
        public EasingCurve Easing;

        /// <summary>
        /// 时间是否受 <see cref="Time.timeScale"/> 影响
        /// </summary>
        public bool UseTimeScale;

        public static TransitionConfig With(
            float duration,
            float delay = 0f,
            EasingCurve easing = default,
            bool timeScale = false)
        {
            return new TransitionConfig()
            {
                Delay = delay,
                Duration = duration,
                Easing = easing,
                UseTimeScale = timeScale
            };
        }
    }
}