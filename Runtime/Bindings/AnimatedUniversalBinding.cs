using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace VentiCola.UI.Bindings
{
    public class AnimatedUniversalBinding<T> : UniversalBinding, IAnimatable
    {
        private Action<AnimatedUniversalBinding<T>, T> m_ApplyAction;
        private SharedValue<TransitionConfig> m_SharedTransitionConfig;
        private TransitionConfig m_TransitionConfig;
        private bool m_IsFirstTransition;
        private T m_From;
        private T m_To;
        private float m_StartTime;
        private float m_CubicBezierParam;

        public void InitializeTransitionConfig(
            Action<AnimatedUniversalBinding<T>, T> applyAction,
            in TransitionConfig config)
        {
            m_ApplyAction = applyAction;
            m_TransitionConfig = config;
            m_IsFirstTransition = true;
        }

        public void InitializeTransitionConfig(
            Action<AnimatedUniversalBinding<T>, T> applyAction,
            SharedValue<TransitionConfig> config)
        {
            m_ApplyAction = applyAction;
            m_SharedTransitionConfig = config;
            m_IsFirstTransition = true;
        }

        protected override void OnDetach()
        {
            m_ApplyAction = null;
            m_SharedTransitionConfig = null;
            m_TransitionConfig = default;

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                m_From = default;
                m_To = default;
            }

            base.OnDetach();
        }

        public void StartTransition(T from, T to, IAnimationUpdater animationUpdater)
        {
            ref TransitionConfig config = ref GetConfig();

            if (!CheckAnimatable(in config))
            {
                m_IsFirstTransition = false;
                m_ApplyAction(this, to);
                return;
            }

            m_IsFirstTransition = false;
            m_From = from;
            m_To = to;
            m_StartTime = GetTime(in config) + config.Delay;
            m_CubicBezierParam = 0;

            animationUpdater.RequestAnimationUpdate(this);
        }

        private ref TransitionConfig GetConfig()
        {
            if (m_SharedTransitionConfig is null)
            {
                return ref m_TransitionConfig;
            }

            return ref m_SharedTransitionConfig.Value;
        }

        private float GetTime(in TransitionConfig config)
        {
            return config.UseTimeScale ? Time.time : Time.unscaledTime;
        }

        private bool CheckAnimatable(in TransitionConfig config)
        {
            if (m_IsFirstTransition || (config.Duration <= 0))
            {
                return false;
            }

            return InterpolationCache<T>.InterpolateFunc is not null;
        }

        void IAnimatable.UpdateAnimation(out bool isFinished)
        {
            ref TransitionConfig config = ref GetConfig();
            float currentTime = GetTime(in config);

            if (currentTime >= m_StartTime)
            {
                float rawProgress = Mathf.Clamp01((currentTime - m_StartTime) / config.Duration);
                float progress = config.Easing.Evaluate(rawProgress, ref m_CubicBezierParam); // remap progress by the curve

                var interpolate = InterpolationCache<T>.InterpolateFunc;

                if (interpolate is null)
                {
                    m_ApplyAction(this, m_To);
                    isFinished = true;
                    return;
                }

                m_ApplyAction(this, interpolate(m_From, m_To, progress));

                if (progress >= 1.0f)
                {
                    isFinished = true;
                    return;
                }
            }

            isFinished = false;
        }
    }
}