// P5 coast / handoff goldens. Bands: docs/PHYSICS.md § Coast (P5).
//
// External goldens (computed OUTSIDE this repo; not derived from OnRails / Kepler):
//
//   MU = 3.986004418e14
//   R  = 6378137
//   J2 = 1.08262668e-3
//
//   python3 - <<'PY'
//   import math
//   MU, R, J2 = 3.986004418e14, 6378137.0, 1.08262668e-3
//   DAY = 86400.0
//   a = R + 420e3
//   n = math.sqrt(MU / a**3)
//   print('nodeSecular', -1.5 * n * J2 * (R / a)**2 * math.cos(math.radians(51.64)) * DAY * 180 / math.pi)
//   print('nodeTenDay', 10 * (-1.5 * n * J2 * (R / a)**2 * math.cos(math.radians(51.64)) * DAY * 180 / math.pi))
//   PY
//
// nodeSecular matches the #111 ISS literal; literature check ≈ −5.0 deg/day at abs 0.1.
using System;
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class CoastTests
    {
        const double RelAnalytic = 1e-12;
        const double AbsKeplerPeriodM = 1e-3;
        const double AbsHandoffM = 1e-3;
        const double AbsHandoffVel = 1e-6;
        /// <summary>RK4 fixed step for keplerRk4Period and the multi-orbit drift check.</summary>
        const double Rk4DtS = 1.0;
        /// <summary>
        /// Relative band on energy and |h| after 100 orbits at <see cref="Rk4DtS"/>.
        /// Measured offline ≈ 8e-14 (energy) / 4e-14 (|h|); 1e-9 leaves ~4 decades of headroom for
        /// libm / platform differences while still catching a wrong force model or step size.
        /// </summary>
        const double RelRk4Conserved = 1e-9;

        // Hard-coded literals from the external Python snippet above.
        const double NodeSecularIssDegPerDay = -4.946640707855562;
        const double NodeTenDayIssDeg = -49.466407078555626;

        static ClassicalElements LeoElliptic()
            => new ClassicalElements
            {
                a = Gravity.R_EARTH + 800e3,
                e = 0.02,
                i = 51.6 * Math.PI / 180.0,
                raan = 40.0 * Math.PI / 180.0,
                argp = 30.0 * Math.PI / 180.0,
                m = 20.0 * Math.PI / 180.0,
            };

        static ClassicalElements HighCircular()
            => new ClassicalElements
            {
                a = Gravity.R_EARTH + 800e3,
                e = 0,
                i = 98.6 * Math.PI / 180.0,
                raan = 0,
                argp = 0,
                m = 0,
            };

        /// <summary>300 × 800 km. Periapsis 300 km &lt; 600 km gate → Numeric even at apoapsis.</summary>
        static ClassicalElements Orbit300x800()
        {
            double rp = Gravity.R_EARTH + 300e3;
            double ra = Gravity.R_EARTH + 800e3;
            return new ClassicalElements
            {
                a = 0.5 * (rp + ra),
                e = (ra - rp) / (ra + rp),
                i = 51.6 * Math.PI / 180.0,
                raan = 10.0 * Math.PI / 180.0,
                argp = 0,
                m = Math.PI, // apoapsis: current altitude ≈ 800 km
            };
        }

        [Test]
        public void Kepler_ElementsRoundTrip_EllipticInclined()
        {
            AssertElementsRoundTrip(LeoElliptic());
        }

        [Test]
        public void Kepler_ElementsRoundTrip_CircularE0()
        {
            AssertElementsRoundTrip(HighCircular());
        }

        [Test]
        public void Kepler_ElementsRoundTrip_HighEccentricityE09()
        {
            var el = new ClassicalElements
            {
                a = Gravity.R_EARTH + 2000e3,
                e = 0.9,
                i = 28.5 * Math.PI / 180.0,
                raan = 15.0 * Math.PI / 180.0,
                argp = 40.0 * Math.PI / 180.0,
                m = 10.0 * Math.PI / 180.0,
            };
            AssertElementsRoundTrip(el);
        }

        [Test]
        public void Kepler_ElementsRoundTrip_NearEquatorialNearCircular()
        {
            // Near-singular: tiny inclination and eccentricity. State round-trip is the contract;
            // element angles may redistribute between Ω/ω/M under the circular/equatorial conventions.
            var el = new ClassicalElements
            {
                a = Gravity.R_EARTH + 500e3,
                e = 1e-8,
                i = 1e-6,
                raan = 20.0 * Math.PI / 180.0,
                argp = 30.0 * Math.PI / 180.0,
                m = 45.0 * Math.PI / 180.0,
            };
            Kepler.ToState(el, out double rx, out double ry, out double rz, out double vx, out double vy, out double vz);
            var back = Kepler.FromState(rx, ry, rz, vx, vy, vz);
            Assert.AreEqual(el.a, back.a, RelAnalytic * el.a, "a");
            Assert.Less(back.e, 1e-6, "e near circular");
            Assert.Less(back.i, 1e-4, "i near equatorial");
            Kepler.ToState(back, out double rx2, out double ry2, out double rz2, out double vx2, out double vy2, out double vz2);
            double dr = Math.Sqrt((rx2 - rx) * (rx2 - rx) + (ry2 - ry) * (ry2 - ry) + (rz2 - rz) * (rz2 - rz));
            double dv = Math.Sqrt((vx2 - vx) * (vx2 - vx) + (vy2 - vy) * (vy2 - vy) + (vz2 - vz) * (vz2 - vz));
            Assert.LessOrEqual(dr, AbsHandoffM, "singular case position");
            Assert.LessOrEqual(dv, AbsHandoffVel, "singular case velocity");
        }

        [Test]
        public void Kepler_Hyperbolic_Throws()
        {
            // Escape speed at LEO radius → energy > 0
            double r = Gravity.R_EARTH + 400e3;
            double vEsc = Math.Sqrt(2.0 * Gravity.MU_EARTH / r);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Kepler.FromState(r, 0, 0, 0, vEsc * 1.01, 0));

            var hyper = new ClassicalElements { a = -Gravity.R_EARTH, e = 1.5, i = 0, raan = 0, argp = 0, m = 0 };
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Kepler.ToState(hyper, out _, out _, out _, out _, out _, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => Kepler.Propagate(hyper, 1.0));
        }

        [Test]
        public void Kepler_AnalyticOnePeriod_ClosesRel1e12()
        {
            foreach (var el in new[] { LeoElliptic(), HighCircular(), new ClassicalElements
            {
                a = Gravity.R_EARTH + 2000e3, e = 0.9, i = 28.5 * Math.PI / 180.0,
                raan = 15.0 * Math.PI / 180.0, argp = 40.0 * Math.PI / 180.0, m = 10.0 * Math.PI / 180.0,
            }})
            {
                Kepler.ToState(el, out double rx0, out double ry0, out double rz0, out double vx0, out double vy0, out double vz0);
                double period = 2.0 * Math.PI * Math.Sqrt(el.a * el.a * el.a / Gravity.MU_EARTH);
                Kepler.PropagateState(rx0, ry0, rz0, vx0, vy0, vz0, period,
                    out double rx, out double ry, out double rz, out double vx, out double vy, out double vz);
                Assert.AreEqual(rx0, rx, RelAnalytic * Math.Abs(rx0) + 1e-9, "rx e=" + el.e);
                Assert.AreEqual(ry0, ry, RelAnalytic * Math.Abs(ry0) + 1e-9, "ry e=" + el.e);
                Assert.AreEqual(rz0, rz, RelAnalytic * Math.Abs(rz0) + 1e-9, "rz e=" + el.e);
                Assert.AreEqual(vx0, vx, RelAnalytic * Math.Abs(vx0) + 1e-12, "vx e=" + el.e);
                Assert.AreEqual(vy0, vy, RelAnalytic * Math.Abs(vy0) + 1e-12, "vy e=" + el.e);
                Assert.AreEqual(vz0, vz, RelAnalytic * Math.Abs(vz0) + 1e-12, "vz e=" + el.e);
            }
        }

        [Test]
        public void Kepler_Rk4OnePeriod_ClosesAbs1mm_AtDt1s()
        {
            // Step size: Rk4DtS = 1.0 s (stated for Physics ε review).
            var el = LeoElliptic();
            Kepler.ToState(el, out double rx0, out double ry0, out double rz0, out double vx0, out double vy0, out double vz0);
            double period = 2.0 * Math.PI * Math.Sqrt(el.a * el.a * el.a / Gravity.MU_EARTH);
            var y = IntegrateRk4(rx0, ry0, rz0, vx0, vy0, vz0, period, Rk4DtS);
            double dr = Math.Sqrt(
                (y[0] - rx0) * (y[0] - rx0) + (y[1] - ry0) * (y[1] - ry0) + (y[2] - rz0) * (y[2] - rz0));
            Assert.LessOrEqual(dr, AbsKeplerPeriodM,
                "RK4 dt=" + Rk4DtS + " s, one-period position close abs 1e-3 m");
        }

        [Test]
        public void Kepler_Rk4_EnergyAndH_DriftOver100Orbits()
        {
            // Same dt as keplerRk4Period. Rel 1e-9: measured ≈ 8e-14 energy / 4e-14 |h| offline.
            var el = LeoElliptic();
            Kepler.ToState(el, out double rx0, out double ry0, out double rz0, out double vx0, out double vy0, out double vz0);
            double e0 = Energy(rx0, ry0, rz0, vx0, vy0, vz0);
            double h0 = AngularMomentum(rx0, ry0, rz0, vx0, vy0, vz0);
            double period = 2.0 * Math.PI * Math.Sqrt(el.a * el.a * el.a / Gravity.MU_EARTH);
            var y = IntegrateRk4(rx0, ry0, rz0, vx0, vy0, vz0, 100.0 * period, Rk4DtS);
            double e1 = Energy(y[0], y[1], y[2], y[3], y[4], y[5]);
            double h1 = AngularMomentum(y[0], y[1], y[2], y[3], y[4], y[5]);
            Assert.AreEqual(e0, e1, RelRk4Conserved * Math.Abs(e0), "energy 100 orbits dt=" + Rk4DtS);
            Assert.AreEqual(h0, h1, RelRk4Conserved * Math.Abs(h0), "|h| 100 orbits dt=" + Rk4DtS);
        }

        [Test]
        public void Handoff_CaptureRelease_Pos1mm_Vel1e6()
        {
            var el = HighCircular();
            Kepler.ToState(el, out double rx0, out double ry0, out double rz0, out double vx0, out double vy0, out double vz0);
            var captured = Handoff.Capture(rx0, ry0, rz0, vx0, vy0, vz0);
            Handoff.Release(captured, out double rx, out double ry, out double rz, out double vx, out double vy, out double vz);
            double dr = Math.Sqrt((rx - rx0) * (rx - rx0) + (ry - ry0) * (ry - ry0) + (rz - rz0) * (rz - rz0));
            double dv = Math.Sqrt((vx - vx0) * (vx - vx0) + (vy - vy0) * (vy - vy0) + (vz - vz0) * (vz - vz0));
            Assert.LessOrEqual(dr, AbsHandoffM, "handoffRoundTrip position abs 1 mm");
            Assert.LessOrEqual(dv, AbsHandoffVel, "handoffRoundTrip velocity abs 1e-6 m/s");
        }

        [Test]
        public void Handoff_NoJumpAtHandoffInstant()
        {
            // Continuity at the mode switch: Capture/Release at dt = 0 must not jump in r or v.
            // Also Numeric and OnRails forced at dt = 0 must match the input state.
            var el = HighCircular();
            Kepler.ToState(el, out double rx0, out double ry0, out double rz0, out double vx0, out double vy0, out double vz0);
            Assert.AreEqual(CoastMode.OnRails, Handoff.SelectCoast(rx0, ry0, rz0, vx0, vy0, vz0));

            Handoff.PropagateCoast(rx0, ry0, rz0, vx0, vy0, vz0, 0,
                out double rxN, out double ryN, out double rzN, out double vxN, out double vyN, out double vzN,
                CoastMode.Numeric);
            Handoff.PropagateCoast(rx0, ry0, rz0, vx0, vy0, vz0, 0,
                out double rxR, out double ryR, out double rzR, out double vxR, out double vyR, out double vzR,
                CoastMode.OnRails);

            Assert.LessOrEqual(Dist(rxN, ryN, rzN, rx0, ry0, rz0), AbsHandoffM, "numeric dt0 pos");
            Assert.LessOrEqual(Dist(vxN, vyN, vzN, vx0, vy0, vz0), AbsHandoffVel, "numeric dt0 vel");
            Assert.LessOrEqual(Dist(rxR, ryR, rzR, rx0, ry0, rz0), AbsHandoffM, "onrails dt0 pos");
            Assert.LessOrEqual(Dist(vxR, vyR, vzR, vx0, vy0, vz0), AbsHandoffVel, "onrails dt0 vel");
            Assert.LessOrEqual(Dist(rxN, ryN, rzN, rxR, ryR, rzR), AbsHandoffM, "mode switch pos jump");
            Assert.LessOrEqual(Dist(vxN, vyN, vzN, vxR, vyR, vzR), AbsHandoffVel, "mode switch vel jump");
        }

        [Test]
        public void Handoff_GateUsesPerigee_NotCurrentAltitude()
        {
            // 300×800 km at apoapsis: current altitude ≈ 800 km but periapsis = 300 km → Numeric.
            var el = Orbit300x800();
            Assert.AreEqual(300e3, Handoff.PeriapsisAltitudeM(el), 1.0, "periapsis altitude");
            Assert.Less(Handoff.PeriapsisAltitudeM(el), Handoff.OnRailsPerigeeM);
            Assert.AreEqual(CoastMode.Numeric, Handoff.SelectCoast(el),
                "perigee gate: stays in drag-capable Numeric propagator");

            Kepler.ToState(el, out double rx, out double ry, out double rz, out double vx, out double vy, out double vz);
            double r = Math.Sqrt(rx * rx + ry * ry + rz * rz);
            double alt = r - Gravity.R_EARTH;
            Assert.Greater(alt, Handoff.OnRailsPerigeeM, "currently above 600 km at apoapsis");
            Assert.AreEqual(CoastMode.Numeric, Handoff.SelectCoast(rx, ry, rz, vx, vy, vz),
                "state at apoapsis still Numeric because periapsis is 300 km");
        }

        [Test]
        public void Handoff_SelectsOnRailsWhenPerigeeAbove600km()
        {
            var high = HighCircular();
            Assert.AreEqual(CoastMode.OnRails, Handoff.SelectCoast(high));
            Assert.GreaterOrEqual(Handoff.PeriapsisAltitudeM(high), Handoff.OnRailsPerigeeM);
        }

        [Test]
        public void OnRails_NodeSecular_ExternalLiteral()
        {
            // ISS-like: 420 km circular, 51.64°. Literal from the Python snippet in the file header.
            double a = Gravity.R_EARTH + 420e3;
            var el = new ClassicalElements { a = a, e = 0, i = 51.64 * Math.PI / 180.0, raan = 0, argp = 0, m = 0 };
            OnRails.SecularRates(el.a, el.e, el.i, out _, out double dO, out _, out _);
            double degPerDay = dO * 86400.0 * 180.0 / Math.PI;
            Assert.AreEqual(NodeSecularIssDegPerDay, degPerDay, 1e-3, "onRailsNode vs external literal");
            // Literature sanity (same band as #111): ISS node ≈ −5.0 deg/day.
            Assert.AreEqual(-5.0, degPerDay, 0.1, "nodeLiterature");

            var after = OnRails.Propagate(el, 10.0 * 86400.0);
            double dRaanDeg = (after.raan - el.raan) * 180.0 / Math.PI;
            if (dRaanDeg > 180) dRaanDeg -= 360;
            Assert.AreEqual(NodeTenDayIssDeg, dRaanDeg, 1e-6, "10-day node vs external literal");
        }

        [Test]
        public void CliPropagate_KeplerAndOnRails()
        {
            var el = HighCircular();
            Kepler.ToState(el, out double rx, out double ry, out double rz, out double vx, out double vy, out double vz);
            string[] args =
            {
                "propagate", "kepler",
                rx.ToString("R"), ry.ToString("R"), rz.ToString("R"),
                vx.ToString("R"), vy.ToString("R"), vz.ToString("R"),
                "60"
            };
            Assert.AreEqual(0, Kestrel.Core.Cli.Commands.Run(args, out string json));
            StringAssert.Contains("\"rx\":", json);
            StringAssert.Contains("\"mode\":\"kepler\"", json);

            args[1] = "onrails";
            Assert.AreEqual(0, Kestrel.Core.Cli.Commands.Run(args, out string json2));
            StringAssert.Contains("\"mode\":\"onrails\"", json2);
        }

        static void AssertElementsRoundTrip(ClassicalElements el)
        {
            Kepler.ToState(el, out double rx, out double ry, out double rz, out double vx, out double vy, out double vz);
            var back = Kepler.FromState(rx, ry, rz, vx, vy, vz);
            Assert.AreEqual(el.a, back.a, RelAnalytic * el.a, "a");
            Assert.AreEqual(el.e, back.e, RelAnalytic + 1e-14, "e");
            Assert.AreEqual(el.i, back.i, RelAnalytic, "i");
            Assert.AreEqual(el.raan, back.raan, RelAnalytic, "raan");
            Assert.AreEqual(el.argp, back.argp, RelAnalytic, "argp");
            Assert.AreEqual(el.m, back.m, RelAnalytic, "m");
        }

        static double[] IntegrateRk4(double rx, double ry, double rz, double vx, double vy, double vz,
            double duration, double dt)
        {
            var y = new[] { rx, ry, rz, vx, vy, vz };
            var rk = new RK4(6);
            var g = new double[3];
            Deriv f = (t, state, dydt) =>
            {
                Gravity.Vec(state[0], state[1], state[2], g);
                dydt[0] = state[3]; dydt[1] = state[4]; dydt[2] = state[5];
                dydt[3] = g[0]; dydt[4] = g[1]; dydt[5] = g[2];
            };
            int steps = (int)Math.Floor(duration / dt);
            for (int k = 0; k < steps; k++) rk.Step(f, k * dt, y, dt);
            double rem = duration - steps * dt;
            if (rem > 1e-15) rk.Step(f, steps * dt, y, rem);
            return y;
        }

        static double Energy(double rx, double ry, double rz, double vx, double vy, double vz)
        {
            double r = Math.Sqrt(rx * rx + ry * ry + rz * rz);
            return 0.5 * (vx * vx + vy * vy + vz * vz) - Gravity.MU_EARTH / r;
        }

        static double AngularMomentum(double rx, double ry, double rz, double vx, double vy, double vz)
        {
            double hx = ry * vz - rz * vy, hy = rz * vx - rx * vz, hz = rx * vy - ry * vx;
            return Math.Sqrt(hx * hx + hy * hy + hz * hz);
        }

        static double Dist(double ax, double ay, double az, double bx, double by, double bz)
        {
            double dx = ax - bx, dy = ay - by, dz = az - bz;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}
