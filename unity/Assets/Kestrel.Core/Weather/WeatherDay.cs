// One deterministic launch-day weather sample (surface + upper shear). No Unity types.
// Shear profile is for visuals and LCC only — not consumed by Headless / Ascent3Dof.
namespace Kestrel.Core.Weather
{
    public struct WeatherDay
    {
        public string siteId;
        public int dayOfYear;
        public int month;
        public uint seed;
        public double windMs;
        public double gustMs;
        public double directionDeg;
        public double tempC;
        public double cloudFraction;
        public bool precip;
        public double ceilingM;
        public bool thunderDay;
        public double lightningKm;
        /// <summary>Characteristic upper-level speed shear (1/s) across the ascent band (LCC).</summary>
        public double shearPerSec;
        /// <summary>
        /// Horizontal wind (m/s) at <see cref="ShearAltitudeKm"/> levels for visuals / commit display.
        /// Not fed into AscentSim in P4.
        /// </summary>
        public double[] shearWindMs;
    }

    public static class ShearProfile
    {
        /// <summary>Geometric altitudes (km) for the visual / LCC upper-wind profile.</summary>
        public static readonly double[] AltitudeKm = { 0, 3, 6, 9, 12 };

        /// <summary>
        /// Build a 5-level wind profile from surface wind and shear rate.
        /// Pure kinematics for display; AscentSim does not read this.
        /// </summary>
        public static double[] FromSurface(double surfaceWindMs, double shearPerSec)
        {
            var w = new double[AltitudeKm.Length];
            for (int i = 0; i < AltitudeKm.Length; i++)
                w[i] = surfaceWindMs + shearPerSec * (AltitudeKm[i] * 1000.0);
            return w;
        }
    }
}
