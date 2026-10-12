// Fictional Pier 7 coastline. Wavelengths, the breakwater width and the building clusters are original
// numbers — not traced from a chart, a satellite image, or any real range. +X is downrange (east, to sea);
// +Z is north. The pad origin sits on a short breakwater. Haven is open water downrange; nothing is built there.
using System;

namespace Kestrel.Site
{
    public static class SiteHorizon
    {
        /// <summary>Same sphere the chase camera uses to keep the horizon in frame.</summary>
        public const float EarthRadius = 6371000f;
        /// <summary>Water and far land extend past the geometric horizon through a high ascent (~200 km).</summary>
        public const float WaterRadius = 1700000f;
        public const float FarClip = 1800000f;

        public static float Distance(float altitude)
        {
            double h = altitude > 0f ? altitude : 0f;
            return (float)Math.Sqrt(2.0 * EarthRadius * h + h * h);
        }

        /// <summary>Paraxial drop below the local tangent plane, metres. Matches Distance: at the horizon, drop ≈ h + h²/2R.</summary>
        public static float Drop(float x, float z)
        {
            double d2 = (double)x * x + (double)z * z;
            return (float)(d2 / (2.0 * EarthRadius));
        }
    }

    public static class Coastline
    {
        public const float ApronRadius = 78f;
        public const float BeachWidth = 140f;
        /// <summary>Detailed grid around the pad. Far strips continue the coast outside it.</summary>
        public const float NearExtent = 3600f;
        public const float RoadHalfWidth = 6.5f;
        const float PierSigma = 200f;
        const float PierReach = 1100f;

        public static readonly float[] Scrub = { 0.36f, 0.41f, 0.26f, 1f };
        public static readonly float[] ScrubPatch = { 0.22f, 0.34f, 0.15f, 1f };
        public static readonly float[] Beach = { 0.76f, 0.70f, 0.52f, 1f };
        public static readonly float[] Road = { 0.27f, 0.26f, 0.25f, 1f };
        public static readonly float[] Apron = { 0.47f, 0.46f, 0.43f, 1f };
        public static readonly float[] Building = { 0.18f, 0.20f, 0.24f, 1f };
        public static readonly float[] BuildingRoof = { 0.28f, 0.30f, 0.33f, 1f };
        public static readonly float[] Veg = { 0.16f, 0.30f, 0.13f, 1f };
        public static readonly float[] DeepWater = { 0.05f, 0.18f, 0.20f, 0f };
        public static readonly float[] ShallowWater = { 0.24f, 0.42f, 0.36f, 1f };

        /// <summary>Shore line x (metres). Land is x &lt;= ShoreX(z). Zero-mean waves plus a breakwater under the pad.</summary>
        public static float ShoreX(float z)
        {
            double km = z * 0.001;
            double wave = 4800.0 * Math.Sin(km * 0.07)
                + 1900.0 * Math.Sin(km * 0.18 + 1.1)
                + 600.0 * Math.Sin(km * 0.15 + 2.4);
            double at0 = 1900.0 * Math.Sin(1.1) + 600.0 * Math.Sin(2.4);
            double natural = -820.0 + (wave - at0);
            double pier = PierReach * Math.Exp(-(z * (double)z) / (2.0 * PierSigma * PierSigma));
            return (float)(natural + pier);
        }

        public static bool IsWater(float x, float z) => x > ShoreX(z);
        public static bool IsLand(float x, float z) => x <= ShoreX(z);
        public static bool IsApron(float x, float z)
        {
            double d = (double)x * x + (double)z * z;
            return d <= (double)ApronRadius * ApronRadius;
        }

        public static bool IsBeach(float x, float z)
        {
            if (IsWater(x, z) || IsApron(x, z)) return false;
            return ShoreX(z) - x < BeachWidth;
        }

        public static float Hash01(int ix, int iz, int seed)
        {
            uint n = (uint)(ix * 374761393 + iz * 668265263 + seed * 1442695041);
            n = (n ^ (n >> 13)) * 1274126177u;
            n ^= n >> 16;
            return (n & 0xFFFFFF) / 16777215f;
        }

        public static float ValueNoise(float x, float z, float freq, int seed)
        {
            double fx = x * (double)freq, fz = z * (double)freq;
            int x0 = (int)Math.Floor(fx), z0 = (int)Math.Floor(fz);
            float tx = (float)(fx - x0), tz = (float)(fz - z0);
            tx = tx * tx * (3f - 2f * tx); tz = tz * tz * (3f - 2f * tz);
            float a = Hash01(x0, z0, seed), b = Hash01(x0 + 1, z0, seed);
            float c = Hash01(x0, z0 + 1, seed), d = Hash01(x0 + 1, z0 + 1, seed);
            return a + (b - a) * tx + (c - a) * tz + (a - b - c + d) * tx * tz;
        }

        public static bool IsScrubPatch(float x, float z, int seed)
        {
            if (!IsLand(x, z) || IsBeach(x, z) || IsApron(x, z)) return false;
            return ValueNoise(x, z, 0.0042f, seed + 3) > 0.60f;
        }

        /// <summary>Height above the local sea plane before Earth curvature. Water is slightly below the lip.</summary>
        public static float Relief(float x, float z, int seed)
        {
            if (IsWater(x, z)) return -0.08f;
            if (IsApron(x, z))
            {
                // Stays below the deck plinth (y = 0) even after the near-grid bias.
                float d = (float)Math.Sqrt((double)x * x + (double)z * z) / ApronRadius;
                return Lerp(-0.12f, -0.06f, d * d);
            }
            float inland = ShoreX(z) - x;
            float beachT = Saturate(inland / BeachWidth);
            float dune = ValueNoise(x, z, 0.0075f, seed) - 0.5f;
            float fine = ValueNoise(x, z, 0.021f, seed + 11) - 0.5f;
            float scrub = Math.Abs(dune) * 0.85f + Math.Abs(fine) * 0.22f;
            if (beachT < 1f) return Lerp(0.04f, 0.35f, Smooth(beachT));
            return scrub;
        }

        public static float SurfaceY(float x, float z, int seed, float bias) => Relief(x, z, seed) - SiteHorizon.Drop(x, z) + bias;

        public static void WriteLandColor(float x, float z, int seed, bool road, float[] dst, int o)
        {
            float[] src = ApronColor(x, z) ? Apron : road ? Road : IsBeach(x, z) ? Beach : IsScrubPatch(x, z, seed) ? ScrubPatch : Scrub;
            dst[o] = src[0]; dst[o + 1] = src[1]; dst[o + 2] = src[2]; dst[o + 3] = 1f;
        }

        public static void WriteWaterColor(float x, float z, float[] dst, int o)
        {
            float offshore = x - ShoreX(z);
            float shore = Saturate(1f - offshore / 160f);
            dst[o] = Lerp(DeepWater[0], ShallowWater[0], shore);
            dst[o + 1] = Lerp(DeepWater[1], ShallowWater[1], shore);
            dst[o + 2] = Lerp(DeepWater[2], ShallowWater[2], shore);
            dst[o + 3] = shore;
        }

        static bool ApronColor(float x, float z) => IsApron(x, z);

        public static float Saturate(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
        public static float Smooth(float t) { t = Saturate(t); return t * t * (3f - 2f * t); }

        public static bool DeepInsideNear(float x, float z)
        {
            const float m = NearExtent - 100f;
            return x >= -m && x <= m && z >= -m && z <= m;
        }
    }
}
