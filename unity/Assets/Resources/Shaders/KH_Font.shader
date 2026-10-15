// Unlit cutout for Unity dynamic font atlases. The hangar used to draw labels with a
// runtime TextMeshPro atlas; on device that atlas filled in as a solid bar, so the wordmark
// was a white slab and the buttons had no words. This pass reads the OS font texture.
// Alpha8, straight alpha, and red-channel coverage all show up as glyphs rather than a quad.
Shader "Kestrel/Font"
{
    Properties
    {
        _BaseMap ("Font", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
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
                // (1,1,1,a) and (a,a,a,a) keep coverage in alpha. (a,a,a,1) keeps it in rgb.
                // (0,0,0,a) must not multiply by rgb or the glyph disappears.
                half rgb = max(samp.r, max(samp.g, samp.b));
                half coverage = samp.a < 0.999h ? samp.a : rgb;
                return half4(i.color.rgb, i.color.a * coverage);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
