using System;
using UnityEngine;
using VentiCola.UI.Bindings.LowLevel;

namespace VentiCola.UI.Bindings
{
    /// <summary>
    /// 表示一条缓动曲线（ReadOnly）
    /// </summary>
    /// <remarks>在没有序列化的要求时，请考虑使用 <see cref="EasingUtility"/> 下的方法</remarks>
    [Serializable]
    public struct EasingCurve
    {
        [SerializeField] private EasingCurveType m_Type;
        [SerializeField] private AnimationCurve m_Curve;
        [SerializeField] private Vector2 m_ControlPoint1;
        [SerializeField] private Vector2 m_ControlPoint2;

        /// <summary>
        /// 创建一条基础的缓动曲线
        /// </summary>
        /// <param name="curveType">缓动曲线类型。该值不可以为 <see cref="EasingCurveType.QuadraticBezier"/>、<see cref="EasingCurveType.CubicBezier"/>、<see cref="EasingCurveType.Custom"/></param>
        /// <exception cref="ArgumentException"><paramref name="curveType"/> 为任意一个不可取的值</exception>
        public EasingCurve(EasingCurveType curveType)
        {
            switch (curveType)
            {
                case EasingCurveType.QuadraticBezier:
                    throw new ArgumentException("use EasingCurve(Vector2 p1) to create a QuadraticBezier Curve.", nameof(curveType));

                case EasingCurveType.CubicBezier:
                    throw new ArgumentException("use EasingCurve(Vector2 p1, Vector2 p2) to create a CubicBezier Curve.", nameof(curveType));

                case EasingCurveType.Custom:
                    throw new ArgumentException("use EasingCurve(AnimationCurve curve) to create a Custom Curve.", nameof(curveType));
            }

            m_Type = curveType;
            m_Curve = null;
            m_ControlPoint1 = Vector2.zero;
            m_ControlPoint2 = Vector2.zero;
        }

        /// <summary>
        /// 创建一条二阶贝塞尔缓动曲线
        /// </summary>
        /// <param name="p1">控制点，x 坐标必须在 [0, 1] 范围内</param>
        /// <exception cref="ArgumentOutOfRangeException">控制点的 x 坐标不在范围内</exception>
        public EasingCurve(Vector2 p1)
        {
            if (p1.x is not (>= 0 and <= 1))
            {
                throw new ArgumentOutOfRangeException(nameof(p1), "The x component of bezier control point must be in range [0, 1].");
            }

            m_Type = EasingCurveType.QuadraticBezier;
            m_Curve = null;
            m_ControlPoint1 = p1;
            m_ControlPoint2 = Vector2.zero;
        }

        /// <summary>
        /// 创建一条二阶贝塞尔缓动曲线
        /// </summary>
        /// <param name="p1">控制点，x 坐标必须在 [0, 1] 范围内</param>
        /// <exception cref="ArgumentOutOfRangeException">控制点的 x 坐标不在范围内</exception>
        public EasingCurve((float x, float y) p1)
        {
            if (p1.x is not (>= 0 and <= 1))
            {
                throw new ArgumentOutOfRangeException(nameof(p1), "The x component of bezier control point must be in range [0, 1].");
            }

            m_Type = EasingCurveType.QuadraticBezier;
            m_Curve = null;
            m_ControlPoint1 = new Vector2(p1.x, p1.y);
            m_ControlPoint2 = Vector2.zero;
        }

        /// <summary>
        /// 创建一条三阶贝塞尔缓动曲线
        /// </summary>
        /// <param name="p1">控制点 1，x 坐标必须在 [0, 1] 范围内</param>
        /// <param name="p2">控制点 2，x 坐标必须在 [0, 1] 范围内</param>
        /// <exception cref="ArgumentOutOfRangeException">某一个控制点的 x 坐标不在范围内</exception>
        public EasingCurve(Vector2 p1, Vector2 p2)
        {
            if (p1.x is not (>= 0 and <= 1))
            {
                throw new ArgumentOutOfRangeException(nameof(p1), "The x component of bezier control point must be in range [0, 1].");
            }

            if (p2.x is not (>= 0 and <= 1))
            {
                throw new ArgumentOutOfRangeException(nameof(p2), "The x component of bezier control point must be in range [0, 1].");
            }

            m_Type = EasingCurveType.CubicBezier;
            m_Curve = null;
            m_ControlPoint1 = p1;
            m_ControlPoint2 = p2;
        }

        /// <summary>
        /// 创建一条三阶贝塞尔缓动曲线
        /// </summary>
        /// <param name="p1">控制点 1，x 坐标必须在 [0, 1] 范围内</param>
        /// <param name="p2">控制点 2，x 坐标必须在 [0, 1] 范围内</param>
        /// <exception cref="ArgumentOutOfRangeException">某一个控制点的 x 坐标不在范围内</exception>
        public EasingCurve((float x, float y) p1, (float x, float y) p2)
        {
            if (p1.x is not (>= 0 and <= 1))
            {
                throw new ArgumentOutOfRangeException(nameof(p1), "The x component of bezier control point must be in range [0, 1].");
            }

            if (p2.x is not (>= 0 and <= 1))
            {
                throw new ArgumentOutOfRangeException(nameof(p2), "The x component of bezier control point must be in range [0, 1].");
            }

            m_Type = EasingCurveType.CubicBezier;
            m_Curve = null;
            m_ControlPoint1 = new Vector2(p1.x, p1.y);
            m_ControlPoint2 = new Vector2(p2.x, p2.y);
        }

        /// <summary>
        /// 创建一条自定义缓动曲线
        /// </summary>
        /// <param name="curve">曲线</param>
        /// <exception cref="ArgumentNullException"><paramref name="curve"/> 为 null</exception>
        public EasingCurve(AnimationCurve curve)
        {
            m_Type = EasingCurveType.Custom;
            m_Curve = curve ?? throw new ArgumentNullException(nameof(curve));
            m_ControlPoint1 = Vector2.zero;
            m_ControlPoint2 = Vector2.zero;
        }

        /// <summary>
        /// 获取缓动曲线的类型
        /// </summary>
        public EasingCurveType CurveType => m_Type;

        /// <summary>
        /// 计算缓动曲线的值
        /// </summary>
        /// <param name="t">时间，范围 [0, 1]</param>
        /// <param name="lastCubicBezierParam">上一帧三次贝塞尔曲线的参数值，作为这一帧的猜测值</param>
        /// <returns>在时间为 <paramref name="t"/> 时，缓动曲线的值</returns>
        /// <exception cref="NotImplementedException">曲线未被实现</exception>
        public float Evaluate(float t, ref float lastCubicBezierParam) => m_Type switch
        {
            EasingCurveType.Linear => EasingUtility.Linear(t),

            EasingCurveType.EaseInSine => EasingUtility.EaseInSine(t),
            EasingCurveType.EaseOutSine => EasingUtility.EaseOutSine(t),
            EasingCurveType.EaseInOutSine => EasingUtility.EaseInOutSine(t),

            EasingCurveType.EaseInQuad => EasingUtility.EaseInQuad(t),
            EasingCurveType.EaseOutQuad => EasingUtility.EaseOutQuad(t),
            EasingCurveType.EaseInOutQuad => EasingUtility.EaseInOutQuad(t),

            EasingCurveType.EaseInCubic => EasingUtility.EaseInCubic(t),
            EasingCurveType.EaseOutCubic => EasingUtility.EaseOutCubic(t),
            EasingCurveType.EaseInOutCubic => EasingUtility.EaseInOutCubic(t),

            EasingCurveType.EaseInQuart => EasingUtility.EaseInQuart(t),
            EasingCurveType.EaseOutQuart => EasingUtility.EaseOutQuart(t),
            EasingCurveType.EaseInOutQuart => EasingUtility.EaseInOutQuart(t),

            EasingCurveType.EaseInQuint => EasingUtility.EaseInQuint(t),
            EasingCurveType.EaseOutQuint => EasingUtility.EaseOutQuint(t),
            EasingCurveType.EaseInOutQuint => EasingUtility.EaseInOutQuint(t),

            EasingCurveType.EaseInExpo => EasingUtility.EaseInExpo(t),
            EasingCurveType.EaseOutExpo => EasingUtility.EaseOutExpo(t),
            EasingCurveType.EaseInOutExpo => EasingUtility.EaseInOutExpo(t),

            EasingCurveType.EaseInCirc => EasingUtility.EaseInCirc(t),
            EasingCurveType.EaseOutCirc => EasingUtility.EaseOutCirc(t),
            EasingCurveType.EaseInOutCirc => EasingUtility.EaseInOutCirc(t),

            EasingCurveType.EaseInBack => EasingUtility.EaseInBack(t),
            EasingCurveType.EaseOutBack => EasingUtility.EaseOutBack(t),
            EasingCurveType.EaseInOutBack => EasingUtility.EaseInOutBack(t),

            EasingCurveType.EaseInElastic => EasingUtility.EaseInElastic(t),
            EasingCurveType.EaseOutElastic => EasingUtility.EaseOutElastic(t),
            EasingCurveType.EaseInOutElastic => EasingUtility.EaseInOutElastic(t),

            EasingCurveType.EaseInBounce => EasingUtility.EaseInBounce(t),
            EasingCurveType.EaseOutBounce => EasingUtility.EaseOutBounce(t),
            EasingCurveType.EaseInOutBounce => EasingUtility.EaseInOutBounce(t),

            EasingCurveType.QuadraticBezier => EasingUtility.QuadraticBezier(m_ControlPoint1, t),
            EasingCurveType.CubicBezier => EasingUtility.CubicBezier(m_ControlPoint1, m_ControlPoint2, t, ref lastCubicBezierParam),

            EasingCurveType.Custom => m_Curve.Evaluate(t),

            _ => throw new NotImplementedException(m_Type.ToString())
        };
    }
}