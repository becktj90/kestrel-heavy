using System.Collections.Generic;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class PhysicsGoldenTests
    {
        const double REL = 1e-9, ABS = 1e-9;

        [Test]
        public void Atmosphere_ISA1976()
        {
            int n = 0;
            foreach (var c in Golden.Cases("atmosphere"))
            {
                double z = c.Obj("in").Num("z"); var o = c.Obj("out"); var a = Atmosphere.At(z);
                Golden.Near("atmosphere", a.T, o["T"], REL, ABS, $"T@{z}");
                Golden.Near("atmosphere", a.p, o["p"], REL, ABS, $"p@{z}");
                Golden.Near("atmosphere", a.rho, o["rho"], REL, 1e-15, $"rho@{z}");
                Golden.Near("atmosphere", a.a, o["a"], REL, ABS, $"a@{z}");
                Golden.Near("atmosphere", Atmosphere.Geopotential(z), o["h"], REL, ABS, $"h@{z}");
                n++;
            }
            Assert.That(n, Is.GreaterThanOrEqualTo(20));
        }

        [Test]
        public void RocketEquation()
        {
            foreach (var c in Golden.Cases("rocket-eq"))
            {
                var i = c.Obj("in"); string fn = c.Str("fn"); double got;
                switch (fn)
                {
                    case "deltaV": got = RocketEq.DeltaV(i.Num("isp"), i.Num("m0"), i.Num("mf")); break;
                    case "massFlow": got = RocketEq.MassFlow(i.Num("thrust"), i.Num("isp")); break;
                    case "ispAt": got = RocketEq.IspAt(i.Num("ispSL"), i.Num("ispVac"), i.Num("p")); break;
                    case "thrustAt": got = RocketEq.ThrustAt(i.Num("thrustVac"), i.Num("ispSL"), i.Num("ispVac"), i.Num("p")); break;
                    case "burnTime": got = RocketEq.BurnTime(i.Num("propMass"), i.Num("thrustVac"), i.Num("ispVac")); break;
                    default: Assert.Fail("unknown fn " + fn); return;
                }
                Golden.Near("rocket-eq", got, c["out"], REL, ABS, fn);
            }
        }

        [Test]
        public void AeroGravityEarth()
        {
            foreach (var c in Golden.Cases("aero-gravity-earth"))
            {
                var i = c.Obj("in"); string fn = c.Str("fn"); double got;
                switch (fn)
                {
                    case "cdMach": got = Aero.CdMach(i.Num("mach"), i.Num("cd0")); break;
                    case "dragForce": got = Aero.DragForce(i.Num("rho"), i.Num("v"), i.Num("a"), i.Num("cd0"), i.Num("area")); break;
                    case "gravityAccel": got = Gravity.Accel(i.Num("r")); break;
                    case "siteRotationSpeed": got = Earth.SiteRotationSpeed(i.Num("lat")); break;
                    default: Assert.Fail("unknown fn " + fn); return;
                }
                Golden.Near("aero-gravity-earth", got, c["out"], REL, ABS, fn);
            }
        }

        [Test]
        public void OrbitalElements_And_CircularVelocity()
        {
            foreach (var c in Golden.Cases("orbit"))
            {
                var i = c.Obj("in");
                if (c.Str("fn") == "circularVelocity") { Golden.Near("orbit", Orbit.CircularVelocity(i.Num("alt")), c["out"], REL, ABS, "vcirc"); continue; }
                var r = i.Arr("r"); var v = i.Arr("v"); var o = c.Obj("out");
                var e = Orbit.Elements((double)r[0], (double)r[1], (double)r[2], (double)v[0], (double)v[1], (double)v[2]);
                void N(string k, double x) => Golden.Near("orbit", x, o[k], REL, 1e-6, "orbit." + k);
                N("a", e.a); N("e", e.e); N("i", e.i); N("energy", e.energy); N("h", e.h); N("periapsis", e.periapsis);
                N("apoapsis", e.apoapsis); N("periAlt", e.periAlt); N("apoAlt", e.apoAlt); N("period", e.period);
            }
        }

        [Test]
        public void Integrator_RK4_ExactForCubic_And_FixedStepper()
        {
            // y' = 3t^2 -> y = t^3: RK4 is exact for polynomials up to degree 4 in t.
            var rk = new RK4(1); var y = new[] { 0.0 }; double t = 0;
            for (int k = 0; k < 100; k++) { rk.Step((tt, s, d) => d[0] = 3 * tt * tt, t, y, 0.01); t += 0.01; }
            Assert.AreEqual(1.0, y[0], 1e-12);
            int steps = 0; var fs = new FixedStepper((_, __) => steps++, 1.0 / 120);
            fs.Advance(1.0 / 60); fs.Advance(1.0 / 60);
            Assert.AreEqual(4, steps); Assert.That(fs.Alpha, Is.InRange(0.0, 1.0));
        }
    }
}
