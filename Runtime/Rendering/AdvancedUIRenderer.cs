using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VentiCola.UI.Internal;
using static VentiCola.UI.Rendering.BlurUtility;

namespace VentiCola.UI.Rendering
{
    [DisallowMultipleRendererFeature]
    public class AdvancedUIRenderer : ScriptableRendererFeature
    {
        private static AdvancedUIRenderer s_Instance;

        [SerializeField] private BlurSettings m_Blur = new();
        [SerializeField, LayerPopup] private int m_UILayer = 5; // Builtin Layer: "UI"
        [SerializeField, LayerPopup] private int m_TopUILayer = 0;
        [NonSerialized] private CustomRenderPass m_ScriptablePass;

        public static bool UIChanged { get; set; }

        public static BlurOption BlurOpt { get; set; }

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
            m_ScriptablePass.UIChanged = UIChanged;
            m_ScriptablePass.BlurOpt = BlurOpt; // 防止 main camera 没开启
            renderer.EnqueuePass(m_ScriptablePass);
            UIChanged = false;
        }

        private Shader GetBlurShader()
        {
            var shaders = UIRuntimeSettings.Instance.DefaultShaders;

            return m_Blur.Algorithm switch
            {
                BlurAlgorithm.Gaussian => shaders.GaussianBlur,
                BlurAlgorithm.Box => shaders.BoxBlur,
                BlurAlgorithm.Kawase => shaders.KawaseBlur,
                BlurAlgorithm.Dual => shaders.DualBlur,
                _ => null,
            };
        }

        private class CustomRenderPass : ScriptableRenderPass
        {
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

            public bool UIChanged;
            public BlurOption BlurOpt;

            public RenderTexture BlurDestination;
            private int m_FrameCountWithoutBlur;

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

                UpdateBlurDestination(in descriptor);
                cmd.GetTemporaryRT(ShaderConstants._TempBlurTexture, descriptor, Blur.FilterMode);
            }

            private void UpdateBlurDestination(in RenderTextureDescriptor descriptor)
            {
                int? lastInstanceID = null;

                if (BlurDestination != null)
                {
                    lastInstanceID = BlurDestination.GetInstanceID();
                    RenderTexture.ReleaseTemporary(BlurDestination);
                }

                BlurDestination = RenderTexture.GetTemporary(descriptor);
                BlurDestination.filterMode = Blur.FilterMode;
                Shader.SetGlobalTexture(ShaderConstants._UIBlurTexture, BlurDestination);

                if (BlurDestination.GetInstanceID() != lastInstanceID)
                {
                    UIChanged = true; // 屏幕分辨率等设置发生变化
                    Debug.LogWarning("NEW Blur RT");
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
                        m_FrameCountWithoutBlur++;
                        break;

                    case BlurOption.FullScreenStatic when UIChanged:
                        goto case BlurOption.FullScreenDynamic;

                    case BlurOption.FullScreenStatic:
                        DoFastRenderWithCachedBlurTex(ref context, ref renderingData);
                        m_FrameCountWithoutBlur++; // 实际上没有执行 Blur
                        break;

                    case BlurOption.FullScreenDynamic:
                        DoFullRenderWithBlur(ref context, ref renderingData, true);
                        m_FrameCountWithoutBlur = 0;
                        break;

                    case BlurOption.TextureDynamic:
                        DoFullRenderWithBlur(ref context, ref renderingData, false);
                        m_FrameCountWithoutBlur = 0;
                        break;

                    default:
                        Debug.LogErrorFormat("Unknown blur option: {0}.", BlurOpt.ToString());
                        m_FrameCountWithoutBlur++;
                        break;
                }
            }

            private void DoFullRenderWithBlur(ref ScriptableRenderContext context, ref RenderingData renderingData, bool renderBlurLayer)
            {
                RenderTargetIdentifier cameraTarget = renderingData.cameraData.renderer.cameraColorTarget;
                RenderTargetIdentifier blurTarget = BlurDestination;

                // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
                // Currently there's an issue which results in mismatched markers.
                CommandBuffer cmd = CommandBufferPool.Get();

                using (new ProfilingScope(cmd, m_FullRenderWithBlurProfilingSampler))
                {
                    // first render ui
                    cmd.DisableKeyword(in ShaderConstants.VENTI_COLA_ENABLE_UI_BLUR);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    RenderUI(context, ref renderingData, UILayerMask);

                    cmd.EnableKeyword(in ShaderConstants.VENTI_COLA_ENABLE_UI_BLUR);

                    // downsampling
                    cmd.SetRenderTarget(blurTarget,
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,     // color
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare); // depth
                    cmd.Blit(cameraTarget, BuiltinRenderTextureType.CurrentActive);

                    // execute blur
                    switch (Blur.Algorithm)
                    {
                        case BlurAlgorithm.Gaussian:
                            GaussianBlur(Blur, BlurMaterial, cmd, in blurTarget);
                            break;
                    }

                    // reset the render target to cameraTarget
                    cmd.SetRenderTarget(cameraTarget,
                        RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,  // color
                        RenderBufferLoadAction.Load, RenderBufferStoreAction.Store); // depth

                    if (renderBlurLayer)
                    {
                        // render the blur layer
                        cmd.Blit(blurTarget, BuiltinRenderTextureType.CurrentActive);
                    }

                    // execute before rendering top UI
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // render top UI
                    RenderUI(context, ref renderingData, TopUILayerMask);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            private void DoFastRenderWithCachedBlurTex(ref ScriptableRenderContext context, ref RenderingData renderingData)
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
                // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
                // Currently there's an issue which results in mismatched markers.
                CommandBuffer cmd = CommandBufferPool.Get();

                using (new ProfilingScope(cmd, m_FullRenderWithBlurProfilingSampler))
                {
                    cmd.DisableKeyword(in ShaderConstants.VENTI_COLA_ENABLE_UI_BLUR);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // render top UI only
                    RenderUI(context, ref renderingData, TopUILayerMask);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            private void RenderUI(ScriptableRenderContext context, ref RenderingData renderingData, int layerMask)
            {
                var drawSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, SortingCriteria.CommonTransparent);
                var filterSettings = new FilteringSettings(RenderQueueRange.transparent, layerMask);
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                OnDidRender?.Invoke(m_FrameCountWithoutBlur);

                if (BlurOpt == BlurOption.Disable)
                {
                    if (BlurDestination != null)
                    {
                        RenderTexture.ReleaseTemporary(BlurDestination);
                        BlurDestination = null;
                    }

                    return;
                }

                cmd.ReleaseTemporaryRT(ShaderConstants._TempBlurTexture);
            }
        }
    }
}