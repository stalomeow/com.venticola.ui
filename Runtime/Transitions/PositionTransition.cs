using System;
using UnityEngine;

namespace VentiCola.UI.Transitions
{
    [AddComponentMenu("")]
    [CustomTransition("Transform/Position")]
    public class PositionTransition : TransitionBaseTween
    {
        public enum PositionAxis
        {
            XAxis = 0,
            YAxis = 1,
            ZAxis = 2
        }

        public enum PositionSpace
        {
            Local,
            World,
            [InspectorName("Anchored (RectTransform)")]
            Anchored
        }

        [SerializeField] private Transform m_Target;
        [SerializeField] private PositionAxis m_Axis = PositionAxis.XAxis;
        [SerializeField] private PositionSpace m_Space = PositionSpace.Local;

        [Header("Values")]

        [SerializeField] private float m_Enter = 0;
        [SerializeField] private TweenConfig m_EnterTransition;
        [SerializeField] private float m_Active = 0;
        [SerializeField] private TweenConfig m_ExitTransition;
        [SerializeField] private float m_Exit = 0;

        [NonSerialized] private float m_From;
        [NonSerialized] private float m_To;

        protected override void PrepareNewTween(TransitionType type, bool prevFinished, out TweenConfig tween)
        {
            if (type == TransitionType.Enter)
            {
                m_From = prevFinished ? m_Enter : GetValue();
                m_To = m_Active;
                tween = m_EnterTransition;
            }
            else
            {
                m_From = prevFinished ? m_Active : GetValue();
                m_To = m_Exit;
                tween = m_ExitTransition;
            }
        }

        protected override void SetTweenValue(float percentage)
        {
            SetValue(Mathf.LerpUnclamped(m_From, m_To, percentage));
        }

        protected float GetValue()
        {
            Vector3 pos = m_Space switch
            {
                PositionSpace.Local => m_Target.localPosition,
                PositionSpace.World => m_Target.position,
                PositionSpace.Anchored => (m_Target as RectTransform).anchoredPosition3D,
                _ => throw new NotImplementedException(),
            };
            return pos[(int)m_Axis];
        }

        protected void SetValue(float value)
        {
            if (m_Space == PositionSpace.Local)
            {
                Vector3 pos = m_Target.localPosition;
                pos[(int)m_Axis] = value;
                m_Target.localPosition = pos;
            }
            else if (m_Space == PositionSpace.World)
            {
                Vector3 pos = m_Target.position;
                pos[(int)m_Axis] = value;
                m_Target.position = pos;
            }
            else if (m_Space == PositionSpace.Anchored)
            {
                RectTransform rectTransform = m_Target as RectTransform;
                Vector3 pos = rectTransform.anchoredPosition3D;
                pos[(int)m_Axis] = value;
                rectTransform.anchoredPosition3D = pos;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
