// Cheap opaque ocean. Long swells move the vertices; a shorter ripple lives in the normal so sun glint
// stays smooth on a coarse mesh. The glint is one soft moving highlight, capped so it cannot strobe
// (well under 3 Hz, and never a full-screen flash). Shore foam comes from vertex alpha.
Shader "Kestrel/Water"
{
    Properties
    {
        _SunDir ("Sun Dir", Vector) = (0.3, 0.8, -0.4, 0)
        _SunColor ("Sun Color", Color) = (1.3, 1.25, 1.15, 1)
        _WaveAmp ("Wave Amp", Float) = 0.32
        _GlintPower ("Glint Power", Float) = 28
        _GlintStrength ("Glint Strength", Float) = 0.45
        _GlintCap ("Glint Cap", Float) = 0.32
        _FoamColor ("Foam", Color) = (0.72, 0.78, 0.74, 1)
        _GlintColor ("Glint", Color) = (1.0, 0.96, 0.86, 1)
        _FogColor ("Fog", Color) = (0.75, 0.86, 0.94, 1)
        _FogRange ("start end on", Vector) = (5000, 150000, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
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
                float _WaveAmp;
                float _GlintPower;
                float _GlintStrength;
                float _GlintCap;
                float4 _FoamColor;
                float4 _GlintColor;
                float4 _FogColor;
                float4 _FogRange;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 stableXZ : TEXCOORD1;
                float4 color : COLOR;
            };

            float WaveHeight(float2 xz, float t)
            {
                float2 k1 = float2(0.075, 0.028);
                float2 k2 = float2(-0.041, 0.063);
                return sin(dot(xz, k1) + t * 0.45) * _WaveAmp
                    + sin(dot(xz, k2) + t * 0.28) * _WaveAmp * 0.45;
            }

            float3 WaveNormal(float2 xz, float t)
            {
                float2 k1 = float2(0.075, 0.028);
                float2 k2 = float2(-0.041, 0.063);
                float2 k3 = float2(0.11, -0.07);
                float2 g = k1 * cos(dot(xz, k1) + t * 0.45) * _WaveAmp;
                g += k2 * cos(dot(xz, k2) + t * 0.28) * _WaveAmp * 0.45;
                g += k3 * cos(dot(xz, k3) + t * 0.55) * 0.06;
                return normalize(float3(-g.x, 1.0, -g.y));
            }

            Varyings vert(Attributes input)
            {
                Varyings o;
                float t = _Time.y;
                float3 pos = input.positionOS.xyz;
                pos.y += WaveHeight(pos.xz, t);
                VertexPositionInputs p = GetVertexPositionInputs(pos);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.stableXZ = input.positionOS.xz;
                o.color = input.color;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y;
                float3 n = WaveNormal(input.stableXZ, t);
                float3 v = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);
                float3 l = normalize(_SunDir.xyz);
                float3 h = normalize(v + l);
                float spec = pow(saturate(dot(n, h)), _GlintPower) * _GlintStrength;
                spec = min(spec, _GlintCap);
                float shore = saturate(input.color.a);
                float foam = smoothstep(0.55, 0.95, shore);
                half3 col = input.color.rgb;
                col = lerp(col, _FoamColor.rgb, foam * 0.55);
                col += _GlintColor.rgb * spec * _SunColor.rgb;
                // Same ramp as SiteFog.Weight / KH_Terrain. One variant so device fog cannot strip the pass.
                float dist = length(input.positionWS - _WorldSpaceCameraPos.xyz);
                float span = max(_FogRange.y - _FogRange.x, 0.001);
                float w = saturate((dist - _FogRange.x) / span) * _FogRange.z;
                col = lerp(col, _FogColor.rgb, w);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
