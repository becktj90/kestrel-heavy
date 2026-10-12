// Analytic coast with first-order J2 secular rates (time warp / spent stages).
// Rates match Geopotential.Rates (P2); inlined until that type lands on main.
// a, e, i held fixed; Ω, ω, M advance. No drag (P5 decay PR).
using System;

namespace Kestrel.Core.Physics
{
    public static class OnRails
    {
        /// <summary>EGM96 / NIMA TR8350.2 J2 print (same as data/bodies.json and Geopotential.J2).</summary>
        public const double J2 = 1.08262668e-3;

        /// <summary>
        /// First-order J2 secular rates (rad/s): Ω̇, ω̇, Ṁ.
        /// Ω̇ = −(3/2) n J2 (R/p)² cos i; ω̇ = (3/4) n J2 (R/p)² (5 cos² i − 1);
        /// Ṁ = n + (3/4) n J2 (R/p)² √(1−e²) (3 cos² i − 1).
        /// </summary>
        public static void SecularRates(double a, double e, double iRad,
            out double n, out double dOmega, out double dArgp, out double dMeanAnom,
            double mu = Gravity.MU_EARTH, double re = Gravity.R_EARTH, double j2 = J2)
        {
            n = Math.Sqrt(mu / (a * a * a));
            double p = a * (1.0 - e * e);
            double ci = Math.Cos(iRad), ci2 = ci * ci;
            double factor = 1.5 * n * j2 * (re / p) * (re / p);
            dOmega = -factor * ci;
            dArgp = 0.5 * factor * (5.0 * ci2 - 1.0);
            dMeanAnom = n + factor * Math.Sqrt(1.0 - e * e) * (1.5 * ci2 - 0.5);
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
