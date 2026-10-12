// Analytic coast with first-order J2 secular rates (time warp / spent stages).
// Rates come from Geopotential.Rates (P2). a, e, i held fixed; Ω, ω, M advance.
// No drag (P5 decay PR).
using System;

namespace Kestrel.Core.Physics
{
    public static class OnRails
    {
        /// <summary>EGM96 / NIMA TR8350.2 J2 print (same as <see cref="Geopotential.J2"/>).</summary>
        public const double J2 = Geopotential.J2;

        /// <summary>
        /// First-order J2 secular rates (rad/s): Ω̇, ω̇, Ṁ via <see cref="Geopotential.Rates"/>.
        /// </summary>
        public static void SecularRates(double a, double e, double iRad,
            out double n, out double dOmega, out double dArgp, out double dMeanAnom,
            double mu = Gravity.MU_EARTH, double re = Gravity.R_EARTH, double j2 = J2)
        {
            var r = Geopotential.Rates(a, e, iRad, mu, re, j2);
            n = r.n;
            dOmega = r.dOmega;
            dArgp = r.dArgp;
            dMeanAnom = r.dMeanAnom;
        }

        public static ClassicalElements Propagate(ClassicalElements el, double dt,
            double mu = Gravity.MU_EARTH)
        {
            SecularRates(el.a, el.e, el.i, out _, out double dO, out double dW, out double dM, mu);
            var o = el;
            o.raan = WrapTwoPi(el.raan + dO * dt);
            o.argp = WrapTwoPi(el.argp + dW * dt);
            o.m = WrapTwoPi(el.m + dM * dt);
            return o;
        }

        public static void PropagateState(double rx0, double ry0, double rz0, double vx0, double vy0, double vz0,
            double dt, out double rx, out double ry, out double rz, out double vx, out double vy, out double vz,
            double mu = Gravity.MU_EARTH)
        {
            var el = Kepler.FromState(rx0, ry0, rz0, vx0, vy0, vz0, mu);
            Kepler.ToState(Propagate(el, dt, mu), out rx, out ry, out rz, out vx, out vy, out vz, mu);
        }

        static double WrapTwoPi(double a)
        {
            a %= 2.0 * Math.PI;
            if (a < 0) a += 2.0 * Math.PI;
            return a;
        }
    }
}
