using System.Collections.Generic;

namespace VentiCola.UI.Bindings
{
    public interface ILayoutBinding : IBinding
    {
        bool EnableChildBindingRendering { get; }

        HashSet<IBinding> DirtyChildBindings { get; }

        void GetChildBindings(List<IBinding> results);
    }
}
