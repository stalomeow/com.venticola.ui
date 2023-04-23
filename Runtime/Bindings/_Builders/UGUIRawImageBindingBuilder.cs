#if PACKAGE_UGUI
using System;
using UnityEngine;
using UnityEngine.UI;
using VentiCola.UI.Bindings.LowLevel;

namespace VentiCola.UI.Bindings
{
    public static class UGUIRawImageBindingBuilder
    {
        #region Texture

        private static readonly Func<RawImage, Texture> s_TextureGetter = (RawImage self) => self.texture;
        private static readonly Action<RawImage, Texture> s_TextureSetter = (RawImage self, Texture value) => self.texture = value;

        public static RawImage texture(this RawImage self, Func<Texture> value)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_TextureGetter, s_TextureSetter, value);
            return self;
        }

        public static RawImage texture(this RawImage self, Func<Texture> value, in TransitionConfig transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_TextureGetter, s_TextureSetter, value, in transitionConfig);
            return self;
        }

        public static RawImage texture(this RawImage self, Func<Texture> value, SharedValue<TransitionConfig> transitionConfig)
        {
            BindingUtility.BindComponentValue(self.gameObject, s_TextureGetter, s_TextureSetter, value, transitionConfig);
            return self;
        }

        #endregion
    }
}
#endif