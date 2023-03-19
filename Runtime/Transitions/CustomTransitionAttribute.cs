using System;

namespace VentiCola.UI.Transitions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class CustomTransitionAttribute : Attribute
    {
        public string MenuPath { get; }

        public CustomTransitionAttribute(string menuPath)
        {
            MenuPath = menuPath;
        }
    }
}