using System;
using UnityEngine;

namespace VentiCola.UI.Bindings
{
    [Serializable]
    public sealed class SharedValue<T> where T : struct
    {
        [SerializeField] private T m_Value;

        public ref T Value => ref m_Value;
    }
}