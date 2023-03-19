using UnityEngine;
using UnityEngine.UI;

namespace VentiCola.UI.Events
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI Events/Empty Graphic")]
    public sealed class EmptyGraphic : Graphic
    {
        public override void SetAllDirty() { }

        public override void SetLayoutDirty() { }

        public override void SetMaterialDirty() { }

        public override void SetVerticesDirty() { }

        public override void Rebuild(CanvasUpdate update) { }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}