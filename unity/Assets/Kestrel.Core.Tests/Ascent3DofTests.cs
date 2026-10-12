using System;
using System.Collections.Generic;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class Ascent3DofTests
    {
        const double Rel = 1e-9;
        const double Abs = 1e-9;

        [Test]
        public void IssFromCape_InertialBranches()
        {
            double north = LaunchAzimuth.InertialNortherlyDeg(51.6, 28.5);
            double south = LaunchAzimuth.InertialSoutherlyDeg(51.6, 28.5);
            Assert.AreEqual(44.9751330989884, north, 0.01, "northerly");
            Assert.AreEqual(135.0248669010116, south, 0.01, "southerly");
            double s = Math.Cos(51.6 * Math.PI / 180) / Math.Cos(28.5 * Math.PI / 180);
            Assert.AreEqual(Math.Asin(s) * 180 / Math.PI, north, 1e-12, "closed form");
            Assert.AreEqual(180 - north, south, 1e-12, "southerly is the supplement");
            Assert.AreEqual(north, LaunchAzimuth.InertialNortherlyDeg(51.6, -28.5), 1e-12, "cosine of latitude is even");
        }

        [Test]
        public void IssFromCape_RotatingBranches()
        {
            double speed = Orbit.CircularVelocity(0);
            double radius = Gravity.R_EARTH;
            double north = LaunchAzimuth.RotatingNortherlyDeg(51.6, 28.5, speed, radius);
            double south = LaunchAzimuth.RotatingSoutherlyDeg(51.6, 28.5, speed, radius);
            Assert.AreEqual(42.801031904016824, north, 1e-6, "northerly rotating");
            Assert.AreEqual(137.1989680959832, south, 1e-6, "southerly rotating");
            Assert.AreEqual(Heading(LaunchAzimuth.InertialNortherlyDeg(51.6, 28.5), 28.5, speed, radius), north, 1e-9);
            Assert.AreEqual(Heading(LaunchAzimuth.InertialSoutherlyDeg(51.6, 28.5), 28.5, speed, radius), south, 1e-9);
        }

        [Test]
        public void DueEast_WhenInclinationEqualsLatitude()
        {
            Assert.AreEqual(90, LaunchAzimuth.InertialNortherlyDeg(28.5, 28.5), 1e-9);
            Assert.AreEqual(90, LaunchAzimuth.InertialSoutherlyDeg(28.5, 28.5), 1e-9);
            Assert.AreEqual(0, LaunchAzimuth.InertialNortherlyDeg(90, 28.5), 1e-12);
            Assert.AreEqual(180, LaunchAzimuth.InertialSoutherlyDeg(90, 28.5), 1e-12);
            double speed = Orbit.CircularVelocity(0);
            Assert.AreEqual(90, LaunchAzimuth.RotatingNortherlyDeg(28.5, 28.5, speed, Gravity.R_EARTH), 1e-9);
        }

        [Test]
        public void UnreachableInclination_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LaunchAzimuth.InertialNortherlyDeg(0, 28.5));
            Assert.Throws<ArgumentOutOfRangeException>(() => LaunchAzimuth.InertialSoutherlyDeg(0, 28.5));
            Assert.Throws<ArgumentOutOfRangeException>(() => LaunchAzimuth.RotatingNortherlyDeg(0, 28.5, 7500, Gravity.R_EARTH));
            Assert.Throws<ArgumentOutOfRangeException>(() => LaunchAzimuth.RotatingSoutherlyDeg(10, 28.5, 7500, Gravity.R_EARTH));
            Assert.Throws<ArgumentOutOfRangeException>(() => LaunchAzimuth.InertialNortherlyDeg(51.6, 90));
        }

        static double Heading(double inertialAzDeg, double latitudeDeg, double speed, double radius)
        {
            double beta = inertialAzDeg * Math.PI / 180.0;
            double vNorth = speed * Math.Cos(beta);
            double vEast = speed * Math.Sin(beta) - Earth.OMEGA_EARTH * radius * Math.Cos(latitudeDeg * Math.PI / 180.0);
            double deg = Math.Atan2(vEast, vNorth) * 180.0 / Math.PI;
            if (deg < 0) deg += 360.0;
            return deg;
        }

        [Test]
        public void Planar_MatchesHeadless_OnSimulateCases()
        {
            int n = 0;
            foreach (var c in Golden.Cases("simulate"))
            {
                n++;
                var inp = c.Obj("in"); var oo = inp.Obj("opts");
                var stages = new List<SimStage>();
                foreach (JsonObject s in inp.Arr("stages"))
                    stages.Add(new SimStage { thrustSL = s.Num("thrustSL"), thrustVac = s.Num("thrustVac"), ispSL = s.Num("ispSL"), ispVac = s.Num("ispVac"), propMass = s.Num("propMass"), dryMass = s.Num("dryMass"), diameter = s.Num("diameter"), cd = s.Num("cd", 0.3) });
                var opts = new SimOptions
                {
                    payload = oo.Num("payload", 0), targetAlt = oo.Num("targetAlt", 200e3), latitude = oo.Num("latitude", 28.5), dt = oo.Num("dt", 0.05),
                    tMax = oo.Num("tMax", 3000), sampleEvery = oo.Num("sampleEvery", 0), vacuum = oo.Bool("vacuum", false),
                    rotate = !oo.ContainsKey("rotate") || oo.Bool("rotate"), cutoff = !oo.ContainsKey("cutoff") || oo.Bool("cutoff"),
                };
                Compare(c.Str("name"), stages, opts);
            }
            Assert.AreEqual(13, n);
        }

        [Test]
        public void Planar_MatchesHeadless_OnCatalogAscents()
        {
            var doc = Golden.File("catalog-flight");
            int n = 0;
            foreach (JsonObject c in doc.Arr("cases"))
            {
                if (c.Str("kind") != "ascent") continue;
                n++;
                var stages = new List<SimStage>();
                foreach (JsonObject s in c.Obj("in").Arr("stages"))
                    stages.Add(new SimStage { thrustSL = s.Num("thrustSL"), thrustVac = s.Num("thrustVac"), ispSL = s.Num("ispSL"), ispVac = s.Num("ispVac"), propMass = s.Num("propMass"), dryMass = s.Num("dryMass"), diameter = s.Num("diameter"), cd = s.Num("cd", 0.3) });
                var o = c.Obj("in").Obj("opts");
                var opts = new SimOptions
                {
                    payload = o.Num("payload"), targetAlt = o.Num("targetAlt", 200e3), latitude = o.Num("latitude", 28.5),
                    dt = o.Num("dt", 0.05), tMax = o.Num("tMax", 3000), sampleEvery = o.Num("sampleEvery"),
                    vacuum = o.Bool("vacuum"), rotate = !o.ContainsKey("rotate") || o.Bool("rotate"), cutoff = !o.ContainsKey("cutoff") || o.Bool("cutoff"),
                };
                Compare(c.Obj("subject").Str("id"), stages, opts);
            }
            Assert.AreEqual(25, n);
        }

        static void Compare(string name, IList<SimStage> stages, SimOptions opts)
        {
            var a = Headless.Simulate(stages, opts);
            var sim = new Ascent3Dof(stages, opts);
            while (sim.Tick()) { }
            var b = sim.Finish();
            Assert.AreEqual(0.0, sim.Y[2], name + " z");
            Assert.AreEqual(0.0, sim.Y[5], name + " vz");
            Assert.AreEqual(a.outcome, b.outcome, name);
            Close(a.time, b.time, name + ".time");
            Close(a.maxQ, b.maxQ, name + ".maxQ");
            Close(a.maxQTime, b.maxQTime, name + ".maxQTime");
            Close(a.maxG, b.maxG, name + ".maxG");
            Close(a.lossGravity, b.lossGravity, name + ".lossG");
            Close(a.lossDrag, b.lossDrag, name + ".lossD");
            Close(a.lossSteering, b.lossSteering, name + ".lossS");
            Close(a.idealDeltaV, b.idealDeltaV, name + ".idealDV");
            Close(a.deltaVExpended, b.deltaVExpended, name + ".dv");
            Close(a.finalMass, b.finalMass, name + ".m");
            Close(a.x, b.x, name + ".x"); Close(a.y, b.y, name + ".y");
            Close(a.vx, b.vx, name + ".vx"); Close(a.vy, b.vy, name + ".vy"); Close(a.m, b.m, name + ".stateM");
            Close(a.orbit.a, b.orbit.a, name + ".a"); Close(a.orbit.e, b.orbit.e, name + ".e");
            Close(a.orbit.i, b.orbit.i, name + ".i");
            Close(a.orbit.periAlt, b.orbit.periAlt, name + ".peri");
            Close(a.orbit.apoAlt, b.orbit.apoAlt, name + ".apo");
            Assert.AreEqual(a.events.Count, b.events.Count, name + " events");
            for (int i = 0; i < a.events.Count; i++)
            {
                Assert.AreEqual(a.events[i].type, b.events[i].type, name + " event " + i);
                Assert.AreEqual(a.events[i].stage, b.events[i].stage, name + " stage " + i);
                Close(a.events[i].t, b.events[i].t, name + ".ev" + i + ".t");
                if (a.events[i].alt.HasValue) Close(a.events[i].alt.Value, b.events[i].alt.Value, name + ".ev" + i + ".alt");
            }
            Assert.AreEqual(a.samples.Count, b.samples.Count, name + " samples");
            for (int i = 0; i < a.samples.Count; i++)
            {
                var p = a.samples[i]; var q = b.samples[i];
                Close(p.t, q.t, name + ".s" + i + ".t");
                Close(p.alt, q.alt, name + ".s" + i + ".alt");
                Close(p.v, q.v, name + ".s" + i + ".v");
                Close(p.pitch, q.pitch, name + ".s" + i + ".pitch");
                Assert.AreEqual(p.stage, q.stage, name); Assert.AreEqual(p.mode, q.mode, name);
            }
        }

        static void Close(double expected, double actual, string what)
        {
            double diff = Math.Abs(actual - expected);
            double scale = Math.Max(Math.Abs(expected), Math.Abs(actual));
            double bound = Math.Max(Abs, Rel * scale);
            if (diff > bound)
                Assert.Fail(what + " expected " + expected.ToString("G17") + " actual " + actual.ToString("G17") + " diff " + diff.ToString("G17"));
        }
    }
}
