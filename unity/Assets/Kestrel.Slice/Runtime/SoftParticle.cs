// Soft round particle sprite for the plume.
//
// Root cause of the stair-stepped plume: KH_Plume.mat (URP Particles/Unlit, created by SliceBootstrap)
// has no _BaseMap, so URP samples its default white texture and every particle is a hard-edged, fully
// opaque square quad. Hundreds of overlapping axis-aligned squares give the blocky staircase silhouette
// seen in every v2 flight shot. MSAA / render scale cannot fix it: the edges are geometry of square quads,
// and the Low preset has msaa 1 anyway. The fix is a texture-space falloff, so the edge is soft at any
// MSAA level, render scale or quality preset.
using UnityEngine;

namespace Kestrel.Slice
{
    public static class SoftParticle
    {
        public const int Size = 64;
        static Texture2D cached;

        /// <summary>Alpha at normalised radius r (0 centre, 1 edge of the quad's inscribed circle).
        /// Smooth (C1) falloff that reaches exactly 0 at r &gt;= 1, so quad corners are invisible.</summary>
        public static float Falloff(float r)
        {
            float t = Mathf.Clamp01(1f - r);
            float s = t * t * (3f - 2f * t);   // smoothstep
            return s * s;                       // denser core, long soft tail
        }

        /// <summary>Shared 64x64 RGBA texture: white RGB, alpha = Falloff(radius). Bilinear, mipmapped, clamped.</summary>
        public static Texture2D Texture
        {
            get
            {
                if (cached != null) return cached;
                var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true) { name = "KH_SoftParticle", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 0 };
                var px = new Color32[Size * Size];
                float c = (Size - 1) * 0.5f;
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        float dx = (x - c) / c, dy = (y - c) / c;
                        px[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * Falloff(Mathf.Sqrt(dx * dx + dy * dy))));
                    }
                tex.SetPixels32(px); tex.Apply(true, true);
                return cached = tex;
            }
        }

        /// <summary>Instance of the plume material with the soft sprite and straight alpha blending.
        /// Alpha (not additive) keeps the plume readable against the bright day sky instead of clipping to flat white.</summary>
        public static Material PlumeMaterial(Material template)
        {
            if (template == null) return null;
            var m = new Material(template) { name = "KH_Plume (soft)" };
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", Texture);
            m.mainTexture = Texture;
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0);            // URP: Alpha
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            // Depth-test against opaques (deck, tower). Explicit so a template edit can't turn it off.
            if (m.HasProperty("_ZTest")) m.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            ApplySoftParticles(m, QualitySettings.softParticles);
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            return m;
        }

        /// <summary>URP soft particles: fade where the sprite meets opaque depth (needs the camera depth texture,
        /// enabled per quality level in QualitySetup for Medium+). Off on Low: plain depth test only.</summary>
        public const float SoftNear = 0f, SoftFar = 3f;
        public static void ApplySoftParticles(Material m, bool on)
        {
            if (m == null) return;
            if (m.HasProperty("_SoftParticlesEnabled")) m.SetFloat("_SoftParticlesEnabled", on ? 1 : 0);
            if (m.HasProperty("_SoftParticlesNearFadeDistance")) m.SetFloat("_SoftParticlesNearFadeDistance", SoftNear);
            if (m.HasProperty("_SoftParticlesFarFadeDistance")) m.SetFloat("_SoftParticlesFarFadeDistance", SoftFar);
            if (on) m.EnableKeyword("_SOFTPARTICLES_ON"); else m.DisableKeyword("_SOFTPARTICLES_ON");
        }
    }
}
