// Engine-free plan for a layered explosion: core flash, fireball, smoke column, sparks, embers.
// Budgets are per quality tier, the plan adapts colour, brightness and light to the launch time of day,
// and FlashLimiter / FlashEnvelope keep the flash photosensitivity-safe (no strobing, capped amplitude).
using System;

namespace Kestrel.Kit
{
    public enum ExplosionLayer { Flash = 0, Fire = 1, Smoke = 2, Sparks = 3, Embers = 4 }

    public static class ExplosionBudgets
    {
        public const int LayerCount = 5;

        // Max live particles per layer. Index: ExplosionLayer. Low / Medium / High.
        static readonly int[] LowCaps = { 1, 16, 20, 16, 8 };
        static readonly int[] MedCaps = { 2, 36, 48, 40, 24 };
        static readonly int[] HighCaps = { 3, 64, 96, 96, 64 };

        public static int Cap(int quality, ExplosionLayer layer)
        {
            var t = quality <= 0 ? LowCaps : quality == 1 ? MedCaps : HighCaps;
            return t[(int)layer];
        }

        public static int TotalParticles(int quality)
        {
            int n = 0;
            for (int i = 0; i < LayerCount; i++) n += Cap(quality, (ExplosionLayer)i);
            return n;
        }

        /// <summary>Realtime flash lights: none on Low (emissive only), one on Medium and High. Borrowed from PadRealtimeLights headroom.</summary>
        public static int Lights(int quality) => quality <= 0 ? 0 : 1;

        /// <summary>Hard ceiling on the flash light, Unity point-light units. Lower at night, where the same light reads brighter.</summary>
        public static float MaxLightIntensity(float night01) => Lerp(8f, 5f, Clamp01(night01));
        /// <summary>Cap on the flash amplitude (0..1) the limiter may grant.</summary>
        public static float FlashAmplitudeCap(float night01) => Lerp(0.85f, 0.6f, Clamp01(night01));

        // ---- size / flash bounds (photosensitivity + "driven by real sim values") ----
        /// <summary>Largest blast radius (m) any plan may use, whatever the vehicle. A 3.6 m stage reads as ~16 m of fireball.</summary>
        public const float MaxBlastRadiusM = 16f;
        /// <summary>Smallest blast radius (m), so a tiny probe still reads as a fireball.</summary>
        public const float MinBlastRadiusM = 0.5f;
        /// <summary>Ceiling on the additive flash particle alpha. Below the old 0.85 so stacked flash cards never clip to a white screen.</summary>
        public const float MaxFlashAlpha = 0.6f;
        /// <summary>Largest share of the screen height one flash card may span (the card, not the soft blob inside it).</summary>
        public const float MaxFlashScreenFraction = 0.30f;
        /// <summary>Per-particle size jitter (x1.3 in ExplosionFx.Emit) times the size-over-lifetime peak (x1.2) the flash can grow by.</summary>
        public const float FlashGrowthMax = 1.3f * 1.2f;

        /// <summary>Largest flash start size (m) that keeps the card within MaxFlashScreenFraction of the view at <paramref name="distanceM"/>.</summary>
        public static float FlashSizeCap(float distanceM, float verticalFovDeg)
        {
            if (float.IsNaN(distanceM) || distanceM < 1f) distanceM = 1f;
            if (float.IsNaN(verticalFovDeg) || verticalFovDeg < 5f) verticalFovDeg = 5f; else if (verticalFovDeg > 120f) verticalFovDeg = 120f;
            float viewHeight = 2f * distanceM * (float)Math.Tan(verticalFovDeg * 0.5f * Math.PI / 180.0);
            return viewHeight * MaxFlashScreenFraction / FlashGrowthMax;
        }

        internal static float Clamp01(float v) => float.IsNaN(v) ? 0f : v < 0f ? 0f : v > 1f ? 1f : v;
        internal static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }

    /// <summary>Allows at most 3 flashes per second (WCAG 2.3.1) by refusing a new one inside MinInterval.</summary>
    public struct FlashLimiter
    {
        public const float MaxHz = 3f;
        public const float MinInterval = 1f / MaxHz + 0.01f;
        float last;
        bool any;

        /// <summary>Amplitude granted for a flash requested at <paramref name="now"/> (seconds, real time). 0 when refused.</summary>
        public float Grant(float now, float requested, float cap)
        {
            if (float.IsNaN(now) || float.IsNaN(requested) || requested <= 0f) return 0f;
            if (any && now - last < MinInterval && now >= last) return 0f;
            any = true; last = now;
            return requested < cap ? requested : cap;
        }

        public void Reset() { any = false; last = 0f; }
    }

    /// <summary>Single-peak light envelope: slew-limited rise, hold, exponential fall. Never oscillates.</summary>
    public struct FlashEnvelope
    {
        public float Level;
        float target, hold;

        public void Trigger(float amplitude, float holdSeconds)
        {
            if (!(amplitude > 0f)) return;
            if (amplitude > target || Level <= 0.001f) target = amplitude;
            hold = holdSeconds > 0f ? holdSeconds : 0f;
        }

        /// <summary>rise: units/s toward the peak; decay: 1/s exponential fall after the hold.</summary>
        public float Step(float dt, float rise, float decay)
        {
            if (!(dt > 0f)) return Level;
            if (hold > 0f)
            {
                hold -= dt;
                float up = rise * dt;
                Level = Math.Min(target, Level + up);
            }
            else
            {
                Level *= (float)Math.Exp(-decay * dt);
                if (Level < 0.001f) { Level = 0f; target = 0f; }
            }
            return Level;
        }

        public void Reset() { Level = 0f; target = 0f; hold = 0f; }
    }

    public struct ExplosionPlan
    {
        public int Flash, Fire, Smoke, Sparks, Embers;      // particles to emit per layer (never above the tier cap)
        public float Radius;                                // blast radius, m
        public float FlashSize, FireSize, SmokeSize, SparkSize, EmberSize;
        public float FireLife, SmokeLife, SparkLife, EmberLife;
        public float FireSpeed, SparkSpeed, EmberSpeed, SmokeRise;
        public Rgb3 FlashColor, FireColor, SmokeColor, SparkColor, EmberColor;
        public float FlashAlpha, FireAlpha, SmokeAlpha;     // particle alpha / emissive scale (exposure adapted)
        public float LightIntensity, LightRange, LightHold, LightRise, LightDecay;
        public Rgb3 LightColor;
        public float Night;                                 // 0 day .. 1 night used for the plan
        public bool Calm;                                   // reduce-motion plan: no flash, no sparks, slow, steady glow

        public int Count(ExplosionLayer l)
        {
            switch (l)
            {
                case ExplosionLayer.Flash: return Flash;
                case ExplosionLayer.Fire: return Fire;
                case ExplosionLayer.Smoke: return Smoke;
                case ExplosionLayer.Sparks: return Sparks;
                default: return Embers;
            }
        }

        public int Total => Flash + Fire + Smoke + Sparks + Embers;

        static float Luma(Rgb3 c) => 0.2126f * c.R + 0.7152f * c.G + 0.0722f * c.B;
        public static float Luminance(Rgb3 c) => Luma(c);

        /// <param name="flashGrant">Amplitude from FlashLimiter.Grant; 0 means this event may not flash (fire and smoke still play).</param>
        public static ExplosionPlan Build(in CinematicEvent e, int quality, TimeOfDayPreset tod, bool reduce, float flashGrant)
        {
            reduce |= e.Reduce;
            if (quality < 0) quality = 0; else if (quality > 2) quality = 2;
            float night = tod != null ? ExplosionBudgets.Clamp01(tod.NightFactor) : 0f;
            float energy = ExplosionBudgets.Clamp01(e.Energy01);
            float kind = e.Kind == CinematicKind.EngineFailure ? (e.Overpressure ? 0.7f : 0.45f) : 1f;
            float s = ExplosionBudgets.Lerp(0.35f, 1f, energy) * kind;
            float baseRadius = float.IsNaN(e.Radius) ? 1f : e.Radius;
            float radius = Math.Max(ExplosionBudgets.MinBlastRadiusM, baseRadius) * (e.Kind == CinematicKind.EngineFailure ? 2.2f : 4.5f) * ExplosionBudgets.Lerp(0.6f, 1f, energy);
            if (radius > ExplosionBudgets.MaxBlastRadiusM) radius = ExplosionBudgets.MaxBlastRadiusM;

            var p = new ExplosionPlan { Radius = radius, Night = night, Calm = reduce };
            bool flashes = !reduce && flashGrant > 0f && (e.Kind != CinematicKind.EngineFailure || e.Overpressure);
            float amp = flashes ? Math.Min(flashGrant, ExplosionBudgets.FlashAmplitudeCap(night)) : 0f;

            p.Flash = flashes ? Scale(quality, ExplosionLayer.Flash, s, 1) : 0;
            p.Fire = Scale(quality, ExplosionLayer.Fire, s * (reduce ? 0.5f : 1f), 1);
            p.Smoke = Scale(quality, ExplosionLayer.Smoke, s * (e.Kind == CinematicKind.EngineFailure ? 1.1f : 0.9f), 1);
            p.Sparks = reduce ? 0 : Scale(quality, ExplosionLayer.Sparks, s * (e.Grounded ? 1f : 0.8f), 1);
            p.Embers = Scale(quality, ExplosionLayer.Embers, s * (reduce ? 0.25f : 1f), reduce ? 0 : 1);

            // Exposure: the same additive fire blows out at night and washes out under a high sun.
            float expo = ExplosionBudgets.Lerp(1f, 0.7f, night);
            p.FlashSize = radius * 1.6f;
            p.FireSize = radius * 0.9f;
            p.SmokeSize = radius * 1.1f;
            p.SparkSize = 0.12f + 0.1f * s;
            p.EmberSize = 0.1f + 0.08f * s;
            p.FireLife = reduce ? 1.8f : 1.1f;
            p.SmokeLife = (e.Kind == CinematicKind.EngineFailure ? 3.0f : 4.5f) * (reduce ? 0.8f : 1f);
            p.SparkLife = 0.9f;
            p.EmberLife = reduce ? 2.0f : 2.8f;
            p.FireSpeed = reduce ? radius * 0.15f : radius * 0.9f;
            p.SparkSpeed = reduce ? 0f : 14f + 14f * s;
            p.EmberSpeed = reduce ? 0.5f : 3f + 4f * s;
            p.SmokeRise = (e.Grounded ? 2.2f : 3.2f) * (reduce ? 0.5f : 1f);

            p.FlashColor = new Rgb3(1f, 0.94f, 0.8f);
            p.FireColor = new Rgb3(1f, 0.52f, 0.14f);
            p.SparkColor = new Rgb3(1f, 0.8f, 0.4f);
            p.EmberColor = new Rgb3(1f, 0.36f, 0.08f);
            p.FlashAlpha = Math.Min(ExplosionBudgets.MaxFlashAlpha, amp * expo);
            p.FireAlpha = Math.Min(1f, (reduce ? 0.55f : 0.5f + 0.5f * Math.Max(amp, 0.4f)) * expo);

            // Smoke is lit by the scene: albedo x (direct sun + ambient), plus a little fire under-glow in the dark.
            Rgb3 smoke = new Rgb3(0.2f, 0.19f, 0.18f);
            if (tod != null)
            {
                float sunLit = tod.SunIntensity * 0.5f * (1f - night);
                float r = smoke.R * (tod.SunColor.R * sunLit + tod.AmbientEquator.R * tod.AmbientIntensity);
                float g = smoke.G * (tod.SunColor.G * sunLit + tod.AmbientEquator.G * tod.AmbientIntensity);
                float b = smoke.B * (tod.SunColor.B * sunLit + tod.AmbientEquator.B * tod.AmbientIntensity);
                smoke = new Rgb3(Cl(r, 0.03f, 0.7f), Cl(g, 0.03f, 0.7f), Cl(b, 0.03f, 0.7f));
            }
            smoke = new Rgb3(Cl(smoke.R + 0.10f * night, 0f, 1f), Cl(smoke.G + 0.035f * night, 0f, 1f), Cl(smoke.B + 0.008f * night, 0f, 1f));
            p.SmokeColor = smoke;
            p.SmokeAlpha = ExplosionBudgets.Lerp(0.6f, 0.5f, night);

            // Light that throws the flash on the pad and vehicle. Boosted under daylight (the sun hides it), capped hard.
            float lightBase = flashes ? 6f * amp * ExplosionBudgets.Lerp(1.3f, 1f, night) : 0f;
            if (reduce && ExplosionBudgets.Lights(quality) > 0) lightBase = 1.2f * Math.Max(0.3f, energy);
            if (ExplosionBudgets.Lights(quality) == 0) lightBase = 0f;
            p.LightIntensity = Math.Min(lightBase * ExplosionBudgets.Lerp(0.6f, 1f, energy), ExplosionBudgets.MaxLightIntensity(night));
            p.LightRange = Math.Max(20f, radius * 12f);
            p.LightColor = new Rgb3(1f, 0.7f, 0.4f);
            p.LightHold = reduce ? 1.0f : 0.06f;
            p.LightRise = reduce ? 1.5f : 25f;       // units/s: peak reached over >= 40 ms, never a one-frame step
            p.LightDecay = reduce ? 0.9f : 4.5f;     // exponential, ~0.5 s to a fifth
            return p;
        }

        /// <summary>
        /// The same plan with the flash card shrunk so it never spans more than MaxFlashScreenFraction of the view from the camera
        /// <paramref name="distanceM"/> away. Fire and smoke keep their blast-radius size: they are the explosion, the flash is the glare.
        /// </summary>
        public ExplosionPlan LimitFlashToView(float distanceM, float verticalFovDeg)
        {
            float cap = ExplosionBudgets.FlashSizeCap(distanceM, verticalFovDeg);
            var p = this;
            if (p.FlashSize > cap) p.FlashSize = cap;
            return p;
        }

        static int Scale(int quality, ExplosionLayer layer, float s, int min)
        {
            int cap = ExplosionBudgets.Cap(quality, layer);
            if (cap <= 0) return 0;
            int n = (int)Math.Round(cap * s);
            if (n < min) n = min;
            return n > cap ? cap : n;
        }

        static float Cl(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
    }
}
