// Pad gulls. One unlit-ish forward pass: a sky tint with a hard floor so a bird stays a
// silhouette in the haze and never a floating black speck. No texture, no shadow variant.
Shader "Kestrel/Bird"
{
    Properties
    {
        _SunDir ("Sun Dir", Vector) = (0.3, 0.8, -0.4, 0)
        _SunColor ("Sun Color", Color) = (1.3, 1.25, 1.15, 1)
        _FogColor ("Fog", Color) = (0.62, 0.76, 0.88, 1)
        _FogRange ("start end on", Vector) = (5000, 150000, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _SunDir;
                float4 _SunColor;
                float4 _FogColor;
                float4 _FogRange;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.color = input.color;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Presence rides in alpha. A stable world dither dissolves a bird as it leaves
                // instead of popping, and a hidden bird contributes no fragments.
                float dither = frac(sin(dot(input.positionWS.xz, float2(12.9898, 78.233))) * 43758.5453);
                clip(input.color.a - dither * 0.98);

                half3 n = normalize(input.normalWS);
                half3 sun = normalize(_SunDir.xyz);
                // Wrap so the underside of a wing stays in the sky family. ndotl alone went to black.
                half wrap = saturate(dot(n, sun) * 0.25 + 0.82);
                half3 sky = max(_FogColor.rgb, half3(0.48, 0.60, 0.72));
                // Backlit gull: darker than the air behind it, still that air's colour. Not a black dot.
                // A little of the sun keeps the lit side warm without blowing the silhouette out.
                half3 lit = sky * input.color.rgb * lerp(0.70, 0.92, wrap);
                lit *= lerp(half3(1, 1, 1), _SunColor.rgb, 0.08);
                float dist = length(input.positionWS - _WorldSpaceCameraPos.xyz);
                float span = max(_FogRange.y - _FogRange.x, 0.001);
                float w = saturate((dist - _FogRange.x) / span) * _FogRange.z;
                float nearHaze = saturate(dist / 1600.0);
                half3 col = lerp(lit, sky, saturate(w + nearHaze * 0.5));
                col = max(col, sky * 0.30);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
