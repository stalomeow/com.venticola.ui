using System;

namespace VentiCola.UI
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class LazyComputedAttribute : Attribute
    {
        public bool NoBranches { get; set; }

        public LazyComputedAttribute() { }
    }
}