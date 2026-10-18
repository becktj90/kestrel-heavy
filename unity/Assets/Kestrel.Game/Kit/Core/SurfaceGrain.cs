// Procedural surface grain for terrain and the pad. No shipped bitmaps: a 64² RGBA tile per kind,
// mean near white so it multiplies a vertex colour or a URP base colour without painting it dark.
// Original noise. Not sampled from a photograph.
using System;

namespace Kestrel.Kit
{
    public enum SurfaceKind { Concrete, Steel, Grass, Sand, Water, Bird }

    public static class SurfaceGrain
    {
        public const int Size = 64;
        public static int Bytes => Size * Size * 4;

        public static void Fill(SurfaceKind kind, byte[] rgba)
        {
            if (rgba == null || rgba.Length < Bytes) throw new ArgumentException("rgba buffer is short");
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                byte r, g, b, a;
                switch (kind)
                {
                    case SurfaceKind.Concrete: Concrete(x, y, out r, out g, out b, out a); break;
                    case SurfaceKind.Steel: Steel(x, y, out r, out g, out b, out a); break;
                    case SurfaceKind.Grass: Grass(x, y, out r, out g, out b, out a); break;
                    case SurfaceKind.Sand: Sand(x, y, out r, out g, out b, out a); break;
                    case SurfaceKind.Water: Water(x, y, out r, out g, out b, out a); break;
                    default: Bird(x, y, out r, out g, out b, out a); break;
                }
                int i = (y * Size + x) * 4;
                rgba[i] = r; rgba[i + 1] = g; rgba[i + 2] = b; rgba[i + 3] = a;
            }
        }

        /// <summary>Mean of the RGB channels, 0..1. Tests hold the multiply-grain near white.</summary>
        public static float Mean(byte[] rgba)
        {
            double s = 0; int n = 0;
            for (int i = 0; i + 3 < rgba.Length; i += 4)
            {
                if (rgba[i + 3] < 8) continue;
                s += (rgba[i] + rgba[i + 1] + rgba[i + 2]) / 3.0; n++;
            }
            return n == 0 ? 0f : (float)(s / n / 255.0);
        }

        static float Hash(int x, int y)
        {
            uint n = (uint)(x * 374761393 + y * 668265263) ^ 0x9E3779B9u;
            n = (n ^ (n >> 13)) * 1274126177u;
            return (n & 65535u) / 65535f;
        }

        static float Smooth(int x, int y)
        {
            float fx = x / 4f, fy = y / 4f;
            int x0 = (int)Math.Floor(fx), y0 = (int)Math.Floor(fy);
            float tx = fx - x0, ty = fy - y0;
            tx = tx * tx * (3f - 2f * tx); ty = ty * ty * (3f - 2f * ty);
            float a = Hash(x0, y0), b = Hash(x0 + 1, y0), c = Hash(x0, y0 + 1), d = Hash(x0 + 1, y0 + 1);
            return a + (b - a) * tx + (c - a) * ty + (a - b - c + d) * tx * ty;
        }

        static byte U(float v)
        {
            if (v < 0f) v = 0f; if (v > 1f) v = 1f;
            return (byte)(v * 255f + 0.5f);
        }

        static void Concrete(int x, int y, out byte r, out byte g, out byte b, out byte a)
        {
            bool seam = (x % 32) < 2 || (y % 32) < 2;
            float n = Smooth(x, y);
            float v = seam ? 0.72f + 0.06f * n : 0.92f + 0.16f * (n - 0.5f);
            float speck = Hash(x * 3, y * 5) > 0.92f ? 0.08f : 0f;
            r = U(v - speck); g = U(v - speck * 0.5f); b = U(v - 0.02f); a = 255;
        }

        static void Steel(int x, int y, out byte r, out byte g, out byte b, out byte a)
        {
            float brush = 0.5f + 0.5f * (float)Math.Sin(y * 0.85f + Smooth(x, y) * 2f);
            float v = 0.88f + 0.14f * (brush - 0.5f) + 0.04f * (Hash(x, y) - 0.5f);
            r = U(v); g = U(v + 0.01f); b = U(v + 0.025f); a = 255;
        }

        static void Grass(int x, int y, out byte r, out byte g, out byte b, out byte a)
        {
            float n = Smooth(x, y);
            float blade = Hash(x, y) > 0.82f ? 0.08f : 0f;
            r = U(0.78f + 0.2f * n); g = U(0.92f + 0.16f * (n - 0.3f) + blade); b = U(0.72f + 0.1f * n); a = 255;
        }

        static void Sand(int x, int y, out byte r, out byte g, out byte b, out byte a)
        {
            float n = Smooth(x * 2, y * 2);
            float v = 0.9f + 0.16f * (n - 0.5f);
            r = U(v + 0.04f); g = U(v); b = U(v - 0.06f); a = 255;
        }

        static void Water(int x, int y, out byte r, out byte g, out byte b, out byte a)
        {
            float rip = 0.5f + 0.5f * (float)Math.Sin((x + Smooth(x, y) * 6f) * 0.45f);
            float v = 0.86f + 0.2f * (rip - 0.5f);
            r = U(v - 0.04f); g = U(v); b = U(v + 0.06f); a = 255;
        }

        /// <summary>
        /// The old web bird: a thin amber ring and a dark M wing with an amber edge.
        /// Corners stay clear so a billboard does not draw a box.
        /// </summary>
        static void Bird(int x, int y, out byte r, out byte g, out byte b, out byte a)
        {
            float u = (x + 0.5f) / Size * 2f - 1f;
            float v = (y + 0.5f) / Size * 2f - 1f;
            float rad = (float)Math.Sqrt(u * u + v * v);
            float ring = Math.Abs(rad - 0.72f);
            bool amberRing = ring < 0.045f && rad < 0.9f;
            // M: two arches. Screen y is down in the texture; the wing rises toward the middle.
            float wing = WingY(u);
            float dw = Math.Abs(v - wing);
            bool amberWing = dw < 0.11f && Math.Abs(u) < 0.86f && wing > -1f;
            bool darkWing = dw < 0.055f && Math.Abs(u) < 0.84f && wing > -1f;
            if (darkWing) { r = 30; g = 36; b = 48; a = 255; }
            else if (amberWing || amberRing) { r = 255; g = 207; b = 93; a = amberRing ? (byte)180 : (byte)230; }
            else { r = 0; g = 0; b = 0; a = 0; }
        }

        /// <summary>Wing height in texture space (-1..1, +1 up). -2 when this column has no wing.</summary>
        public static float WingY(float u)
        {
            if (u < -0.82f || u > 0.82f) return -2f;
            // Two arches, wrists high and the body low: the web game's M wing.
            float t = Math.Abs(u) / 0.82f;
            float arch = 1f - (2f * t - 1f) * (2f * t - 1f);
            return -0.05f + arch * 0.55f * (0.35f + 0.65f * (1f - t));
        }
    }
}
