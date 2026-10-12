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

        /// <summary>#129-v7 plume sprite profile: opaque to r = 0.55, smooth edge to 0 at r = 0.85 (solid 0.70,
        /// 5% fringe 0.82). Keeps the edge soft but the fringe close to the solid part, so a sprite can start at the
        /// nozzle plane without a halo above it.</summary>
        public static float TightFalloff(float r)
        {
            float t = Mathf.Clamp01((r - 0.55f) / 0.30f);
            return 1f - t * t * (3f - 2f * t);
        }

        static Texture2D tight;
        public static Texture2D TightTexture => tight != null ? tight : (tight = Build("KH_TightParticle", TightFalloff));

        static Texture2D Build(string name, System.Func<float, float> f)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 0 };
            var px = new Color32[Size * Size]; float c = (Size - 1) * 0.5f;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float dx = (x - c) / c, dy = (y - c) / c;
                    px[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * f(Mathf.Sqrt(dx * dx + dy * dy))));
                }
            tex.SetPixels32(px); tex.Apply(true, true); return tex;
        }

        /// <summary>Plume (body/core/diamonds) material: PlumeMaterial with the tight sprite.</summary>
        public static Material TightPlumeMaterial(Material template)
        {
            var m = PlumeMaterial(template); if (m == null) return null;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", TightTexture);
            m.mainTexture = TightTexture; m.name = "KH_Plume (tight)";
            return m;
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
            EnsureTransparent(m, additive: false);
            ApplySoftParticles(m, false);   // #129: keyword was still on; the deck-contact fade ate the plume
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            return m;
        }

        /// <summary>Setting _Surface/_Blend from code does not run the URP material GUI, so keywords and tags are
        /// not updated. Set them explicitly so a template asset in any state renders as transparent alpha/additive.</summary>
        public static void EnsureTransparent(Material m, bool additive)
        {
            if (m == null) return;
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", additive ? 2 : 0);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON"); m.DisableKeyword("_ALPHAMODULATE_ON");
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0);
        }

        /// <summary>URP soft particles: fade where the sprite meets opaque depth (needs the camera depth texture,
        /// enabled per quality level in QualitySetup for Medium+). Off on Low: plain depth test only.</summary>
        public const float SoftNear = 0f, SoftFar = 1f;   // was 3 m: faded the body wherever it met the deck
        public static void ApplySoftParticles(Material m, bool on)
        {
            if (m == null) return;
            if (m.HasProperty("_SoftParticlesEnabled")) m.SetFloat("_SoftParticlesEnabled", on ? 1 : 0);
            if (m.HasProperty("_SoftParticlesNearFadeDistance")) m.SetFloat("_SoftParticlesNearFadeDistance", SoftNear);
            if (m.HasProperty("_SoftParticlesFarFadeDistance")) m.SetFloat("_SoftParticlesFarFadeDistance", SoftFar);
            if (on) m.EnableKeyword("_SOFTPARTICLES_ON"); else m.DisableKeyword("_SOFTPARTICLES_ON");
        }

        /// <summary>Additive core material with an HDR base colour (bloom picks it up on High/Ultra; kept low (~1.5 High, ~1.1 others)
        /// so the additive core stays a small bright zone instead of saturating to white).</summary>
        public static Material CoreMaterial(Material template, bool hdr)
        {
            var m = TightPlumeMaterial(template); if (m == null) return null;
            m.name = "KH_PlumeCore (tight, HDR alpha)";
            // #129 "blue core": additive blending ADDS the sky behind it, so the faint edges of the core read as pale
            // blue against the day sky. Alpha-blend an HDR colour instead (bloom still picks it up on High).
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            var c = new Color(1f, 0.96f, 0.85f) * (hdr ? PlumeShape.CoreHdrHigh : PlumeShape.CoreHdrLow); c.a = 1f;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); else m.color = c;
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 2;   // over the body
            EnsureTransparent(m, additive: false);
            return m;
        }

        /// <summary>Alpha-blended cloud material with soft-particle depth fade OFF: the fade was erasing the cloud
        /// where it sits on the deck (distance ~0 to the depth buffer).</summary>
        public static Material CloudMaterial(Material template)
        {
            var m = PlumeMaterial(template); if (m == null) return null;
            m.name = "KH_GroundCloud (soft)";
            if (m.HasProperty("_SoftParticlesEnabled")) m.SetFloat("_SoftParticlesEnabled", 0);
            m.DisableKeyword("_SOFTPARTICLES_ON");
            if (m.HasProperty("_CameraFadingEnabled")) m.SetFloat("_CameraFadingEnabled", 0);
            m.DisableKeyword("_FADING_ON");
            return m;
        }
    }
}
