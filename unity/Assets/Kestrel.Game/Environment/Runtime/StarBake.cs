// Stamp the BSC star list into an equirectangular texture the sky shader samples once.
// Source catalogue: data/astro/bsc5p-compact.csv (Yale BSC5, NASA HEASARC, public domain). See LICENSES.md.
using System.Collections.Generic;
using Kestrel.Core.Sky;
using UnityEngine;

namespace Kestrel.Game.Environment
{
    public static class StarBake
    {
        public static Texture2D Bake(IReadOnlyList<StarPoint> stars, int width, int height)
        {
            if (width < 8) width = 8;
            if (height < 8) height = 8;
            var acc = new Color[width * height];
            if (stars != null)
            {
                for (int i = stars.Count - 1; i >= 0; i--)
                {
                    var s = stars[i];
                    SkyBodies.EquirectTexel(s.RaDeg, s.DecDeg, width, height, out int cx, out int cy);
                    var col = SkyBodies.ColorFromBv(s.BV);
                    float bright = Mathf.Clamp01((float)s.Brightness);
                    // Magnitude is a steep scale; keep dim stars visible without blowing Sirius out.
                    float gain = Mathf.Lerp(0.35f, 1f, Mathf.Pow(bright, 0.35f));
                    int radius = bright > 0.25f ? 2 : 1;
                    Stamp(acc, width, height, cx, cy, col, gain, radius);
                }
            }
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "KH_Stars",
                wrapModeU = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            tex.SetPixels(acc);
            tex.Apply(false, true);
            return tex;
        }

        static void Stamp(Color[] px, int w, int h, int cx, int cy, Rgb col, float gain, int radius)
        {
            float rr = (radius + 0.75f) * (radius + 0.75f);
            for (int dy = -radius; dy <= radius; dy++)
            {
                int y = cy + dy;
                if (y < 0 || y >= h) continue;
                for (int dx = -radius; dx <= radius; dx++)
                {
                    float fall = 1f - (dx * dx + dy * dy) / rr;
                    if (fall <= 0f) continue;
                    int x = cx + dx;
                    x %= w;
                    if (x < 0) x += w;
                    int i = y * w + x;
                    float a = gain * fall;
                    var p = px[i];
                    p.r = Mathf.Min(1f, p.r + (float)col.R * a);
                    p.g = Mathf.Min(1f, p.g + (float)col.G * a);
                    p.b = Mathf.Min(1f, p.b + (float)col.B * a);
                    p.a = 1f;
                    px[i] = p;
                }
            }
        }
    }
}
