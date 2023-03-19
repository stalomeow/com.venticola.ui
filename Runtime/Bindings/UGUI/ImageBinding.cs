using System;
using UnityEngine;
using UnityEngine.UI;

namespace VentiCola.UI.Bindings.UGUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("UI Bindings/UGUI/Image > Source Image")]
    public class ImageBinding : BindingBase
    {
        [SerializeField]
        [PropertyTypeConstraints(typeof(Sprite))]
        private PropertyLikeProxy m_SourceImage;

        [NonSerialized] private Image m_Image;
        [NonSerialized] private Sprite m_Sprite;

        public override void InitializeObject(ILayoutBinding parent, TemplateObject? templateObject = null)
        {
            base.InitializeObject(parent, templateObject);

            m_Image = GetComponent<Image>();
        }

        public override void ResetObject()
        {
            if (m_Image != null)
            {
                m_Image.sprite = null;
                m_Image = null;
            }

            m_Sprite = null;

            base.ResetObject();
        }

        public override void CalculateValues(out bool changed)
        {
            var sprite = m_SourceImage.GetValue<Sprite>();
            changed = (sprite != m_Sprite);
            m_Sprite = sprite;
        }

        public override void RenderSelf()
        {
            m_Image.sprite = m_Sprite;
        }

        public override bool HasCoveredAllBranchesSinceFirstRendering()
        {
            // 默认属性中无分支代码。
            // Computed 属性因为有一层封装，所以相当于无分支代码。
            // 手动实现的属性，不推荐写，所以假设无分支代码。
            // 手动实现的方法，不清楚是否有分支，所以全部当成有分支处理。
            return m_SourceImage.IsRealProperty;
        }
    }
}