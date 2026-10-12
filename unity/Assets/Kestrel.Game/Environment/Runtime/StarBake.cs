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
                    float gain = Mathf.Clamp01((float)SkyBodies.DisplayGain(s.VMag));
                    Stamp(acc, width, height, cx, cy, col, gain);
                }
            }
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "KH_Stars",
                wrapModeU = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                anisoLevel = 0,
                hideFlags = HideFlags.HideAndDontSave,
            };
            tex.SetPixels(acc);
            tex.Apply(false, true);
            return tex;
        }

        static void Stamp(Color[] px, int w, int h, int cx, int cy, Rgb col, float gain)
        {
            if (cy < 0 || cy >= h) return;
            int x = cx % w;
            if (x < 0) x += w;
            int i = cy * w + x;
            // One texel. A brighter star wins a shared cell. The shader draws it as 1–2 pixels.
            if (px[i].a >= gain) return;
            px[i] = new Color((float)col.R * gain, (float)col.G * gain, (float)col.B * gain, gain);
        }
    }
}
