// Apogee trips are scored on the flown, drag-aware peak of the coast, not the vacuum Kepler apoapsis at cutoff.
// The same value (MissionScore.FlownApoAltM) feeds FlightBatch, the designer's reference ascent (ReachesApogee),
// the live report, the launch timeline (LaunchPredictor) and the live pad result (FlightDriver -> Sim.Finish).
//
// Measured before the retune (the old KH-1 board starter, one gg-kerolox-small, 345 kg): burnout at 4.5 km reads
// 9.736 km Kepler at the 0.2 s batch step (10.251 km at 1/120 s), a pass of the 9.5-10.5 km band, but drag holds the
// coast to 6.584 km at both steps. The old KH-2 starter (1380 kg) read 100.43 km Kepler and flew 91.44 km.
// The retune keeps the bands and adds propellant (KH-1 525 kg, KH-2 1415 kg).
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class ApogeeDragTests
    {
        /// <summary>Kestrel Physics: drag off, the coast integrator equals the Kepler apoapsis to this relative band.</summary>
        const double RelVacuum = 1e-6;
        /// <summary>Kestrel Physics: the flown peak at 0.2 s and 1/120 s agree to 0.5%.</summary>
        const double RelStep = 0.005;
        /// <summary>Kestrel Physics: the tutorial starter peaks at 9.7 km or more on every seed (band 9.5-10.5 km).</summary>
        const double Kh1FloorM = 9700;

        Catalog catalog;
        Campaign campaign;
        HangarData hangar;

        [OneTimeSetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
            HomeAutoBuild.InstallMissionOrbits(catalog, campaign);
        }

        SimResult FlyBatchSetup(CampaignMission m, Design d, ulong seed, double dt)
        {
            var setup = MissionFlightSetup.Build(catalog, m, d, dt);
            var mon = FailureMonitor.Create(catalog, setup.Flown, setup.TargetAltM, FlightBatch.DesignHash(d), seed);
            mon.Gentle = false;
            setup.Options.monitor = mon;
            return Headless.Simulate(setup.Stages, setup.Options);
        }

        JsonObject StarterJson(string id) => HomeAutoBuild.Choose(catalog, hangar.templates, campaign.ById(id)).Design;
        Design Starter(string id) => Design.FromJson(StarterJson(id));

        /// <summary>The pre-retune KH-1 board starter: the same fork with the old 345 kg load.</summary>
        Design OldKh1Starter()
        {
            var json = StarterJson("m1");
            JsonMut.Set((JsonObject)json.Arr("stages")[0], "propMass", 345d);
            return Design.FromJson(json);
        }

        [Test]
        public void TheOldKH1StarterReadAPassOnKeplerAndFlewShort()
        {
            var m = campaign.ById("m1");
            var d = OldKh1Starter();
            var sim = FlyBatchSetup(m, d, 1, MissionFlightSetup.BatchDt);
            Assert.AreEqual("suborbital", sim.outcome);
            Assert.AreEqual(9736, sim.orbit.apoAlt, 1, "vacuum Kepler apoapsis at burnout");
            Assert.AreEqual(6584, sim.coastApexAlt, 1, "drag-aware flown peak");
            Assert.IsTrue(MissionScore.AltitudeWithin(sim.orbit.apoAlt, m.ApoKm * 1000), "Kepler reads a pass");
            Assert.IsFalse(MissionScore.AltitudeWithin(sim.coastApexAlt, m.ApoKm * 1000), "the flight peaks short");
            Assert.AreEqual(sim.coastApexAlt, MissionScore.FlownApoAltM(sim));
            for (ulong seed = 1; seed <= 5; seed++)
            {
                var f = FlightBatch.Fly(catalog, m, d, seed);
                Assert.AreEqual(6.584, f.apoKm, 0.001, "batch scores the flown peak, seed " + seed);
                Assert.IsFalse(f.passed, "seed " + seed);
            }
        }

        [TestCase("m1", 9700, 10300)]
        [TestCase("m2", 97000, 103000)]
        public void TheRetunedStartersPeakMidBandOnEverySeed(string id, double lowM, double highM)
        {
            var m = campaign.ById(id);
            var d = Starter(id);
            Assert.That(lowM, Is.GreaterThan(m.ApoKm * 1000 * (1 - MissionScore.AltitudeFraction)), "the check sits inside the band");
            Assert.That(highM, Is.LessThan(m.ApoKm * 1000 * (1 + MissionScore.AltitudeFraction)), "the check sits inside the band");
            if (id == "m1") Assert.AreEqual(Kh1FloorM, lowM);
            for (ulong seed = 1; seed <= 5; seed++)
            {
                var f = FlightBatch.Fly(catalog, m, d, seed);
                Assert.IsTrue(f.passed, id + " seed " + seed + " apo " + f.apoKm.ToString("0.000") + " " + f.failure);
                Assert.IsNull(f.failure, id + " seed " + seed);
                Assert.That(f.apoKm * 1000, Is.InRange(lowM, highM), id + " seed " + seed);
            }
        }

        [Test]
        public void TheDesignerReadsTheSameFlownPeak()
        {
            ReferenceAscent.Reset();
            try
            {
                foreach (var json in new[] { StarterJson("m1"), OldKh1Starter().ToJson() })
                {
                    ReferenceAscent.Reset();
                    var job = new ReferenceJob(catalog, json);
                    while (!job.Step(1000)) { }
                    var readout = Readout.Quick(catalog, json);
                    Assert.IsTrue(readout.predictedApo_m.HasValue);
                    var batch = FlightBatch.Fly(catalog, campaign.ById("m1"), Design.FromJson(json), 1);
                    Assert.AreEqual(batch.apoKm * 1000, readout.predictedApo_m.Value, 1e-6, "designer and batch: one function");
                    Assert.AreEqual(batch.passed, readout.ReachesApogee, "designer Ready matches the batch at " + batch.apoKm.ToString("0.000") + " km");
                }
            }
            finally { ReferenceAscent.Reset(); }
        }

        [TestCase("m1", 1.0 / 120)]
        [TestCase("m1", 0.2)]
        [TestCase("m2", 1.0 / 120)]
        [TestCase("m2", 0.2)]
        public void DragOff_TheCoastEqualsKepler(string id, double dt)
        {
            var m = campaign.ById(id);
            var sim = FlyBatchSetup(m, Starter(id), 1, dt);
            Assert.That(sim.coastApexAlt, Is.LessThan(sim.orbit.apoAlt), "drag only lowers the peak");
            Assert.That(sim.coastApexAlt, Is.GreaterThan(sim.orbit.periAlt));

            var setup = MissionFlightSetup.Build(catalog, m, Starter(id), dt);
            var ascent = new AscentSim(setup.Stages, setup.Options);
            while (ascent.Tick()) { }
            double kepler = ascent.Finish().orbit.apoAlt;
            setup.Options.vacuum = true;
            double coast = ascent.CoastApexAlt();
            Assert.AreEqual(kepler, coast, RelVacuum * Math.Abs(kepler), "no drag: the coast peak is the Kepler apoapsis");
        }

        [TestCase("m1")]
        [TestCase("m2")]
        public void TheFlownPeakConvergesInStepSize(string id)
        {
            var m = campaign.ById(id);
            var d = Starter(id);
            double coarse = MissionScore.FlownApoAltM(FlyBatchSetup(m, d, 1, MissionFlightSetup.BatchDt));
            double fine = MissionScore.FlownApoAltM(FlyBatchSetup(m, d, 1, MissionFlightSetup.LiveDt));
            Assert.AreEqual(fine, coarse, RelStep * fine, id + " 0.2 s " + coarse.ToString("0") + " m vs 1/120 s " + fine.ToString("0") + " m");
        }

        /// <summary>
        /// Kestrel Physics: the coast uses the unpowered drag coefficient, never lower than the powered one, in the
        /// ascent's own atmosphere, rotation and wind. The sim has one Cd0 per body (Stack.BaseCd) and no plume or base-drag
        /// term, so the coast Cd is the powered Cd. An independent re-integration with that Cd reproduces the coast peak,
        /// and the same integration with the Cd cut by 10% peaks higher, so a lower coast Cd would be caught.
        /// </summary>
        [TestCase("m1", 0.2)]
        [TestCase("m1", 1.0 / 120)]
        [TestCase("m2", 0.2)]
        public void TheCoastUsesThePoweredDragCoefficientAndTheAscentAtmosphere(string id, double dt)
        {
            var m = campaign.ById(id);
            var d = Starter(id);
            var setup = MissionFlightSetup.Build(catalog, m, d, dt);
            Assert.AreEqual(1, setup.Stages.Count, "a single-stage hop: one body Cd powered and coasting");
            var ascent = new AscentSim(setup.Stages, setup.Options);
            while (ascent.Tick()) { }
            Assert.IsFalse(ascent.Thrusting);
            double cd = setup.Stages[0].cd;
            Assert.AreEqual(Stack.BaseCd(d), cd, 1e-12, "powered Cd0 is the body build-up");
            double coast = ascent.CoastApexAlt();
            double same = Reintegrate(ascent.Y, ascent.T, setup.Options, cd, setup.Stages[0].diameter, dt);
            double lower = Reintegrate(ascent.Y, ascent.T, setup.Options, 0.9 * cd, setup.Stages[0].diameter, dt);
            Assert.AreEqual(same, coast, 1e-9 * same, "coast Cd0 " + cd.ToString("0.000") + " equals the powered Cd0");
            Assert.That(lower, Is.GreaterThan(coast + 1), "a lower coast Cd would raise the peak");
        }

        /// <summary>Test-side coast: central gravity, ISA 1976, Earth-rotating air, Cd(Mach) on the given Cd0. No thrust.</summary>
        static double Reintegrate(double[] y0, double t0, SimOptions o, double cd0, double diameter, double dt)
        {
            double omega = o.rotate ? Earth.SiteRotationSpeed(o.latitude) / Gravity.R_EARTH : 0;
            double area = Math.PI * diameter * diameter / 4;
            var s = (double[])y0.Clone();
            var rk = new RK4(5);
            Deriv f = (t, y, dy) =>
            {
                double r2 = y[0] * y[0] + y[1] * y[1], r = Math.Sqrt(r2), gk = -Gravity.MU_EARTH / (r2 * r);
                double ax = gk * y[0], ay = gk * y[1];
                double alt = r - Gravity.R_EARTH;
                if (alt < 300e3)
                {
                    var atm = Atmosphere.At(alt);
                    double avx = y[2] + omega * y[1], avy = y[3] - omega * y[0];
                    double v = Math.Sqrt(avx * avx + avy * avy);
                    if (atm.rho > 0 && v > 0)
                    {
                        double D = 0.5 * atm.rho * v * v * Aero.CdMach(v / atm.a, cd0) * area;
                        ax -= D * avx / (v * y[4]); ay -= D * avy / (v * y[4]);
                    }
                }
                dy[0] = y[2]; dy[1] = y[3]; dy[2] = ax; dy[3] = ay; dy[4] = 0;
            };
            double tt = t0;
            for (int i = 0; i < 400000; i++)
            {
                double r = Math.Sqrt(s[0] * s[0] + s[1] * s[1]);
                if ((s[2] * s[0] + s[3] * s[1]) / r <= 0) return r - Gravity.R_EARTH;
                rk.Step(f, tt, s, dt);
                tt += dt;
            }
            return double.NaN;
        }

        /// <summary>
        /// FlightDriver's path: MissionFlightSetup at the 1/120 s pad step, Tick until done, then Sim.Finish(). Its
        /// Result carries the same drag-aware peak the batch scores, within the step band, and the report reads it.
        /// </summary>
        [TestCase("m1")]
        [TestCase("m2")]
        public void TheLivePadResultPeaksAtTheBatchValue(string id)
        {
            var m = campaign.ById(id);
            var d = Starter(id);
            var site = new MissionFlightSite { HasLatitude = true, LatitudeDeg = HomeAutoBuild.LatitudeDeg, Wind = null };
            var setup = MissionFlightSetup.Build(catalog, m, d, FlightDriverStep, site);
            setup.Options.monitor = FailureMonitor.Create(catalog, setup.Flown, setup.TargetAltM, FlightBatch.DesignHash(d), 1);
            var sim = new AscentSim(setup.Stages, setup.Options);
            double clock = 0;
            int guard = 0;
            while (!sim.Done && guard++ < 1000000)
            {
                clock += 1.0 / 60;
                while (sim.T < clock && !sim.Done) sim.Tick();
            }
            var result = sim.Finish();
            double live = MissionScore.FlownApoAltM(result);
            var batch = FlightBatch.Fly(catalog, m, d, 1);
            Assert.IsFalse(double.IsNaN(result.coastApexAlt), "a clean hop carries the flown peak");
            Assert.AreEqual(batch.apoKm * 1000, live, RelStep * live, id + " live " + live.ToString("0") + " m");
            Assert.IsTrue(MissionScore.AltitudeWithin(live, m.ApoKm * 1000), id);
        }

        const double FlightDriverStep = MissionFlightSetup.LiveDt;

        [Test]
        public void TheTimelineShowsTheSameFlownApogee()
        {
            foreach (var json in new[] { StarterJson("m1"), OldKh1Starter().ToJson() })
            {
                var d = Design.FromJson(json);
                var p = LaunchPredictor.Predict(catalog, d, "standard", 1);
                var input = LaunchPredictor.Inputs(catalog, d);
                var sim = Headless.Simulate(input.stages, input.opts);
                double flown = MissionScore.FlownApoAltM(sim);
                var batch = FlightBatch.Fly(catalog, campaign.ById("m1"), d, 1);
                Assert.AreEqual(batch.apoKm * 1000, flown, 1e-6, "timeline and batch: one apogee");
                Assert.AreEqual(batch.passed, p.Success, "timeline pass matches the batch");
                Assert.AreEqual(Math.Floor(flown / 100 + 1e-9) / 10, p.ApoKm, 1e-9, "timeline apogee is the flown peak");
                Assert.That(flown, Is.LessThan(sim.orbit.apoAlt));
            }
        }

        [Test]
        public void OrbitsAndFailuresKeepTheKeplerApoapsis()
        {
            var m = campaign.ById("m3");
            var sim = FlyBatchSetup(m, Starter("m3"), 1, MissionFlightSetup.BatchDt);
            Assert.That(sim.orbit.periAlt, Is.GreaterThan(0));
            Assert.IsTrue(double.IsNaN(sim.coastApexAlt));
            Assert.AreEqual(sim.orbit.apoAlt, MissionScore.FlownApoAltM(sim));
            Assert.IsTrue(double.IsNaN(MissionScore.FlownApoAltM(null)));
        }
    }
}
