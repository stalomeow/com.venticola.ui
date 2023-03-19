using System;

namespace VentiCola.UI.Factories
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class CustomPageFactoryAttribute : Attribute
    {
        public string DisplayName { get; }

        public CustomPageFactoryAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}