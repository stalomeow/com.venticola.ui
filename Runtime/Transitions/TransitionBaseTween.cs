using System;
using UnityEngine;

namespace VentiCola.UI.Transitions
{
    public abstract class TransitionBaseTween : TransitionBase
    {
        [NonSerialized] private TweenConfig m_CurrentTween = null;
        [NonSerialized] private bool m_Finished = true;
        [NonSerialized] private float m_StartTime = 0f;

        protected TransitionBaseTween() { }

        public override bool IsFinished => m_Finished;

        public override void BeginTransition(TransitionType type, bool forceRestart, bool instant)
        {
            if (forceRestart)
            {
                m_Finished = true;
            }

            PrepareNewTween(type, m_Finished, out m_CurrentTween);

            if (!instant && (m_CurrentTween.Delay > 0.001f || m_CurrentTween.Duration > 0.001f))
            {
                m_Finished = false;
                m_StartTime = GetTime() + m_CurrentTween.Delay;
                SetTweenValue(m_CurrentTween.Easing.Evaluate(0));
            }
            else
            {
                m_Finished = true;
                SetTweenValue(m_CurrentTween.Easing.Evaluate(1));
            }
        }

        public override void UpdateTransition()
        {
            if (m_Finished)
            {
                return;
            }

            float time = GetTime();

            if (time < m_StartTime)
            {
                return;
            }

            float t = Mathf.Clamp01((time - m_StartTime) / m_CurrentTween.Duration);

            if (Mathf.Approximately(t, 1f))
            {
                t = 1f;
                m_Finished = true;
            }

            SetTweenValue(m_CurrentTween.Easing.Evaluate(t));
        }

        private float GetTime()
        {
            return m_CurrentTween.IgnoreTimeScale ? Time.unscaledTime : Time.time;
        }

        protected abstract void PrepareNewTween(TransitionType type, bool prevFinished, out TweenConfig tween);

        protected abstract void SetTweenValue(float percentage);
    }
}