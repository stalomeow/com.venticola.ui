using System;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VentiCola.UI.Bindings
{
    public static class UGUIButtonBindingBuilder
    {
        #region OnClick

        private static readonly Func<Button, UnityEvent> s_OnClickGetter = (Button self) => self.onClick;

        public static Button onClick(this Button self, UnityAction handler)
        {
            BindingUtility.BindComponentEvent(self.gameObject, s_OnClickGetter, handler);
            return self;
        }

        #endregion
    }
}
