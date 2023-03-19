using System;

namespace VentiCola.UI.Internals
{
    public interface ICustomScope
    {
#if UNITY_EDITOR
        (Type type, string name, string tooltip)[] GetVarHintsInEditor();
#endif

        T GetVariable<T>(string name);
    }
}