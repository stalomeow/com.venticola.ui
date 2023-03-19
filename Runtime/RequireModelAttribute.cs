using System;

namespace VentiCola.UI
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RequireModelAttribute : Attribute
    {
        public Type ModelType { get; }

        public RequireModelAttribute(Type modelType)
        {
            ModelType = modelType;
        }
    }
}