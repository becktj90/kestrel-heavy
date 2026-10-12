// Procedural URP material library keyed by MaterialRole. No textures shipped: colour, smoothness and metallic
// come from the table below; stripes and wear come from a tiny generated texture (64x8, ~2 KB) shared by all.
// Materials are created on first use and cached; all have GPU instancing enabled.
using System.Collections.Generic;
using UnityEngine;

namespace Kestrel.Kit
{
    public struct MaterialSpec
    {
        public Color BaseColor; public float Metallic, Smoothness; public bool Striped;
        public MaterialSpec(Color c, float m, float s, bool striped = false) { BaseColor = c; Metallic = m; Smoothness = s; Striped = striped; }
    }

    public static class MaterialLibrary
    {
        public const string LitShader = "Universal Render Pipeline/Lit";
        public const string FallbackShader = "Universal Render Pipeline/Simple Lit";

        /// <summary>Role -> look. Generic fictional pad palette (no real-facility livery).</summary>
        public static MaterialSpec Spec(MaterialRole r) => r switch
        {
            MaterialRole.PaintedSteel      => new MaterialSpec(new Color(0.78f, 0.80f, 0.82f), 0.35f, 0.45f),
            MaterialRole.Galvanized        => new MaterialSpec(new Color(0.62f, 0.64f, 0.66f), 0.90f, 0.55f),
            MaterialRole.Concrete          => new MaterialSpec(new Color(0.55f, 0.54f, 0.51f), 0.00f, 0.15f),
            MaterialRole.RubberHose        => new MaterialSpec(new Color(0.07f, 0.07f, 0.08f), 0.00f, 0.30f),
            MaterialRole.Insulation        => new MaterialSpec(new Color(0.86f, 0.84f, 0.78f), 0.00f, 0.20f),
            MaterialRole.WarningStripes    => new MaterialSpec(new Color(1f, 1f, 1f), 0.10f, 0.35f, striped: true),
            MaterialRole.SafetyYellowSteel => new MaterialSpec(new Color(0.95f, 0.76f, 0.10f), 0.30f, 0.40f),
            MaterialRole.Glass             => new MaterialSpec(new Color(0.20f, 0.28f, 0.32f), 0.00f, 0.92f),
            _ => new MaterialSpec(Color.magenta, 0, 0)
        };

        static readonly Dictionary<MaterialRole, Material> Cache = new Dictionary<MaterialRole, Material>();
        static Texture2D _stripe;

        public static Material Get(MaterialRole role)
        {
            if (Cache.TryGetValue(role, out var m) && m != null) return m;
            var sh = Shader.Find(LitShader);
            if (sh == null) sh = Shader.Find(FallbackShader);
            if (sh == null) { Debug.LogError("[Kestrel.Kit] URP Lit shader not found"); return null; }
            var s = Spec(role);
            m = new Material(sh) { name = "Kit_" + role, enableInstancing = true };
            m.SetColor("_BaseColor", s.BaseColor);
            m.SetFloat("_Metallic", s.Metallic);
            m.SetFloat("_Smoothness", s.Smoothness);
            if (s.Striped) { m.SetTexture("_BaseMap", StripeTexture()); m.SetTextureScale("_BaseMap", new Vector2(4, 1)); }
            Cache[role] = m;
            return m;
        }

        /// <summary>Black/yellow 45-degree hazard stripes, generated once (64x8 RGBA32 + mips, about 2.7 KB).</summary>
        public static Texture2D StripeTexture()
        {
            if (_stripe != null) return _stripe;
            const int W = 64, H = 8;
            _stripe = new Texture2D(W, H, TextureFormat.RGBA32, true) { name = "Kit_Stripes", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                px[y * W + x] = ((x + y) / 8) % 2 == 0 ? new Color32(20, 20, 20, 255) : new Color32(242, 194, 26, 255);
            _stripe.SetPixels32(px); _stripe.Apply(true, true);
            return _stripe;
        }

        /// <summary>Bytes of generated textures the library holds (counts toward the 8 MB environment budget).</summary>
        public static long GeneratedTextureBytes => _stripe == null ? 0 : (long)(64 * 8 * 4 * 1.34f);

        public static void ClearCache() { foreach (var m in Cache.Values) if (m != null) Object.DestroyImmediate(m); Cache.Clear(); }
    }
}
