#if PACKAGE_URP
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VentiCola.UI.Effects
{
    [DisallowMultipleRendererFeature]
    public class UIBackgroundBlurFeature : ScriptableRendererFeature
    {
        public enum DownsamplingType
        {
            None = 0,
            _2x = 2,
            _4x = 4,
            _8x = 8,
            _16x = 16
        }

        // Precomputed shader ids to save some CPU cycles (mostly affects mobile)
        private static class ShaderConstants
        {
            public static readonly int _TempBlurTex = Shader.PropertyToID("_TempBlurTex");
            public static readonly int _GaussianBlurSize = Shader.PropertyToID("_GaussianBlurSize");
        }

        private class CustomRenderPass : ScriptableRenderPass
        {
            private readonly ProfilingSampler m_ProfilingSampler = new("Blur And Render UI");
            private readonly List<ShaderTagId> m_ShaderTagIdList = new()
            {
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly")
            };

            public BlurAlgorithm AlgorithmOfBlur;
            public DownsamplingType Downsampling;
            public FilterMode TexFilterMode;
            public RenderTargetHandle Destination;
            public int Iterations;
            public float Spread;
            public Material BlurMaterial;
            public int TopUILayerMask;

            public bool ExecuteBlur;

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor; // copy the value

                descriptor.useMipMap = false;
                descriptor.autoGenerateMips = false;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = 1;
                descriptor.memoryless = RenderTextureMemoryless.Depth;

                if (Downsampling != DownsamplingType.None)
                {
                    descriptor.width /= (int)Downsampling;
                    descriptor.height /= (int)Downsampling;
                }

                cmd.GetTemporaryRT(Destination.id, descriptor, TexFilterMode);
                cmd.GetTemporaryRT(ShaderConstants._TempBlurTex, descriptor, TexFilterMode);
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

                if (ExecuteBlur)
                {
                    RenderTargetIdentifier cameraTarget = renderingData.cameraData.renderer.cameraColorTarget;
                    RenderTargetIdentifier blurTarget = Destination.Identifier();

                    // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
                    // Currently there's an issue which results in mismatched markers.
                    CommandBuffer cmd = CommandBufferPool.Get();

                    using (new ProfilingScope(cmd, m_ProfilingSampler))
                    {
                        // downsampling
                        cmd.SetRenderTarget(blurTarget,
                            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,     // color
                            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare); // depth
                        cmd.Blit(cameraTarget, BuiltinRenderTextureType.CurrentActive);

                        // execute blur
                        switch (AlgorithmOfBlur)
                        {
                            case BlurAlgorithm.Gaussian5x5Kernel:
                                GaussianBlur(cmd, in blurTarget);
                                break;
                        }

                        // reset the render target to cameraTarget
                        cmd.SetRenderTarget(cameraTarget,
                            RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,  // color
                            RenderBufferLoadAction.Load, RenderBufferStoreAction.Store); // depth

                        // execute before rendering top UI
                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();

                        // render top UI
                        RenderTopUI(context, ref renderingData);
                    }

                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
                else
                {
                    // render top UI directly
                    using (new ProfilingScope(null, m_ProfilingSampler))
                    {
                        RenderTopUI(context, ref renderingData);
                    }
                }
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(Destination.id);
                cmd.ReleaseTemporaryRT(ShaderConstants._TempBlurTex);

                Destination = default;
            }

            private void GaussianBlur(CommandBuffer cmd, in RenderTargetIdentifier blurTarget)
            {
                for (int i = 0; i < Iterations; i++)
                {
                    cmd.SetGlobalFloat(ShaderConstants._GaussianBlurSize, 1.0f + i * Spread);

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

            private void RenderTopUI(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var drawSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, SortingCriteria.CommonTransparent);
                var filterSettings = new FilteringSettings(RenderQueueRange.transparent, TopUILayerMask);
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
            }
        }

        public static UIBackgroundBlurFeature Instance
        {
            get => s_Instance;
        }

        private static UIBackgroundBlurFeature s_Instance;

        [Header("Blur")]
        [SerializeField] private BlurAlgorithm m_Algorithm = BlurAlgorithm.Gaussian5x5Kernel;
        [SerializeField] private DownsamplingType m_Downsampling = DownsamplingType._2x;
        [SerializeField] private FilterMode m_FilterMode = FilterMode.Bilinear;
        [SerializeField, Range(1, 10)] private int m_Iterations = 3;
        [SerializeField, Range(0.2f, 3.0f)] private float m_Spread = 0.6f;
        [SerializeField] private Shader m_Shader;

        [Header("Rendering")]
        [SerializeField] private LayerMask m_TopUILayerMask = 0;

        [NonSerialized] private RenderTargetHandle m_UIBlurTex;
        [NonSerialized] private CustomRenderPass m_ScriptablePass;

        public bool UIChanged { get; set; } = false;

        public override void Create()
        {
            m_UIBlurTex.Init("_UIBlurTex");

            m_ScriptablePass = new CustomRenderPass
            {
                // Configures where the render pass should be injected.
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
            SetPassConfigs();

            s_Instance = this;
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

            m_ScriptablePass.Downsampling = m_Downsampling;
            m_ScriptablePass.TexFilterMode = m_FilterMode;
            m_ScriptablePass.Destination = m_UIBlurTex;
            m_ScriptablePass.Iterations = m_Iterations;
            m_ScriptablePass.Spread = m_Spread;

            if (m_Shader != null)
            {
                m_ScriptablePass.BlurMaterial = CoreUtils.CreateEngineMaterial(m_Shader);
            }
            else
            {
                m_ScriptablePass.BlurMaterial = null;
            }

            m_ScriptablePass.TopUILayerMask = m_TopUILayerMask;
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.ExecuteBlur = true;// UIChanged;
            renderer.EnqueuePass(m_ScriptablePass);
            UIChanged = false;
        }
    }
}
#endif // PACKAGE_URP