using System;

namespace VentiCola.UI
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class CustomControllerForUIPageAttribute : Attribute
    {
        public string ViewPrefabKey { get; }

        public UICacheType CacheType { get; set; } = UICacheType.LastOnly;

        public bool ClearHistory { get; set; } = false;

        public CustomControllerForUIPageAttribute(string viewPrefabKey)
        {
            ViewPrefabKey = viewPrefabKey;
        }
    }
}