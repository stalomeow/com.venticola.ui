using System;
using UnityEngine;
using VentiCola.UI.Internal;

namespace VentiCola.UI.Bindings.LowLevel
{
    public sealed class RootBinding : BaseBinding, IDisposable
    {
        public void Initialize(GameObject mountTarget)
        {
            BaseInitialize(ref mountTarget, setDirty: false);
        }

        public void Dispose()
        {
            DetachChildren(Range.All);
            OnDetach();
        }

        protected override void OnExecute(IAnimationUpdater animationUpdater)
        {
            throw new NotImplementedException($"The {nameof(OnExecute)}() method of {TypeUtility.GetFriendlyTypeName(GetType(), false)} should never be executed!");
        }
    }
}