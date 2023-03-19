using System;
using UnityEngine;

namespace VentiCola.UI.Transitions
{
    [Serializable]
    public class TweenConfig
    {
        [Min(0)] public float Delay = 0;
        [Min(0)] public float Duration = 0.25f;
        public EasingCurve Easing;
        public bool IgnoreTimeScale = true;
    }
}