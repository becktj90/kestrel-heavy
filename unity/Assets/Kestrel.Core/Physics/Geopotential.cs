// Zonal J2–J4 gravity, first-order secular rates, and a first-order mean ↔ osculating map.
// Constants match data/bodies.json (EGM96 J2 print; J3/J4 from NIMA TR8350.2).
// Tesseral terms and degrees above 4 are out of scope. Ascent stays point-mass.
using System;

namespace Kestrel.Core.Physics
{
    public struct SecularRates
    {
        public double n, dOmega, dArgp, dMeanAnom;
    }

    public struct MeanElements
    {
        public double a, e, i, raan, argp, m;
    }

    public static class Geopotential
    {
        public const double J2 = 1.08262668e-3;
        public const double J3 = -2.53265648e-6;
        public const double J4 = -1.61962159e-6;
        public const double CriticalInclinationDeg = 63.43494882292201;
        public const double CriticalInclinationRetroDeg = 116.56505117707799;
        /// <summary>Mean solar rate, deg/day, for the SSO solver (365.2422 day year).</summary>
        public const double MeanSunDegPerDay = 360.0 / 365.2422;

        /// <summary>
        /// Point-mass plus zonal J2–J4. z is the Earth pole.
        /// Montenbruck &amp; Gill, Satellite Orbits, eqs. 3.36–3.38.
        /// Same closed form as web/js/physics/gravity.js gravityZonal.
        /// </summary>
        public static void Accel(double x, double y, double z, double[] o,
            double mu = Gravity.MU_EARTH, double re = Gravity.R_EARTH,
            double j2 = J2, double j3 = J3, double j4 = J4)
        {
            double r2 = x * x + y * y + z * z;
            double r = Math.Sqrt(r2);
            double r3 = r2 * r;
            double r5 = r2 * r3;
            double r7 = r5 * r2;
            double zr2 = (z * z) / r2;
            double zr4 = zr2 * zr2;
            double re2 = re * re;
            double re3 = re2 * re;
            double re4 = re2 * re2;
            double pm = -mu / r3;

            // J2: −(3/2) μ J2 Re² / r⁵ · (x(1−5zr²), y(1−5zr²), z(3−5zr²))
            double k2 = -1.5 * mu * j2 * re2 / r5;
            // J3: −(5/2) μ J3 Re³ / r⁷ · (x z (3−7zr²), y z (3−7zr²), 6z² − 7z⁴/r² − 3r²/5)
            double k3 = -2.5 * mu * j3 * re3 / r7;
            // J4: +(15/8) μ J4 Re⁴ / r⁷ · (x(1−14zr²+21zr⁴), …, z(5−70/3 zr²+21zr⁴))
            double k4 = 1.875 * mu * j4 * re4 / r7;

            o[0] = pm * x
                + k2 * x * (1.0 - 5.0 * zr2)
                + k3 * x * z * (3.0 - 7.0 * zr2)
                + k4 * x * (1.0 - 14.0 * zr2 + 21.0 * zr4);
            o[1] = pm * y
                + k2 * y * (1.0 - 5.0 * zr2)
                + k3 * y * z * (3.0 - 7.0 * zr2)
                + k4 * y * (1.0 - 14.0 * zr2 + 21.0 * zr4);
            o[2] = pm * z
                + k2 * z * (3.0 - 5.0 * zr2)
                + k3 * (6.0 * z * z - 7.0 * z * z * zr2 - 0.6 * r2)
                + k4 * z * (5.0 - (70.0 / 3.0) * zr2 + 21.0 * zr4);
        }

        public static SecularRates Rates(double a, double e, double iRad,
            double mu = Gravity.MU_EARTH, double re = Gravity.R_EARTH, double j2 = J2)
        {
            double n = Math.Sqrt(mu / (a * a * a));
            double p = a * (1.0 - e * e);
            double ci = Math.Cos(iRad), ci2 = ci * ci;
            double factor = 1.5 * n * j2 * (re / p) * (re / p);
            return new SecularRates
            {
                n = n,
                dOmega = -factor * ci,
                dArgp = 0.5 * factor * (5.0 * ci2 - 1.0),
                dMeanAnom = n + factor * Math.Sqrt(1.0 - e * e) * (1.5 * ci2 - 0.5),
            };
        }

        public static double NodeRateDegPerDay(double a, double e, double iDeg)
            => Rates(a, e, iDeg * Math.PI / 180.0).dOmega * 86400.0 * 180.0 / Math.PI;

        public static double ArgpRateDegPerDay(double a, double e, double iDeg)
            => Rates(a, e, iDeg * Math.PI / 180.0).dArgp * 86400.0 * 180.0 / Math.PI;

        public static double SsoInclinationDeg(double a, double sunDegPerDay = MeanSunDegPerDay)
        {
            double n = Math.Sqrt(Gravity.MU_EARTH / (a * a * a));
            double cosI = -(sunDegPerDay * Math.PI / 180.0 / 86400.0)
                / (1.5 * n * J2 * (Gravity.R_EARTH / a) * (Gravity.R_EARTH / a));
            return Math.Acos(cosI) * 180.0 / Math.PI;
        }

        public static double FrozenEccentricity(double a, double iDeg)
            => -0.5 * (J3 / J2) * (Gravity.R_EARTH / a) * Math.Sin(iDeg * Math.PI / 180.0);

        /// <summary>
        /// First-order J2 short-period map, mean → osculating. Finite at e = 0.
        /// Inverse of <see cref="OsculatingToMean"/>.
        /// </summary>
        public static MeanElements MeanToOsculating(MeanElements m)
        {
            double p = m.a * Math.Max(1.0 - m.e * m.e, 1e-12);
            double kappa = J2 * (Gravity.R_EARTH / p) * (Gravity.R_EARTH / p);
            double si = Math.Sin(m.i);
            double c = kappa * (1.0 - 1.5 * si * si);
            double u = m.argp + m.m;
            double cos2u = Math.Cos(2.0 * u);
            double sin2u = Math.Sin(2.0 * u);
            return new MeanElements
            {
                a = m.a * (1.0 + c * cos2u),
                e = m.e + 0.5 * c * sin2u * si,
                i = m.i,
                raan = m.raan,
                argp = m.argp,
                m = m.m,
            };
        }

        public static MeanElements OsculatingToMean(MeanElements o)
        {
            // Fixed-point iteration to convergence (not a fixed pass count).
            double a = o.a, e = o.e;
            double si = Math.Sin(o.i);
            double u = o.argp + o.m;
            double cos2u = Math.Cos(2.0 * u);
            double sin2u = Math.Sin(2.0 * u);
            for (int k = 0; k < 32; k++)
            {
                double p = a * Math.Max(1.0 - e * e, 1e-12);
                double kappa = J2 * (Gravity.R_EARTH / p) * (Gravity.R_EARTH / p);
                double c = kappa * (1.0 - 1.5 * si * si);
                double aNext = o.a / (1.0 + c * cos2u);
                double eNext = o.e - 0.5 * c * sin2u * si;
                bool done = Math.Abs(aNext - a) <= 1e-15 * Math.Max(Math.Abs(aNext), 1.0)
                    && Math.Abs(eNext - e) <= 1e-15;
                a = aNext; e = eNext;
                if (done) break;
            }
            return new MeanElements
            {
                a = a,
                e = e,
                i = o.i,
                raan = o.raan,
                argp = o.argp,
                m = o.m,
            };
        }
    }
}
