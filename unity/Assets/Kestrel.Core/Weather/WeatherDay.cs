// One deterministic launch-day weather sample (surface + upper shear). No Unity types.
// Shear profile is for visuals and LCC only — not consumed by Headless / Ascent3Dof (known simplification).
using System;

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
        /// <summary>Distance of nearest strike from the site (km), not from the flight path.</summary>
        public double lightningKm;
        /// <summary>Characteristic upper-level speed shear (1/s) across the jet layer (LCC).</summary>
        public double shearPerSec;
        /// <summary>
        /// Horizontal wind (m/s) at <see cref="ShearProfile.AltitudeKm"/> levels for visuals / commit display.
        /// Not fed into AscentSim in P4 (see PHYSICS.md — ignored load-relief/q·α, drift, steering loss).
        /// </summary>
        public double[] shearWindMs;
    }

    public static class ShearProfile
    {
        /// <summary>Geometric altitudes (km) for the visual / LCC upper-wind profile.</summary>
        public static readonly double[] AltitudeKm = { 0, 3, 6, 9, 12 };

        /// <summary>Altitude (km) of the jet peak in the shaped profile.</summary>
        public const double JetAltKm = 10.5;

        /// <summary>
        /// Characteristic depth (m) converting shear rate → unsaturated jet peak increment:
        /// rawPeak = surface + shearPerSec × ShearLayerM, then <see cref="SoftCap"/>.
        /// </summary>
        public const double ShearLayerM = 8000.0;

        /// <summary>
        /// Soft-saturation asymptote (m/s) for jet-peak wind. Replaces a hard clip so the ~3% of
        /// shear-fail days (rawPeak ≳ 100) are not all pinned exactly at the ceiling.
        /// Real jets are ~30–70 m/s; rare cores approach this asymptote from below.
        /// </summary>
        public const double JetCapMs = 100.0;

        /// <summary>Exponent for algebraic soft cap: raw / (1 + (raw/cap)^n)^(1/n).</summary>
        public const double SoftCapExponent = 4.0;

        /// <summary>
        /// Algebraic soft saturation toward <paramref name="cap"/> (n = <see cref="SoftCapExponent"/>).
        /// Nearly linear below ~70 m/s; asymptotes to cap without a hard knee.
        /// </summary>
        public static double SoftCap(double raw, double cap = JetCapMs, double n = SoftCapExponent)
        {
            if (raw <= 0) return 0;
            if (cap <= 0) return 0;
            double x = raw / cap;
            return raw / Math.Pow(1.0 + Math.Pow(x, n), 1.0 / n);
        }

        /// <summary>
        /// Build a 5-level wind profile peaking near jet level.
        /// Smoothstep rise from surface to <see cref="JetAltKm"/>, mild decay above.
        /// Jet peak = SoftCap(surface + shearPerSec × ShearLayerM). Pure display kinematics.
        /// </summary>
        public static double[] FromSurface(double surfaceWindMs, double shearPerSec)
        {
            double rawPeak = surfaceWindMs + shearPerSec * ShearLayerM;
            double jetPeak = SoftCap(rawPeak);
            var w = new double[AltitudeKm.Length];
            for (int i = 0; i < AltitudeKm.Length; i++)
            {
                double alt = AltitudeKm[i];
                double t = alt / JetAltKm;
                if (t <= 1.0)
                {
                    double s = t * t * (3.0 - 2.0 * t); // smoothstep
                    w[i] = surfaceWindMs + (jetPeak - surfaceWindMs) * s;
                }
                else
                {
                    double u = (alt - JetAltKm) / (AltitudeKm[AltitudeKm.Length - 1] - JetAltKm);
                    w[i] = jetPeak - 0.15 * (jetPeak - surfaceWindMs) * u;
                }
            }
            return w;
        }
    }
}
