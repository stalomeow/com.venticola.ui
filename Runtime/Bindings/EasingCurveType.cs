using UnityEngine;

namespace VentiCola.UI.Bindings
{
    public enum EasingCurveType
    {
        Linear = 0,

        [InspectorName("Ease In/Sine")] EaseInSine,
        [InspectorName("Ease Out/Sine")] EaseOutSine,
        [InspectorName("Ease In Out/Sine")] EaseInOutSine,

        [InspectorName("Ease In/Quad")] EaseInQuad,
        [InspectorName("Ease Out/Quad")] EaseOutQuad,
        [InspectorName("Ease In Out/Quad")] EaseInOutQuad,

        [InspectorName("Ease In/Cubic")] EaseInCubic,
        [InspectorName("Ease Out/Cubic")] EaseOutCubic,
        [InspectorName("Ease In Out/Cubic")] EaseInOutCubic,

        [InspectorName("Ease In/Quart")] EaseInQuart,
        [InspectorName("Ease Out/Quart")] EaseOutQuart,
        [InspectorName("Ease In Out/Quart")] EaseInOutQuart,

        [InspectorName("Ease In/Quint")] EaseInQuint,
        [InspectorName("Ease Out/Quint")] EaseOutQuint,
        [InspectorName("Ease In Out/Quint")] EaseInOutQuint,

        [InspectorName("Ease In/Expo")] EaseInExpo,
        [InspectorName("Ease Out/Expo")] EaseOutExpo,
        [InspectorName("Ease In Out/Expo")] EaseInOutExpo,

        [InspectorName("Ease In/Circ")] EaseInCirc,
        [InspectorName("Ease Out/Circ")] EaseOutCirc,
        [InspectorName("Ease In Out/Circ")] EaseInOutCirc,

        [InspectorName("Ease In/Back")] EaseInBack,
        [InspectorName("Ease Out/Back")] EaseOutBack,
        [InspectorName("Ease In Out/Back")] EaseInOutBack,

        [InspectorName("Ease In/Elastic")] EaseInElastic,
        [InspectorName("Ease Out/Elastic")] EaseOutElastic,
        [InspectorName("Ease In Out/Elastic")] EaseInOutElastic,

        [InspectorName("Ease In/Bounce")] EaseInBounce,
        [InspectorName("Ease Out/Bounce")] EaseOutBounce,
        [InspectorName("Ease In Out/Bounce")] EaseInOutBounce,

        [InspectorName("Bezier/Quadratic")] QuadraticBezier,
        [InspectorName("Bezier/Cubic")] CubicBezier,

        Custom
    }
}