using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VentiCola.UI.Internal;

namespace VentiCola.UI.Rendering
{
    [DisallowMultipleRendererFeature]
    public class AdvancedUIRenderer : ScriptableRendererFeature
    {
        private enum DownsamplingType
        {
            None = 0,
            _2x = 2,
            _4x = 4,
            _8x = 8,
            _16x = 16
        }

        [Serializable]
        private class BlurSettings
        {
            public BlurAlgorithm Algorithm = BlurAlgorithm.Gaussian5x5Kernel;
            public DownsamplingType Downsampling = DownsamplingType._4x;
            public FilterMode FilterMode = FilterMode.Bilinear;

            [Range(1, 10)]
            public int Iterations = 3;

            [Range(0.2f, 3.0f)]
            public float Spread = 0.5f;
        }

        private static AdvancedUIRenderer s_Instance;

        [SerializeField] private BlurSettings m_Blur = new();
        [SerializeField, LayerPopup] private int m_UILayer = 5; // Builtin Layer: "UI"
        [SerializeField, LayerPopup] private int m_TopUILayer = 0;
        [NonSerialized] private CustomRenderPass m_ScriptablePass;

        public static bool UIChanged { get; set; }

        public static bool UseBlur { get; set; }

        public static int TopLayer => s_Instance.m_TopUILayer;

        public static int NormalLayer => s_Instance.m_UILayer;

        public static event Action<int> OnDidRender;

        public override void Create()
        {
            m_ScriptablePass = new CustomRenderPass
            {
                // Configures where the render pass should be injected.
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
            SetPassConfigs();

            s_Instance = this;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            CoreUtils.Destroy(m_ScriptablePass.BlurMaterial);
            m_ScriptablePass.BlurMaterial = null;

            if (m_ScriptablePass.BlurDestination != null)
            {
                RenderTexture.ReleaseTemporary(m_ScriptablePass.BlurDestination);
                //Destroy(m_ScriptablePass.BlurDestination);
                m_ScriptablePass.BlurDestination = null;
            }
        }

        private void OnValidate()
        {
            if (m_ScriptablePass != null)
            {
                SetPassConfigs();
            }
        }

        private void SetPassConfigs()
        {
            CoreUtils.Destroy(m_ScriptablePass.BlurMaterial); // if material is not null, it will be destroyed.

            m_ScriptablePass.Blur = m_Blur;

            Shader blurShader = GetBlurShader();

            if (blurShader != null)
            {
                m_ScriptablePass.BlurMaterial = CoreUtils.CreateEngineMaterial(blurShader);
            }
            else
            {
                m_ScriptablePass.BlurMaterial = null;
            }

            m_ScriptablePass.TopUILayerMask = (1 << m_TopUILayer);
            m_ScriptablePass.UILayerMask = (1 << m_UILayer);
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (UseBlur && UIChanged)
            {
                m_ScriptablePass.ExecuteBlur = 1; // 防止 main camera 没开启
            }

            m_ScriptablePass.HasBlur = UseBlur;
            renderer.EnqueuePass(m_ScriptablePass);
            UIChanged = false;
        }

        private Shader GetBlurShader()
        {
            var shaders = UIRuntimeSettings.Instance.DefaultShaders;

            return m_Blur.Algorithm switch
            {
                BlurAlgorithm.Gaussian5x5Kernel => shaders.GaussianBlur5x5,
                BlurAlgorithm.Gaussian3x3Kernel => shaders.GaussianBlur3x3,
                BlurAlgorithm.Box => shaders.BoxBlur,
                BlurAlgorithm.Kawase => shaders.KawaseBlur,
                BlurAlgorithm.Dual => shaders.DualBlur,
                _ => null,
            };
        }

        private class CustomRenderPass : ScriptableRenderPass
        {
            // Precomputed shader ids to save some CPU cycles (mostly affects mobile)
            private static class ShaderConstants
            {
                public static readonly int _TempBlurTex = Shader.PropertyToID("_TempBlurTex");
                public static readonly int _GaussianBlurSize = Shader.PropertyToID("_GaussianBlurSize");
            }

            private readonly ProfilingSampler m_FullRenderWithBlurProfilingSampler = new("Full UI Rendering With Blur");
            private readonly List<ShaderTagId> m_ShaderTagIdList = new()
            {
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly")
            };

            public BlurSettings Blur;
            public Material BlurMaterial;
            public int TopUILayerMask;
            public int UILayerMask;

            public int ExecuteBlur;
            public bool HasBlur;

            public RenderTexture BlurDestination;
            private int m_FrameCountWithoutBlur;

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                if (ExecuteBlur == 0)
                {
                    m_FrameCountWithoutBlur++;
                    return;
                }

                m_FrameCountWithoutBlur = 0;

                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor; // copy the value

                descriptor.useMipMap = false;
                descriptor.autoGenerateMips = false;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = 1;
                descriptor.memoryless = RenderTextureMemoryless.Depth;

                if (Blur.Downsampling != DownsamplingType.None)
                {
                    descriptor.width /= (int)Blur.Downsampling;
                    descriptor.height /= (int)Blur.Downsampling;
                }

                UpdateBlurDestination(in descriptor);
                cmd.GetTemporaryRT(ShaderConstants._TempBlurTex, descriptor, Blur.FilterMode);
            }

            private void UpdateBlurDestination(in RenderTextureDescriptor descriptor)
            {
                if (BlurDestination != null)
                {
                    RenderTexture.ReleaseTemporary(BlurDestination);
                }

                BlurDestination = RenderTexture.GetTemporary(descriptor);
                BlurDestination.filterMode = Blur.FilterMode;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                // TODO: Support Blit in XR.
                // https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@12.1/manual/renderer-features/how-to-fullscreen-blit-in-xr-spi.html
                // NOTE: Do not use the `cmd.Blit` method in URP XR projects because that method has compatibility issues with the URP XR integration.
                // Using `cmd.Blit` might implicitly enable or disable XR shader keywords, which breaks XR SPI rendering.

                // https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.Blit.html
                // Often the previous content of the Blit dest does not need to be preserved.
                // In this case, it is recommended to activate the dest render target explicitly with the appropriate load and store actions using SetRenderTarget.
                // The Blit dest should then be set to BuiltinRenderTextureType.CurrentActive.

                if (ExecuteBlur > 0)
                {
                    ExecuteBlur--;
                    DoFullRenderWithBlur(ref context, ref renderingData);
                }
                else if (HasBlur)
                {
                    DoFastRenderWithBlur(ref context, ref renderingData);
                }
                else
                {
                    DoFastRenderWithoutBlur(ref context, ref renderingData);
                }
            }

            private void DoFullRenderWithBlur(ref ScriptableRenderContext context, ref RenderingData renderingData)
            {
                RenderTargetIdentifier cameraTarget = renderingData.cameraData.renderer.cameraColorTarget;
                RenderTargetIdentifier blurTarget = BlurDestination;

                // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
                // Currently there's an issue which results in mismatched markers.
                CommandBuffer cmd = CommandBufferPool.Get();

                using (new ProfilingScope(cmd, m_FullRenderWithBlurProfilingSampler))
                {
                    // first render ui
                    RenderUI(context, ref renderingData, UILayerMask);

                    // downsampling
                    cmd.SetRenderTarget(blurTarget,
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,     // color
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare); // depth
                    cmd.Blit(cameraTarget, BuiltinRenderTextureType.CurrentActive);

                    // execute blur
                    switch (Blur.Algorithm)
                    {
                        case BlurAlgorithm.Gaussian5x5Kernel:
                            GaussianBlur(cmd, in blurTarget);
                            break;
                    }

                    // reset the render target to cameraTarget
                    cmd.SetRenderTarget(cameraTarget,
                        RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,  // color
                        RenderBufferLoadAction.Load, RenderBufferStoreAction.Store); // depth

                    // render the blur layer
                    cmd.Blit(blurTarget, BuiltinRenderTextureType.CurrentActive);

                    // execute before rendering top UI
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // render top UI
                    RenderUI(context, ref renderingData, TopUILayerMask);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            private void DoFastRenderWithBlur(ref ScriptableRenderContext context, ref RenderingData renderingData)
            {
                RenderTargetIdentifier blurTarget = BlurDestination;

                // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
                // Currently there's an issue which results in mismatched markers.
                CommandBuffer cmd = CommandBufferPool.Get();

                using (new ProfilingScope(cmd, m_FullRenderWithBlurProfilingSampler))
                {
                    // render the blur layer instead of ui below
                    cmd.Blit(blurTarget, BuiltinRenderTextureType.CurrentActive);

                    // execute before rendering top UI
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // render top UI
                    RenderUI(context, ref renderingData, TopUILayerMask);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            private void DoFastRenderWithoutBlur(ref ScriptableRenderContext context, ref RenderingData renderingData)
            {
                using (new ProfilingScope(null, m_FullRenderWithBlurProfilingSampler))
                {
                    // render top UI only
                    RenderUI(context, ref renderingData, TopUILayerMask);
                }
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                OnDidRender?.Invoke(m_FrameCountWithoutBlur);

                if (ExecuteBlur == 0)
                {
                    return;
                }

                cmd.ReleaseTemporaryRT(ShaderConstants._TempBlurTex);
            }

            private void GaussianBlur(CommandBuffer cmd, in RenderTargetIdentifier blurTarget)
            {
                for (int i = 0; i < Blur.Iterations; i++)
                {
                    cmd.SetGlobalFloat(ShaderConstants._GaussianBlurSize, 1.0f + i * Blur.Spread);

                    // vertical
                    cmd.SetRenderTarget(ShaderConstants._TempBlurTex,
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,     // color
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare); // depth
                    cmd.Blit(blurTarget, BuiltinRenderTextureType.CurrentActive, BlurMaterial, 0);

                    // horizontal
                    cmd.SetRenderTarget(blurTarget,
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,     // color
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare); // depth
                    cmd.Blit(ShaderConstants._TempBlurTex, BuiltinRenderTextureType.CurrentActive, BlurMaterial, 1);
                }
            }

            private void RenderUI(ScriptableRenderContext context, ref RenderingData renderingData, int layerMask)
            {
                var drawSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, SortingCriteria.CommonTransparent);
                var filterSettings = new FilteringSettings(RenderQueueRange.transparent, layerMask);
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
            }
        }
    }
}