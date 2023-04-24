using UnityEngine;

namespace VentiCola.UI.Rendering
{
    internal enum BlurAlgorithm
    {
        [InspectorName("Gaussian")]
        Gaussian,

        [InspectorName("Box (Not Implemented)")]
        Box,

        [InspectorName("Kawase (Not Implemented)")]
        Kawase,

        [InspectorName("Dual (Not Implemented)")]
        Dual
    }
}