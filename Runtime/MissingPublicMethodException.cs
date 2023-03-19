using System;

namespace VentiCola.UI
{
    public class MissingPublicMethodException : Exception
    {
        public MissingPublicMethodException(string methodName)
            : base($"Can not find public method '{methodName}'.") { }
    }
}
