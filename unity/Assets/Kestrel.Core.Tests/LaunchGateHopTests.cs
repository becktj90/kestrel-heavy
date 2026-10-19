// F1 (NAV_BUGS_LIVE): DESIGN FIRST on KH-2 showed "848 m/s to spare" and TWR 4.39, but Launch was greyed
// with "Your rocket ran out of fuel at T+0 s". Root cause: the designer's reference ascent aimed the apogee
// trip at its apogee (100 km), so the predict run called burnout at 20 km "out of fuel", and the risk row
// printed that at T+0. The pad flight aims at the 1 km hop floor (MissionAim). Also: only a validation block
// disables Launch now; a predicted failure asks once and flies. And KH-2's trip is a hop, not LEO 200 km.
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
            Assert.That(reference.opts.targetAlt, Is.EqualTo(200000), "KH-3 is a 200 km circle");
            Assert.IsFalse(MissionAim.IsSuborbital(catalog, design.Obj("payload").Str("orbit")));
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
    }
}
