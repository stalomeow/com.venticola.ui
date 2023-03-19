using System;
using UnityEngine;

namespace VentiCola.UI.Transitions
{
    [AddComponentMenu("")]
    [CustomTransition("Graphics/Alpha")]
    public class AlphaTransition : TransitionBaseTween
    {
        [SerializeField] private CanvasGroup m_Target;

        [Header("Values")]

        [SerializeField, Range(0, 1)] private float m_Enter = 0;
        [SerializeField]              private TweenConfig m_EnterTransition;
        [SerializeField, Range(0, 1)] private float m_Active = 1;
        [SerializeField]              private TweenConfig m_ExitTransition;
        [SerializeField, Range(0, 1)] private float m_Exit = 0;

        [NonSerialized] private float m_From;
        [NonSerialized] private float m_To;

        protected override void PrepareNewTween(TransitionType type, bool prevFinished, out TweenConfig tween)
        {
            if (type == TransitionType.Enter)
            {
                m_From = prevFinished ? m_Enter : m_Target.alpha; // 避免 alpha 值突变
                m_To = m_Active;
                tween = m_EnterTransition;
            }
            else
            {
                m_From = prevFinished ? m_Active : m_Target.alpha; // 避免 alpha 值突变
                m_To = m_Exit;
                tween = m_ExitTransition;
            }
        }

        protected override void SetTweenValue(float percentage)
        {
            m_Target.alpha = Mathf.LerpUnclamped(m_From, m_To, percentage);
        }
    }
}