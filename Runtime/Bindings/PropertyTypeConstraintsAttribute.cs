using System;

namespace VentiCola.UI.Bindings
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class PropertyTypeConstraintsAttribute : Attribute
    {
        public Type[] Constraints { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="constraints">需要同时满足的限制</param>
        public PropertyTypeConstraintsAttribute(params Type[] constraints)
        {
            Constraints = constraints;
        }
    }
}