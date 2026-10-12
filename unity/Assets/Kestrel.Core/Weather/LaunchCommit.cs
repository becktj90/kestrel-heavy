// Fictional launch commit criteria modelled on public range weather facts (45th Weather Squadron class).
// Original wording. Does not change Flight.Weather.Hold; KID/Relaxed never commit-hold (DECISIONS #5).
// Citations / fictional labels: docs/PHYSICS.md § Weather (P4) LCC citation table.
using System;
using Kestrel.Core.Flight;

namespace Kestrel.Core.Weather
{
    public enum LccVerdict
    {
        Go = 0,
        Hold = 1,
        Scrub = 2,
    }

    public struct LccResult
    {
        public LccVerdict verdict;
        /// <summary>False in KID/Relaxed — weather is visual only (matches Weather.Hold allowed=false).</summary>
        public bool commitAllowed;
        public bool surfaceWind;
        public bool gust;
        public bool lightning;
        public bool precip;
        public bool ceiling;
        public bool temperature;
        public bool shear;
        public int violationCount;
        /// <summary>
        /// Minutes to wait before re-poll when lightning is active (Eastern Range–class 30 min).
        /// Zero when lightning is not violated. Distance is measured from the site, not the flight path.
        /// </summary>
        public int holdMinutes;
    }

    public static class LaunchCommit
    {
        // Limits: see PHYSICS.md citation table (value / source-or-fictional / real-world analogue).
        public const double SurfaceWindLimitMs = Flight.Weather.DefaultWindLimitMs; // 12 — fictional; Shuttle-class ~15 m/s peak
        public const double GustLimitMs = 16.0; // fictional
        public const double LightningLimitKm = 18.52; // 10 nmi — 45 WS / NASA-STD lightning LCC class
        public const double CeilingLimitM = 1500.0; // fictional
        public const double TempMinC = 2.0; // fictional; Shuttle ~1.7 °C (35 °F)
        public const double TempMaxC = 35.0; // fictional; Shuttle ~37 °C (99 °F)
        /// <summary>
        /// Characteristic upper shear limit (1/s). Fictional, jet-level class: real jet shear is about
        /// 0.005–0.015 /s. Generator draws 0.002…0.018 so the tail can exceed this.
        /// </summary>
        public const double ShearLimitPerSec = 0.012;
        /// <summary>Eastern Range–class wait after lightning within 10 NM of the site (not a scrub).</summary>
        public const int LightningHoldMinutes = 30;

        /// <summary>Evaluate LCC rules on a day sample. Policy: KID/Relaxed never hold.</summary>
        public static LccResult Evaluate(WeatherDay day, string difficulty, bool relaxed = false)
        {
            bool surfaceWind = day.windMs > SurfaceWindLimitMs;
            bool gust = day.gustMs > GustLimitMs;
            // Distance from the site (game simplification — real rule uses flight-path distance).
            bool lightning = day.lightningKm < LightningLimitKm;
            bool precip = day.precip;
            bool ceiling = day.ceilingM < CeilingLimitM;
            bool temperature = day.tempC < TempMinC || day.tempC > TempMaxC;
            bool shear = day.shearPerSec > ShearLimitPerSec;

            int n = 0;
            if (surfaceWind) n++;
            if (gust) n++;
            if (lightning) n++;
            if (precip) n++;
            if (ceiling) n++;
            if (temperature) n++;
            if (shear) n++;

            bool commitAllowed = !(difficulty == "KID" || relaxed);
            LccVerdict verdict;
            if (!commitAllowed || n == 0) verdict = LccVerdict.Go;
            else verdict = LccVerdict.Hold; // lightning is a timed Hold, not Scrub (45 WS class)

            return new LccResult
            {
                verdict = verdict,
                commitAllowed = commitAllowed,
                surfaceWind = surfaceWind,
                gust = gust,
                lightning = lightning,
                precip = precip,
                ceiling = ceiling,
                temperature = temperature,
                shear = shear,
                violationCount = n,
                holdMinutes = lightning && commitAllowed && n > 0 ? LightningHoldMinutes : 0,
            };
        }

        /// <summary>Build a day with only the listed fields set; others are benign Go defaults.</summary>
        public static WeatherDay HandCase(
            double windMs = 5, double gustMs = 7, double lightningKm = 100,
            bool precip = false, double ceilingM = 5000, double tempC = 22, double shearPerSec = 0.01)
            => new WeatherDay
            {
                siteId = SiteClimatology.HavenId,
                dayOfYear = 1,
                month = 1,
                seed = 0,
                windMs = windMs,
                gustMs = gustMs,
                directionDeg = 90,
                tempC = tempC,
                cloudFraction = 0.2,
                precip = precip,
                ceilingM = ceilingM,
                thunderDay = lightningKm < LightningLimitKm,
                lightningKm = lightningKm,
                shearPerSec = shearPerSec,
                shearWindMs = ShearProfile.FromSurface(windMs, shearPerSec),
            };
    }
}
