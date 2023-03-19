using UnityEngine;
using UnityEngine.UI;

namespace VentiCola.UI.Effects
{
    public enum BlendMode
    {

    }

    [AddComponentMenu("UI/Raw Image (Blur)", 13)]
    public class BlurImage : RawImage
    {
        [SerializeField] private Color m_BlurColor = Color.white;
        [SerializeField, Range(0.0f, 1.0f)] private float m_BlendFactor = 0f;
        [SerializeField] private BlendMode m_BlendMode;
    }
}
