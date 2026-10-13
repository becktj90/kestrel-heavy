// Pure layout for the retro Home art (Trevor's pick "mix-v2": space-age poster + cream blueprint rocket +
// mission-control console). No UnityEngine UI types, so EditMode tests can check every device size.
// All marks are fictional Kestrel Heavy marks. Generic vehicle shape, no real vehicle or agency.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kestrel.Game.Shell
{
    public static class HomeArt
    {
        // Palette (mix-v2). Mirrored in KH_Tokens.uss (--kh-home-*); HomeArtTests keep them in sync.
        public static readonly Color32 Navy = new Color32(0x1e, 0x2e, 0x48, 255);
        public static readonly Color32 NavyTop = new Color32(0x14, 0x1f, 0x34, 255);
        public static readonly Color32 SunOuter = new Color32(0xe8, 0x8c, 0x3c, 255);
        public static readonly Color32 SunInner = new Color32(0xc4, 0x48, 0x2c, 255);
        public static readonly Color32 Cream = new Color32(0xfa, 0xf0, 0xd7, 255);
        public static readonly Color32 Console = new Color32(0x3c, 0x34, 0x2e, 255);
        public static readonly Color32 Hills = new Color32(0x3c, 0x34, 0x2e, 255);

        public const int Stripes = 8;
        public const float StripeH = 0.018f, StripeGap = 0.0455f;   // fractions of the sun diameter band area (scaled by r)

        public struct Layout
        {
            public float W, H, TitleBottom, ConsoleTop;
            public Vector2 SunC; public float SunR, SunInnerR;
            public float RocketUnit, RocketBase, RocketCx;
            public float RocketTop => RocketBase - 11.3f * RocketUnit;
            public float TowerX;
        }

        /// <summary>Sun sits between the title and the console; the rocket stands on the horizon just above the
        /// console and never reaches into the title block or the buttons.</summary>
        public static Layout Compute(float W, float H, float titleBottom, float consoleTop)
        {
            var L = new Layout { W = W, H = H, TitleBottom = titleBottom, ConsoleTop = consoleTop };
            float room = Mathf.Max(1f, consoleTop - titleBottom);
            L.SunR = Mathf.Min(W * 0.43f, room * 0.48f);
            L.SunInnerR = L.SunR * 0.76f;
            L.SunC = new Vector2(W * 0.5f, titleBottom + room * 0.47f);
            L.RocketBase = consoleTop - 4f;
            // 11.3 u nose-to-base must fit under the title (+12 px gap); width 4.2 u inside the column.
            L.RocketUnit = Mathf.Max(0f, Mathf.Min((L.RocketBase - titleBottom - 12f) / 11.3f, W / 9.5f));
            L.RocketCx = W * 0.5f;
            L.TowerX = W * 0.5f - 3.6f * L.RocketUnit;
            return L;
        }

        /// <summary>Navy stripe bands across the lower sun (poster look): (y0, y1) in px.</summary>
        public static List<(float y0, float y1)> StripeBands(Layout L)
        {
            var b = new List<(float, float)>();
            float d = 2 * L.SunR;
            for (int i = 0; i < Stripes; i++)
            {
                float y0 = L.SunC.y + L.SunR * 0.18f + i * StripeGap * d;
                float y1 = y0 + StripeH * d;
                if (y0 >= L.SunC.y + L.SunR) break;
                b.Add((y0, Mathf.Min(y1, L.SunC.y + L.SunR)));
            }
            return b;
        }

        /// <summary>Polygon of the circle (c, r) between y0 and y1 (y down), sampled so stripes hug the sun edge.</summary>
        public static List<Vector2> CircleBand(Vector2 c, float r, float y0, float y1, int samples = 8)
        {
            var pts = new List<Vector2>();
            y0 = Mathf.Clamp(y0, c.y - r, c.y + r); y1 = Mathf.Clamp(y1, c.y - r, c.y + r);
            for (int i = 0; i <= samples; i++) { float y = Mathf.Lerp(y0, y1, i / (float)samples); pts.Add(new Vector2(c.x + HalfChord(c, r, y), y)); }
            for (int i = samples; i >= 0; i--) { float y = Mathf.Lerp(y0, y1, i / (float)samples); pts.Add(new Vector2(c.x - HalfChord(c, r, y), y)); }
            return pts;
        }
        public static float HalfChord(Vector2 c, float r, float y) { float dy = y - c.y; return Mathf.Sqrt(Mathf.Max(0f, r * r - dy * dy)); }

        /// <summary>Deterministic star field above the sun (fractions of W and of the title band), seed fixed.</summary>
        public static List<Vector3> Stars(int count = 48, int seed = 5)
        {
            var s = new List<Vector3>(count); uint x = (uint)seed * 2654435761u + 1;
            float Next() { x ^= x << 13; x ^= x >> 17; x ^= x << 5; return (x & 0xFFFFFF) / (float)0x1000000; }
            for (int i = 0; i < count; i++) s.Add(new Vector3(Next(), Next(), 1f + Next() * 1.5f));
            return s;
        }

        /// <summary>Paper grain: 128x128 RGBA32 bytes, cream speckle with low alpha. Generated at runtime (0 IPA bytes).</summary>
        public const int GrainSize = 128;
        public static byte[] GrainRgba(int seed = 11)
        {
            var px = new byte[GrainSize * GrainSize * 4]; uint x = (uint)seed * 747796405u + 2891336453u;
            for (int i = 0; i < GrainSize * GrainSize; i++)
            {
                x ^= x << 13; x ^= x >> 17; x ^= x << 5;
                int v = (int)(x & 0xFF);
                byte a = (byte)(v > 200 ? 46 : v < 40 ? 30 : 0);
                bool light = (x & 0x100) != 0;
                px[i * 4] = light ? (byte)250 : (byte)20; px[i * 4 + 1] = light ? (byte)240 : (byte)16; px[i * 4 + 2] = light ? (byte)215 : (byte)12; px[i * 4 + 3] = a;
            }
            return px;
        }

        /// <summary>Console button routes. Only Hangar has a screen today (the designer = AppScreen.Design).</summary>
        public static readonly (string id, string label, bool live, string a11y)[] ConsoleButtons =
        {
            ("hangar", "HANGAR", true, "Hangar. Opens the rocket designer."),
            ("missions", "MISSIONS", false, "Missions. Coming soon."),
            ("codex", "CODEX", false, "Codex. Coming soon."),
            ("settings", "SETTINGS", false, "Settings. Coming soon."),
        };
    }
}
