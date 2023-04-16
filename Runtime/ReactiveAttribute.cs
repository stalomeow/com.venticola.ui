using System;

namespace VentiCola.UI
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ReactiveAttribute : Attribute
    {
        public bool LazyComputed { get; set; }
    }
}