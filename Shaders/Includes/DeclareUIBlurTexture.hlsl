#ifndef VENTI_COLA_DECLARE_UI_BLUR_TEXTURE_INCLUDED
#define VENTI_COLA_DECLARE_UI_BLUR_TEXTURE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_UIBlurTexture);
SAMPLER(sampler_UIBlurTexture);

float4 SampleUIBlurColor(float2 uv)
{
	return float4(SAMPLE_TEXTURE2D(_UIBlurTexture, sampler_UIBlurTexture, uv).rgb, 1);
}

#endif // VENTI_COLA_DECLARE_UI_BLUR_TEXTURE_INCLUDED