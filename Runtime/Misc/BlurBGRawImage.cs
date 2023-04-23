using UnityEngine;
using UnityEngine.UI;
using VentiCola.UI.Rendering;

namespace VentiCola.UI.Misc
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Raw Image (Blur BG)", 12)]
    public class BlurBGRawImage : RawImage
    {
        public override Material defaultMaterial => BlurUtility.BlurBackgroundMaterial;
    }
}