using System;

namespace VentiCola.UI
{
    [Serializable]
    public struct UIConfig
    {
        public string PrefabKey;
        public UICacheType CacheType;
        public UIRenderOption RenderOption;
        public bool ClearHistory;
        public bool IsAdditive;
    }
}