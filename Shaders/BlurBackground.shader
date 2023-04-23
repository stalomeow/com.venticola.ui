Shader "VentiCola/UI/Blur Background"
{
    Properties
    {
        [PerRendererData] _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ VENTI_COLA_ENABLE_UI_BLUR

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Includes/DeclareUIBlurTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
                float4 positionNDC : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                // NOTE: float4 ComputeScreenPos(float4 positionCS) is deprecated
                VertexPositionInputs inputs = GetVertexPositionInputs(v.positionOS.xyz);

                o.positionCS = inputs.positionCS;
                o.color = v.color;
                o.uv = v.uv;
                o.positionNDC = inputs.positionNDC;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;

#if VENTI_COLA_ENABLE_UI_BLUR
                half2 screenUV = i.positionNDC.xy / i.positionNDC.w;
                half4 blur = SampleUIBlurColor(screenUV);
                return half4(lerp(blur.rgb, tex.rgb, tex.a), 1);
#else
                return tex;
#endif
            }
            ENDHLSL
        }
    }

    Fallback "UI/Default"
}