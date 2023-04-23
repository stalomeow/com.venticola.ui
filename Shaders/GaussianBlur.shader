Shader "VentiCola/Effects/Gaussian Blur"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
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

        Cull Off
        ZTest Always
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            half2 uv          : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            half2 uv[5]       : TEXCOORD0;
        };

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_TexelSize;
        CBUFFER_END

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        float _GaussianBlurSize; // input from C# code

        Varyings vertVertical(Attributes i)
        {
            Varyings o = (Varyings)0;

            o.positionCS = TransformObjectToHClip(i.positionOS.xyz);

            o.uv[0] = i.uv;
            o.uv[1] = i.uv + float2(0, _MainTex_TexelSize.y * 1.0) * _GaussianBlurSize;
            o.uv[2] = i.uv - float2(0, _MainTex_TexelSize.y * 1.0) * _GaussianBlurSize;
            o.uv[3] = i.uv + float2(0, _MainTex_TexelSize.y * 2.0) * _GaussianBlurSize;
            o.uv[4] = i.uv - float2(0, _MainTex_TexelSize.y * 2.0) * _GaussianBlurSize;

            return o;
        }

        Varyings vertHorizontal(Attributes i)
        {
            Varyings o = (Varyings)0;

            o.positionCS = TransformObjectToHClip(i.positionOS.xyz);

            o.uv[0] = i.uv;
            o.uv[1] = i.uv + float2(_MainTex_TexelSize.x * 1.0, 0) * _GaussianBlurSize;
            o.uv[2] = i.uv - float2(_MainTex_TexelSize.x * 1.0, 0) * _GaussianBlurSize;
            o.uv[3] = i.uv + float2(_MainTex_TexelSize.x * 2.0, 0) * _GaussianBlurSize;
            o.uv[4] = i.uv - float2(_MainTex_TexelSize.x * 2.0, 0) * _GaussianBlurSize;

            return o;
        }

        half4 frag(Varyings i) : SV_Target
        {
            static float weight[3] = { 0.4026, 0.2442, 0.0545 };

            half3 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[0]).rgb * weight[0];

            UNITY_UNROLL
            for (int j = 1; j < 3; j++)
            {
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[j * 2 - 1]).rgb * weight[j];
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[j * 2    ]).rgb * weight[j];
            }

            return half4(color.rgb, 1);
        }
        ENDHLSL

        Pass
        {
            Name "Gaussian Blur - Vertical"

            HLSLPROGRAM
            #pragma vertex vertVertical
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            Name "Gaussian Blur - Horizontal"

            HLSLPROGRAM
            #pragma vertex vertHorizontal
            #pragma fragment frag
            ENDHLSL
        }
    }

    Fallback Off
}