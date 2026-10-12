// Fictional launch commit criteria modelled on public range weather facts (45th Weather Squadron class).
// Original wording. Does not change Flight.Weather.Hold; KID/Relaxed never commit-hold (DECISIONS #5).
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
    }

    public static class LaunchCommit
    {
        public const double SurfaceWindLimitMs = Flight.Weather.DefaultWindLimitMs; // 12
        public const double GustLimitMs = 16.0;
        public const double LightningLimitKm = 18.52; // 10 nmi
        public const double CeilingLimitM = 1500.0;
        public const double TempMinC = 2.0;
        public const double TempMaxC = 35.0;
        public const double ShearLimitPerSec = 0.030;

        /// <summary>Evaluate LCC rules on a day sample. Policy: KID/Relaxed never hold.</summary>
        public static LccResult Evaluate(WeatherDay day, string difficulty, bool relaxed = false)
        {
            bool surfaceWind = day.windMs > SurfaceWindLimitMs;
            bool gust = day.gustMs > GustLimitMs;
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
            else if (lightning) verdict = LccVerdict.Scrub;
            else verdict = LccVerdict.Hold;

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
            };
    }
}
