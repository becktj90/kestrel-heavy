// Engine-free plan for the shockwave ring and the heat haze that sit on top of explosions, fireballs and the plume.
// Quality gated: Low draws nothing, Medium one ring and two haze cards, High two rings and four cards.
// Photosensitivity: alpha is low and falls monotonically (a ring is a single soft expanding edge, never a flash),
// and reduce-motion draws nothing. Scaling is honest: blast radius follows energy^(1/3).
using System;

namespace Kestrel.Kit
{
    public struct ShockwaveEvent
    {
        public float X, Y, Z;
        public float Energy01;      // 0 small .. 1 full vehicle
        public float Radius;        // source radius (m)
        public bool Grounded;       // ring lies on the deck/ground; otherwise a camera-facing air ring
        public bool Reduce;
    }

    public struct HazeEvent
    {
        public float X, Y, Z;
        public float Intensity01;   // 0 faint .. 1 fireball
        public float Seconds;       // how long the haze lives
        public bool Reduce;
    }

    public static partial class CinematicEvents
    {
        public static event Action<ShockwaveEvent> ShockwaveRaised;
        public static event Action<HazeEvent> HazeRaised;

        /// <summary>Ring expanding from an explosion or an ignition overpressure. Energy scales its size and strength.</summary>
        public static void Shockwave(float x, float y, float z, float energy01, bool grounded, bool reduce, float radius = 2f)
        {
            var h = ShockwaveRaised;
            if (h != null) h(new ShockwaveEvent { X = x, Y = y, Z = z, Energy01 = energy01, Radius = radius, Grounded = grounded, Reduce = reduce });
        }

        /// <summary>Heat shimmer over a fireball or the plume.</summary>
        public static void HeatHaze(float x, float y, float z, float intensity01, float seconds, bool reduce)
        {
            var h = HazeRaised;
            if (h != null) h(new HazeEvent { X = x, Y = y, Z = z, Intensity01 = intensity01, Seconds = seconds, Reduce = reduce });
        }

        public static void ClearShockwaveSubscribers() { ShockwaveRaised = null; HazeRaised = null; }
    }

    public static class ShockwaveBudgets
    {
        public static int Rings(int quality) => quality <= 0 ? 0 : quality == 1 ? 1 : 2;
        public static int HazeCards(int quality) => quality <= 0 ? 0 : quality == 1 ? 2 : 4;
        /// <summary>Ceiling on any ring or haze alpha. Low enough that an additive ring is never a flash.</summary>
        public const float MaxRingAlpha = 0.35f;
        public const float MaxHazeAlpha = 0.14f;
    }

    public struct ShockwavePlan
    {
        public bool Draw;
        public float MaxRadius, Duration, PeakAlpha, Thickness01;   // thickness as a fraction of the radius
        public Rgb3 Color;
        public bool Grounded;

        /// <summary>Radius at age t: Sedov-like r ~ t^0.4, reaching MaxRadius at Duration.</summary>
        public float RadiusAt(float t)
        {
            if (!(t > 0f) || Duration <= 0f) return 0f;
            float u = t / Duration; if (u > 1f) u = 1f;
            return MaxRadius * (float)Math.Pow(u, 0.4);
        }

        /// <summary>Alpha at age t: instant start at PeakAlpha is avoided by a 60 ms ramp, then a monotone fall.</summary>
        public float AlphaAt(float t)
        {
            if (!(t > 0f) || t >= Duration) return 0f;
            float ramp = Math.Min(1f, t / 0.06f);
            float fall = (float)Math.Pow(1f - t / Duration, 1.5);
            return PeakAlpha * ramp * fall;
        }

        public static ShockwavePlan Build(in ShockwaveEvent e, int quality, TimeOfDayPreset tod)
        {
            var p = new ShockwavePlan { Grounded = e.Grounded };
            if (e.Reduce || ShockwaveBudgets.Rings(quality) == 0) return p;
            float energy = e.Energy01 < 0f ? 0f : e.Energy01 > 1f ? 1f : e.Energy01;
            if (float.IsNaN(energy) || energy < 0.05f) return p;
            float night = tod != null ? Math.Max(0f, Math.Min(1f, tod.NightFactor)) : 0f;
            p.Draw = true;
            // Blast radius scales with the cube root of energy: 8x energy doubles the radius.
            p.MaxRadius = Math.Max(1f, e.Radius) * 14f * (float)Math.Pow(energy, 1.0 / 3.0);
            p.Duration = 0.9f + 0.9f * energy;
            p.PeakAlpha = Math.Min(ShockwaveBudgets.MaxRingAlpha, 0.12f + 0.23f * energy) * (1f - 0.3f * night);
            p.Thickness01 = 0.08f + 0.06f * energy;
            p.Color = Rgb3.Lerp(new Rgb3(0.92f, 0.9f, 0.86f), new Rgb3(1f, 0.72f, 0.46f), night);
            return p;
        }

        public static float HazeAlpha(in HazeEvent h, int quality, TimeOfDayPreset tod)
        {
            if (h.Reduce || ShockwaveBudgets.HazeCards(quality) == 0 || !(h.Intensity01 > 0f)) return 0f;
            float night = tod != null ? Math.Max(0f, Math.Min(1f, tod.NightFactor)) : 0f;
            float i = Math.Min(1f, h.Intensity01);
            // Haze reads against a bright sky and nearly vanishes in the dark.
            return Math.Min(ShockwaveBudgets.MaxHazeAlpha, 0.03f + 0.11f * i) * (1f - 0.6f * night);
        }
    }
}
