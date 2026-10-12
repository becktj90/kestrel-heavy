// "Launch anyway" override (DECISIONS #38): show a game-abstraction reliability penalty once, no nagging.
// Weights are fictional — not derived from flight data. Penalty multiplies failure probability
// (equivalently: reliability ← reliability × (1 − penalty)). Lightning Hold is never overridable.
using System;

namespace Kestrel.Core.Weather
{
    public static class LaunchAnyway
    {
        // Fictional game-abstraction weights (also in WeatherTests header). Sum of active, capped at 1.
        // Lightning has no weight: it is a non-overridable Hold with a 30-minute clock.
        public const double WeightSurfaceWind = 0.15;
        public const double WeightGust = 0.10;
        public const double WeightPrecip = 0.20;
        public const double WeightCeiling = 0.15;
        public const double WeightTemperature = 0.10;
        public const double WeightShear = 0.20;

        /// <summary>
        /// True when Standard/Engineer may override a soft Hold.
        /// Lightning Holds are never overridable (30-minute clock). KID/Relaxed never override.
        /// </summary>
        public static bool CanOverride(LccResult r)
            => r.commitAllowed && r.verdict == LccVerdict.Hold && !r.lightning;

        /// <summary>
        /// Game-abstraction reliability penalty in [0, 1] for overridable violations.
        /// Multiplies failure probability (fictional weights, not flight-derived).
        /// Zero when Go, when commit is not allowed (KID/Relaxed), or when lightning blocks override.
        /// </summary>
        public static double ReliabilityPenalty(LccResult r)
        {
            if (!CanOverride(r)) return 0;
            double p = 0;
            if (r.surfaceWind) p += WeightSurfaceWind;
            if (r.gust) p += WeightGust;
            if (r.precip) p += WeightPrecip;
            if (r.ceiling) p += WeightCeiling;
            if (r.temperature) p += WeightTemperature;
            if (r.shear) p += WeightShear;
            return Math.Min(1.0, p);
        }
    }
}
