using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using VentiCola.UI.Internal;
using static VentiCola.UI.Rendering.BlurUtils;

namespace VentiCola.UI.Rendering
{
    [DisallowMultipleRendererFeature]
    public class AdvancedUIRenderer : ScriptableRendererFeature
    {
        private static AdvancedUIRenderer s_Instance;

        [SerializeField]
        private BlurSettings m_Blur = new();

        [SerializeField, LayerPopup]
        [FormerlySerializedAs("m_UILayer")]
        private int m_HiddenUILayer = 0;

        [SerializeField, LayerPopup]
        [FormerlySerializedAs("m_TopUILayer")]
        private int m_VisibleUILayer = 5; // Builtin Layer: "UI"

        [NonSerialized]
        private CustomRenderPass m_ScriptablePass;

        public static bool UIChanged { get; set; }

        public static BlurOption BlurOpt { get; set; }

        public static int VisibleUILayer => s_Instance.m_VisibleUILayer;

        public static int HiddenUILayer => s_Instance.m_HiddenUILayer;

        public static event Action<int> OnDidRender;

        public override void Create()
        {
            m_ScriptablePass = new CustomRenderPass
            {
                // Configures where the render pass should be injected.
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents,
                Blur = m_Blur
            };

            s_Instance = this;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (m_ScriptablePass?.BlurTexture != null)
            {
                RenderTexture.ReleaseTemporary(m_ScriptablePass.BlurTexture);
                m_ScriptablePass.BlurTexture = null;
            }
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.ShouldRerender = UIChanged;
            m_ScriptablePass.BlurOpt = BlurOpt;
            renderer.EnqueuePass(m_ScriptablePass);

            UIChanged = false;
        }

        private class CustomRenderPass : ScriptableRenderPass
        {
            private readonly ProfilingSampler m_FullRenderWithBlurProfilingSampler = new("Render UI and Blur (Full)");
            private readonly ProfilingSampler m_FastRenderWithCachedBlurTexProfilingSampler = new("Render UI and Blur (Cached)");
            private readonly ProfilingSampler m_FastRenderWithoutBlurProfilingSampler = new("Render UI");
            private readonly List<ShaderTagId> m_ShaderTagIdList = new()
            {
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly")
            };

            public BlurSettings Blur;
            public bool ShouldRerender;
            public BlurOption BlurOpt;
            public RenderTexture BlurTexture;
            private int m_FrameCountWithoutExecutingBlur;

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                if (BlurOpt == BlurOption.Disable)
                {
                    return;
                }

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

                UpdateBlurTexture(in descriptor);
                cmd.GetTemporaryRT(ShaderConstants._TempBlurTexture, descriptor, Blur.FilterMode);
            }

            private void UpdateBlurTexture(in RenderTextureDescriptor descriptor)
            {
                int? lastInstanceID = null;

                if (BlurTexture != null)
                {
                    lastInstanceID = BlurTexture.GetInstanceID();
                    RenderTexture.ReleaseTemporary(BlurTexture);
                }

                BlurTexture = RenderTexture.GetTemporary(descriptor);
                BlurTexture.filterMode = Blur.FilterMode;

                if (BlurTexture.GetInstanceID() != lastInstanceID)
                {
                    // 屏幕分辨率等设置发生变化
                    Shader.SetGlobalTexture(ShaderConstants._UIBlurTexture, BlurTexture);
                    ShouldRerender = true;

                    // Debug.LogWarning("NEW Blur RT");
                }
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

                switch (BlurOpt)
                {
                    case BlurOption.Disable:
                        DoFastRenderWithoutBlur(ref context, ref renderingData);
                        m_FrameCountWithoutExecutingBlur++;
                        break;

                    case BlurOption.FullScreenStatic when ShouldRerender:
                        goto case BlurOption.FullScreenDynamic;

                    case BlurOption.FullScreenStatic:
                        DoFastRenderWithCachedBlurTex(ref context, ref renderingData);
                        m_FrameCountWithoutExecutingBlur++; // 实际上没有执行 Blur
                        break;

                    case BlurOption.FullScreenDynamic:
                        DoFullRenderWithBlur(ref context, ref renderingData, true);
                        m_FrameCountWithoutExecutingBlur = 0;
                        break;

                    case BlurOption.TextureDynamic:
                        DoFullRenderWithBlur(ref context, ref renderingData, false);
                        m_FrameCountWithoutExecutingBlur = 0;
                        break;

                    default:
                        Debug.LogErrorFormat("Unknown blur option: {0}.", BlurOpt.ToString());
                        m_FrameCountWithoutExecutingBlur++;
                        break;
                }
            }

            private void DoFullRenderWithBlur(ref ScriptableRenderContext context, ref RenderingData renderingData, bool applyBlurToScreen)
            {
                RenderTargetIdentifier cameraTarget = renderingData.cameraData.renderer.cameraColorTarget;
                RenderTargetIdentifier blurTarget = BlurTexture;

                // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
                // Currently there's an issue which results in mismatched markers.
                CommandBuffer cmd = CommandBufferPool.Get();

                using (new ProfilingScope(cmd, m_FullRenderWithBlurProfilingSampler))
                {
                    // render hidden ui
                    RenderUI(context, ref renderingData, 1 << HiddenUILayer); // Keyword is disabled here by default

                    // downsampling
                    cmd.SetRenderTarget(blurTarget,
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,     // color
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare); // depth
                    cmd.Blit(cameraTarget, BuiltinRenderTextureType.CurrentActive);

                    // execute blur
                    switch (Blur.Algorithm)
                    {
                        case BlurAlgorithm.Gaussian:
                            GaussianBlur(cmd, in blurTarget, Blur.Iterations, Blur.Spread);
                            break;
                    }

                    // reset the render target to cameraTarget
                    cmd.SetRenderTarget(cameraTarget,
                        RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,  // color
                        RenderBufferLoadAction.Load, RenderBufferStoreAction.Store); // depth

                    if (applyBlurToScreen)
                    {
                        // render the blur layer
                        cmd.Blit(blurTarget, BuiltinRenderTextureType.CurrentActive);
                    }
                    else
                    {
                        cmd.EnableKeyword(in ShaderConstants.VENTI_COLA_ENABLE_UI_BLUR);
                    }

                    // execute before rendering top UI
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // render top UI
                    RenderUI(context, ref renderingData, 1 << VisibleUILayer);

                    if (!applyBlurToScreen)
                    {
                        cmd.DisableKeyword(in ShaderConstants.VENTI_COLA_ENABLE_UI_BLUR);
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            private void DoFastRenderWithCachedBlurTex(ref ScriptableRenderContext context, ref RenderingData renderingData)
            {
                RenderTargetIdentifier blurTarget = BlurTexture;

                // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
                // Currently there's an issue which results in mismatched markers.
                CommandBuffer cmd = CommandBufferPool.Get();

                using (new ProfilingScope(cmd, m_FastRenderWithCachedBlurTexProfilingSampler))
                {
                    // render the cached blur texture instead of the UI below
                    cmd.Blit(blurTarget, BuiltinRenderTextureType.CurrentActive);

                    // execute before rendering top UI
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // render top UI
                    RenderUI(context, ref renderingData, 1 << VisibleUILayer);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            private void DoFastRenderWithoutBlur(ref ScriptableRenderContext context, ref RenderingData renderingData)
            {
                using (new ProfilingScope(null, m_FastRenderWithoutBlurProfilingSampler))
                {
                    // render top UI only
                    RenderUI(context, ref renderingData, 1 << VisibleUILayer);
                }
            }

            private void RenderUI(ScriptableRenderContext context, ref RenderingData renderingData, int layerMask)
            {
                var drawSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, SortingCriteria.CommonTransparent);
                var filterSettings = new FilteringSettings(RenderQueueRange.transparent, layerMask);
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                OnDidRender?.Invoke(m_FrameCountWithoutExecutingBlur);

                if (BlurOpt == BlurOption.Disable)
                {
                    if (BlurTexture != null)
                    {
                        RenderTexture.ReleaseTemporary(BlurTexture);
                        BlurTexture = null;
                    }

                    return;
                }

                cmd.ReleaseTemporaryRT(ShaderConstants._TempBlurTexture);
            }
        }
    }
}