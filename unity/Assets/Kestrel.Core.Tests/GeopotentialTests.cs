// P2: zonal J2–J4, secular rates, mean ↔ osculating. Bands are "Gravity (P2)" in docs/PHYSICS.md.
using System;
using Kestrel.Core.Bodies;
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class GeopotentialTests
    {
        static readonly double[][] Points =
        {
            new[] { Gravity.R_EARTH + 400e3, 0, 0 },
            new[] { 0, Gravity.R_EARTH + 400e3, 0 },
            new[] { 0, 0, Gravity.R_EARTH + 400e3 },
            new[] { 0, 0, -(Gravity.R_EARTH + 400e3) },
            new[] { 5e6, 3e6, 4e6 },
            new[] { -2e6, 6e6, -1e6 },
        };

        [Test]
        public void EarthRecordCarriesJ2J3J4()
        {
            var earth = BodyCatalog.Require(BodyCatalog.LoadFromRepo(Golden.Root), "earth");
            Assert.AreEqual(Geopotential.J2, earth.j2);
            Assert.AreEqual(Geopotential.J3, earth.j3);
            Assert.AreEqual(Geopotential.J4, earth.j4);
        }

        [Test]
        public void Accel_MatchesClosedFormAtSixPoints()
        {
            var o = new double[3];
            var expect = new double[3];
            foreach (var p in Points)
            {
                Geopotential.Accel(p[0], p[1], p[2], o);
                ClosedFormAccel(p[0], p[1], p[2], expect);
                for (int k = 0; k < 3; k++)
                {
                    double scale = Math.Max(Math.Abs(expect[k]), 1e-300);
                    Assert.AreEqual(expect[k], o[k], 1e-12 * scale, "pt " + p[0] + "," + p[1] + "," + p[2] + " k" + k);
                }
            }
        }

        [Test]
        public void Accel_ReducesToPointMassWhenJnZero()
        {
            var o = new double[3];
            var pm = new double[3];
            Geopotential.Accel(7e6, -1e6, 2e6, o, Gravity.MU_EARTH, Gravity.R_EARTH, 0, 0, 0);
            Gravity.Vec(7e6, -1e6, 2e6, pm);
            Assert.AreEqual(pm[0], o[0], 0);
            Assert.AreEqual(pm[1], o[1], 0);
            Assert.AreEqual(pm[2], o[2], 0);
        }

        [Test]
        public void NodeSecular_MatchesIndependentJ2ClosedForm()
        {
            // Independent first-order J2: Ω̇ = −1.5 n J2 (R/p)² cos i. Not the −5.0 deg/day round number.
            double a = Gravity.R_EARTH + 420e3;
            double e = 0;
            double iDeg = 51.64;
            double expect = IndependentNodeRateDegPerDay(a, e, iDeg);
            double got = Geopotential.NodeRateDegPerDay(a, e, iDeg);
            Assert.AreEqual(expect, got, 1e-3, "nodeSecular");
            TestContext.WriteLine("nodeSecular closed-form " + expect.ToString("G17") + " got " + got.ToString("G17")
                + " err " + Math.Abs(got - expect).ToString("G17"));
        }

        [Test]
        public void SsoInclination_MatchesIndependentJ2ClosedForm()
        {
            // First-order J2 only. J3/J4 are not in the SSO solver.
            double a = Gravity.R_EARTH + 800e3;
            double expect = IndependentSsoInclinationDeg(a);
            double got = Geopotential.SsoInclinationDeg(a);
            Assert.AreEqual(expect, got, 0.01, "ssoInclination");
            TestContext.WriteLine("ssoInclination J2-only " + expect.ToString("G17") + " got " + got.ToString("G17")
                + " err " + Math.Abs(got - expect).ToString("G17"));
            Assert.AreEqual(0.0, Geopotential.ArgpRateDegPerDay(a, 0, Geopotential.CriticalInclinationDeg), 1e-9);
            Assert.AreEqual(Geopotential.CriticalInclinationDeg, Math.Acos(Math.Sqrt(0.2)) * 180 / Math.PI, 1e-12);
            Assert.AreEqual(0.0, Geopotential.ArgpRateDegPerDay(a, 0, Geopotential.CriticalInclinationRetroDeg), 1e-9);
        }

        [Test]
        public void FrozenOrbitEccentricity_MatchesIndependentFormula()
        {
            // e = −(J3 / 2 J2) (R/a) sin i. Independent transcription, not the 0.001 round number.
            double a = Gravity.R_EARTH + 800e3;
            double iDeg = 98.6;
            double expect = -(Geopotential.J3 / (2.0 * Geopotential.J2)) * (Gravity.R_EARTH / a)
                * Math.Sin(iDeg * Math.PI / 180.0);
            double got = Geopotential.FrozenEccentricity(a, iDeg);
            Assert.AreEqual(expect, got, 1e-5, "frozenE");
            TestContext.WriteLine("frozenE formula " + expect.ToString("G17") + " got " + got.ToString("G17")
                + " err " + Math.Abs(got - expect).ToString("G17"));
        }

        [Test]
        public void MeanOsculating_RoundTrip_InverseToConvergence()
        {
            var mean = new MeanElements
            {
                a = Gravity.R_EARTH + 700e3,
                e = 0.001,
                i = 51.6 * Math.PI / 180,
                raan = 0.3,
                argp = 0.7,
                m = 1.1,
            };
            var osc = Geopotential.MeanToOsculating(mean);
            var back = Geopotential.OsculatingToMean(osc);
            Assert.AreEqual(mean.a, back.a, 1.0, "a");
            Assert.AreEqual(mean.e, back.e, 1e-7, "e");
            Assert.AreEqual(mean.i, back.i, 0);
            Assert.AreEqual(mean.raan, back.raan, 0);
            // A second pass does not move the result: the inverse has converged.
            var again = Geopotential.OsculatingToMean(Geopotential.MeanToOsculating(back));
            Assert.AreEqual(back.a, again.a, 1e-9);
            Assert.AreEqual(back.e, again.e, 1e-12);
        }

        [Test]
        public void NumericJ2_NodeDrift_FitsWholeOrbits()
        {
            // Circular ISS-like. J2 only. Fit Ω over an integer number of orbits (≥ 50).
            // J2² / J2 ≈ J2 ≈ 1e-3; the band is rel 0.01 so that first-order secular is the claim.
            double a = Gravity.R_EARTH + 420e3;
            double i = 51.64 * Math.PI / 180.0;
            double n = Math.Sqrt(Gravity.MU_EARTH / (a * a * a));
            double period = 2.0 * Math.PI / n;
            const int orbits = 50;
            Assert.GreaterOrEqual(orbits, 50);
            double span = orbits * period;
            double v = n * a;
            double ci = Math.Cos(i), si = Math.Sin(i);
            var y = new double[6];
            y[0] = a; y[1] = 0; y[2] = 0;
            y[3] = 0; y[4] = v * ci; y[5] = v * si;
            var rk = new RK4(6);
            var g = new double[3];
            Deriv deriv = (t, s, dy) =>
            {
                Geopotential.Accel(s[0], s[1], s[2], g, Gravity.MU_EARTH, Gravity.R_EARTH, Geopotential.J2, 0, 0);
                dy[0] = s[3]; dy[1] = s[4]; dy[2] = s[5];
                dy[3] = g[0]; dy[4] = g[1]; dy[5] = g[2];
            };
            // Land steps on the period so the span is exact.
            double dt = period / 200.0;
            int steps = orbits * 200;
            double sumT = 0, sumO = 0, sumTT = 0, sumTO = 0;
            int nSamp = 0;
            double prev = Raan(y);
            double unwrapped = prev;
            for (int s = 0; s < steps; s++)
            {
                rk.Step(deriv, s * dt, y, dt);
                double om = Raan(y);
                double d = om - prev;
                if (d > Math.PI) d -= 2 * Math.PI;
                if (d < -Math.PI) d += 2 * Math.PI;
                unwrapped += d;
                prev = om;
                double tt = (s + 1) * dt;
                sumT += tt; sumO += unwrapped; sumTT += tt * tt; sumTO += tt * unwrapped;
                nSamp++;
            }
            Assert.AreEqual(span, steps * dt, 1e-9 * span, "integer orbits");
            double slope = (nSamp * sumTO - sumT * sumO) / (nSamp * sumTT - sumT * sumT);
            double secular = IndependentNodeRateRadPerSec(a, 0, i);
            double rel = Math.Abs(slope - secular) / Math.Abs(secular);
            TestContext.WriteLine("nodeNumeric orbits " + orbits + " period " + period.ToString("G17")
                + " slope " + slope.ToString("G17") + " secular " + secular.ToString("G17")
                + " rel err " + rel.ToString("G17"));
            Assert.Less(rel, 0.01, "nodeNumeric rel 0.01; J2² terms are about 1e-3");
        }

        /// <summary>Independent first-order J2 nodal rate, deg/day. Does not call Geopotential.Rates.</summary>
        static double IndependentNodeRateDegPerDay(double a, double e, double iDeg)
            => IndependentNodeRateRadPerSec(a, e, iDeg * Math.PI / 180.0) * 86400.0 * 180.0 / Math.PI;

        static double IndependentNodeRateRadPerSec(double a, double e, double iRad)
        {
            double n = Math.Sqrt(Gravity.MU_EARTH / (a * a * a));
            double p = a * (1.0 - e * e);
            return -1.5 * n * Geopotential.J2 * (Gravity.R_EARTH / p) * (Gravity.R_EARTH / p) * Math.Cos(iRad);
        }

        /// <summary>Independent first-order J2 SSO inclination. No J3, no J4.</summary>
        static double IndependentSsoInclinationDeg(double a)
        {
            double n = Math.Sqrt(Gravity.MU_EARTH / (a * a * a));
            double sun = Geopotential.MeanSunDegPerDay * Math.PI / 180.0 / 86400.0;
            double cosI = -sun / (1.5 * n * Geopotential.J2 * (Gravity.R_EARTH / a) * (Gravity.R_EARTH / a));
            return Math.Acos(cosI) * 180.0 / Math.PI;
        }

        static double Raan(double[] s)
        {
            double hx = s[1] * s[5] - s[2] * s[4];
            double hy = s[2] * s[3] - s[0] * s[5];
            return Math.Atan2(hx, -hy);
        }

        /// <summary>Independent transcription of Montenbruck &amp; Gill 3.36–3.38.</summary>
        static void ClosedFormAccel(double x, double y, double z, double[] o)
        {
            double mu = Gravity.MU_EARTH, re = Gravity.R_EARTH;
            double j2 = Geopotential.J2, j3 = Geopotential.J3, j4 = Geopotential.J4;
            double r2 = x * x + y * y + z * z;
            double r = Math.Sqrt(r2);
            double r3 = r2 * r, r5 = r2 * r3, r7 = r5 * r2;
            double zr2 = (z * z) / r2, zr4 = zr2 * zr2;
            double pm = -mu / r3;
            double k2 = -1.5 * mu * j2 * re * re / r5;
            double k3 = -2.5 * mu * j3 * re * re * re / r7;
            double k4 = 1.875 * mu * j4 * re * re * re * re / r7;
            o[0] = pm * x + k2 * x * (1 - 5 * zr2) + k3 * x * z * (3 - 7 * zr2) + k4 * x * (1 - 14 * zr2 + 21 * zr4);
            o[1] = pm * y + k2 * y * (1 - 5 * zr2) + k3 * y * z * (3 - 7 * zr2) + k4 * y * (1 - 14 * zr2 + 21 * zr4);
            o[2] = pm * z + k2 * z * (3 - 5 * zr2) + k3 * (6 * z * z - 7 * z * z * zr2 - 0.6 * r2)
                + k4 * z * (5 - (70.0 / 3.0) * zr2 + 21 * zr4);
        }
    }
}
