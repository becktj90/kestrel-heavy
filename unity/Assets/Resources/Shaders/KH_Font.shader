// Unlit cutout for Unity dynamic font atlases. The hangar used to draw labels with a
// runtime TextMeshPro atlas; on device that atlas filled in as a solid bar, so the wordmark
// was a white slab and the buttons had no words. This pass reads the OS font texture.
// Coverage is read from ONE channel chosen by the C# side from the atlas format (_CoverageMode).
Shader "Kestrel/Font"
{
    Properties
    {
        _BaseMap ("Font", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        // 0 = coverage lives in alpha (Alpha8 and RGBA32 font atlases). 1 = coverage lives in red (R8 / RGB24, no alpha).
        // Set from the atlas format in MainMenuText.Fill. Never guessed per pixel.
        _CoverageMode ("Coverage channel (0 alpha, 1 red)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent+100"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }

        Pass
        {
            Name "Font"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _CoverageMode;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color * _BaseColor;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 samp = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                // One channel, chosen by the atlas format. The old per-pixel guess (alpha below 0.999, else rgb) read
                // rgb inside glyph bodies, where alpha is 1: a (0,0,0,a) atlas then drew only the antialiased edge,
                // i.e. hollow outlined grey letters.
                half cov = _CoverageMode > 0.5h ? samp.r : samp.a;
                // Small-size dilation: pull the soft edge in so 48 px bold stems read solid.
                cov = saturate((cov - 0.12h) * 1.45h);
                return half4(i.color.rgb, i.color.a * cov);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
