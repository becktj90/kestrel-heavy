// #440 (Physics): in sensible air a stage with control authority must not hold a fixed pitch far off the
// air-relative velocity. The clamp limits the commanded in-plane angle of attack to the same 15 deg the
// breakup rule allows (FailureModel.TumbleAlphaRad), at the same 20 kPa (TumbleQPa), slewed at
// RecoverySim.PitchRate. Below 20 kPa the program pitch flies unchanged, so no-input flights must be
// bit-identical: the clamp never engages on them, and AlphaClampSteps is the proof (0 steps => PitchCmd
// is never touched => the flight is exactly the pre-clamp flight).
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Save;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class AlphaClampTests
    {
        Catalog catalog;
        HangarData hangar;
        Campaign campaign;

        [OneTimeSetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
        }

        Design Starter(int index)
        {
            var mission = campaign.Mission(index);
            var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
            var design = Design.FromJson(choice.Design);
            if (index == 8) design.safetyFactor = 1.8; // m8 board starter is the strength-restored design
            return design;
        }

        SimResult FlyNoInput(int index)
        {
            var mission = campaign.Mission(index);
            var design = Starter(index);
            var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.BatchDt);
            return Headless.Simulate(setup.Stages, setup.Options);
        }

        // #440 rule (3): no-input goldens stay bit-identical. The clamp is a no-op on a flight that never
        // commands more than 15 deg off the air-relative velocity in sensible air, and the counter proves it:
        // 0 steps means PitchCmd was never moved, so the flight is exactly the pre-clamp flight.
        // Pins re-measured on the #464 rebase onto main ee1b329 (#463's fairing-mass jettison moved m3/m10:
        // m3 206.0x194.0 -> 202.8x197.2, m10 202.3x197.7 -> 203.2x196.8; m1/m2 unchanged). The clamp still
        // never engages on any of them (steps = 0), so the flights are the pre-clamp flights on current main.
        [Test]
        public void NoInput_LeavesTheGoldenFlightBitIdentical([Values(1, 2, 3, 10)] int index)
        {
            var r = FlyNoInput(index);
            Assert.AreEqual(0, r.alphaClampSteps, "m" + index + ": the clamp must never engage on a no-input flight");
            // Smoke check that this is the golden flight, not a degenerate one.
            double apoKm = r.orbit.apoAlt / 1000, periKm = r.orbit.periAlt / 1000;
            switch (index)
            {
                case 1: Assert.AreEqual("suborbital", r.outcome); Assert.AreEqual(13.9, apoKm, 1.0); break;
                case 2: Assert.AreEqual("suborbital", r.outcome); Assert.AreEqual(111.6, apoKm, 1.0); break;
                case 3: Assert.AreEqual("orbit", r.outcome); Assert.AreEqual(202.8, apoKm, 1.0); Assert.AreEqual(197.2, periKm, 1.0); break;
                case 10: Assert.AreEqual("orbit", r.outcome); Assert.AreEqual(203.2, apoKm, 1.0); Assert.AreEqual(196.8, periKm, 1.0); break;
            }
        }

        // #440 rule (4), m8 with a -3 deg nudge held from T+20, live pad (director + briefing wind, seed 1).
        // Measured on main ee1b329 (rebase; was cab8bf4: S1 55.7 km, gamma 2.3 deg, tumble T+456): the S1 stages
        // low (56.5 km, gamma 2.5 deg, v 4.79 km/s) and the S2 holds a fixed 68.8 deg
        // pitch at alpha ~69 deg; it falls back and the alpha>15 deg / q>20 kPa breakup rule trips at T+488.
        // The clamp DOES engage (AlphaClampSteps > 0) but it cannot save this flight: the breakup rule trips on
        // the very first step where q exceeds 20 kPa, while alpha is already ~71 deg, and the 10 deg/s slew
        // closes only ~0.08 deg in one LiveDt step. The clamp is a guard for a stage that pitches over too far
        // IN sensible air; a stage that ENTERS sensible air already at alpha 71 deg is a guidance failure
        // (the S2's PEG-lite saturates at 71.8 deg on an underpowered S2, TWR ~0.42), not a clamp failure.
        // So the flight is still a miss, and the tumble is pinned here as the measured current behaviour.
        [Test]
        public void M8_NudgeHeldFromT20_StillMissesAndTheClampEngages()
        {
            int index = 8;
            var mission = campaign.Mission(index);
            var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
            var design = Design.FromJson(choice.Design);
            design.safetyFactor = 1.8;
            ulong hash = FlightBatch.DesignHash(design);
            ulong seed = 1;
            var card = MissionBriefing.Load(Golden.Root, index, hash, seed);
            bool auto = StepperProgress.Read(choice.Design).AutoAssistOn(DesignerDepth.Simple);
            double target = Stack.RequiredDeltaV(catalog, design.payloadOrbit).targetAlt;
            double qLim = FailureMonitor.StructuralQLimPa(catalog, design);
            FailureMonitor monitor = FailureMonitor.Engages(design) ? FailureMonitor.Create(catalog, design, target, hash, seed) : null;
            var director = MissionFlightSetup.Director(catalog, design, monitor?.Events, card?.Wind, qLim, FailureMonitor.GentleTier(design),
                target, hash, seed, auto, false, false, card?.Bonuses);
            var site = new MissionFlightSite { Wind = card?.Wind };
            if (card != null && card.Site != null) { site.HasLatitude = true; site.LatitudeDeg = card.Site.LatitudeDeg; }
            var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt, site);
            var opts = setup.Options;
            if (monitor != null) opts.monitor = monitor;
            var sim = new AscentSim(setup.Stages, opts);
            var directed = new DirectedFlight(sim, director, opts);
            double nudgeRad = -3 * Math.PI / 180.0;
            int guard = 0;
            while (!sim.Done && guard++ < 300000)
            {
                // Held player nudge: the director's command first, then override the pitch offset before the tick.
                directed.Apply();
                if (sim.T >= 20) sim.LivePitchOffsetRad = nudgeRad;
                sim.Tick();
                if (!sim.Done)
                {
                    director.Advance(directed.Sample(), MissionFlightSetup.LiveDt);
                    directed.Apply();
                }
            }
            var r = directed.Finish();
            Assert.AreNotEqual("orbit", r.outcome, "m8 -3 deg: still a miss");
            Assert.That(r.alphaClampSteps, Is.GreaterThan(0), "the clamp must engage once q exceeds 20 kPa with alpha > 15 deg");
            Assert.IsNotNull(r.failure, "the nudged m8 flight fails");
            Assert.AreEqual("tumble", r.failure.kind, "pinned current behaviour: see the comment above");
        }
    }
}
