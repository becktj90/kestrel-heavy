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
                ClosedForm(p[0], p[1], p[2], expect);
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
        public void IssNodeRate_AndSso_AndCriticalInclination()
        {
            double iss = Geopotential.NodeRateDegPerDay(Gravity.R_EARTH + 420e3, 0, 51.64);
            Assert.AreEqual(-5.0, iss, 0.1);
            double sso = Geopotential.SsoInclinationDeg(Gravity.R_EARTH + 800e3);
            Assert.AreEqual(98.6, sso, 0.1);
            Assert.AreEqual(0.0, Geopotential.ArgpRateDegPerDay(Gravity.R_EARTH + 800e3, 0, Geopotential.CriticalInclinationDeg), 1e-9);
            Assert.AreEqual(Geopotential.CriticalInclinationDeg, Math.Acos(Math.Sqrt(0.2)) * 180 / Math.PI, 1e-12);
            Assert.AreEqual(0.0, Geopotential.ArgpRateDegPerDay(Gravity.R_EARTH + 800e3, 0, Geopotential.CriticalInclinationRetroDeg), 1e-9);
        }

        [Test]
        public void FrozenOrbitEccentricity_At800KmSso()
        {
            double e = Geopotential.FrozenEccentricity(Gravity.R_EARTH + 800e3, 98.6);
            Assert.AreEqual(0.001, e, 0.0001); // ±10 % of 0.001
        }

        [Test]
        public void MeanOsculating_RoundTrip()
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
        }

        [Test]
        public void NumericJ2_NodeDrift_MatchesSecularWithinOnePercent()
        {
            // Circular ISS-like: a = Re+420 km, i = 51.64°. J2 only. 10 days.
            double a = Gravity.R_EARTH + 420e3;
            double i = 51.64 * Math.PI / 180.0;
            double n = Math.Sqrt(Gravity.MU_EARTH / (a * a * a));
            double v = n * a;
            // Ω = 0, u = 0: r along x', v along nodal direction in the orbital plane.
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
            const double dt = 30.0;
            const double span = 10.0 * 86400.0;
            double sumT = 0, sumO = 0, sumTT = 0, sumTO = 0;
            int nSamp = 0;
            double prev = Raan(y);
            double unwrapped = prev;
            for (double t = 0; t < span; t += dt)
            {
                rk.Step(deriv, t, y, dt);
                double om = Raan(y);
                double d = om - prev;
                if (d > Math.PI) d -= 2 * Math.PI;
                if (d < -Math.PI) d += 2 * Math.PI;
                unwrapped += d;
                prev = om;
                double tt = t + dt;
                sumT += tt; sumO += unwrapped; sumTT += tt * tt; sumTO += tt * unwrapped;
                nSamp++;
            }
            double slope = (nSamp * sumTO - sumT * sumO) / (nSamp * sumTT - sumT * sumT);
            double secular = Geopotential.Rates(a, 0, i).dOmega;
            Assert.AreEqual(secular, slope, 0.01 * Math.Abs(secular));
        }

        static double Raan(double[] s)
        {
            double hx = s[1] * s[5] - s[2] * s[4];
            double hy = s[2] * s[3] - s[0] * s[5];
            return Math.Atan2(hx, -hy);
        }

        /// <summary>Independent transcription of Montenbruck &amp; Gill 3.36–3.38.</summary>
        static void ClosedForm(double x, double y, double z, double[] o)
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
