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
        {
            var input = Stack.SimInputs(catalog, design, 1.0 / 120.0, 0);
            input.opts.targetAlt = mission.GuidanceAltM;
            if (input.opts.guidance != null) input.opts.guidance.targetAlt = mission.GuidanceAltM;
            input.opts.monitor = FailureMonitor.Create(catalog, design, mission.GuidanceAltM, 1, 1);
            return Headless.Simulate(input.stages, input.opts);
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
