using System;
using UnityEngine;
using VentiCola.UI.Transitions;

namespace VentiCola.UI.Bindings
{
    [Serializable]
    public struct ValueTransition
    {
        public bool Enable;
        public EasingCurve Curve;
        [Min(0)] public float Delay;
        [Min(0)] public float Duration;
        public bool IgnoreTimeScale;
    }
}