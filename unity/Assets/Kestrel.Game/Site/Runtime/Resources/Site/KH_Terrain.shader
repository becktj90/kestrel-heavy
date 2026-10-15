// Vertex-colour scrub / beach / road / silhouettes. One forward pass, no textures, no shadow variants.
Shader "Kestrel/Terrain"
{
    Properties
    {
        _SunDir ("Sun Dir", Vector) = (0.3, 0.8, -0.4, 0)
        _SunColor ("Sun Color", Color) = (1.3, 1.25, 1.15, 1)
        _Ambient ("Ambient", Color) = (0.38, 0.44, 0.50, 1)
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
                float4 _Ambient;
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
                half3 n = normalize(input.normalWS);
                half ndotl = saturate(dot(n, normalize(_SunDir.xyz)));
                half3 col = input.color.rgb * (_Ambient.rgb + _SunColor.rgb * ndotl);
                // Grain and a broader blotch, so scrub, beach and apron read as a surface rather than a flat fill.
                float2 xz = input.positionWS.xz;
                float grain = frac(sin(dot(xz, float2(12.9898, 78.233))) * 43758.5453);
                float blot = frac(sin(dot(floor(xz * 0.08), float2(19.19, 47.77))) * 12543.2);
                col *= lerp(0.90, 1.08, grain) * lerp(0.94, 1.05, blot);
                // Per-pixel fog. Same ramp as SiteFog.Weight. One variant: multi_compile_fog is stripped
                // from player builds (no scene has fog at build time) and the coast then fails to draw
                // once SkyRig turns fog on.
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
