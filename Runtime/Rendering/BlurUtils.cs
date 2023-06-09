using System;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VentiCola.UI.Rendering
{
    internal static class BlurUtils
    {
        public static class ShaderConstants
        {
            public static readonly GlobalKeyword VENTI_COLA_ENABLE_UI_BLUR = GlobalKeyword.Create("VENTI_COLA_ENABLE_UI_BLUR");

            // Precomputed shader ids to save some CPU cycles (mostly affects mobile)
            public static readonly int _UIBlurTexture = Shader.PropertyToID("_UIBlurTexture");
            public static readonly int _TempBlurTexture = Shader.PropertyToID("_TempBlurTexture");
            public static readonly int _GaussianBlurSize = Shader.PropertyToID("_GaussianBlurSize");
        }

        private static Material s_BlurBackgroundMaterial;
        private static Material s_GaussianBlurMaterial;

        public static Material BlurBackgroundMaterial
        {
            get
            {
                if (s_BlurBackgroundMaterial == null)
                {
                    Shader shader = UIRuntimeSettings.Instance.DefaultShaders.BlurBackground;
                    s_BlurBackgroundMaterial = new Material(shader);
                }

                return s_BlurBackgroundMaterial;
            }
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void ResetGlobalShaderKeywords()
        {
            // 在 Play Mode 启用的 Keyword 会一直保持启用状态（即使退出 Play Mode），需要在 Editor Reload 时手动禁用掉
            Shader.DisableKeyword(in ShaderConstants.VENTI_COLA_ENABLE_UI_BLUR);
        }
#endif

        private static Material GetOrCreateMaterial(ref Material location, BlurAlgorithm algorithm)
        {
            if (location == null)
            {
                var shaders = UIRuntimeSettings.Instance.DefaultShaders;
                Shader shader = algorithm switch
                {
                    BlurAlgorithm.Gaussian => shaders.GaussianBlur,
                    BlurAlgorithm.Box => shaders.BoxBlur,
                    BlurAlgorithm.Kawase => shaders.KawaseBlur,
                    BlurAlgorithm.Dual => shaders.DualBlur,
                    _ => throw new NotSupportedException(),
                };
                location = new Material(shader);
            }

            return location;
        }

        public static void GaussianBlur(CommandBuffer cmd, in RenderTargetIdentifier blurTarget, int iterations, float spread)
        {
            Material material = GetOrCreateMaterial(ref s_GaussianBlurMaterial, BlurAlgorithm.Gaussian);

            for (int i = 0; i < iterations; i++)
            {
                cmd.SetGlobalFloat(ShaderConstants._GaussianBlurSize, 1.0f + i * spread);

                // vertical
                cmd.SetRenderTarget(ShaderConstants._TempBlurTexture,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,     // color
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare); // depth
                cmd.Blit(blurTarget, BuiltinRenderTextureType.CurrentActive, material, 0);

                // horizontal
                cmd.SetRenderTarget(blurTarget,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,     // color
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare); // depth
                cmd.Blit(ShaderConstants._TempBlurTexture, BuiltinRenderTextureType.CurrentActive, material, 1);
            }
        }
    }
}