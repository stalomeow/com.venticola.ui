using UnityEngine;

namespace VentiCola.UI.Rendering
{
    public enum BlurAlgorithm
    {
        [InspectorName("Gaussian (5x5 Kernel)")]
        Gaussian5x5Kernel,

        [InspectorName("Gaussian (3x3 Kernel)")]
        Gaussian3x3Kernel,

        [InspectorName("Box (Not Implemented)")]
        Box,

        [InspectorName("Kawase (Not Implemented)")]
        Kawase,

        [InspectorName("Dual (Not Implemented)")]
        Dual
    }
}