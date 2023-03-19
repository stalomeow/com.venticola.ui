using System;
using UnityEngine;
using UnityEngine.UI;

namespace VentiCola.UI.Bindings.UGUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    [AddComponentMenu("UI Bindings/UGUI/Text - Legacy > Text")]
    public class TextBinding : BindingBase
    {
        [SerializeField, TextArea] private string m_Format;

        [SerializeField]
        [PropertyProxyOptions(CompactDisplay = true)]
        private PropertyLikeProxy[] m_Args;

        [NonSerialized] private Text m_Text;
        [NonSerialized] private object[] m_CachedArray;
        [NonSerialized] private string m_TextContent;

        public override void InitializeObject(ILayoutBinding parent, TemplateObject? templateObject = null)
        {
            base.InitializeObject(parent, templateObject);

            m_Text = GetComponent<Text>();
        }

        public override void ResetObject()
        {
            // 不重置 m_Text.text 了，反正就一个 string，感觉重置的开销更大。
            m_Text = null;
            m_TextContent = null;

            base.ResetObject();
        }

        public override void CalculateValues(out bool changed)
        {
            if (m_CachedArray is null || m_CachedArray.Length != m_Args.Length)
            {
                m_CachedArray = new object[m_Args.Length];
            }

            for (int i = 0; i < m_Args.Length; i++)
            {
                m_CachedArray[i] = m_Args[i].GetValue<object>();
            }

            string content = string.Format(m_Format, args: m_CachedArray);
            changed = (content != m_TextContent);
            m_TextContent = content;
        }

        public override void RenderSelf()
        {
            m_Text.text = m_TextContent;
        }

        public override bool HasCoveredAllBranchesSinceFirstRendering()
        {
            for (int i = 0; i < m_Args.Length; i++)
            {
                if (!m_Args[i].IsRealProperty)
                {
                    return false;
                }
            }

            return true;
        }
    }
}