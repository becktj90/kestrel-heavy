// KH-1 is the hopper Home launches. Three engines at full throttle cross the 35 kPa
// wall at T+26 s. The armed fork is one engine, a short load, SF 3 and a 60% max-Q cap.
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class Kh1FirstHopTests
    {
        Catalog catalog;
        HangarData hangar;
        Campaign campaign;

        [SetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
        }

        [Test]
        public void DefaultDesignClearsTheFirstHop()
        {
            var mission = campaign.Mission(1);
            var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
            Assert.AreEqual("hopper", choice.TemplateId);
            Assert.AreEqual("Enough for this hop.", choice.ReachLine);
            Assert.IsTrue(choice.LaunchEnabled, choice.ReachLine);
            var design = Design.FromJson(choice.Design);
            Assert.AreEqual(1, design.stages[0].engineCount);
            Assert.AreEqual(HomeAutoBuild.FirstHopPropKg, design.stages[0].propMass, 0.1);
            Assert.AreEqual(HomeAutoBuild.FirstHopSafetyFactor, design.safetyFactor, 0.01);
            Assert.IsTrue(design.guidance.maxQThrottleSet);
            Assert.AreEqual(HomeAutoBuild.FirstHopMaxQPercent, design.guidance.maxQThrottle, 0.1);
            Assert.IsTrue(design.guidance.gLimitSet);

            double qLim = FailureMonitor.StructuralQLimPa(catalog, design);
            Assert.That(qLim, Is.GreaterThan(70000), "SF 3 holds well above the 35 kPa wall");

            var seeds = campaign.Seeds;
            Assert.That(seeds.Count, Is.GreaterThanOrEqualTo(1));
            int n = Math.Min(5, seeds.Count);
            for (int i = 0; i < n; i++)
            {
                var flight = FlightBatch.Fly(catalog, mission, design, seeds[i]);
                Assert.IsTrue(flight.passed, "seed " + seeds[i] + " apo " + flight.apoKm.ToString("0.00") + " fail " + flight.failure + " g " + flight.maxG.ToString("0.00") + " q " + flight.qPeakKPa.ToString("0.0"));
                Assert.That(flight.qPeakKPa * 1000, Is.LessThan(qLim * 0.85), "seed " + seeds[i]);
                Assert.That(flight.maxG, Is.LessThan(7.2), "seed " + seeds[i]);
            }

            var fine = Fly(catalog, design, mission);
            Assert.IsTrue(FlightDebrief.GoalMet(mission, fine.failure == null && fine.outcome != "failure", fine.orbit.apoAlt, fine.orbit.periAlt, fine.maxG, null),
                "pad step apo " + (fine.orbit.apoAlt / 1000).ToString("0.00") + " fail " + (fine.failure != null ? fine.failure.kind : "-") + " g " + fine.maxG.ToString("0.00") + " q " + (fine.maxQ / 1000).ToString("0.00"));
            Assert.That(fine.maxQ, Is.LessThan(qLim * 0.85));
            Assert.That(fine.maxG, Is.LessThan(7.2));

            var guided = FlyGuided(catalog, design, mission, qLim);
            Assert.IsTrue(FlightDebrief.GoalMet(mission, guided.failure == null && guided.outcome != "failure", guided.orbit.apoAlt, guided.orbit.periAlt, guided.maxG, null),
                "guided apo " + (guided.orbit.apoAlt / 1000).ToString("0.00") + " fail " + (guided.failure != null ? guided.failure.kind : "-"));
            Assert.That(guided.maxQ, Is.LessThan(qLim * 0.85));
            Assert.That(guided.maxG, Is.LessThan(7.2));
        }

        [Test]
        public void MaxQPitchHold_StaysOnTheCaughtAngleWhileTheCapBites()
        {
            var mission = campaign.Mission(2);
            var design = Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, mission).Design);
            var input = Stack.SimInputs(catalog, design, 0.2, 0.2);
            var sim = new AscentSim(input.stages, input.opts);
            while (sim.Tick()) { }
            bool sawTurn = false;
            bool holding = false;
            double held = 0;
            int heldSamples = 0;
            int longest = 0;
            bool released = false;
            foreach (var s in sim.Samples)
            {
                if (s.stage > 1)
                {
                    if (holding) released = true;
                    break;
                }
                if (s.mode == 2 || s.mode == 3 || s.mode == 4)
                {
                    if (holding) released = true;
                    if (s.mode == 2) sawTurn = true;
                    holding = false;
                    if (heldSamples > longest) longest = heldSamples;
                }
                if (!sawTurn || s.mode != 1) continue;
                if (!holding)
                {
                    holding = true;
                    held = s.pitch;
                    heldSamples = 1;
                    continue;
                }
                heldSamples++;
                Assert.That(s.pitch, Is.EqualTo(held).Within(1e-6),
                    "t " + s.t.ToString("0.0") + " q " + (s.q / 1000).ToString("0.0"));
            }
            if (heldSamples > longest) longest = heldSamples;
            Assert.That(longest, Is.GreaterThan(3), "the cap holds the caught pitch for more than one step");
            Assert.IsTrue(released, "the hold ends when the cap lets go");
        }

        [Test]
        public void PadAimMatchesTheBatch_AndFrameBatchDoesNotChangeApogee()
        {
            var mission = campaign.Mission(1);
            var design = Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, mission).Design);
            var sheet = Stack.SimInputs(catalog, design, 1.0 / 120.0, 0);
            Assert.That(sheet.opts.targetAlt, Is.GreaterThan(mission.GuidanceAltM * 5),
                "the design orbit aims at the goal, not the hop's depletion floor");
            Assert.That(sheet.opts.guidance, Is.Not.Null);
            Assert.That(sheet.opts.guidance.targetAlt, Is.EqualTo(sheet.opts.targetAlt).Within(0.01));

            var orbit = new CampaignMission { TargetKind = "circular", ApoKm = 200, PeriKm = 200 };
            var kept = new SimOptions { targetAlt = 200000, guidance = GuidanceProfile.Builtin(180000) };
            Assert.IsFalse(MissionAim.Apply(kept, orbit));
            Assert.AreEqual(200000, kept.targetAlt, 0.01);
            Assert.AreEqual(180000, kept.guidance.targetAlt, 0.01);

            // The orbit altitude is the goal, not the depletion floor. The monitor then
            // calls the hop out of fuel and the flight does not pass.
            var lofted = Fly(catalog, design, mission, false);
            Assert.AreEqual("prop-depletion", lofted.failure != null ? lofted.failure.kind : "",
                "apo " + (lofted.orbit.apoAlt / 1000).ToString("0.00"));
            Assert.IsFalse(MissionScore.AltitudeWithin(lofted.orbit.apoAlt, mission.ApoKm * 1000));

            double one = ApoBatched(catalog, design, mission, 1);
            double slow = ApoBatched(catalog, design, mission, 12);
            Assert.That(slow, Is.EqualTo(one).Within(0.05),
                "12 ticks per slow frame apo " + (slow / 1000).ToString("0.000") + " vs " + (one / 1000).ToString("0.000"));
            Assert.IsTrue(MissionScore.AltitudeWithin(one, mission.ApoKm * 1000),
                "aimed apo " + (one / 1000).ToString("0.00") + " km");
            Assert.IsTrue(FlightDebrief.GoalMet(mission, true, one, 0, 4, null),
                "aimed apo " + (one / 1000).ToString("0.00"));
        }

        [Test]
        public void DesignThrottleStaysOnUntilTheMaxQCallIsTaken()
        {
            var mission = campaign.Mission(1);
            var design = Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, mission).Design);
            double qLim = FailureMonitor.StructuralQLimPa(catalog, design);
            var vehicle = AscentDecisions.FromDesign(design, catalog, null, null, qLim, mission.GuidanceAltM, false, null, 1, 1);
            Assert.IsTrue(vehicle.MaxQSet);
            var director = AscentDirector.Start(vehicle, null, null, true, false, true);
            director.Advance(new AscentSample { T = 1, QPa = 1000, Thrusting = true, MassKg = 2000, ThrustN = 80000, PropFraction = 1 }, 0.05);
            Assert.IsFalse(director.Command.SuppressDesignThrottle, "the design cap is the guided path until the call is accepted");
        }

        static SimResult Fly(Catalog catalog, Design design, CampaignMission mission)
            => Fly(catalog, design, mission, true);

        /// <summary>
        /// <paramref name="aim"/> applies the campaign depletion floor. Without it the pad
        /// keeps the design orbit's target, which is the bug the first hop used to fly.
        /// </summary>
        static SimResult Fly(Catalog catalog, Design design, CampaignMission mission, bool aim)
        {
            var input = Stack.SimInputs(catalog, design, 1.0 / 120.0, 0);
            if (aim) MissionAim.Apply(input.opts, mission);
            input.opts.monitor = FailureMonitor.Create(catalog, design, aim ? mission.GuidanceAltM : input.opts.targetAlt, 1, 1);
            return Headless.Simulate(input.stages, input.opts);
        }

        /// <summary>Same fixed 1/120 step, delivered one tick at a time or a slow-frame batch of <paramref name="batch"/>.</summary>
        static double ApoBatched(Catalog catalog, Design design, CampaignMission mission, int batch)
        {
            var input = Stack.SimInputs(catalog, design, 1.0 / 120.0, 0);
            MissionAim.Apply(input.opts, mission);
            input.opts.monitor = FailureMonitor.Create(catalog, design, mission.GuidanceAltM, 1, 1);
            input.opts.tMax = 400;
            var sim = new AscentSim(input.stages, input.opts);
            int guard = 0;
            while (!sim.Done && guard < 80000)
            {
                for (int i = 0; i < batch && !sim.Done; i++)
                {
                    sim.Tick();
                    guard++;
                }
            }
            return sim.Finish().orbit.apoAlt;
        }

        static SimResult FlyGuided(Catalog catalog, Design design, CampaignMission mission, double qLim)
        {
            var input = Stack.SimInputs(catalog, design, 1.0 / 120.0, 0);
            input.opts.targetAlt = mission.GuidanceAltM;
            if (input.opts.guidance != null) input.opts.guidance.targetAlt = mission.GuidanceAltM;
            var monitor = FailureMonitor.Create(catalog, design, mission.GuidanceAltM, 1, 1);
            input.opts.monitor = monitor;
            var vehicle = AscentDecisions.FromDesign(design, catalog, monitor.Events, null, qLim, mission.GuidanceAltM, false, null, 1, 1);
            var director = AscentDirector.Start(vehicle, monitor.Events, null, true, false, true);
            var sim = new AscentSim(input.stages, input.opts);
            int n = 0;
            while (!sim.Done && sim.T < 400 && n++ < 80000)
            {
                var cmd = director.Command;
                double scale = cmd.ThrottleScale;
                if (double.IsNaN(scale) || scale <= 0) scale = 1;
                else if (scale > 1) scale = 1;
                if (input.opts.guidance != null && cmd.SuppressDesignThrottle)
                    input.opts.guidance.maxQThrottleSet = false;
                sim.LiveThrottleScale = scale;
                sim.Tick();
                if (monitor.Event != null)
                {
                    sim.BreakUp(null);
                    break;
                }
                director.Advance(Sample(sim), 1.0 / 120.0);
            }
            return sim.Finish();
        }

        static AscentSample Sample(AscentSim sim)
        {
            double x = sim.Y[0], y = sim.Y[1];
            double r = Math.Sqrt(x * x + y * y);
            double vx = sim.Y[2], vy = sim.Y[3];
            double mass = sim.Y[4];
            return new AscentSample
            {
                T = sim.T,
                AltM = r - Gravity.R_EARTH,
                SpeedMps = Math.Sqrt(vx * vx + vy * vy),
                QPa = sim.LastQ,
                G = mass > 1 && sim.LastThrust > 0 ? sim.LastThrust / mass / Atmosphere.G0 : 0,
                MassKg = mass,
                ThrustN = sim.LastThrust,
                PropFraction = double.IsNaN(sim.PropellantFraction) ? 1 : sim.PropellantFraction,
                StageIndex = sim.StageIdx,
                Thrusting = sim.Thrusting,
            };
        }
    }
}
