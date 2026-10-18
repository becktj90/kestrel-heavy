// Engine-free plan for the staging flash: a brief capped flash at stage/fairing separation and a soft ignition glow
// when the next stage lights. Reuses ExplosionPlan's FlashLimiter (3 Hz, shared with explosions through FlashGovernor),
// FlashEnvelope (single peak, slew-limited) and ExplosionBudgets caps, so a separation right next to a failure
// can never push the screen past three flashes a second.
// Time of day: the amplitude cap and light ceiling fall toward night (a point light is far more glaring in the dark).
// Reduce-motion: no separation flash at all; the ignition glow becomes a dim, slow, steady light.
using System;

namespace Kestrel.Kit
{
    public enum StagingFlashKind { Separation = 0, Ignition = 1 }

    public struct StagingFlashEvent
    {
        public StagingFlashKind Kind;
        public float X, Y, Z;       // scene metres (the separation plane or the engine bells)
        public float Intensity01;   // 0 small .. 1 large stage
        public bool Reduce;
    }

    public static partial class CinematicEvents
    {
        public static event Action<StagingFlashEvent> StagingFlashRaised;

        /// <summary>Gameplay: OnStageSeparation / OnFairingSeparation (Separation) and OnIgnition for a later stage (Ignition).</summary>
        public static void StagingFlash(StagingFlashKind kind, float x, float y, float z, float intensity01, bool reduce)
        {
            var h = StagingFlashRaised;
            if (h != null) h(new StagingFlashEvent { Kind = kind, X = x, Y = y, Z = z, Intensity01 = intensity01, Reduce = reduce });
        }

        public static void ClearStagingFlashSubscribers() { StagingFlashRaised = null; }
    }

    /// <summary>One process-wide 3 Hz flash budget shared by explosions and staging flashes.</summary>
    public static class FlashGovernor
    {
        static FlashLimiter limiter;
        public static float Grant(float now, float requested, float cap) => limiter.Grant(now, requested, cap);
        public static void Reset() { limiter.Reset(); }
    }

    public static class StagingFlashBudgets
    {
        /// <summary>Pooled glow particles. Low has no light (ExplosionBudgets.Lights) so it keeps a few particles instead.</summary>
        public static int Particles(int quality) => quality <= 0 ? 2 : quality == 1 ? 4 : 8;
        public const float SeparationShare = 0.55f;   // of the explosion light ceiling
        public const float IgnitionShare = 0.40f;
        public const float CalmIgnitionShare = 0.20f;
        public const float MinIgnitionRise = 1.2f;    // envelope units/s: an ignition glow takes >= 0.8 s to build, never a pop
    }

    public struct StagingFlashPlan
    {
        public bool Draw;
        public StagingFlashKind Kind;
        public int Particles;
        public float ParticleSize, ParticleAlpha;
        public Rgb3 Color;
        public float LightIntensity, LightRange;    // 0 when the tier has no light
        public float LightRise, LightHold, LightDecay;
        public bool Calm;

        /// <param name="flashGrant">Amplitude from FlashGovernor (0 refused). Only the separation flash needs one: the ignition glow rises too slowly to be a flash.</param>
        public static StagingFlashPlan Build(in StagingFlashEvent e, int quality, TimeOfDayPreset tod, bool reduce, float flashGrant)
        {
            var p = new StagingFlashPlan { Kind = e.Kind };
            float night = tod != null ? Math.Max(0f, Math.Min(1f, tod.NightFactor)) : 0f;
            float i = float.IsNaN(e.Intensity01) ? 0f : Math.Max(0f, Math.Min(1f, e.Intensity01));
            if (!(i > 0f)) return p;
            float ceiling = ExplosionBudgets.MaxLightIntensity(night);
            bool light = ExplosionBudgets.Lights(quality) > 0;
            if (e.Kind == StagingFlashKind.Separation)
            {
                if (reduce || !(flashGrant > 0f)) return p;
                float amp = Math.Min(flashGrant, ExplosionBudgets.FlashAmplitudeCap(night));
                p.Draw = true;
                p.Particles = StagingFlashBudgets.Particles(quality);
                p.ParticleSize = 1.2f + 2.0f * i;
                p.ParticleAlpha = amp * (0.85f - 0.25f * night);
                p.Color = Rgb3.Lerp(new Rgb3(1f, 0.96f, 0.88f), new Rgb3(1f, 0.80f, 0.55f), night);
                p.LightIntensity = light ? ceiling * StagingFlashBudgets.SeparationShare * amp * (0.4f + 0.6f * i) : 0f;
                p.LightRange = 10f + 24f * i;
                p.LightRise = 14f; p.LightHold = 0.05f; p.LightDecay = 9f;
                return p;
            }
            p.Draw = true;
            p.Calm = reduce;
            p.Particles = Math.Max(1, StagingFlashBudgets.Particles(quality) / 3);
            p.ParticleSize = 1.6f + 2.4f * i;
            p.ParticleAlpha = (reduce ? 0.18f : 0.32f) * (1f - 0.3f * night);
            p.Color = Rgb3.Lerp(new Rgb3(1f, 0.74f, 0.42f), new Rgb3(1f, 0.62f, 0.30f), night);
            float share = reduce ? StagingFlashBudgets.CalmIgnitionShare : StagingFlashBudgets.IgnitionShare;
            p.LightIntensity = light ? ceiling * share * (0.5f + 0.5f * i) : 0f;
            p.LightRange = 14f + 30f * i;
            p.LightRise = reduce ? 0.6f : StagingFlashBudgets.MinIgnitionRise; p.LightHold = reduce ? 2f : 1.2f; p.LightDecay = reduce ? 0.8f : 1.5f;
            return p;
        }
    }
}
