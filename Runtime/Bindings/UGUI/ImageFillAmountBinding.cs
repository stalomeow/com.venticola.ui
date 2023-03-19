using System;
using UnityEngine;
using UnityEngine.UI;

namespace VentiCola.UI.Bindings.UGUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("UI Bindings/UGUI/Image > Fill Amount")]
    public class ImageFillAmountBinding : BindingBaseWithTransition
    {
        [SerializeField]
        [PropertyTypeConstraints(typeof(float))]
        private PropertyLikeProxy m_FillAmount;

        [NonSerialized] private Image m_Image;
        [NonSerialized] private float m_StartFillAmount;
        [NonSerialized] private float m_TargetFillAmount;

        public override void InitializeObject(ILayoutBinding parent, TemplateObject? templateObject = null)
        {
            base.InitializeObject(parent, templateObject);

            m_Image = GetComponent<Image>();
        }

        public override void ResetObject()
        {
            m_Image = null;

            base.ResetObject();
        }

        public override void CalculateValues(out bool changed)
        {
            var fillAmount = m_FillAmount.GetValue<float>();
            changed = (fillAmount != m_TargetFillAmount);
            m_TargetFillAmount = fillAmount;
        }

        protected override void OnWillRenderView(bool hasTransition)
        {
            m_StartFillAmount = m_Image.fillAmount;
        }

        protected override void OnDidRenderView(bool hasTransition) { }

        protected override void RenderInterpolatedView(float percentage)
        {
            m_Image.fillAmount = Mathf.LerpUnclamped(m_StartFillAmount, m_TargetFillAmount, percentage);
        }

        public override bool HasCoveredAllBranchesSinceFirstRendering()
        {
            return m_FillAmount.IsRealProperty;
        }
    }
}