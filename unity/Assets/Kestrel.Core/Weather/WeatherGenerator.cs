// Seeded site climatology → one WeatherDay. Deterministic (mulberry32, same as Sensors).
// Does not replace Flight.Weather.Hold — that policy stub stays for timeline goldens.
using System;
using Kestrel.Core.Flight;

namespace Kestrel.Core.Weather
{
    public static class WeatherGenerator
    {
        /// <summary>
        /// Fictional lognormal shear median (1/s). Chosen so typical jet-layer shear is ~0.004–0.005 /s.
        /// </summary>
        public const double ShearLogMedianPerSec = 0.0045;

        /// <summary>
        /// Lognormal σ of ln(shear). Paired with <see cref="ShearLogMedianPerSec"/> so
        /// P(shear &gt; LaunchCommit.ShearLimitPerSec) = <see cref="ShearExceedanceTarget"/> (= 0.03).
        /// </summary>
        public const double ShearLogSigma = 0.521497546971786;

        /// <summary>Documented annual fictional shear-LCC exceedance rate (lognormal tail).</summary>
        public const double ShearExceedanceTarget = 0.03;

        /// <summary>
        /// Draw one day of weather at <paramref name="siteId"/> for <paramref name="dayOfYear"/> (1…365)
        /// from <paramref name="seed"/>. Difficulty is recorded for callers; the draw itself is site+seed+doy.
        /// </summary>
        public static WeatherDay Generate(string siteId, int dayOfYear, uint seed, string difficulty = "STANDARD")
        {
            if (siteId != SiteClimatology.HavenId)
                throw new ArgumentOutOfRangeException(nameof(siteId), "only haven is defined in P4");
            if (dayOfYear < 1 || dayOfYear > 365)
                throw new ArgumentOutOfRangeException(nameof(dayOfYear), "dayOfYear must be 1…365");

            int month = SiteClimatology.MonthFromDayOfYear(dayOfYear);
            int mi = month - 1;
            uint state = seed == 0 ? 1u : seed;

            double u0 = Sensors.Mulberry32(ref state);
            double u1 = Sensors.Mulberry32(ref state);
            double u2 = Sensors.Mulberry32(ref state);
            double u3 = Sensors.Mulberry32(ref state);
            double u4 = Sensors.Mulberry32(ref state);
            double u5 = Sensors.Mulberry32(ref state);
            double u6 = Sensors.Mulberry32(ref state);
            double u7 = Sensors.Mulberry32(ref state);
            double u8 = Sensors.Mulberry32(ref state);
            double u9 = Sensors.Mulberry32(ref state);
            double u10 = Sensors.Mulberry32(ref state);

            // Rayleigh wind with monthly mean: scale = μ / √(π/2)
            double meanWind = SiteClimatology.MeanWindMs[mi];
            double scale = meanWind / Math.Sqrt(Math.PI / 2.0);
            double windMs = scale * Math.Sqrt(-2.0 * Math.Log(Math.Max(u0, 1e-12)));
            double gustMs = windMs * (1.15 + 0.35 * u1);
            double directionDeg = 360.0 * u2;

            double r = Math.Sqrt(-2.0 * Math.Log(Math.Max(u3, 1e-12)));
            double tempC = SiteClimatology.MeanTempC[mi] + SiteClimatology.TempSigmaC * r * Math.Cos(2.0 * Math.PI * u4);

            double cloud = u5;
            bool precip = u6 < (0.12 + 0.25 * cloud);
            double ceilingM = 400.0 + 9000.0 * (1.0 - cloud) * (1.0 - cloud);
            bool thunderDay = u7 < SiteClimatology.ThunderDayProbability[mi];
            // Lightning distance from the site (km). On thunder days the draw can fall inside 10 nmi.
            double lightningKm = thunderDay ? 2.0 + 30.0 * u8 : 80.0 + 120.0 * u8;

            // Characteristic shear (1/s): fictional lognormal, median 0.0045 /s, P(>0.012)=0.03.
            // Box-Muller N(0,1) from (u9,u10) → exp(μ + σ Z), μ = ln(median).
            double rz = Math.Sqrt(-2.0 * Math.Log(Math.Max(u9, 1e-12)));
            double z = rz * Math.Cos(2.0 * Math.PI * u10);
            double shearPerSec = Math.Exp(Math.Log(ShearLogMedianPerSec) + ShearLogSigma * z);
            double[] shearWindMs = ShearProfile.FromSurface(windMs, shearPerSec);

            return new WeatherDay
            {
                siteId = siteId,
                dayOfYear = dayOfYear,
                month = month,
                seed = seed,
                windMs = windMs,
                gustMs = gustMs,
                directionDeg = directionDeg,
                tempC = tempC,
                cloudFraction = cloud,
                precip = precip,
                ceilingM = ceilingM,
                thunderDay = thunderDay,
                lightningKm = lightningKm,
                shearPerSec = shearPerSec,
                shearWindMs = shearWindMs,
            };
        }
    }
}
