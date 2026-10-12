// Vertex-colour scrub / beach / road / silhouettes. One forward pass, no textures, no shadow variants.
Shader "Kestrel/Terrain"
{
    Properties
    {
        _SunDir ("Sun Dir", Vector) = (0.3, 0.8, -0.4, 0)
        _SunColor ("Sun Color", Color) = (1.3, 1.25, 1.15, 1)
        _Ambient ("Ambient", Color) = (0.38, 0.44, 0.50, 1)
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
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _SunDir;
                float4 _SunColor;
                float4 _Ambient;
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
                // Per-pixel fog: far coast quads are kilometres wide, so a vertex factor would smear the horizon.
                real fogFactor = InitializeInputDataFog(float4(input.positionWS, 1.0), 0.0);
                col = MixFog(col, fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
