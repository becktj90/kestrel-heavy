// Physics (KH-7 follow-up): the fairing call's "perfect" press, Auto-assist's press and the flown sim's "fairing" event
// all wait for one heat gate: the first time ½ρv³ (air-relative speed) drops under the payload's MaxHeatFlux and stays
// under it for 2 s. Before, Auto-assist pressed at the green zone of a window that opens 12 km under the design's
// jettison height (about 99 to 107 km), where ½ρv³ was 1,300 to 13,000 W/m², so a passing flight warned "Payload heat
// is past the limit", and the sim's marker sat at a fixed 110 km whatever the heat. A player's own early press with
// Auto-assist off still damages the payload.
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FairingHeatGateTests
    {
        /// <summary>The live pad's ascent (AscentFlight.Prepare + FlightDriver.Launch) for (design hash, seed 1), with the
        /// briefing card's wind and site, not yet flown.</summary>
        static DirectedFlight StartLive(Design d, CampaignMission m, bool autoAssist)
        {
            var c = StarterFixture.Catalog;
            ulong hash = FlightBatch.DesignHash(d), seed = 1;
            var card = MissionBriefing.Load(Golden.Root, int.Parse(m.Id.Substring(1)), hash, seed);
            double target = Stack.RequiredDeltaV(c, d.payloadOrbit).targetAlt;
            if (MissionAim.IsApogeeHop(m)) target = m.GuidanceAltM;
            double qLim = FailureMonitor.StructuralQLimPa(c, d);
            FailureMonitor monitor = FailureMonitor.Engages(d) ? FailureMonitor.Create(c, d, target, hash, seed) : null;
            var director = MissionFlightSetup.Director(c, d, monitor?.Events, card?.Wind, qLim, FailureMonitor.GentleTier(d),
                target, hash, seed, autoAssist, false, false, card?.Bonuses);
            var site = new MissionFlightSite { Wind = card?.Wind };
            if (card != null && card.Site != null) { site.HasLatitude = true; site.LatitudeDeg = card.Site.LatitudeDeg; }
            var setup = MissionFlightSetup.Build(c, m, d, MissionFlightSetup.LiveDt, site);
            if (monitor != null) setup.Options.monitor = monitor;
            var flight = MissionFlightSetup.Start(setup, director);
            flight.Checks = new PlayedChecks(c, d, setup.Stages, setup.Options, flight.Sim);
            return flight;
        }

        /// <summary>Replays the heat gate on a plain sim (no director) the way any reader would: tick, then feed
        /// (t, altitude, air-relative speed). The time it opens, or NaN. The flown sim's "fairing" event must land there.</summary>
        internal static double ReplayGateT(IList<SimStage> stages, SimOptions o)
        {
            var sim = new AscentSim(stages, o);
            var gate = new FairingHeatGate(o.fairingJettisonAltM, o.fairingMaxHeatFlux);
            double w = o.rotate ? Earth.SiteRotationSpeed(o.latitude) / Gravity.R_EARTH : 0;
            while (!sim.Done)
            {
                sim.Tick();
                double alt = JsMath.Hypot(sim.Y[0], sim.Y[1]) - Gravity.R_EARTH;
                if (gate.Update(sim.T, alt, JsMath.Hypot(sim.Y[2] + w * sim.Y[1], sim.Y[3] - w * sim.Y[0]))) return sim.T;
            }
            return double.NaN;
        }

        static IEnumerable<(CampaignMission m, Design d)> DefaultStarters()
        {
            StarterFixture.EnsureBases();
            foreach (var m in StarterFixture.Campaign.Missions)
            {
                if (StarterBases.For(m.Id) == null) continue;
                var r = StarterFixture.Rocket(m.Id);
                if (r.Check().Blocked || !r.Verdict().Survives) continue;
                yield return (m, Design.FromJson(r.BuildDesign()));
            }
        }

        [Test]
        public void Gate_OpensOnlyAfterTheHeatStaysUnderTheLimitFor2s_AboveTheArmingHeight()
        {
            const double limit = 1135, jett = 110000;
            // Speeds that put ½ρv³ well over and well under the limit at 100 km (ρ ≈ 5.6e-7 kg/m³).
            double hotV = System.Math.Cbrt(4 * limit / AscentDecisions.HeatFlux(100000, 1));
            double coolV = System.Math.Cbrt(0.25 * limit / AscentDecisions.HeatFlux(100000, 1));
            Assert.Greater(AscentDecisions.HeatFlux(100000, hotV), limit);
            Assert.Less(AscentDecisions.HeatFlux(100000, coolV), limit);

            var g = new FairingHeatGate(jett, limit);
            Assert.IsFalse(g.Update(0, 50000, 1), "below the arming height (98 km) nothing counts, even in cool air");
            Assert.IsFalse(g.Update(1, 100000, coolV), "cool, the 2 s hold starts");
            Assert.IsFalse(g.Update(2.9, 100000, coolV), "1.9 s cool is not enough");
            Assert.IsFalse(g.Update(3.0, 100000, hotV), "hot again: the hold resets");
            Assert.IsFalse(g.Update(4.0, 100000, coolV));
            Assert.IsFalse(g.Update(5.9, 100000, coolV));
            Assert.IsTrue(g.Update(6.0, 100000, coolV), "2 s under the limit opens the gate");
            Assert.AreEqual(6.0, g.OpenT, 1e-12);
            Assert.IsTrue(g.Update(7.0, 100000, hotV), "once open it stays open");

            var none = new FairingHeatGate(jett, 0);
            for (int i = 0; i < 10; i++) Assert.IsFalse(none.Update(i, 120000, 1), "a payload with no limit never opens the gate");
        }

        [Test]
        public void EveryDefaultStarter_AutoAssistOn_JettisonsAtTheHeatGate_NoPayloadDamage_AndTheSimEventAgrees()
        {
            int fairings = 0, flown = 0;
            foreach (var (m, d) in DefaultStarters())
            {
                var f = StartLive(d, m, true);
                var sim = f.Run();
                flown++;
                var score = f.Director.Score();
                Assert.IsFalse(score.PayloadDamaged, m.Id + ": fairing off at " + (f.Director.FairingAltM / 1000).ToString("0.0")
                    + " km, " + score.FairingFlux.ToString("0") + " W/m2");
                var events = sim.events.FindAll(e => e.type == "fairing");
                if (double.IsNaN(f.Director.FairingT))
                {
                    Assert.AreEqual(0, events.Count, m.Id + ": no director jettison, no sim fairing event");
                    continue;
                }
                fairings++;
                var gate = f.Director.FairingGate;
                Assert.IsTrue(gate.Open, m.Id + ": the gate opened");
                Assert.AreEqual(gate.OpenT, f.Director.FairingT, 1e-9, m.Id + ": Auto-assist pressed the moment the gate opened");
                Assert.Less(AscentDecisions.HeatFlux(f.Director.FairingAltM, f.Director.FairingSpeedMps), gate.LimitWm2, m.Id);
                Assert.AreEqual(1, events.Count, m.Id + ": one sim fairing event");
                Assert.AreEqual(f.Director.FairingT, events[0].t, 1e-9, m.Id + ": the sim's fairing event is the director's jettison");
                Assert.AreEqual(f.Director.FairingAltM, events[0].alt.Value, 1e-6, m.Id);
                TestContext.WriteLine(m.Id + " fairing T+" + events[0].t.ToString("0.00") + " at " + (events[0].alt.Value / 1000).ToString("0.0")
                    + " km, " + score.FairingFlux.ToString("0") + " / " + gate.LimitWm2.ToString("0") + " W/m2");
            }
            Assert.GreaterOrEqual(flown, 20, "every board starter was flown");
            Assert.GreaterOrEqual(fairings, 15, "starters with a fairing call");
        }

        [Test]
        public void EveryDefaultStarter_AutoAssistOff_LettingTheWindowRunOut_IsAlsoHeatSafe()
        {
            foreach (var (m, d) in DefaultStarters())
            {
                var f = StartLive(d, m, false);
                f.Run();
                Assert.IsFalse(f.Director.Score().PayloadDamaged, m.Id + ": the window ran out at "
                    + (f.Director.FairingAltM / 1000).ToString("0.0") + " km, " + f.Director.Score().FairingFlux.ToString("0") + " W/m2");
            }
        }

        [Test]
        public void AManualEarlyPress_WithAutoAssistOff_StillDamagesThePayload()
        {
            int fairings = 0;
            foreach (var (m, d) in DefaultStarters())
            {
                var early = StartLive(d, m, false);
                bool tapped = false;
                while (early.Step()) if (!tapped && early.Director.Choose(AscentCall.Fairing, true)) tapped = true;
                if (!tapped) continue;
                fairings++;
                Assert.IsTrue(early.Director.Score().PayloadDamaged, m.Id + ": a press the moment the fairing call opens ("
                    + (early.Director.FairingAltM / 1000).ToString("0.0") + " km, " + early.Director.Score().FairingFlux.ToString("0") + " W/m2) damages");
            }
            Assert.GreaterOrEqual(fairings, 15, "starters with a fairing call");
        }
    }
}
