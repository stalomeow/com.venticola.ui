using System.Runtime.CompilerServices;
using UnityEngine;

namespace VentiCola.UI.Bindings.LowLevel
{
    public static class EasingUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Linear(float t) => t;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInSine(float t) => 1 - Mathf.Cos(t * Mathf.PI * 0.5f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutSine(float t) => Mathf.Sin(t * Mathf.PI * 0.5f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInOutSine(float t) => -0.5f * (Mathf.Cos(Mathf.PI * t) - 1);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInQuad(float t) => t * t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutQuad(float t) => 1 - (1 - t) * (1 - t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInOutQuad(float t) => t switch
        {
            < 0.5f => 2 * t * t,
            _ => 1 - 2 * (1 - t) * (1 - t)
        };


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInCubic(float t) => t * t * t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutCubic(float t) => 1 - (1 - t) * (1 - t) * (1 - t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInOutCubic(float t) => t switch
        {
            < 0.5f => 4 * t * t * t,
            _ => 1 - 4 * (1 - t) * (1 - t) * (1 - t)
        };


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInQuart(float t) => t * t * t * t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutQuart(float t) => 1 - (1 - t) * (1 - t) * (1 - t) * (1 - t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInOutQuart(float t) => t switch
        {
            < 0.5f => 8 * t * t * t * t,
            _ => 1 - 8 * (1 - t) * (1 - t) * (1 - t) * (1 - t)
        };


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInQuint(float t) => t * t * t * t * t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutQuint(float t) => 1 - (1 - t) * (1 - t) * (1 - t) * (1 - t) * (1 - t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInOutQuint(float t) => t switch
        {
            < 0.5f => 16 * t * t * t * t * t,
            _ => 1 - 16 * (1 - t) * (1 - t) * (1 - t) * (1 - t) * (1 - t)
        };


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInExpo(float t) => t switch
        {
            0 => 0,
            _ => Mathf.Pow(2, 10 * (t - 1))
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutExpo(float t) => t switch
        {
            1 => 1,
            _ => 1 - Mathf.Pow(2, -10 * t)
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInOutExpo(float t) => t switch
        {
            0 => 0,
            1 => 1,
            < 0.5f => 0.5f * Mathf.Pow(2, 20 * t - 10),
            _ => 1 - 0.5f * Mathf.Pow(2, -20 * t + 10)
        };


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInCirc(float t) => 1 - Mathf.Sqrt(1 - t * t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutCirc(float t) => Mathf.Sqrt(1 - (1 - t) * (1 - t));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInOutCirc(float t) => t switch
        {
            < 0.5f => 0.5f * (1 - Mathf.Sqrt(1 - 4 * t * t)),
            _ => 0.5f * (1 + Mathf.Sqrt(1 - 4 * (1 - t) * (1 - t)))
        };


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInBack(float t) => 2.70158f * t * t * t - 1.70158f * t * t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutBack(float t) => 1 + 2.70158f * (t - 1) * (t - 1) * (t - 1) + 1.70158f * (t - 1) * (t - 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c2 = c1 * 1.525f;

            return t switch
            {
                < 0.5f => 2 * t * t * ((c2 + 1) * 2 * t - c2),
                _ => 1 + 2 * (t - 1) * (t - 1) * ((c2 + 1) * 2 * (t - 1) + c2)
            };
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInElastic(float t) => t switch
        {
            0 => 0,
            1 => 1,
            _ => -Mathf.Pow(2, 10 * t - 10) * Mathf.Sin((t * 10 - 10.75f) * Mathf.PI * 2 / 3)
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutElastic(float t) => t switch
        {
            0 => 0,
            1 => 1,
            _ => 1 + Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * Mathf.PI * 2 / 3)
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInOutElastic(float t) => t switch
        {
            0 => 0,
            1 => 1,
            < 0.5f => -0.5f * Mathf.Pow(2, 20 * t - 10) * Mathf.Sin((20 * t - 11.125f) * (2 * Mathf.PI) / 4.5f),
            _ => 1 + 0.5f * Mathf.Pow(2, -20 * t + 10) * Mathf.Sin((20 * t - 11.125f) * (2 * Mathf.PI) / 4.5f)
        };


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInBounce(float t) => (t switch
        {
            <= 0.1f => -(t - 0.05f) * (t - 0.05f) + 0.0025f,
            <= 0.3f => -(t - 0.20f) * (t - 0.20f) + 0.0100f,
            <= 0.7f => -(t - 0.50f) * (t - 0.50f) + 0.0400f,
            _       => -(t - 1.00f) * (t - 1.00f) + 0.0900f
        }) / 0.09f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseOutBounce(float t) => 1 - EaseInBounce(1 - t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float EaseInOutBounce(float t) => t switch
        {
            < 0.5f => 0.5f * EaseInBounce(2 * t),
            _ => 0.5f * (1 + EaseOutBounce(2 * t - 1))
        };


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float QuadraticBezier(Vector2 p1, float t)
        {
            float a; // 贝塞尔曲线的参数

            if (Mathf.Approximately(p1.x, 0.5f))
            {
                a = t; // 特殊情况，一元一次方程
            }
            else
            {
                // 判别式肯定大于等于 0，因为 t 是曲线上一个点的横坐标，至少有一个参数与之对应
                float halfSqrtDelta = Mathf.Sqrt(p1.x * p1.x - 2 * p1.x * t + t);
                float denominator = 2 * p1.x - 1;

                // 将求根公式的两个结果看作两个二元函数
                // 解一定是下面这个（因为 a 也必须在 [0, 1] 范围内）
                a = (p1.x - halfSqrtDelta) / denominator;
            }

            float b = p1.y * 2;
            return a * ((1 - b) * a + b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CubicBezier(Vector2 p1, Vector2 p2, float t, ref float lastParam)
        {
            if (t == 0)
            {
                lastParam = 0;
                return 0;
            }

            if (t == 1)
            {
                lastParam = 1;
                return 1;
            }

            const float accuracy = 0.001f; // 1/3 the desired accuracy in X
            const float zero = 0.000001f;  // computational zero

            float bx = p1.x * 3;
            float cx = p2.x * 3;

            float bottom = 0f;
            float top = 1f;

            while (top - bottom > zero)
            {
                GetXAndDx(bx, cx, lastParam, out float x, out float dx);
                float absDx = dx < 0 ? -dx : dx;

                if (x > t)
                {
                    top = lastParam;
                }
                else
                {
                    bottom = lastParam;
                }

                if (Mathf.Abs(x - t) < accuracy * absDx)
                {
                    break;
                }

                if (absDx > zero)
                {
                    float next = lastParam - (x - t) / dx;

                    if (next >= top)
                    {
                        lastParam = (lastParam + top) * 0.5f;
                    }
                    else if (next <= bottom)
                    {
                        lastParam = (lastParam + bottom) * 0.5f;
                    }
                    else
                    {
                        lastParam = next;
                    }
                }
                else
                {
                    lastParam = (top + bottom) * 0.5f;
                }
            }

            return GetY(3 * p1.y, 3 * p2.y, lastParam);

            static void GetXAndDx(float bx, float cx, float t, out float x, out float dx)
            {
                float s = 1.0f - t;
                float t2 = t * t;
                float s2 = s * s;

                x = bx * t * s2 + cx * t2 * s + t2 * t;
                dx = bx * s2 + 2 * (cx - bx) * s * t + (3 - cx) * t2;
            }

            static float GetY(float by, float cy, float t)
            {
                float s = 1.0f - t;
                float t2 = t * t;
                float s2 = s * s;

                return by * t * s2 + cy * t2 * s + t2 * t;
            }
        }
    }
}