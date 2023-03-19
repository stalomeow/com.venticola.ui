using System;

namespace VentiCola.UI
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public sealed class EditAsEnumAttribute : Attribute
    {
        public Type EnumType { get; }

        public EditAsEnumAttribute(Type enumType)
        {
            EnumType = enumType;
        }
    }
}