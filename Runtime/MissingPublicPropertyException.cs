using System;

namespace VentiCola.UI
{
    public class MissingPublicPropertyException : Exception
    {
        public MissingPublicPropertyException(string propertyName)
            : base($"Can not find public property '{propertyName}'.") { }
    }
}
