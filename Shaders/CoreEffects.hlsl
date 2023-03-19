#ifndef VENTI_COLA_UI_CORE_EFFECTS_INCLUDED
#define VENTI_COLA_UI_CORE_EFFECTS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_UIBlurTex);
SAMPLER(sampler_UIBlurTex);

float3 SampleUIBlurColor(float2 uv)
{
	return SAMPLE_TEXTURE2D(_UIBlurTex, sampler_UIBlurTex, uv);
}

#endif // VENTI_COLA_UI_CORE_EFFECTS_INCLUDED