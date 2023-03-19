using UnityEngine;
using VentiCola.UI.Bindings;

namespace VentiCola.UI
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    internal sealed class UIManagerUpdater : MonoBehaviour
    {
        private void Start()
        {
            gameObject.hideFlags = HideFlags.NotEditable;
            DontDestroyOnLoad(gameObject);
        }

        private void LateUpdate()
        {
            //StructureBindingBase.UpdateAllDirtyBindings();
            //BindingBaseCommon.UpdateAllDirtyBindings();
        }
    }
}