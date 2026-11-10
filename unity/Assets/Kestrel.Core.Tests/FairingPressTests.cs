// Trevor 2026-11-09 (KH-3, TestFlight 218/219): "the jettison of fairing was super delayed after I hit the button".
// The JETTISON FAIRING press resolved the director's call (and judged the payload heat at that moment), but the sim only
// dropped the fairing, and the picture only split the halves, at the sim's own release: the design's jettison height,
// or the payload heat gate. The call opens 12 km below that height, so a press waited tens of seconds for the halves.
// Now a pressed jettison is the jettison: the fairing leaves the stack on the next physics step (one 1/120 s step,
// within the frame), with the same mass drop and "fairing" event the automatic release uses, so FlightStaging splits
// the halves at once. Auto-assist and a lapsed window still fly the sim's own release (no golden moves).
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FairingPressTests
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

        sealed class Flight
        {
            public AscentSim Sim;
            public AscentDirector Director;
            public DirectedFlight Directed;
            public SimOptions Opts;
        }

        /// <summary>The live pad's flight for a board starter (FlightToLandingPlaythroughTests.Play, FlightDriver's
        /// DirectedFlight), Auto-assist as given so the test decides who presses.</summary>
        Flight Build(int index, bool autoAssist)
        {
            var mission = campaign.Mission(index);
            var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
            var design = Design.FromJson(choice.Design);
            ulong hash = FlightBatch.DesignHash(design);
            ulong seed = 1;
            var card = MissionBriefing.Load(Golden.Root, index, hash, seed);
            double target = Stack.RequiredDeltaV(catalog, design.payloadOrbit).targetAlt;
            if (MissionAim.IsApogeeHop(mission)) target = mission.GuidanceAltM;
            double qLim = FailureMonitor.StructuralQLimPa(catalog, design);
            FailureMonitor monitor = FailureMonitor.Engages(design) ? FailureMonitor.Create(catalog, design, target, hash, seed) : null;
            var director = MissionFlightSetup.Director(catalog, design, monitor?.Events, card?.Wind, qLim, FailureMonitor.GentleTier(design),
                target, hash, seed, autoAssist, false, false, card?.Bonuses);
            var site = new MissionFlightSite { Wind = card?.Wind };
            if (card != null && card.Site != null) { site.HasLatitude = true; site.LatitudeDeg = card.Site.LatitudeDeg; }
            var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt, site);
            var opts = setup.Options;
            if (monitor != null) opts.monitor = monitor;
            var sim = new AscentSim(setup.Stages, opts);
            return new Flight { Sim = sim, Director = director, Directed = new DirectedFlight(sim, director, opts), Opts = opts };
        }

        static SimEvent FairingEvent(AscentSim sim)
        {
            SimEvent found = null;
            int n = 0;
            foreach (var e in sim.Events)
                if (e.type == "fairing") { found = found ?? e; n++; }
            Assert.That(n, Is.LessThanOrEqualTo(1), "the fairing leaves once");
            return found;
        }

        [Test, Timeout(120000)]
        public void Kh3_PressedJettison_LeavesOnTheNextStep_NotAtTheReleaseHeight()
        {
            // Unpressed (no Auto-assist, nobody taps): the sim's own release, as before.
            var idle = Build(3, false);
            Assert.That(idle.Opts.fairingJettisonAltM, Is.GreaterThan(0), "KH-3 flies a fairing");
            double idleOpenT = double.NaN;
            int guard = 0;
            while (idle.Directed.Step() && guard++ < 400000)
            {
                if (double.IsNaN(idleOpenT) && idle.Director.IsOpen(AscentCall.Fairing)) idleOpenT = idle.Sim.T;
                Assert.IsFalse(idle.Director.Command.JettisonFairing, "only a press orders the jettison");
            }
            var idleEvent = FairingEvent(idle.Sim);
            Assert.IsNotNull(idleEvent, "the sim releases the fairing on its own");
            Assert.IsFalse(double.IsNaN(idleOpenT), "the fairing call opened");
            TestContext.WriteLine("unpressed: call opens T+" + idleOpenT.ToString("0.00") + ", fairing leaves T+" + idleEvent.t.ToString("0.00"));

            // Pressed the moment the call opens (Trevor's tap).
            var f = Build(3, false);
            double pressT = double.NaN, massBefore = double.NaN;
            guard = 0;
            while (guard++ < 400000)
            {
                if (!f.Directed.Step()) break;
                if (f.Director.IsOpen(AscentCall.Fairing))
                {
                    pressT = f.Sim.T;
                    massBefore = f.Sim.Y[4];
                    Assert.IsFalse(f.Sim.FairingJettisoned, "still on when the call opens");
                    Assert.IsTrue(f.Director.ChoosePrimary(AscentCall.Fairing), "JETTISON FAIRING");
                    Assert.IsTrue(f.Director.Command.JettisonFairing, "the press orders the jettison");
                    f.Directed.Step();
                    break;
                }
            }
            Assert.IsFalse(double.IsNaN(pressT), "the call opened");
            var ev = FairingEvent(f.Sim);
            Assert.IsNotNull(ev, "the fairing left on the step after the press");
            Assert.IsTrue(f.Sim.FairingJettisoned);
            Assert.That(ev.t, Is.InRange(pressT, pressT + MissionFlightSetup.LiveDt + 1e-9), "within one physics step of the press");
            Assert.AreEqual(f.Opts.fairingMassKg, ev.mass ?? 0, 1e-9, "the same shell mass the automatic release drops");
            Assert.That(massBefore - f.Sim.Y[4], Is.GreaterThanOrEqualTo(f.Opts.fairingMassKg - 1e-6), "the shell is off the stack");
            Assert.That(ev.t, Is.LessThan(idleEvent.t - 1.0), "earlier than the old wait for the release height / heat gate");
            Assert.AreEqual(pressT, f.Director.FairingT, 1e-9, "the rail and the heat verdict quote the same moment");

            // The rest of the flight never releases it twice.
            guard = 0;
            while (f.Directed.Step() && guard++ < 400000) { }
            FairingEvent(f.Sim);
        }

        [Test, Timeout(120000)]
        public void AutoAssist_FliesTheSimsOwnRelease_Unchanged()
        {
            // Auto-assist accepts the call itself (untouched), so it never orders a jettison: the flown release is the
            // sim's own, the same state the batch and the goldens fly.
            var auto = Build(3, true);
            int guard = 0;
            while (auto.Directed.Step() && guard++ < 400000)
                Assert.IsFalse(auto.Director.Command.JettisonFairing, "Auto-assist never orders the jettison");
            var bare = Build(3, true);
            guard = 0;
            while (bare.Directed.Step() && guard++ < 400000) { }
            var a = FairingEvent(auto.Sim);
            var b = FairingEvent(bare.Sim);
            Assert.IsNotNull(a);
            Assert.AreEqual(b.t, a.t, 0, "deterministic");
        }

        [Test]
        public void JettisonFairingNow_IsANoOpWithoutAFairingOrOnceItHasGone()
        {
            var f = Build(3, false);
            Assert.IsTrue(f.Sim.JettisonFairingNow(), "first jettison");
            Assert.IsFalse(f.Sim.JettisonFairingNow(), "never twice");
            int fairings = 0;
            foreach (var e in f.Sim.Events) if (e.type == "fairing") fairings++;
            Assert.AreEqual(1, fairings);

            var none = Build(3, false);
            none.Opts.fairingJettisonAltM = 0; // a design that flies no fairing
            Assert.IsFalse(none.Sim.JettisonFairingNow(), "no fairing, nothing to drop");
            Assert.IsFalse(none.Sim.FairingJettisoned);
        }
    }
}
