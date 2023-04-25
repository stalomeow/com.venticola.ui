using System;

namespace VentiCola.UI
{
    /// <summary>
    /// UI 的配置
    /// </summary>
    [Serializable]
    public struct UIConfig
    {
        /// <summary>
        /// 用于加载预制体的 Key
        /// </summary>
        public string PrefabKey;

        /// <summary>
        /// UI 的缓存类型
        /// </summary>
        public UICacheType CacheType;

        /// <summary>
        /// UI 的渲染设置
        /// </summary>
        public UIRenderOption RenderOption;

        // TODO: Support 'ClearHistory'?
        // public bool ClearHistory;

        /// <summary>
        /// 是否以叠加的形式打开
        /// </summary>
        public bool IsAdditive;
    }
}