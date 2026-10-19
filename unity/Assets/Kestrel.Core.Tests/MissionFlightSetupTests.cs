// B-01 and B-03: the live pad flight and the campaign batch build their sim inputs in one place
// (MissionFlightSetup). The live run used to stop at T+600 s, before any orbital winner reached
// orbit (docs/audits/DESIGNER_MISSION_DISCREPANCIES.md: insertion at T+525..900 s).
// The expected orbits come from data/campaign.json and the ±5% pass band, not from this sim.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class MissionFlightSetupTests
    {
        /// <summary>The old live cap from Slice/FlightDriver.cs.</summary>
        const double OldLiveTMax = 600;

        Catalog catalog;
        Campaign campaign;

        [SetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
        }

        List<(CampaignMission mission, Design design)> Winners()
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-winners.json")));
            var list = new List<(CampaignMission, Design)>();
            foreach (JsonObject row in file.Arr("missions"))
            {
                var mission = campaign.ById(row.Str("id"));
                Assert.IsNotNull(mission, row.Str("id"));
                list.Add((mission, Design.FromJson((JsonObject)row.Arr("designs")[0])));
            }
            return list;
        }

        static SimResult FlyLive(Catalog catalog, MissionFlightInputs setup, Design design, ulong seed, double tMax)
        {
            // What FlightDriver does with no director: the seeded monitor, then fixed ticks until done.
            setup.Options.monitor = FailureMonitor.Create(catalog, setup.Flown, setup.TargetAltM, FlightBatch.DesignHash(design), seed);
            setup.Options.monitor.Gentle = false;
            setup.Options.tMax = tMax;
            var sim = new AscentSim(setup.Stages, setup.Options);
            while (!sim.Done) sim.Tick();
            return sim.Finish();
        }

        static readonly HashSet<string> StepOnly = new HashSet<string> { "dt", "monitor", "guidance" };

        /// <summary>Every public SimOptions field except the step and the caller's failure monitor.</summary>
        static void AssertSameOptions(SimOptions live, SimOptions batch, string where)
        {
            foreach (var f in typeof(SimOptions).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (StepOnly.Contains(f.Name)) continue;
                Assert.AreEqual(f.GetValue(batch), f.GetValue(live), where + " SimOptions." + f.Name);
            }
            Assert.AreEqual(batch.guidance == null, live.guidance == null, where + " guidance");
            if (batch.guidance != null)
            {
                Assert.AreEqual(batch.guidance.targetAlt, live.guidance.targetAlt, where + " guidance.targetAlt");
                Assert.AreEqual(Headless.LegacyPitch(batch.guidance), Headless.LegacyPitch(live.guidance), where + " guidance pitch");
            }
        }

        static void AssertSameStages(IList<SimStage> live, IList<SimStage> batch, string where)
        {
            Assert.AreEqual(batch.Count, live.Count, where + " stage count");
            for (int i = 0; i < batch.Count; i++)
            {
                foreach (var f in typeof(SimStage).GetFields(BindingFlags.Public | BindingFlags.Instance))
                    Assert.AreEqual(f.GetValue(batch[i]), f.GetValue(live[i]), where + " stage " + i + " " + f.Name);
            }
        }

        [Test]
        public void LiveAndBatch_BuildTheSameInputs_ExceptTheStep()
        {
            int seen = 0;
            foreach (var (mission, design) in Winners())
            {
                var live = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt);
                var batch = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.BatchDt);
                Assert.AreEqual(1.0 / 120, live.Options.dt, mission.Id);
                Assert.AreEqual(0.2, batch.Options.dt, mission.Id);
                AssertSameOptions(live.Options, batch.Options, mission.Id);
                AssertSameStages(live.Stages, batch.Stages, mission.Id);
                Assert.AreEqual(batch.TargetAltM, live.TargetAltM, mission.Id);
                Assert.AreEqual(batch.BoilOffKg, live.BoilOffKg, mission.Id);
                // The batch flies exactly this setup.
                var flight = FlightBatch.Fly(catalog, mission, design, campaign.Seeds[0]);
                Assert.AreEqual(batch.BoilOffKg, flight.boilOffKg, mission.Id + " batch boil-off");
                seen++;
            }
            Assert.AreEqual(20, seen);
        }

        [Test]
        public void Kh4Winner_BatchStepSetup_IsWhatFlightBatchFlies()
        {
            var (mission, design) = Winners().Find(w => w.mission.Id == "m4");
            var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.BatchDt);
            setup.Options.monitor = FailureMonitor.Create(catalog, setup.Flown, setup.TargetAltM, FlightBatch.DesignHash(design), campaign.Seeds[0]);
            setup.Options.monitor.Gentle = false;
            var sim = Headless.Simulate(setup.Stages, setup.Options);
            var flight = FlightBatch.Fly(catalog, mission, design, campaign.Seeds[0]);
            Assert.AreEqual(flight.apoKm, sim.orbit.apoAlt / 1000.0, "bit-exact on one platform");
            Assert.AreEqual(flight.periKm, sim.orbit.periAlt / 1000.0, "bit-exact on one platform");
        }

        [Test]
        public void Setup_RunsAtLeast2500s_FromStackSimInputs()
        {
            foreach (var (mission, design) in Winners())
            {
                double fromStack = Stack.SimInputs(catalog, design, MissionFlightSetup.LiveDt).opts.tMax;
                var live = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt);
                Assert.GreaterOrEqual(live.Options.tMax, 2500, mission.Id);
                Assert.AreEqual(fromStack, live.Options.tMax, mission.Id + " tMax comes from Stack.SimInputs");
                var free = MissionFlightSetup.Build(catalog, null, design, MissionFlightSetup.LiveDt);
                Assert.GreaterOrEqual(free.Options.tMax, 2500, mission.Id + " free build");
            }
        }

        [Test]
        public void Setup_HoldsOutCoastBoilOffAndStrapOnLandingFuel()
        {
            var winners = Winners();
            // KH-10 lands the strap-ons: 14,000 kg loaded, 2,800 kg kept for landing (campaign-winners.json).
            var (m10, d10) = winners.Find(w => w.mission.Id == "m10");
            Assert.IsTrue(m10.LandBoosters);
            var s10 = MissionFlightSetup.Build(catalog, m10, d10, MissionFlightSetup.LiveDt);
            Assert.AreEqual(14000, d10.strapOns.propMass, "the design is not changed");
            Assert.AreEqual(14000 - 2800, s10.Flown.strapOns.propMass, 1e-9);
            // KH-9 coasts 3 h (campaign.json). The upper stage loses that boil-off before launch.
            var (m9, d9) = winners.Find(w => w.mission.Id == "m9");
            Assert.AreEqual(3, m9.CoastHours);
            var s9 = MissionFlightSetup.Build(catalog, m9, d9, MissionFlightSetup.LiveDt);
            int top = d9.stages.Count - 1;
            Assert.Greater(s9.BoilOffKg, 0);
            Assert.AreEqual(d9.stages[top].propMass - s9.BoilOffKg, s9.Flown.stages[top].propMass, 1e-9);
            // A free build holds nothing out.
            var free = MissionFlightSetup.Build(catalog, null, d10, MissionFlightSetup.LiveDt);
            Assert.AreEqual(14000, free.Flown.strapOns.propMass);
            Assert.AreEqual(0, free.BoilOffKg);
        }

        [Test]
        public void Setup_ApogeeHop_KeepsTheMissionAimAndCutoff()
        {
            var (m1, d1) = Winners().Find(w => w.mission.Id == "m1");
            var raw = Stack.SimInputs(catalog, d1, MissionFlightSetup.LiveDt).opts;
            Assert.IsTrue(MissionAim.Apply(raw, m1));
            var setup = MissionFlightSetup.Build(catalog, m1, d1, MissionFlightSetup.LiveDt);
            Assert.AreEqual(1000, setup.Options.targetAlt, "KH-1 depletion floor (CampaignMission.GuidanceAltM)");
            Assert.AreEqual(raw.targetAlt, setup.Options.targetAlt);
            Assert.AreEqual(raw.cutoff, setup.Options.cutoff);
        }

        [Test]
        public void Setup_ReferencePadIsCalm28p5_SiteOverridesLatitude()
        {
            var (m4, d4) = Winners().Find(w => w.mission.Id == "m4");
            var reference = MissionFlightSetup.Build(catalog, m4, d4, MissionFlightSetup.LiveDt);
            Assert.AreEqual(28.5, reference.Options.latitude);
            Assert.IsNull(reference.Options.wind);
            // North Reach, 57.5° (the briefing pads in the audit).
            var site = MissionFlightSetup.Build(catalog, m4, d4, MissionFlightSetup.LiveDt, new MissionFlightSite { HasLatitude = true, LatitudeDeg = 57.5 });
            Assert.AreEqual(57.5, site.Options.latitude);
            Assert.IsNull(site.Options.wind);
        }

        [Test]
        public void Kh4Winner_LiveStep_ReachesOrbit_PastTheOld600sCap()
        {
            var (mission, design) = Winners().Find(w => w.mission.Id == "m4");
            // campaign.json: KH-4 is a 500 km circular orbit; the pass band is ±5%.
            Assert.AreEqual("circular", mission.TargetKind);
            Assert.AreEqual(500, mission.ApoKm);
            Assert.AreEqual(500, mission.PeriKm);
            var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt);
            var flown = FlyLive(catalog, setup, design, campaign.Seeds[0], setup.Options.tMax);
            Assert.AreEqual("orbit", flown.outcome);
            Assert.IsNull(flown.failure);
            Assert.Greater(flown.time, OldLiveTMax, "insertion is after the old live cap");
            Assert.That(flown.orbit.apoAlt / 1000.0, Is.InRange(475.0, 525.0));
            Assert.That(flown.orbit.periAlt / 1000.0, Is.InRange(475.0, 525.0));

            // The old cap: the same flight stopped at T+600 s, suborbital (audit B-01).
            var capped = FlyLive(catalog, MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt), design, campaign.Seeds[0], OldLiveTMax);
            Assert.AreEqual("timeout", capped.outcome);
            Assert.Less(capped.orbit.periAlt, 0);
        }

        [Test]
        public void EveryOrbitalWinner_LiveStep_ReachesOrbit()
        {
            int seen = 0;
            foreach (var (mission, design) in Winners())
            {
                if (!mission.HasInclination) continue;
                var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt);
                var flown = FlyLive(catalog, setup, design, campaign.Seeds[0], setup.Options.tMax);
                Assert.AreEqual("orbit", flown.outcome, mission.Id + " t=" + flown.time.ToString("0"));
                Assert.Greater(flown.orbit.periAlt, 120e3, mission.Id);
                seen++;
            }
            Assert.AreEqual(18, seen);
        }

        [Test]
        public void FlightDriver_UsesTheSharedSetup_AndNoFixedRunLength()
        {
            string src = File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Slice", "Runtime", "FlightDriver.cs"));
            StringAssert.Contains("MissionFlightSetup.Build(", src);
            StringAssert.DoesNotContain("opts.tMax =", src);
            StringAssert.DoesNotContain("tMax = 600", src);
            StringAssert.DoesNotContain("Stack.SimInputs(", src);
        }
    }
}
