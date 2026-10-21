// Apogee trips are scored on the flown, drag-aware peak of the coast, not the vacuum Kepler apoapsis at cutoff.
// The same value (MissionScore.FlownApoAltM) feeds FlightBatch, the designer's reference ascent (ReachesApogee)
// and the live report. KH-1's board starter cuts off at 4.6 km reading 9.74 km Kepler (a pass, band >= 9.5 km),
// then drag holds the coast to 6.58 km (a miss).
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class ApogeeDragTests
    {
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

        Design Starter(string id) => Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, campaign.ById(id)).Design);

        [Test]
        public void KH1StarterKeplerSaysPassTheFlownPeakSaysMiss()
        {
            var m = campaign.ById("m1");
            var d = Starter("m1");
            var sim = FlyBatchSetup(m, d, 1, MissionFlightSetup.BatchDt);
            Assert.AreEqual("suborbital", sim.outcome);
            Assert.AreEqual(9736, sim.orbit.apoAlt, 1, "vacuum Kepler apoapsis at cutoff");
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

        [Test]
        public void TheDesignerReadsTheSameFlownPeak()
        {
            ReferenceAscent.Reset();
            try
            {
                var json = HomeAutoBuild.Choose(catalog, hangar.templates, campaign.ById("m1")).Design;
                var job = new ReferenceJob(catalog, json);
                while (!job.Step(1000)) { }
                var readout = Readout.Quick(catalog, json);
                Assert.IsTrue(readout.predictedApo_m.HasValue);
                var batch = FlightBatch.Fly(catalog, campaign.ById("m1"), Design.FromJson(json), 1);
                Assert.AreEqual(batch.apoKm * 1000, readout.predictedApo_m.Value, 1e-6, "designer and batch: one function");
                Assert.IsFalse(readout.ReachesApogee, "6.6 km of a 10 km goal");
            }
            finally { ReferenceAscent.Reset(); }
        }

        [TestCase("m1", 1.0 / 120)]
        [TestCase("m1", 0.2)]
        [TestCase("m2", 1.0 / 120)]
        [TestCase("m2", 0.2)]
        public void DragOnlyLowersThePeakAndTheCoastIntegratorMatchesKeplerInVacuum(string id, double dt)
        {
            var m = campaign.ById(id);
            var sim = FlyBatchSetup(m, Starter(id), 1, dt);
            Assert.That(sim.coastApexAlt, Is.LessThan(sim.orbit.apoAlt));
            Assert.That(sim.coastApexAlt, Is.GreaterThan(sim.orbit.periAlt));

            var setup = MissionFlightSetup.Build(catalog, m, Starter(id), dt);
            var ascent = new AscentSim(setup.Stages, setup.Options);
            while (ascent.Tick()) { }
            setup.Options.vacuum = true;
            Assert.AreEqual(ascent.Finish().orbit.apoAlt, ascent.CoastApexAlt(), 1e-6, "no drag: the coast peak is the Kepler apoapsis");
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
