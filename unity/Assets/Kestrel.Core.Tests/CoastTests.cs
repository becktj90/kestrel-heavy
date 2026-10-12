// P5 coast / handoff goldens. Bands: docs/PHYSICS.md § Coast (P5).
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

        [Test]
        public void Kepler_ElementsRoundTrip_Rel1e12()
        {
            var el = LeoElliptic();
            Kepler.ToState(el, out double rx, out double ry, out double rz, out double vx, out double vy, out double vz);
            var back = Kepler.FromState(rx, ry, rz, vx, vy, vz);
            Assert.AreEqual(el.a, back.a, RelAnalytic * el.a, "a");
            Assert.AreEqual(el.e, back.e, RelAnalytic, "e");
            Assert.AreEqual(el.i, back.i, RelAnalytic, "i");
            Assert.AreEqual(el.raan, back.raan, RelAnalytic, "raan");
            Assert.AreEqual(el.argp, back.argp, RelAnalytic, "argp");
            Assert.AreEqual(el.m, back.m, RelAnalytic, "m");
        }

        [Test]
        public void Kepler_AnalyticOnePeriod_ClosesRel1e12()
        {
            var el = LeoElliptic();
            Kepler.ToState(el, out double rx0, out double ry0, out double rz0, out double vx0, out double vy0, out double vz0);
            double period = 2.0 * Math.PI * Math.Sqrt(el.a * el.a * el.a / Gravity.MU_EARTH);
            Kepler.PropagateState(rx0, ry0, rz0, vx0, vy0, vz0, period,
                out double rx, out double ry, out double rz, out double vx, out double vy, out double vz);
            Assert.AreEqual(rx0, rx, RelAnalytic * Math.Abs(rx0) + 1e-9, "rx");
            Assert.AreEqual(ry0, ry, RelAnalytic * Math.Abs(ry0) + 1e-9, "ry");
            Assert.AreEqual(rz0, rz, RelAnalytic * Math.Abs(rz0) + 1e-9, "rz");
            Assert.AreEqual(vx0, vx, RelAnalytic * Math.Abs(vx0) + 1e-12, "vx");
            Assert.AreEqual(vy0, vy, RelAnalytic * Math.Abs(vy0) + 1e-12, "vy");
            Assert.AreEqual(vz0, vz, RelAnalytic * Math.Abs(vz0) + 1e-12, "vz");
        }

        [Test]
        public void Kepler_Rk4OnePeriod_ClosesAbs1mm()
        {
            var el = LeoElliptic();
            Kepler.ToState(el, out double rx0, out double ry0, out double rz0, out double vx0, out double vy0, out double vz0);
            double period = 2.0 * Math.PI * Math.Sqrt(el.a * el.a * el.a / Gravity.MU_EARTH);
            var y = new[] { rx0, ry0, rz0, vx0, vy0, vz0 };
            var rk = new RK4(6);
            var g = new double[3];
            Deriv f = (t, state, dydt) =>
            {
                Gravity.Vec(state[0], state[1], state[2], g);
                dydt[0] = state[3]; dydt[1] = state[4]; dydt[2] = state[5];
                dydt[3] = g[0]; dydt[4] = g[1]; dydt[5] = g[2];
            };
            const double dt = 1.0;
            int steps = (int)Math.Floor(period / dt);
            for (int k = 0; k < steps; k++) rk.Step(f, k * dt, y, dt);
            double rem = period - steps * dt;
            if (rem > 1e-15) rk.Step(f, steps * dt, y, rem);

            double dr = Math.Sqrt(
                (y[0] - rx0) * (y[0] - rx0) + (y[1] - ry0) * (y[1] - ry0) + (y[2] - rz0) * (y[2] - rz0));
            Assert.LessOrEqual(dr, AbsKeplerPeriodM, "RK4 one-period position close abs 1e-3 m");
        }

        [Test]
        public void Handoff_CaptureRelease_Abs1mm()
        {
            var el = HighCircular();
            Kepler.ToState(el, out double rx0, out double ry0, out double rz0, out double vx0, out double vy0, out double vz0);
            var captured = Handoff.Capture(rx0, ry0, rz0, vx0, vy0, vz0);
            Handoff.Release(captured, out double rx, out double ry, out double rz, out _, out _, out _);
            double dr = Math.Sqrt((rx - rx0) * (rx - rx0) + (ry - ry0) * (ry - ry0) + (rz - rz0) * (rz - rz0));
            Assert.LessOrEqual(dr, AbsHandoffM, "ascent end → on-rails elements → state, abs 1 mm");
        }

        [Test]
        public void Handoff_SelectsOnRailsAbove600km()
        {
            var high = HighCircular();
            Assert.AreEqual(CoastMode.OnRails, Handoff.SelectCoast(high));
            Assert.GreaterOrEqual(Handoff.PeriapsisAltitudeM(high), Handoff.OnRailsPerigeeM);

            var low = new ClassicalElements
            {
                a = Gravity.R_EARTH + 400e3,
                e = 0.01,
                i = 51.6 * Math.PI / 180.0,
                raan = 0, argp = 0, m = 0,
            };
            // peri = a(1-e) − R ≈ 400e3 − 0.01*a ≈ 328 km < 600
            Assert.AreEqual(CoastMode.Numeric, Handoff.SelectCoast(low));
            Assert.Less(Handoff.PeriapsisAltitudeM(low), Handoff.OnRailsPerigeeM);
        }

        [Test]
        public void OnRails_AdvancesNodeOverTenDays()
        {
            // ISS-like: 420 km circular, 51.64°. First-order J2 Ω̇ ≈ −4.95 deg/day.
            double a = Gravity.R_EARTH + 420e3;
            var el = new ClassicalElements { a = a, e = 0, i = 51.64 * Math.PI / 180.0, raan = 0, argp = 0, m = 0 };
            OnRails.SecularRates(el.a, el.e, el.i, out _, out double dO, out _, out _);
            double degPerDay = dO * 86400.0 * 180.0 / Math.PI;
            Assert.AreEqual(-4.946640707855562, degPerDay, 1e-3, "nodeSecular external literal (P2)");

            var after = OnRails.Propagate(el, 10.0 * 86400.0);
            double dRaanDeg = (after.raan - el.raan) * 180.0 / Math.PI;
            if (dRaanDeg > 180) dRaanDeg -= 360;
            Assert.AreEqual(10.0 * degPerDay, dRaanDeg, 1e-6, "10-day integrated node");
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
    }
}
