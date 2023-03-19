using UnityEngine;

namespace VentiCola.UI.Bindings
{
    [DisallowMultipleComponent]
    public abstract class LayoutBindingAuthoring : MonoBehaviour
    {
        public abstract ILayoutBinding ProvideBinding();

        public abstract TemplateObject ConvertToTemplate();
    }
}
