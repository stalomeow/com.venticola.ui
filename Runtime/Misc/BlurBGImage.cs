using UnityEngine;
using UnityEngine.UI;
using VentiCola.UI.Rendering;

namespace VentiCola.UI.Misc
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Image (Blur BG)", 11)]
    public class BlurBGImage : Image
    {
        public override Material defaultMaterial => BlurUtility.BlurBackgroundMaterial;
    }
}