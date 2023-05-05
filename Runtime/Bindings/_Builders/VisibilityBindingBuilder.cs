using System;
using UnityEngine;
using VentiCola.UI.Bindings.LowLevel;

namespace VentiCola.UI.Bindings
{
    public static class VisibilityBindingBuilder
    {
        public static GameObject ShowIf(this GameObject self, Func<GameObject, bool> condition, Action renderAction = null)
        {
            BaseBinding
                .Allocate<IfBinding>()
                .Initialize(self, condition, renderAction);
            return self;
        }
    }
}