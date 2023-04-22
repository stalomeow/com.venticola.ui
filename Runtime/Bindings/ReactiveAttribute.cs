using System;

namespace VentiCola.UI.Bindings
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ReactiveAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>类型必须为引用类型</remarks>
        public Type EqualityComparer { get; set; }

        public bool LazyComputed { get; set; }
    }
}