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
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            return m;
        }

        /// <summary>Additive core material with an HDR base colour (bloom picks it up on High/Ultra; on LDR presets
        /// additive over the plume body still saturates to white).</summary>
        public static Material CoreMaterial(Material template, bool hdr)
        {
            var m = PlumeMaterial(template); if (m == null) return null;
            m.name = "KH_PlumeCore (soft, additive HDR)";
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 2);            // URP: Additive
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            var c = new Color(1f, 0.96f, 0.85f) * (hdr ? 4f : 1.5f); c.a = 1f;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); else m.color = c;
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 2;   // over the body
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
