using System;

namespace VentiCola.UI.Bindings
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class PropertyProxyOptionsAttribute : Attribute
    {
        public bool CompactDisplay { get; set; }
    }
}