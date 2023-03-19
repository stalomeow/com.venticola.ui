using UnityEngine;

namespace VentiCola.UI.Effects
{
    public enum BlurAlgorithm
    {
        [InspectorName("Gaussian (5x5 Kernel)")]
        Gaussian5x5Kernel,

        [InspectorName("Gaussian (3x3 Kernel)")]
        Gaussian3x3Kernel,

        Box,

        Kawase,

        Dual
    }
}
