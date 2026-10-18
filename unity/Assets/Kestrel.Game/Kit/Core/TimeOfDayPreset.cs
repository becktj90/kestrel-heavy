// Time of day per launch (engine-free). Gameplay picks the Kestrel.Core.Flight.LaunchTimeOfDay with
// LaunchTime.PickTimeOfDay(missionId, seed); this file only turns that value into a lighting preset.
// Environment/TimeOfDayRig turns the preset into sun, ambient, fog tint and the R1 sky clock.
using System;
using Kestrel.Core.Flight;

namespace Kestrel.Kit
{
    public struct Rgb3
    {
        public float R, G, B;
        public Rgb3(float r, float g, float b) { R = r; G = g; B = b; }
        public static Rgb3 Lerp(Rgb3 a, Rgb3 b, float t) => new Rgb3(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);
    }

    /// <summary>One resolved lighting setup. All colours are sRGB 0..1 display values.</summary>
    public sealed class TimeOfDayPreset
    {
        public LaunchTimeOfDay Tod;
        public float SunElevationDeg, SunAzimuthDeg;   // azimuth: degrees clockwise from north
        public Rgb3 SunColor; public float SunIntensity; public float ShadowStrength;
        public float SunTintMix;                       // how hard to pull the sky's sun colour toward SunColor (0..1)
        public Rgb3 AmbientSky, AmbientEquator, AmbientGround; public float AmbientIntensity;
        public Rgb3 FogColor; public float FogDensity;  // FogDensity 0..1: haze strength multiplier on R1's fog distances
        public float FogTintMix;                       // how hard to pull R1's fog colour toward FogColor
        public Rgb3 SkyZenith, SkyHorizon, HazeColor;  // gradient intent (R1 owns the real sky; used for fake gradients and tests)
        public float StarDensity;                      // 0 none .. 1 full catalogue
        public float Exposure;                         // camera exposure multiplier intent (1 = unchanged)
        public float MoonElevationDeg;                 // night only
        /// <summary>Local civil hour used when the sun cannot be solved (fallback only).</summary>
        public float ApproxLocalHours;

        public float NightFactor => TimeOfDayPresets.NightFactor(SunElevationDeg);
        public bool IsNight => Tod == LaunchTimeOfDay.Night;
    }

    public static class TimeOfDayPresets
    {
        public const string Arg = "-khTimeOfDay", SeedArg = "-khTodSeed";

        /// <summary>Same civil-twilight ramp as PadLighting.NightFactor / SkySun.NightFactor: 0 at ≥ +6°, 1 at ≤ −6°.</summary>
        public static float NightFactor(float sunElevationDeg)
        {
            if (float.IsNaN(sunElevationDeg)) return 0f;
            return Math.Max(0f, Math.Min(1f, (6f - sunElevationDeg) / 12f));
        }

        /// <summary>Deterministic base preset, no variety.</summary>
        public static TimeOfDayPreset Base(LaunchTimeOfDay t)
        {
            switch (t)
            {
                case LaunchTimeOfDay.Sunset: return new TimeOfDayPreset
                {
                    Tod = t, SunElevationDeg = 3f, SunAzimuthDeg = 268f, SunColor = new Rgb3(1.00f, 0.55f, 0.28f), SunIntensity = 0.85f, ShadowStrength = 0.8f, SunTintMix = 0.55f,
                    AmbientSky = new Rgb3(0.36f, 0.34f, 0.52f), AmbientEquator = new Rgb3(0.78f, 0.45f, 0.30f), AmbientGround = new Rgb3(0.20f, 0.15f, 0.16f), AmbientIntensity = 0.62f,
                    FogColor = new Rgb3(0.92f, 0.56f, 0.38f), FogDensity = 1.35f, FogTintMix = 0.6f,
                    SkyZenith = new Rgb3(0.18f, 0.22f, 0.48f), SkyHorizon = new Rgb3(1.00f, 0.52f, 0.25f), HazeColor = new Rgb3(0.95f, 0.50f, 0.32f),
                    StarDensity = 0.08f, Exposure = 1.0f, MoonElevationDeg = -20f, ApproxLocalHours = 18.6f,
                };
                case LaunchTimeOfDay.Night: return new TimeOfDayPreset
                {
                    Tod = t, SunElevationDeg = -24f, SunAzimuthDeg = 300f, SunColor = new Rgb3(0.45f, 0.55f, 0.85f), SunIntensity = 0.02f, ShadowStrength = 0.0f, SunTintMix = 0.0f,
                    AmbientSky = new Rgb3(0.05f, 0.07f, 0.16f), AmbientEquator = new Rgb3(0.06f, 0.08f, 0.16f), AmbientGround = new Rgb3(0.02f, 0.025f, 0.04f), AmbientIntensity = 0.30f,
                    FogColor = new Rgb3(0.03f, 0.04f, 0.09f), FogDensity = 0.8f, FogTintMix = 0.5f,
                    SkyZenith = new Rgb3(0.01f, 0.02f, 0.06f), SkyHorizon = new Rgb3(0.04f, 0.06f, 0.13f), HazeColor = new Rgb3(0.05f, 0.07f, 0.14f),
                    StarDensity = 1.0f, Exposure = 1.25f, MoonElevationDeg = 35f, ApproxLocalHours = 2f,
                };
                default: return new TimeOfDayPreset
                {
                    Tod = LaunchTimeOfDay.Day, SunElevationDeg = 42f, SunAzimuthDeg = 180f, SunColor = new Rgb3(1.00f, 0.96f, 0.88f), SunIntensity = 1.3f, ShadowStrength = 1.0f, SunTintMix = 0.2f,
                    AmbientSky = new Rgb3(0.45f, 0.58f, 0.85f), AmbientEquator = new Rgb3(0.70f, 0.78f, 0.88f), AmbientGround = new Rgb3(0.30f, 0.30f, 0.28f), AmbientIntensity = 1.0f,
                    FogColor = new Rgb3(0.72f, 0.80f, 0.90f), FogDensity = 1.0f, FogTintMix = 0.0f,
                    SkyZenith = new Rgb3(0.20f, 0.42f, 0.82f), SkyHorizon = new Rgb3(0.72f, 0.82f, 0.92f), HazeColor = new Rgb3(0.74f, 0.82f, 0.90f),
                    StarDensity = 0f, Exposure = 1.0f, MoonElevationDeg = -30f, ApproxLocalHours = 13f,
                };
            }
        }

        /// <summary>Preset with seeded variety (sun angles, haze, star density, tint). Same (tod, seed) → identical values.</summary>
        public static TimeOfDayPreset For(LaunchTimeOfDay t, int seed)
        {
            var p = Base(t); var r = new Rng(seed * 7919 + (int)t * 104729 + 13);
            switch (t)
            {
                case LaunchTimeOfDay.Day: p.SunElevationDeg = r.Range(25f, 62f); p.SunAzimuthDeg = r.Range(100f, 250f); p.FogDensity *= r.Range(0.8f, 1.2f); p.ApproxLocalHours = r.Range(10.5f, 15f); break;
                case LaunchTimeOfDay.Sunset:
                    p.SunElevationDeg = r.Range(0.5f, 5.5f); p.SunAzimuthDeg = r.Range(245f, 295f); p.FogDensity *= r.Range(0.85f, 1.3f);
                    p.SunColor = Rgb3.Lerp(p.SunColor, new Rgb3(1f, 0.42f, 0.2f), r.Range(0f, 0.5f)); p.StarDensity = r.Range(0.02f, 0.15f); break;
                default: p.SunElevationDeg = r.Range(-40f, -14f); p.StarDensity = r.Range(0.75f, 1f); p.MoonElevationDeg = r.Range(10f, 60f); p.SunAzimuthDeg = r.Range(0f, 360f); p.Exposure *= r.Range(0.9f, 1.1f); break;
            }
            return p;
        }

        /// <summary>Folds the 64-bit flight seed into the int the preset's small variations use. Same seed, same sky.</summary>
        public static int VarietySeed(ulong seed) => unchecked((int)(seed ^ (seed >> 32)));

        public static bool TryParse(string s, out LaunchTimeOfDay t)
        {
            t = LaunchTimeOfDay.Day; if (string.IsNullOrEmpty(s)) return false;
            switch (s.Trim().ToLowerInvariant())
            {
                case "day": case "noon": case "daytime": t = LaunchTimeOfDay.Day; return true;
                case "sunset": case "dusk": case "golden": t = LaunchTimeOfDay.Sunset; return true;
                case "night": t = LaunchTimeOfDay.Night; return true;
            }
            return false;
        }

        /// <summary>Reads -khTimeOfDay=night|sunset|day (or "-khTimeOfDay night") and optional -khTodSeed=N.</summary>
        public static bool TryParseArgs(string[] args, out LaunchTimeOfDay t, out int seed)
        {
            t = LaunchTimeOfDay.Day; seed = 1; bool found = false;
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i]; if (a == null) continue;
                if (a.StartsWith(Arg + "=", StringComparison.OrdinalIgnoreCase)) found |= TryParse(a.Substring(Arg.Length + 1), out t);
                else if (string.Equals(a, Arg, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length) found |= TryParse(args[i + 1], out t);
                else if (a.StartsWith(SeedArg + "=", StringComparison.OrdinalIgnoreCase)) int.TryParse(a.Substring(SeedArg.Length + 1), out seed);
            }
            return found;
        }

        struct Rng
        {
            uint _s; public Rng(int seed) { _s = (uint)seed * 2654435761u ^ 0x9E3779B9u; if (_s == 0) _s = 1; Next(); }
            uint Next() { _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5; return _s; }
            public float Range(float a, float b) => a + (b - a) * (Next() / 4294967296f);
        }
    }
}
