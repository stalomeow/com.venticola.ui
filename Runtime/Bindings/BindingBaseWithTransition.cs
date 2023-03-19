using System;
using UnityEngine;

namespace VentiCola.UI.Bindings
{
    public abstract class BindingBaseWithTransition : BindingBase
    {
        [SerializeField]
        private ValueTransition m_Transition = new ValueTransition
        {
            Enable = false,
            Delay = 0,
            Duration = 0.25f,
            IgnoreTimeScale = true
        };

        [NonSerialized] private bool m_TransitionEnded = true;
        [NonSerialized] private float m_TransitionStartTime;

        public override void ResetObject()
        {
            m_TransitionEnded = true; // 立刻终止过渡动画

            base.ResetObject();
        }

        private void Update()
        {
            if (m_TransitionEnded)
            {
                return;
            }

            float time = GetCurrentTime();

            if (time < m_TransitionStartTime)
            {
                return;
            }

            float t = Mathf.Clamp01((time - m_TransitionStartTime) / m_Transition.Duration);

            if (Mathf.Approximately(t, 1f))
            {
                t = 1f;
                m_TransitionEnded = true;
            }

            RenderInterpolatedView(m_Transition.Curve.Evaluate(t));

            if (m_TransitionEnded)
            {
                OnDidRenderView(true);
            }
        }

        private float GetCurrentTime()
        {
            return m_Transition.IgnoreTimeScale ? Time.unscaledTime : Time.time;
        }

        public sealed override void RenderSelf()
        {
            ref ValueTransition trans = ref m_Transition;

            // 第一次执行的时候应该快速变到起始状态！
            if (!IsFirstRendering && trans.Enable)
            {
                m_TransitionEnded = false;
                m_TransitionStartTime = GetCurrentTime() + trans.Delay;

                OnWillRenderView(true);
            }
            else
            {
                m_TransitionEnded = true;

                OnWillRenderView(false);
                RenderInterpolatedView(1f);
                OnDidRenderView(false);
            }
        }

        /// <summary>
        /// 在这个方法中记录初始状态。最终状态请在 <see cref="BindingBase.CalculateValues(out bool)"/> 中计算。
        /// </summary>
        /// <param name="hasTransition"></param>
        protected abstract void OnWillRenderView(bool hasTransition);

        protected abstract void OnDidRenderView(bool hasTransition);

        protected abstract void RenderInterpolatedView(float percentage);
    }
}