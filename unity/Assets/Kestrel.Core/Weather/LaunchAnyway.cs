// "Launch anyway" override (DECISIONS #38): show an honest reliability penalty once, no nagging.
// Penalty is a fraction in [0, 1] from the active LCC violations. Lightning scrub cannot override.
using System;

namespace Kestrel.Core.Weather
{
    public static class LaunchAnyway
    {
        // External weights (also in WeatherTests header). Sum of active, capped at 1.
        public const double WeightSurfaceWind = 0.15;
        public const double WeightGust = 0.10;
        public const double WeightLightning = 1.0;
        public const double WeightPrecip = 0.20;
        public const double WeightCeiling = 0.15;
        public const double WeightTemperature = 0.10;
        public const double WeightShear = 0.20;

        /// <summary>True when Standard/Engineer may override a Hold (not Scrub, not KID/Relaxed).</summary>
        public static bool CanOverride(LccResult r)
            => r.commitAllowed && r.verdict == LccVerdict.Hold;

        /// <summary>
        /// Honest reliability penalty in [0, 1] for the current violations.
        /// Shown before confirm. Zero when Go or when commit is not allowed (KID/Relaxed).
        /// </summary>
        public static double ReliabilityPenalty(LccResult r)
        {
            if (!r.commitAllowed || r.verdict == LccVerdict.Go) return 0;
            double p = 0;
            if (r.surfaceWind) p += WeightSurfaceWind;
            if (r.gust) p += WeightGust;
            if (r.lightning) p += WeightLightning;
            if (r.precip) p += WeightPrecip;
            if (r.ceiling) p += WeightCeiling;
            if (r.temperature) p += WeightTemperature;
            if (r.shear) p += WeightShear;
            return Math.Min(1.0, p);
        }
    }
}
