// Kestrel sky. One background pass: Rayleigh gradient, sun disc and glow, three soft cloud
// decks, distance is handled by the fog colour the CPU uploads, stars from the BSC equirect.
// Stars are 1–2 screen pixels (magnitude is brightness, not size). No motion-vector pass:
// a far-plane velocity field turns those points into radial streaks.
// Portrait-safe because every term is a view direction, not a screen-space stretch.
// Mobile: no raymarch, no cubemap, two noise octaves, one texture sample.
Shader "Kestrel/Sky"
{
    Properties
    {
        _Zenith ("Zenith", Color) = (0.25, 0.45, 0.85, 1)
        _Horizon ("Horizon", Color) = (0.75, 0.86, 0.94, 1)
        _Limb ("Limb", Color) = (0.9, 0.8, 0.65, 1)
        _Ground ("Ground", Color) = (0.32, 0.38, 0.44, 1)
        _SunColor ("Sun", Color) = (1, 0.95, 0.85, 1)
        _SunDir ("Sun dir", Vector) = (0.3, 0.8, 0.2, 1)
        _Params ("haze, stars, glow, disc", Vector) = (0.8, 0, 0.8, 1)
        _Milky ("milky, airglow, time, catalog", Vector) = (0, 0, 0, 0)
        _CloudA ("c0 c1 c2 cover", Vector) = (0.2, 0.15, 0.08, 0.3)
        _CloudH ("h0 h1 h2 camAlt", Vector) = (2500, 5500, 9000, 0)
        _Wind ("east north scale precip", Vector) = (0.4, 0.2, 0.00012, 0)
        _Eq0 ("eq x", Vector) = (1, 0, 0, 0)
        _Eq1 ("eq y", Vector) = (0, 0, 1, 0)
        _Eq2 ("eq z", Vector) = (0, 1, 0, 0)
        _GalN ("galactic north", Vector) = (0.2, 0.4, 0.9, 0)
        _StarTex ("star tex size", Vector) = (512, 256, 0, 0)
        _Stars ("Stars", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Sky"
            ZWrite Off
            Cull Off
            Fog { Mode Off }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith;
                half4 _Horizon;
                half4 _Limb;
                half4 _Ground;
                half4 _SunColor;
                float4 _SunDir;
                float4 _Params;
                float4 _Milky;
                float4 _CloudA;
                float4 _CloudH;
                float4 _Wind;
                float4 _Eq0;
                float4 _Eq1;
                float4 _Eq2;
                float4 _GalN;
                float4 _StarTex;
            CBUFFER_END

            TEXTURE2D(_Stars);
            SAMPLER(sampler_Stars);

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                // Linear, not perspective-correct. Perspective correction across the skybox cube
                // stretches points into radial streaks.
                noperspective float3 dirWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                float3 posOS = input.positionOS.xyz;
                o.positionCS = TransformObjectToHClip(posOS);
                o.dirWS = mul((float3x3)UNITY_MATRIX_M, posOS);
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                return noise(p) * 0.65 + noise(p * 2.15 + 17.2) * 0.35;
            }

            // Soft deck. tRay is the distance to the layer along the view; negative means it is behind the camera.
            float3 Deck(float3 dir, float3 col, float height, float cover, float seed)
            {
                if (cover < 0.01) return col;
                float cam = _CloudH.w;
                float dh = height - cam;
                float denom = dir.y;
                float inside = 1.0 - smoothstep(180.0, 700.0, abs(dh));
                float3 body = lerp(float3(0.62, 0.68, 0.76), float3(1.0, 0.98, 0.96), saturate(_SunDir.y * 0.5 + 0.5));
                body = lerp(body, float3(0.55, 0.58, 0.62), saturate(_Wind.w));
                if (inside > 0.02 && cover > 0.15)
                    col = lerp(col, body, inside * saturate(cover * 1.4) * 0.85);

                if (abs(denom) < 1e-3) return col;
                float t = dh / denom;
                if (t < 0.0 || t > 90000.0) return col;
                float2 xz = dir.xz * (t * _Wind.z);
                float2 scroll = _Wind.xy * _Milky.z * (0.015 + seed * 0.004);
                float n = fbm(xz + scroll + seed * 9.0);
                float edge = smoothstep(1.0 - cover, 1.0 - cover + 0.28, n);
                float fade = exp(-t * 0.000028);
                float shade = lerp(0.72, 1.0, saturate(dot(normalize(float3(dir.x, 0.35, dir.z)), _SunDir.xyz)));
                float3 cc = body * shade;
                return col + cc * edge * fade * cover * (1.0 - inside);
            }

            float3 Equatorial(float3 dir)
            {
                return float3(dot(_Eq0.xyz, dir), dot(_Eq1.xyz, dir), dot(_Eq2.xyz, dir));
            }

            // Unity's missing-shader / missing-texture colour. Never return it.
            float3 KillMagenta(float3 c)
            {
                float bad = step(0.90, c.r) * step(0.90, c.b) * step(c.g, 0.20);
                return c * (1.0 - bad);
            }

            // One catalogue texel becomes a 1–2 px star. Brightness is the baked magnitude, not the footprint.
            // The mask variable is starMask: a variable named point is reserved and compiles to a magenta sky.
            float3 StarPoint(float2 uv, float3 eq)
            {
                if (_Milky.w < 0.5)
                {
                    float3 cell = floor(eq * 220.0);
                    float h = hash21(cell.xy + cell.z * 17.0);
                    float2 q = frac(eq.xy * 220.0) - 0.5;
                    float pr = length(q / max(fwidth(eq.xy), float2(1e-4, 1e-4)));
                    if (h > 0.992 && pr < 1.25)
                        return (0.18 + 0.82 * pow(saturate(h), 6.0)) * float3(0.85, 0.92, 1.0);
                    return float3(0, 0, 0);
                }
                float2 size = max(_StarTex.xy, float2(8, 8));
                float2 g = fwidth(uv);
                // atan2 jumps by 1 across the RA cut; that derivative would smear the star into a line.
                if (max(g.x, g.y) > 0.02) return float3(0, 0, 0);
                float2 p = uv * size;
                float2 i = floor(p);
                float2 f = p - i - 0.5;
                float2 px = f / max(g * size, float2(1e-3, 1e-3));
                float r = length(px);
                float starMask = r < 0.55 ? 1.0 : saturate((1.25 - r) / 0.70);
                if (starMask <= 0.0) return float3(0, 0, 0);
                float2 cuv = (i + 0.5) / size;
                float3 sampleCol = SAMPLE_TEXTURE2D_LOD(_Stars, sampler_Stars, cuv, 0).rgb;
                return KillMagenta(sampleCol) * starMask;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.dirWS);
                float3 sunDir = normalize(_SunDir.xyz);
                float mu = dir.y;
                float u = saturate(mu);
                float k = pow(1.0 - u, 0.62);
                float3 col = lerp(_Zenith.rgb, _Horizon.rgb, k);
                float limb = exp(-u * 10.0);
                col = lerp(col, _Limb.rgb, limb * _Params.x * 0.45);
                float below = saturate(-mu);
                col = lerp(col, _Ground.rgb, smoothstep(0.0, 0.22, below));

                col = Deck(dir, col, _CloudH.x, _CloudA.x, 0.0);
                col = Deck(dir, col, _CloudH.y, _CloudA.y, 1.0);
                col = Deck(dir, col, _CloudH.z, _CloudA.z, 2.0);

                float ang2 = 2.0 * (1.0 - saturate(dot(dir, sunDir)));
                float glow = exp(-ang2 / 0.012) * _Params.z;
                float disc = exp(-ang2 / 0.00009) * _Params.w;
                col += _SunColor.rgb * glow * 0.65;
                col = lerp(col, _SunColor.rgb, saturate(disc));

                float3 eq = normalize(Equatorial(dir));
                float su = atan2(eq.y, eq.x) * (0.15915494) + 0.5;
                float sv = asin(clamp(eq.z, -1.0, 1.0)) * (0.31830988) + 0.5;
                float3 starCol = StarPoint(float2(su, sv), eq);
                float sunMask = saturate(1.0 - glow * 1.6);
                float milky = exp(-pow(dot(eq, normalize(_GalN.xyz)), 2.0) * 18.0) * _Milky.x;
                col += milky * float3(0.55, 0.62, 0.82) * sunMask;
                float ag = exp(-abs(mu) * 7.0) * _Milky.y;
                col += ag * float3(0.25, 0.85, 0.55);
                // starCol is already weighted by starMask (1–2 px). Gain rises as the sky goes dark.
                float skyLum = dot(max(col, float3(0, 0, 0)), float3(0.2126, 0.7152, 0.0722));
                float reveal = saturate(1.0 - skyLum);
                col += starCol * _Params.y * sunMask * lerp(1.35, 2.85, reveal);

                return half4(saturate(KillMagenta(col)), 1);
            }
            ENDHLSL
        }
    }

    // When the URP pass does not match, draw black. FallBack Off would otherwise be the magenta error shader.
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Pass
        {
            Name "SkyBlack"
            ZWrite Off
            Cull Off
            Fog { Mode Off }

            CGPROGRAM
            #pragma vertex vertBlack
            #pragma fragment fragBlack
            #include "UnityCG.cginc"
            struct app_black { float4 vertex : POSITION; };
            struct v2fb { float4 pos : SV_POSITION; };
            v2fb vertBlack(app_black v) { v2fb o; o.pos = UnityObjectToClipPos(v.vertex); return o; }
            fixed4 fragBlack() : SV_Target { return fixed4(0, 0, 0, 1); }
            ENDCG
        }
    }
    FallBack Off
}
