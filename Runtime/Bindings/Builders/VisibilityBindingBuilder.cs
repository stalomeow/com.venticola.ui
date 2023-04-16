using System;
using UnityEngine;

namespace VentiCola.UI.Bindings
{
    public static class VisibilityBindingBuilder
    {
        public static GameObject ShowIf(this GameObject self, Func<bool> condition, Action renderAction = null)
        {
            BaseBinding
                .Allocate<IfBinding>()
                .Initialize(self, condition, renderAction);
            return self;
        }
    }
}
