using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace VentiCola.UI.Rendering
{
    [Serializable]
    public class CameraOverrideSettings
    {
        public CameraClearFlags ClearFlags = CameraClearFlags.Nothing;
        public LayerMask CullingMask = 0;
        public bool RenderShadows = false;
        public bool PostProcessing = false;
        public AntialiasingMode Antialiasing = AntialiasingMode.None;
        public CameraOverrideOption OpaqueTexture = CameraOverrideOption.Off;
        public CameraOverrideOption DepthTexture = CameraOverrideOption.Off;

        public void CopyFrom(Camera camera, UniversalAdditionalCameraData urpData)
        {
            ClearFlags = camera.clearFlags;
            CullingMask = camera.cullingMask;
            RenderShadows = urpData.renderShadows;
            PostProcessing = urpData.renderPostProcessing;
            Antialiasing = urpData.antialiasing;
            OpaqueTexture = urpData.requiresColorOption;
            DepthTexture = urpData.requiresDepthOption;
        }

        public void ApplyTo(Camera camera, UniversalAdditionalCameraData urpData)
        {
            camera.clearFlags = ClearFlags;
            camera.cullingMask = CullingMask;
            urpData.renderShadows = RenderShadows;
            urpData.renderPostProcessing = PostProcessing;
            urpData.antialiasing = Antialiasing;
            urpData.requiresColorOption = OpaqueTexture;
            urpData.requiresDepthOption = DepthTexture;
        }
    }
}