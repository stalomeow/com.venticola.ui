using System;
using UnityEngine;

namespace VentiCola.UI.Internal
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class LayerPopupAttribute : PropertyAttribute { }
}