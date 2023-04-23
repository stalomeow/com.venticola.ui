using System;
using UnityEngine;

namespace VentiCola.UI.Rendering
{
    [Serializable]
    internal class BlurSettings
    {
        public BlurAlgorithm Algorithm = BlurAlgorithm.Gaussian;
        public DownsamplingType Downsampling = DownsamplingType._4x;
        public FilterMode FilterMode = FilterMode.Bilinear;

        [Range(1, 10)]
        public int Iterations = 3;

        [Range(0.2f, 3.0f)]
        public float Spread = 0.5f;
    }
}