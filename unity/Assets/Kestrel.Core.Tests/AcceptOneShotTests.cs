// DECISIONS #76 (fun review P1): the big button never hurts. The pitch call's accept confirms the autopilot's pitch
// program: one-shot, no held offset, the same flight as no input. On an engine-out the design cannot hold, the big
// button rides it out and the fatal shutdown is the secondary button.
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class AcceptOneShotTests
    {
        static AscentSample PitchSample() => new AscentSample { T = 5, AltM = 4000, SpeedMps = 120, QPa = 1000, Thrusting = true, PropFraction = 1 };

        [Test]
        public void PitchConfirm_HoldsNoOffset_AndTheNudgeRowStillSteers()
        {
            var d = AscentDirector.Start(new AscentVehicle { Stages = 1 }, null, null, false, true, false);
            d.Advance(PitchSample(), 0.5);
            Assert.IsTrue(d.IsOpen(AscentCall.Pitch));
            var p = d.OpenPrompts()[0];
            Assert.AreEqual("CONFIRM PITCH PROGRAM", p.Primary);
            Assert.IsFalse(p.PrimaryDeclines);
            Assert.IsTrue(d.ChoosePrimary(AscentCall.Pitch));
            Assert.IsTrue(d.IsDone(AscentCall.Pitch));
            Assert.AreEqual(0, d.NudgeDeg, 1e-12);
            Assert.AreEqual(0, d.Command.PitchOffsetRad, 1e-12);
            var s = PitchSample();
            for (int i = 0; i < 20; i++) { s.T += 0.5; d.Advance(s, 0.5); }
            Assert.AreEqual(0, d.Command.PitchOffsetRad, 1e-12, "nothing is held after the call");
            d.NudgePitch(-1);
            Assert.AreEqual(-1, d.NudgeDeg, 1e-12, "manual deviation stays on the Engineer nudge row");
        }

        [Test]
        public void PitchConfirm_FliesTheSameLiveFlightAsNoInput_Kh3()
        {
            var catalog = StarterFixture.Catalog;
            var mission = StarterFixture.Campaign.ById("m3");
            var hangar = HangarData.LoadFromRepo(Golden.Root);
            foreach (var design in new[]
            {
                Design.FromJson(StarterFixture.Rocket("m3").BuildDesign()),
                Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, mission).Design),
            })
            {
                var idle = PlayLoop.Fly(catalog, mission, design, false, PlayLoop.Style.Idle, out _);
                var confirm = PlayLoop.Fly(catalog, mission, design, false, PlayLoop.Style.ConfirmPitchOnly, out var taps);
                StringAssert.Contains("Pitch", taps, "the pitch call was confirmed");
                Assert.AreEqual(idle.outcome, confirm.outcome);
                Assert.AreEqual(idle.orbit.apoAlt, confirm.orbit.apoAlt, 1e-6);
                Assert.AreEqual(idle.orbit.periAlt, confirm.orbit.periAlt, 1e-6);
                Assert.IsNull(confirm.failure);
            }
        }

        /// <summary>KH-3 Home pick, auto-assist off, the big button in green on every call: it broke up (structural-maxq,
        /// T+142) when accept held −1°. Now it wins the live pad in the seed-1 briefing wind.</summary>
        [Test]
        public void Kh3HomePick_BigButtonInGreenEveryCall_WinsTheLivePad()
        {
            var catalog = StarterFixture.Catalog;
            var mission = StarterFixture.Campaign.ById("m3");
            var hangar = HangarData.LoadFromRepo(Golden.Root);
            var design = Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, mission).Design);
            var r = PlayLoop.Fly(catalog, mission, design, false, PlayLoop.Style.TapGreenPrimary, out var taps);
            Assert.IsTrue(LivePadLoop.Passes(catalog, mission, design, r), LivePadLoop.Text(r) + " taps " + taps);
        }

        static AscentDirector EngineOutOpen(AscentVehicle vehicle)
        {
            var director = AscentDirector.Start(vehicle, null, null, false, true, false);
            director.Advance(new AscentSample
            {
                T = 20, AltM = 8000, SpeedMps = 300, QPa = 4000, StageIndex = 0, Thrusting = true,
                PropFraction = 0.8, MassKg = 500000, ThrustN = 1.2e7,
            }, 0.05);
            Assert.IsTrue(director.IsOpen(AscentCall.EngineOut));
            return director;
        }

        static AscentVehicle Weak(bool hot) => new AscentVehicle
        {
            HasAnomaly = true, AnomalyStage = 0, AnomalyEngine = 3, Engines = 4, Redundant = false,
            HotChamber = hot, ThrottleMin = 0.5, QLimPa = 35000,
        };

        [Test]
        public void EngineOut_CannotHold_BigButtonRidesItOut()
        {
            var d = EngineOutOpen(Weak(false));
            var p = d.OpenPrompts()[0];
            Assert.IsTrue(p.PrimaryDeclines);
            Assert.AreEqual("RIDE IT OUT (keep it burning)", p.Primary);
            Assert.AreEqual("SHUT DOWN ENGINE 4 (will not hold)", p.Alternate);
            Assert.IsTrue(p.ShowAlternate && p.DangerAlternate);
            Assert.IsFalse(d.PrimaryAccepts(AscentCall.EngineOut));
            Assert.IsTrue(d.ChoosePrimary(AscentCall.EngineOut));
            Assert.IsFalse(d.Command.Lost, "riding a cool engine out keeps the rocket");
            Assert.AreEqual(1, d.Command.ThrottleScale, 1e-12, "all engines still burning");

            var shut = EngineOutOpen(Weak(false));
            Assert.IsTrue(shut.ChooseAlternate(AscentCall.EngineOut));
            Assert.IsTrue(shut.Command.Lost);
            Assert.AreEqual("engine-out", shut.Command.LossKind);
        }

        [Test]
        public void EngineOut_CannotHold_HotEngine_CopySaysRidingWillFail()
        {
            var d = EngineOutOpen(Weak(true));
            var p = d.OpenPrompts()[0];
            Assert.IsTrue(p.PrimaryDeclines);
            Assert.AreEqual("RIDE IT OUT (hot engine will fail)", p.Primary);
            Assert.IsTrue(d.ChoosePrimary(AscentCall.EngineOut));
            Assert.AreEqual("engine-overpressure", d.Command.LossKind, "honest: a hot engine comes apart either way");
        }

        [Test]
        public void EngineOut_CanHold_BigButtonStillShutsDown()
        {
            var strong = new AscentVehicle
            {
                HasAnomaly = true, AnomalyStage = 0, AnomalyEngine = 3, Engines = 9, Redundant = true,
                HotChamber = false, ThrottleMin = 0.5, QLimPa = 35000,
            };
            var d = EngineOutOpen(strong);
            var p = d.OpenPrompts()[0];
            Assert.IsFalse(p.PrimaryDeclines);
            Assert.AreEqual("SHUT DOWN ENGINE 4 (safe)", p.Primary);
            Assert.AreEqual("RIDE IT OUT (keep it burning)", p.Alternate);
            Assert.IsTrue(d.ChoosePrimary(AscentCall.EngineOut));
            Assert.IsFalse(d.Command.Lost);
            Assert.AreEqual(8.0 / 9.0, d.Command.ThrottleScale, 1e-9, "the lost engine costs its share of thrust");
        }
    }

    public class RideItOutRiskTests
    {
        /// <summary>Every engine part of <paramref name="d"/> set above its rated chamber pressure (pc &gt; pc rated), the
        /// existing hot-chamber model (AscentVehicle.HotChamber).</summary>
        static Design Hot(Catalog c, Design d)
        {
            foreach (var part in d.stages)
            {
                if (part == null || string.IsNullOrEmpty(part.engine) || !c.EngineById.TryGetValue(part.engine, out var e) || !(e.pcRated > 0)) continue;
                part.engineParams = part.engineParams ?? new EngineParams();
                part.engineParams.pc = e.pcRated * 1.05;
                part.engineParams.pcSet = true;
            }
            return d;
        }

        /// <summary>Physics' PR2 condition: the risk the ride-it-out label names exists in the sim. Across the 12 briefing
        /// seeds, KH-20's Starter rocket with hot chambers rides out its engine-out (the big button) and comes apart.</summary>
        [Test]
        public void HotChamber_RideItOut_LosesTheEngine_AcrossBriefingSeeds()
        {
            var catalog = StarterFixture.Catalog;
            var mission = StarterFixture.Campaign.ById("m20");
            int calls = 0, lost = 0;
            var log = new System.Text.StringBuilder();
            for (ulong seed = 1; seed <= 12; seed++)
            {
                var design = Hot(catalog, Design.FromJson(StarterFixture.Rocket("m20").BuildDesign()));
                var r = PlayLoop.Fly(catalog, mission, design, false, PlayLoop.Style.TapGreenPrimary, seed, out var taps);
                log.Append(seed).Append(": ").Append(taps).Append(' ').Append(r.failure?.kind ?? r.outcome).Append('\n');
                if (!taps.Contains("EngineOut+")) continue;
                calls++;
                if (r.failure != null && r.failure.kind == "engine-overpressure") lost++;
            }
            TestContext.WriteLine(log.ToString());
            Assert.Greater(calls, 0, "an engine-out call was ridden out on some seed\n" + log);
            Assert.Greater(lost, 0, "a hot chamber ridden out loses the engine on some seed\n" + log);
            Assert.AreEqual(calls, lost, "the model is deterministic: every hot ride-out comes apart\n" + log);
        }

        /// <summary>The tolerated case: shutdown is safe, and the alternate (ride it out) on a hot chamber fails.
        /// On a cool chamber the sim models no risk, so the label promises none.</summary>
        [Test]
        public void Tolerated_RideItOutAlternate_FailsOnAHotChamber_AndPromisesNoRiskWhenCool()
        {
            foreach (bool hot in new[] { true, false })
            {
                var v = new AscentVehicle
                {
                    HasAnomaly = true, AnomalyStage = 0, AnomalyEngine = 3, Engines = 9, Redundant = true,
                    HotChamber = hot, ThrottleMin = 0.5, QLimPa = 35000,
                };
                var d = AscentDirector.Start(v, null, null, false, true, false);
                d.Advance(new AscentSample
                {
                    T = 20, AltM = 8000, SpeedMps = 300, QPa = 4000, StageIndex = 0, Thrusting = true,
                    PropFraction = 0.8, MassKg = 500000, ThrustN = 1.2e7,
                }, 0.05);
                var p = d.OpenPrompts()[0];
                Assert.AreEqual(hot ? "RIDE IT OUT (hot engine will fail)" : "RIDE IT OUT (keep it burning)", p.Alternate);
                Assert.IsTrue(d.ChooseAlternate(AscentCall.EngineOut));
                Assert.AreEqual(hot, d.Command.Lost);
                if (hot) Assert.AreEqual("engine-overpressure", d.Command.LossKind);
            }
        }
    }

    /// <summary>A live seed-1 flight with a scripted player (LivePadLoop.Fly plus taps), for the play-style checks.</summary>
    static class PlayLoop
    {
        public enum Style { AutoOn, Idle, DeclineAll, TapGreenPrimary, TapGreenSmart, TapEarly, TapLast, ConfirmPitchOnly }

        public static SimResult Fly(Catalog catalog, CampaignMission mission, Design design, bool autoAssist, Style style, out string taps)
            => Fly(catalog, mission, design, autoAssist, style, 1, out taps);

        public static SimResult Fly(Catalog catalog, CampaignMission mission, Design design, bool autoAssist, Style style, ulong seed, out string taps)
        {
            taps = "";
            ulong hash = FlightBatch.DesignHash(design);
            var card = MissionBriefing.Load(Golden.Root, mission.Index, hash, seed);
            var weather = card?.Wind;
            double target = Stack.RequiredDeltaV(catalog, design.payloadOrbit).targetAlt;
            if (MissionAim.IsApogeeHop(mission)) target = mission.GuidanceAltM;
            double qLim = FailureMonitor.StructuralQLimPa(catalog, design);
            var monitor = FailureMonitor.Engages(design) ? FailureMonitor.Create(catalog, design, target, hash, seed) : null;
            var director = MissionFlightSetup.Director(catalog, design, monitor?.Events, weather, qLim, FailureMonitor.GentleTier(design),
                target, hash, seed, autoAssist, false, false, card?.Bonuses);
            var site = new MissionFlightSite { Wind = weather };
            if (card != null && card.Site != null) { site.HasLatitude = true; site.LatitudeDeg = card.Site.LatitudeDeg; }
            var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt, site);
            var opts = setup.Options;
            if (monitor != null) opts.monitor = monitor;
            var sim = new AscentSim(setup.Stages, opts);
            var directed = new DirectedFlight(sim, director, opts);
            var checks = new PlayedChecks(catalog, design, setup.Stages, opts, sim);
            double clock = 0;
            int frames = 0;
            bool stop = false;
            while (!sim.Done && !stop && frames++ < 400000)
            {
                clock += 1.0 / 60.0;
                if (style != Style.AutoOn && style != Style.Idle)
                    foreach (var p in director.OpenPrompts())
                    {
                        bool now = style == Style.TapEarly || (style == Style.TapLast ? p.LeftS <= 0.25 : p.InGreen);
                        if (style == Style.ConfirmPitchOnly && p.Call != AscentCall.Pitch) continue;
                        if (!now) continue;
                        bool primary = style != Style.DeclineAll && !(style == Style.TapGreenSmart && p.Call == AscentCall.Pitch);
                        bool ok = primary ? director.ChoosePrimary(p.Call) : director.ChooseAlternate(p.Call);
                        if (ok) taps += p.Call + (primary ? "+" : "-") + "@" + sim.T.ToString("0") + ",";
                    }
                int guard = 0;
                while (sim.T < clock && !sim.Done && guard++ < 64)
                {
                    int before = sim.Events.Count;
                    directed.Step();
                    checks.NoteEvents(sim, before);
                    if (checks.StopForBreakup(sim)) { stop = true; break; }
                }
            }
            Assert.IsTrue(sim.Done || stop, "the live flight ends");
            return directed.Finish();
        }
    }
}
