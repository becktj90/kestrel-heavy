// F1 (NAV_BUGS_LIVE): DESIGN FIRST on KH-2 showed "848 m/s to spare" and TWR 4.39, but Launch was greyed
// with "Your rocket ran out of fuel at T+0 s". Root cause: the designer's reference ascent aimed the apogee
// trip at its apogee (100 km), so the predict run called burnout at 20 km "out of fuel", and the risk row
// printed that at T+0. The pad flight aims at the 1 km hop floor (MissionAim). Also: only a validation block
// disables Launch now; a predicted failure asks once and flies. And KH-2's trip is a hop, not LEO 200 km.
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class LaunchGateHopTests
    {
        Catalog catalog;
        HangarData hangar;
        Campaign campaign;

        [SetUp]
        public void Load()
        {
            GameSession.Clear();
            PadHandoff.Clear();
            ReferenceAscent.Reset();
            catalog = Catalog.LoadFromRepo(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
        }

        [TearDown]
        public void Clear()
        {
            ReferenceAscent.Reset();
            GameSession.Clear();
            PadHandoff.Clear();
        }

        /// <summary>The design DESIGN FIRST hands the designer, with the reference ascent run the way the panel's ReferenceJob does.</summary>
        LaunchGateResult DesignFirstGate(int missionIndex, out QuickReadout readout, out JsonObject design)
        {
            var mission = campaign.Mission(missionIndex);
            var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
            Assert.IsNotNull(choice?.Design, mission.Id);
            design = choice.Design;
            var job = new ReferenceJob(catalog, design);
            while (!job.Step(1000)) { }
            readout = Readout.Quick(catalog, design);
            Assert.IsFalse(readout.maxQStale, "the reference ascent is cached");
            return LaunchGate.Evaluate(Validate.Engineering(catalog, design), readout);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void DesignFirstHopStarterIsReady(int missionIndex)
        {
            var gate = DesignFirstGate(missionIndex, out var readout, out _);
            Assert.That(readout.margin, Is.GreaterThan(0), "the panel shows speed to spare");
            Assert.That(readout.predictedFailure, Is.Null, "the hop coasts to apogee after burnout; it is not out of fuel");
            Assert.That(gate.state, Is.EqualTo(LaunchGateState.Ready), gate.Line);
            Assert.That(gate.CanLaunch);
        }

        [Test]
        public void ReferenceAscentAimsAHopAtTheSameFloorAsThePadFlight()
        {
            var mission = campaign.Mission(2);
            var design = HomeAutoBuild.Choose(catalog, hangar.templates, mission).Design;
            var reference = ReferenceAscent.Inputs(catalog, design);
            var pad = MissionFlightSetup.Build(catalog, mission, Design.FromJson(design), MissionFlightSetup.BatchDt);
            Assert.That(reference.opts.targetAlt, Is.EqualTo(pad.Options.targetAlt), "predict and pad judge depletion at the same altitude");
            Assert.That(reference.opts.targetAlt, Is.EqualTo(MissionAim.HopDepletionFloorM));
            Assert.That(mission.GuidanceAltM, Is.EqualTo(MissionAim.HopDepletionFloorM));
        }

        [Test]
        public void ReferenceAscentLeavesOrbitTripsOnTheirOrbit()
        {
            var mission = campaign.Mission(3);
            var design = HomeAutoBuild.Choose(catalog, hangar.templates, mission).Design;
            var reference = ReferenceAscent.Inputs(catalog, design);
            var pad = MissionFlightSetup.Build(catalog, mission, Design.FromJson(design), MissionFlightSetup.BatchDt);
            Assert.That(pad.TargetAltM, Is.EqualTo(200000), "KH-3 is a 200 km circle");
            Assert.That(reference.opts.targetAlt, Is.EqualTo(pad.Options.targetAlt));
            Assert.That(reference.opts.guidance?.targetAlt, Is.EqualTo(pad.Options.guidance?.targetAlt));
            Assert.IsFalse(MissionAim.IsSuborbital(catalog, design.Obj("payload").Str("orbit")));
        }

        /// <summary>
        /// F1 root cause: the designer's predict run built its own Stack.SimInputs instead of the mission setup the
        /// pad and FlightBatch fly. Every campaign mission's DESIGN FIRST rocket and golden winner now gets exactly
        /// the MissionFlightSetup inputs (aim, cutoff, run length, boil-off, held-out landing fuel), Predict aside.
        /// </summary>
        [Test]
        public void ReferenceAscentFliesTheMissionFlightSetup()
        {
            var winners = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-winners.json")));
            var designs = new List<(CampaignMission, JsonObject)>();
            foreach (var m in campaign.Missions)
            {
                var choice = HomeAutoBuild.Choose(catalog, hangar.templates, m);
                if (choice?.Design != null) designs.Add((m, choice.Design));
            }
            foreach (JsonObject row in winners.Arr("missions"))
                designs.Add((campaign.ById(row.Str("id")), (JsonObject)row.Arr("designs")[0]));
            Assert.That(designs.Count, Is.GreaterThanOrEqualTo(40));
            foreach (var (mission, json) in designs)
            {
                string at = mission.Id + " " + json.Str("name");
                Assert.That(ReferenceAscent.MissionFor(catalog, Design.FromJson(json))?.Id, Is.EqualTo(mission.Id), at);
                var reference = ReferenceAscent.Inputs(catalog, json);
                var pad = MissionFlightSetup.Build(catalog, mission, Design.FromJson(json), MissionFlightSetup.BatchDt);
                Assert.That(reference.opts.targetAlt, Is.EqualTo(pad.Options.targetAlt), at + " targetAlt");
                Assert.That(reference.opts.guidance?.targetAlt, Is.EqualTo(pad.Options.guidance?.targetAlt), at + " guidance");
                Assert.That(reference.opts.cutoff, Is.EqualTo(pad.Options.cutoff), at + " cutoff");
                Assert.That(reference.opts.tMax, Is.EqualTo(pad.Options.tMax), at + " tMax");
                Assert.That(reference.opts.dt, Is.EqualTo(pad.Options.dt), at + " dt");
                Assert.That(reference.stages.Count, Is.EqualTo(pad.Stages.Count), at + " stages");
                for (int i = 0; i < pad.Stages.Count; i++)
                    Assert.That(reference.stages[i].propMass, Is.EqualTo(pad.Stages[i].propMass), at + " stage " + i + " prop");
                Assert.That(reference.opts.monitor?.Predict ?? true, at + " predict");
            }
        }

        [Test]
        public void AFreeBuildHopAimsAtTheHopFloorOnThePadToo()
        {
            var design = HomeAutoBuild.Choose(catalog, hangar.templates, campaign.Mission(2)).Design;
            var free = JsonMut.CloneObject(design);
            JsonMut.Set(free, "missionId", null);
            JsonMut.Set(free.Obj("payload"), "orbit", "suborbital");
            var d = Design.FromJson(free);
            Assert.IsNull(ReferenceAscent.MissionFor(catalog, d), "a free build has no mission");
            var pad = MissionFlightSetup.Build(catalog, null, d, MissionFlightSetup.LiveDt);
            Assert.That(pad.Options.targetAlt, Is.EqualTo(MissionAim.HopDepletionFloorM), "the live free-build hop is judged at the floor");
            Assert.That(pad.TargetAltM, Is.EqualTo(MissionAim.HopDepletionFloorM));
            Assert.That(ReferenceAscent.Inputs(catalog, free).opts.targetAlt, Is.EqualTo(pad.Options.targetAlt));
            var orbit = JsonMut.CloneObject(free);
            JsonMut.Set(orbit.Obj("payload"), "orbit", "leo200");
            Assert.That(MissionFlightSetup.Build(catalog, null, Design.FromJson(orbit), MissionFlightSetup.LiveDt).Options.targetAlt, Is.EqualTo(200000), "a free orbit build is unchanged");
        }

        [Test]
        public void ARealFuelShortfallSaysWhenItHappened()
        {
            // Strip the hop's load so it really does run dry short of the floor. The row must name the real T+.
            var mission = campaign.Mission(2);
            var design = HomeAutoBuild.Choose(catalog, hangar.templates, mission).Design;
            var stage = (JsonObject)design.Arr("stages")[0];
            JsonMut.Set(stage, "propMass", 1d);
            var sim = ReferenceAscent.Simulate(catalog, design);
            foreach (var risk in sim.risks)
            {
                if (risk.level != "red" || sim.failure == null || risk.kind != sim.failure.kind) continue;
                Assert.That(risk.kid, Is.EqualTo(sim.failure.kid), "the red row is the failure that happened, with its time");
            }
            if (sim.failure != null && sim.failure.t > 0.5)
                StringAssert.DoesNotContain("T+0 s", sim.failure.kid);
        }

        [Test]
        public void TheRedRowForAnEventCarriesTheEventTime()
        {
            // Old orbit aim on a hop: burnout under 100 km is a prop-depletion event at about T+58 s.
            var mission = campaign.Mission(2);
            var design = HomeAutoBuild.Choose(catalog, hangar.templates, mission).Design;
            var input = Kestrel.Core.Vehicle.Stack.SimInputs(catalog, Design.FromJson(design));
            input.opts.monitor.Predict = true;
            var sim = Kestrel.Core.Physics.Headless.Simulate(input.stages, input.opts);
            Assert.IsNotNull(sim.failure, "aimed at 100 km, burnout is called empty");
            Assert.That(sim.failure.t, Is.GreaterThan(10));
            var row = sim.risks.Find(r => r.kind == sim.failure.kind);
            Assert.That(row.level, Is.EqualTo("red"));
            Assert.That(row.kid, Is.EqualTo(sim.failure.kid));
            StringAssert.DoesNotContain("T+0 s", row.kid);
        }

        [Test]
        public void OnlyAValidationBlockDisablesLaunch()
        {
            var predicted = new QuickReadout { margin = 0.2, fairingFits = true, maxQStale = false, predictedFailure = new FailureEvent { kind = "breakup", kid = "It breaks up." } };
            var soft = LaunchGate.Evaluate(new EngineeringReport { ok = true }, predicted);
            Assert.That(soft.state, Is.EqualTo(LaunchGateState.Blocked));
            Assert.That(soft.CanLaunch, "a bad rocket is allowed to fly and fail");
            Assert.That(soft.NeedsConfirm);
            Assert.That(soft.Line, Is.EqualTo("It breaks up."), "the reason still shows");

            var slow = LaunchGate.Evaluate(new EngineeringReport { ok = true }, new QuickReadout { margin = -0.1, fairingFits = true });
            Assert.That(slow.CanLaunch && slow.NeedsConfirm, "too slow still flies after one confirm");

            var rep = new EngineeringReport { ok = true };
            rep.blocks.Add(new Issue { code = "no-engine", kid = "Add an engine.", msg = "no engine", tab = "engines" });
            var hard = LaunchGate.Evaluate(rep, predicted);
            Assert.That(hard.state, Is.EqualTo(LaunchGateState.Blocked));
            Assert.IsFalse(hard.CanLaunch, "a design that cannot be built stays locked");
            Assert.IsFalse(LaunchGate.Evaluate(new EngineeringReport { ok = false }, predicted).CanLaunch);

            var checking = LaunchGate.Evaluate(new EngineeringReport { ok = true }, new QuickReadout { margin = 0.2, fairingFits = true, maxQStale = true });
            Assert.That(checking.CanLaunch && !checking.NeedsConfirm);
        }

        [Test]
        public void KhTwoTripIsAHopNotLowEarthOrbit()
        {
            Assert.That(MissionAim.IsSuborbital(catalog, "kh-m2"));
            Assert.That(MissionAim.IsSuborbital(catalog, "kh-m1"));
            Assert.That(MissionAim.IsSuborbital(catalog, "suborbital"));
            Assert.IsFalse(MissionAim.IsSuborbital(catalog, "kh-m3"));
            Assert.IsFalse(MissionAim.IsSuborbital(catalog, "leo200"));
            Assert.IsFalse(MissionAim.IsSuborbital(catalog, null));
            Assert.That(MissionAim.IsHopTrip("m2", 100000, suborbital: true), "100 km apogee is a hop");
            Assert.IsFalse(MissionAim.IsHopTrip("m2", 100000, suborbital: false), "the old rule only knew <= 80 km");
            Assert.That(MissionAim.IsHopTrip("m1", 10000, false));
            Assert.IsFalse(MissionAim.IsHopTrip("m3", 200000, false));
        }

        // NAV-01 / NAV-02 mirror (test/nav-flow-harness). A risk row is deterministic red only when the predict run
        // can actually fail that way. Negative static stability with CAR >= 1 is held by the gimbal and flew 5/5 in
        // FlightBatch, but read "flipped at T+0 s" and disabled Launch. The payload-fit blocks some of these rockets
        // still show on main are B-04 (#303) and are left out here.
        static bool FitBlock(LaunchGateResult g) => g.code == "payload-doesnt-fit" || g.code == "schema";

        LaunchGateResult GateAfterReference(JsonObject json, out Kestrel.Core.Physics.SimResult sim)
        {
            ReferenceAscent.Reset();
            sim = ReferenceAscent.Simulate(catalog, json);
            ReferenceAscent.Store(ReferenceAscent.CanonicalKey(catalog, json), sim);
            return LaunchGate.Evaluate(Validate.Engineering(catalog, json), Readout.Quick(catalog, json));
        }

        [Test]
        public void GoldenWinnersHaveNoCertainFailureTheyDoNotHave()
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-winners.json")));
            var bad = new List<string>();
            foreach (JsonObject row in file.Arr("missions"))
            {
                var json = JsonMut.CloneObject((JsonObject)row.Arr("designs")[0]);
                var gate = GateAfterReference(json, out var sim);
                var red = LaunchGate.DeterministicRed(sim.risks, Readout.Quick(catalog, json));
                if (sim.failure == null && red != null) bad.Add(row.Str("id") + " " + red.kind);
                if (!gate.Ready && !FitBlock(gate)) bad.Add(row.Str("id") + " gate " + gate.code);
            }
            Assert.That(bad, Is.Empty, string.Join(" | ", bad));
        }

        [Test]
        public void DesignFirstRocketsAreReadyAfterThePredictRun()
        {
            var bad = new List<string>();
            for (int cleared = 0; cleared < 20; cleared++)
            {
                PadHandoff.Clear();
                GameSession.Clear();
                var r = HomeRoutes.DesignFirst(campaign, cleared, catalog, hangar.templates);
                Assert.That(PadHandoff.TryTake(out var json, out _), "m" + (cleared + 1));
                var gate = GateAfterReference(json, out var sim);
                if (gate.Ready || FitBlock(gate)) continue;
                bad.Add(r.Session.MissionId + " " + gate.code + ": " + gate.Line);
            }
            Assert.That(bad, Is.Empty, string.Join(" | ", bad));
        }

        [Test]
        public void UnstableButControlledIsNotACertainFailure()
        {
            var json = JsonMut.CloneObject((JsonObject)MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-winners.json"))).Arr("missions").Find(m => ((JsonObject)m).Str("id") == "m3") is JsonObject row ? (JsonObject)row.Arr("designs")[0] : null);
            var gate = GateAfterReference(json, out var sim);
            Assert.That(sim.minStability, Is.LessThan(0), "the KH-3 winner is statically unstable for part of the climb");
            Assert.That(sim.minCar, Is.GreaterThanOrEqualTo(1), "and the gimbal holds it");
            Assert.IsNull(sim.failure);
            Assert.That(gate.Ready, gate.Line);
            // A real lack of control authority is still red.
            var weak = new QuickReadout { margin = 0.2, fairingFits = true, maxQStale = false, minCar = 0.6, minStability = -1,
                risks = new List<FlightRisk> { new FlightRisk { kind = "tumble", level = "red", kid = "It flips." } } };
            Assert.That(LaunchGate.Evaluate(new EngineeringReport { ok = true }, weak).code, Is.EqualTo("tumble"));
            var stale = new QuickReadout { margin = 0.2, fairingFits = true, maxQStale = true, minCar = 3,
                risks = new List<FlightRisk> { new FlightRisk { kind = "tumble", level = "red", kid = "It flips." } } };
            Assert.That(LaunchGate.DeterministicRed(stale.risks, stale), Is.Not.Null, "no flown ascent, no exemption");
        }

        [Test]
        public void AHopThatReachesItsApogeeIsNotTooSlow()
        {
            var ok = new EngineeringReport { ok = true };
            var reached = new QuickReadout { margin = -0.29, fairingFits = true, maxQStale = false, suborbital = true, targetAlt = 10000, predictedApo_m = 9800 };
            Assert.That(LaunchGate.Evaluate(ok, reached).Ready, "the flown ascent reaches the -5% band");
            var shy = new QuickReadout { margin = -0.29, fairingFits = true, maxQStale = false, suborbital = true, targetAlt = 10000, predictedApo_m = 9000 };
            Assert.That(LaunchGate.Evaluate(ok, shy).code, Is.EqualTo("dv-margin"));
            var orbit = new QuickReadout { margin = -0.1, fairingFits = true, maxQStale = false, suborbital = false, targetAlt = 200000, predictedApo_m = 400000 };
            Assert.That(LaunchGate.Evaluate(ok, orbit).code, Is.EqualTo("dv-margin"), "an orbit needs the speed, not just the height");
            var stale = new QuickReadout { margin = -0.29, fairingFits = true, maxQStale = true, suborbital = true, targetAlt = 10000 };
            Assert.That(LaunchGate.Evaluate(ok, stale).code, Is.EqualTo("dv-margin"), "no flown ascent, no exemption");
        }
    }
}
