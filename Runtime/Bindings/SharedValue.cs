using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace VentiCola.UI.Bindings
{
    [Serializable]
    public sealed class SharedValue<T> where T : struct
    {
        [SerializeField]
        [FormerlySerializedAs("m_Value")]
        public T Value;
    }
}