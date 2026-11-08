// S6 active ascent. Timing grades, design bounds, seed replay, and a live nudge that
// stays off the golden path until a flight actually sets it.
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class AscentDecisionTests
    {
        static readonly string[] Forbidden =
        {
            "spacex", "falcon", "starlink", "nasa", "blue origin", "ula", "starbase", "soyuz",
        };

        [Test]
        public void MaxQSetting_FliesAutomatically_NoCallNoGradeNoMedalCredit()
        {
            // DECISIONS #89 (Physics option b): the design's own max-Q law flies the throttle bucket like a flight
            // computer. There is no call to take or skip, with or without auto-assist, so it is never graded and never
            // counts for or against the medal; the director commands nothing for it.
            var vehicle = new AscentVehicle { QLimPa = 35000, SafetyFactor = 1.4, ThrottleMin = 0.4, FlightCode = "KH-14", MaxQSet = true, MaxQRaw = 60 };
            foreach (bool assist in new[] { true, false })
            {
                var d = AscentDirector.Start(vehicle, null, null, assist, true, false);
                d.Advance(MaxQSample(), 0.05);
                Assert.IsFalse(d.IsOpen(AscentCall.MaxQ), "never offered, auto " + assist);
                Assert.IsFalse(d.Choose(AscentCall.MaxQ, true));
                Assert.IsFalse(d.Skip(AscentCall.MaxQ));
                Assert.IsFalse(d.IsDone(AscentCall.MaxQ));
                Assert.AreEqual(AscentGrade.None, d.GradeOf(AscentCall.MaxQ));
                Assert.AreEqual(1, d.Command.ThrottleScale, 1e-12, "the design law flies the cap, not the director");
                Assert.IsFalse(d.Command.Lost);
                Assert.AreEqual(0, d.Score().Decided, "no max-Q credit either way");
                Assert.AreEqual(AscentMedal.None, d.Score().Medal);
            }
        }

        [Test]
        public void Autopilot_KeepsTheDesignSettings_ButAPlayerTapStillTakesEitherCall()
        {
            // #313 (b), DECISIONS #83. No design max-Q throttle: the call is not offered at all (Physics: "a no-op or
            // greyed out, never a dip that sticks"), so neither the autopilot nor a tap can change the throttle, and it
            // never counts for or against the medal.
            var plain = new AscentVehicle { QLimPa = 35000, ThrottleMin = 0.4 };
            foreach (bool assist in new[] { true, false })
            {
                var d = AscentDirector.Start(plain, null, null, assist, false, false);
                d.Advance(MaxQSample(), 0.05);
                Assert.IsFalse(d.IsOpen(AscentCall.MaxQ), "not offered, auto " + assist);
                Assert.IsFalse(d.Choose(AscentCall.MaxQ, true), "a tap on an absent call does nothing");
                Assert.IsFalse(d.Skip(AscentCall.MaxQ));
                Assert.AreEqual(1, d.Command.ThrottleScale, 1e-12, "the autopilot does not invent a bucket");
                Assert.AreEqual(0, d.Score().Decided, "an absent call costs no medal credit");
            }

            // Pitch (DECISIONS #76): accept confirms the autopilot's pitch program. Neither the autopilot nor a tap adds an offset.
            var sample = new AscentSample { T = 5, AltM = 4000, SpeedMps = 120, QPa = 1000, Thrusting = true, PropFraction = 1 };
            var auto = AscentDirector.Start(new AscentVehicle { Stages = 1 }, null, null, true, false, false);
            var tap = AscentDirector.Start(new AscentVehicle { Stages = 1 }, null, null, false, false, false);
            tap.Advance(sample, 0.5);
            Assert.IsTrue(tap.Choose(AscentCall.Pitch, true));
            for (int i = 0; i < 40; i++)
            {
                sample.T += 0.5;
                auto.Advance(sample, 0.5);
            }
            Assert.IsTrue(auto.IsDone(AscentCall.Pitch));
            Assert.AreEqual(0, auto.NudgeDeg, 1e-12);
            Assert.AreEqual(0, auto.Command.PitchOffsetRad, 1e-12);
            Assert.AreEqual(0, tap.NudgeDeg, 1e-12, "a confirmed pitch program holds no nudge");
            Assert.AreEqual(0, tap.Command.PitchOffsetRad, 1e-12);
            Assert.IsTrue(tap.IsDone(AscentCall.Pitch));
        }

        [Test]
        public void PlayerTapInTheGreen_EarnsGold()
        {
            // DECISIONS #89: with no max-Q call, a tap in the green on a call that remains (the pitch call) earns gold.
            var director = AscentDirector.Start(new AscentVehicle { Stages = 1 }, null, null, false, false, false);
            var sample = new AscentSample { T = 5, AltM = 4000, SpeedMps = 120, QPa = 1000, Thrusting = true, PropFraction = 1 };
            director.Advance(sample, 0.05);
            Assert.IsTrue(director.IsOpen(AscentCall.Pitch));
            for (int i = 0; i < 400 && !director.OpenPrompts()[0].InGreen; i++) { sample.T += 0.05; director.Advance(sample, 0.05); }
            Assert.IsTrue(director.Choose(AscentCall.Pitch, true));
            Assert.AreEqual(AscentGrade.Perfect, director.GradeOf(AscentCall.Pitch));
            var score = director.Score();
            Assert.IsTrue(score.PlayerFlew);
            Assert.AreEqual(AscentMedal.Gold, score.Medal);
            Assert.AreEqual(1, score.StarBonus);
            Assert.AreEqual(3, AscentDecisions.ApplyStars(2, score.Medal));
            Assert.AreEqual(3, AscentDecisions.ApplyStars(3, score.Medal));
            Assert.AreEqual(2, AscentDecisions.ApplyStars(2, AscentMedal.Silver));
            Assert.AreEqual(2, MissionScore.Stars(true, 0.02, 100, 100, false, false, 5, 6));
        }

        [Test]
        public void WeakRocket_CannotBeSavedByEitherEngineCall()
        {
            var weak = new AscentVehicle
            {
                HasAnomaly = true,
                AnomalyStage = 0,
                AnomalyEngine = 3,
                Engines = 4,
                Redundant = false,
                HotChamber = true,
                ThrottleMin = 0.5,
                QLimPa = 35000,
            };
            var shut = FlyEngine(weak, true);
            Assert.IsTrue(shut.Command.Lost);
            Assert.AreEqual("engine-out", shut.Command.LossKind);
            var ride = FlyEngine(weak, false);
            Assert.IsTrue(ride.Command.Lost);
            Assert.AreEqual("engine-overpressure", ride.Command.LossKind);

            var strong = new AscentVehicle
            {
                HasAnomaly = true,
                AnomalyStage = 0,
                AnomalyEngine = 3,
                Engines = 9,
                Redundant = true,
                HotChamber = true,
                ThrottleMin = 0.4,
            };
            var saved = FlyEngine(strong, true);
            Assert.IsFalse(saved.Command.Lost);
            Assert.AreEqual(8.0 / 9.0, saved.Command.ThrottleScale, 1e-9);
            var hotRide = FlyEngine(strong, false);
            Assert.IsTrue(hotRide.Command.Lost, "redundant avionics do not make a hot chamber safe to ride");
        }

        [Test]
        public void ThrottleCannotDropBelowTheEngineFloor()
        {
            Assert.AreEqual(1, AscentDecisions.OfferedThrottle(false, 70, 1), 1e-12);
            Assert.AreEqual(0.70, AscentDecisions.OfferedThrottle(false, 0, 0.4), 1e-12);
            Assert.AreEqual(0.70, AscentDecisions.OfferedThrottle(true, 0.70, 0.4), 1e-12, "a designer fraction stays a fraction");
            Assert.AreEqual(0.65, AscentDecisions.OfferedThrottle(true, 65, 0.4), 1e-12, "a percent above 1.5 is a percent");
            var stuck = new AscentVehicle { QLimPa = 35000, ThrottleMin = 1 };
            var director = AscentDirector.Start(stuck, null, null, true, false, false);
            director.Advance(MaxQSample(), 0.05);
            Assert.AreEqual(1, director.Command.ThrottleScale, 1e-9);
        }

        [Test]
        public void StagingWindow_ClearsSeededJitter_AndALateCallLeavesIt()
        {
            var schedule = Jittered();
            Assert.Less(schedule.SepScale(1), 1);
            var hit = StageDirector(schedule, false);
            hit.Advance(StageSample(), 0.05);
            Assert.IsTrue(hit.IsOpen(AscentCall.Staging));
            Assert.IsTrue(hit.Choose(AscentCall.Staging, true));
            Assert.AreEqual(AscentGrade.Good, hit.GradeOf(AscentCall.Staging));
            Assert.AreEqual(1, schedule.SepScale(1), 1e-12);

            var missed = Jittered();
            double before = missed.SepScale(1);
            var late = StageDirector(missed, false);
            late.Advance(StageSample(), 0.05);
            Assert.IsTrue(late.Skip(AscentCall.Staging));
            Assert.AreEqual(AscentGrade.Late, late.GradeOf(AscentCall.Staging));
            Assert.AreEqual(before, missed.SepScale(1), 1e-12);
            Assert.IsFalse(late.Command.Lost);
        }

        [Test]
        public void FairingHeat_UsesHalfRhoVCubed_AndALowJettisonStillBurns()
        {
            double flux = AscentDecisions.HeatFlux(0, 0);
            Assert.AreEqual(0, flux, 1e-12);
            double rho = Atmosphere.At(20000).rho;
            double speed = 800;
            Assert.AreEqual(0.5 * rho * speed * speed * speed, AscentDecisions.HeatFlux(20000, speed), 1e-6);

            var hot = new AscentVehicle { HasFairing = true, JettisonAltM = 30000, MaxHeatFlux = 1, Stages = 1 };
            var hurt = AscentDirector.Start(hot, null, null, false, false, false);
            hurt.Advance(new AscentSample { T = 40, AltM = 20000, SpeedMps = 800, Thrusting = true, PropFraction = 1 }, 0.05);
            Assert.IsTrue(hurt.Choose(AscentCall.Fairing, true));
            Assert.IsTrue(hurt.Score().PayloadDamaged);
            Assert.Greater(hurt.Score().FairingFlux, 1);

            var safe = new AscentVehicle { HasFairing = true, JettisonAltM = 110000, MaxHeatFlux = 1e12, Stages = 1 };
            var ok = AscentDirector.Start(safe, null, null, false, false, false);
            ok.Advance(new AscentSample { T = 80, AltM = 100000, SpeedMps = 50, Thrusting = false, PropFraction = 1 }, 0.05);
            Assert.IsTrue(ok.Choose(AscentCall.Fairing, true));
            Assert.IsFalse(ok.Score().PayloadDamaged);
        }

        [Test]
        public void Nudge_StaysInsideThreeDegrees_AndMarksTheAmberPath()
        {
            var director = AscentDirector.Start(new AscentVehicle(), null, null, false, true, false);
            // DECISIONS #80: clear of the tower first (Pr4NudgeGateTests pins the gate itself).
            director.Advance(new AscentSample { T = 6, AltM = 400, SpeedMps = AscentDecisions.NudgeGateMps, Thrusting = true, PropFraction = 1 }, 0.05);
            director.SetNudge(9);
            Assert.AreEqual(3, director.NudgeDeg, 1e-12);
            director.NudgePitch(-10);
            Assert.AreEqual(-3, director.NudgeDeg, 1e-12);
            director.Advance(new AscentSample { T = 1, AltM = 100, SpeedMps = 20, Thrusting = true, PropFraction = 1 }, 0.05);
            Assert.IsTrue(director.Track.Nudged);
            Assert.AreEqual(-3 * Math.PI / 180.0, director.Command.PitchOffsetRad, 1e-9);
            double plan = AscentTrack.PlanDownrange(10000, 200000);
            Assert.Greater(plan, 0);
            Assert.Less(plan, AscentTrack.PlanDownrange(200000, 200000));
        }

        [Test]
        public void TwoPrompts_IsTheCap_AndReduceMotionHoldsTheWindowOpen()
        {
            var busy = new AscentVehicle
            {
                // DECISIONS #89: no max-Q call; a staging call is the third candidate instead.
                Stages = 2,
                HasAnomaly = true,
                AnomalyEngine = 1,
                Engines = 9,
                Redundant = true,
                QLimPa = 35000,
            };
            var director = AscentDirector.Start(busy, null, null, false, true, false);
            director.Advance(new AscentSample
            {
                T = 20, AltM = 11000, SpeedMps = 500, QPa = 31000, ThrustN = 8e6, MassKg = 4e5,
                Thrusting = true, PropFraction = 0.1,
            }, 0.05);
            Assert.AreEqual(2, director.OpenCount);
            Assert.AreEqual(AscentLayout.MaxPrompts, director.OpenPrompts().Length);
            int open = (director.IsOpen(AscentCall.EngineOut) ? 1 : 0) + (director.IsOpen(AscentCall.Pitch) ? 1 : 0) + (director.IsOpen(AscentCall.Staging) ? 1 : 0);
            Assert.AreEqual(2, open, "three calls ready, two prompts open");

            var held = AscentDirector.Start(new AscentVehicle { Stages = 1 }, null, null, false, false, true);
            var plain = AscentDirector.Start(new AscentVehicle { Stages = 1 }, null, null, false, false, false);
            var sample = new AscentSample { T = 5, AltM = 4000, SpeedMps = 120, QPa = 1000, Thrusting = true, PropFraction = 1 };
            for (int i = 0; i < 21; i++)
            {
                sample.T += 0.5;
                held.Advance(sample, 0.5);
                plain.Advance(sample, 0.5);
            }
            Assert.IsTrue(held.IsOpen(AscentCall.Pitch), "14 s window is still open");
            Assert.IsTrue(plain.IsDone(AscentCall.Pitch));
            Assert.AreEqual(230f, AscentLayout.ActionZonePt);
            Assert.AreEqual(52f, AscentLayout.ButtonMinPt);
            Assert.AreEqual(104f, AscentLayout.Px(52f, 2f), 0.01f);
        }

        [Test]
        public void FuelBalance_IsDeterministic_AndCrossfeedIsTheOnlyFix()
        {
            bool a = AscentDecisions.FuelImbalance(9, 13, 2, false, false);
            bool b = AscentDecisions.FuelImbalance(9, 13, 2, false, false);
            Assert.AreEqual(a, b);
            Assert.IsFalse(AscentDecisions.FuelImbalance(9, 13, 2, false, true));
            Assert.IsFalse(AscentDecisions.FuelImbalance(9, 13, 0, false, false));
            int bare = 0, shared = 0;
            for (ulong s = 1; s <= 400; s++)
            {
                if (AscentDecisions.FuelImbalance(3, s, 2, false, false)) bare++;
                if (AscentDecisions.FuelImbalance(3, s, 2, true, false)) shared++;
            }
            Assert.Greater(bare, shared);
            Assert.Greater(bare, 50);
            Assert.Less(shared, bare);

            var plumbed = new AscentVehicle { FuelEvent = true, Crossfeed = true, StrapOns = 2, Stages = 1 };
            var fixedFeed = AscentDirector.Start(plumbed, null, null, false, false, false);
            fixedFeed.Advance(new AscentSample { T = 12, AltM = 6000, SpeedMps = 200, QPa = 1000, Thrusting = true, PropFraction = 0.9 }, 4);
            Assert.IsTrue(fixedFeed.IsOpen(AscentCall.FuelBalance));
            fixedFeed.Choose(AscentCall.FuelBalance, true);
            Assert.AreEqual(1, fixedFeed.Command.ThrottleScale, 1e-9);

            var openTanks = new AscentVehicle { FuelEvent = true, Crossfeed = false, StrapOns = 2, Stages = 1 };
            var stuck = AscentDirector.Start(openTanks, null, null, false, false, false);
            stuck.Advance(new AscentSample { T = 12, AltM = 6000, SpeedMps = 200, QPa = 1000, Thrusting = true, PropFraction = 0.9 }, 4);
            stuck.Choose(AscentCall.FuelBalance, true);
            Assert.AreEqual(AscentDecisions.FuelPenalty, stuck.Command.ThrottleScale, 1e-9);
            Assert.IsFalse(stuck.Command.Lost);
        }

        [Test]
        public void Gentle_SkipsRandomCalls_AndCopyStaysFictional()
        {
            var vehicle = new AscentVehicle { HasAnomaly = true, Gentle = true, FuelEvent = true, Stages = 1 };
            var director = AscentDirector.Start(vehicle, null, null, true, false, false);
            director.Advance(MaxQSample(), 0.05);
            Assert.IsFalse(director.IsOpen(AscentCall.EngineOut));
            Assert.IsFalse(director.IsDone(AscentCall.EngineOut));
            string blob = "";
            var prompts = director.OpenPrompts();
            for (int i = 0; i < prompts.Length; i++)
                blob += prompts[i].Eyebrow + prompts[i].Quote + prompts[i].Detail + prompts[i].Primary + prompts[i].Alternate;
            blob += director.Score().Summary;
            string lower = blob.ToLowerInvariant();
            foreach (var bad in Forbidden)
                Assert.IsFalse(lower.Contains(bad), bad);
        }

        [Test]
        public void Bonuses_ReadLossesAndTargets()
        {
            Assert.IsTrue(AscentDecisions.FuelEfficient(1000, 100, 50));
            Assert.IsFalse(AscentDecisions.FuelEfficient(100, 200, 200));
            var vehicle = new AscentVehicle
            {
                BonusAltitude = true,
                AltitudeTargetM = 10000,
                BonusSpeed = true,
                SpeedTargetMps = 400,
                BonusFuel = true,
                BonusPeakG = true,
                BonusMaxG = 4,
                Stages = 1,
            };
            var director = AscentDirector.Start(vehicle, null, null, false, false, false);
            director.Advance(new AscentSample
            {
                T = 30, AltM = 12000, SpeedMps = 500, QPa = 1000, G = 2.4,
                LossGravity = 800, LossDrag = 40, LossSteering = 20,
                Thrusting = true, PropFraction = 0.5,
            }, 0.05);
            var score = director.Score();
            Assert.IsTrue(score.AltitudeHit);
            Assert.IsTrue(score.SpeedHit);
            Assert.IsTrue(score.FuelHit);
            Assert.IsTrue(score.PeakGHit);
        }

        [Test]
        public void SameDesign_BuildsTheSameFuelRoll_AndEngineOutRuleMatchesFailureModel()
        {
            var design = new Design
            {
                version = 3,
                avionicsTier = "redundant",
                safetyFactor = 1.4,
                missionId = "m4",
                stages = new System.Collections.Generic.List<StagePart>
                {
                    new StagePart { engine = "gg-kerolox", engineCount = 9, propellant = "kerolox", propMass = 400000, diameter = 3.7, material = "al-2195" },
                    new StagePart { engine = "gg-kerolox-vac", engineCount = 1, propellant = "kerolox", propMass = 90000, diameter = 3.7, material = "al-2195" },
                },
                strapOns = new StagePart { engine = "gg-kerolox", engineCount = 1, count = 2, crossfeed = false, propellant = "kerolox", propMass = 80000, diameter = 2, material = "al-2195" },
            };
            var catalog = Catalog.LoadFromRepo(Golden.Root);
            var card = MissionBriefing.Load(Golden.Root, 4, 7, 13);
            var a = AscentDecisions.FromDesign(design, catalog, null, card.Wind, 35000, 200000, false, card.Bonuses, 7, 13);
            var b = AscentDecisions.FromDesign(design, catalog, null, card.Wind, 35000, 200000, false, card.Bonuses, 7, 13);
            Assert.AreEqual(a.FuelEvent, b.FuelEvent);
            Assert.AreEqual("KH-14", a.FlightCode);
            Assert.IsTrue(a.BonusPeakG);
            Assert.AreEqual(4, a.BonusMaxG, 1e-9);
            Assert.IsFalse(a.Crossfeed);
            Assert.Greater(a.Stages, 1);
            Assert.IsTrue(AscentDecisions.ToleratesEngineOut((int)FailureModel.EngineOutMinCount, true, 2e6, 1e5));
            Assert.IsFalse(AscentDecisions.ToleratesEngineOut((int)FailureModel.EngineOutMinCount - 1, true, 2e6, 1e5));
            Assert.IsFalse(AscentDecisions.ToleratesEngineOut(9, false, 2e6, 1e5));
        }

        [Test]
        public void LiveNudge_LeavesTheUntouchedSimOnTheGoldenStep()
        {
            var opts = new SimOptions { dt = 0.05, tMax = 20, vacuum = true };
            var plain = new AscentSim(Headless.MediumLifter(), opts);
            var identity = new AscentSim(Headless.MediumLifter(), new SimOptions { dt = 0.05, tMax = 20, vacuum = true });
            identity.LiveThrottleScale = 1;
            identity.LivePitchOffsetRad = 0;
            var nudged = new AscentSim(Headless.MediumLifter(), new SimOptions { dt = 0.05, tMax = 20, vacuum = true });
            nudged.LiveThrottleScale = 0.7;
            nudged.LivePitchOffsetRad = -2 * Math.PI / 180.0;
            for (int i = 0; i < 80; i++)
            {
                plain.Tick();
                identity.Tick();
                nudged.Tick();
            }
            Assert.AreEqual(plain.Y[0], identity.Y[0], 0);
            Assert.AreEqual(plain.Y[1], identity.Y[1], 0);
            Assert.AreEqual(plain.Y[2], identity.Y[2], 0);
            Assert.AreEqual(plain.Y[3], identity.Y[3], 0);
            Assert.AreEqual(plain.Y[4], identity.Y[4], 0);
            Assert.AreEqual(plain.PitchCmd, identity.PitchCmd, 0);
            Assert.IsTrue(plain.Y[4] != nudged.Y[4] || plain.PitchCmd != nudged.PitchCmd);
        }

        static AscentSample MaxQSample() => new AscentSample
        {
            T = 58,
            AltM = 11200,
            SpeedMps = 512,
            QPa = 31000,
            G = 2.6,
            Thrusting = true,
            PropFraction = 0.7,
            MassKg = 400000,
            ThrustN = 7e6,
        };

        static AscentSample StageSample() => new AscentSample
        {
            T = 140,
            AltM = 60000,
            SpeedMps = 1800,
            QPa = 2000,
            StageIndex = 0,
            Thrusting = true,
            PropFraction = 0.08,
            MassKg = 80000,
            ThrustN = 1e6,
        };

        static AscentDirector FlyEngine(AscentVehicle vehicle, bool shutdown)
        {
            var director = AscentDirector.Start(vehicle, null, null, false, true, false);
            director.Advance(new AscentSample
            {
                T = 20,
                AltM = 8000,
                SpeedMps = 300,
                QPa = 4000,
                StageIndex = 0,
                Thrusting = true,
                PropFraction = 0.8,
                MassKg = 500000,
                ThrustN = 1.2e7,
            }, 0.05);
            Assert.IsTrue(director.IsOpen(AscentCall.EngineOut), vehicle.Engines + " engines");
            director.Choose(AscentCall.EngineOut, shutdown);
            return director;
        }

        static AscentDirector StageDirector(EventSchedule schedule, bool auto)
        {
            var vehicle = new AscentVehicle { Stages = 2, CanStage = true, QLimPa = 80000, ThrottleMin = 0.4 };
            return AscentDirector.Start(vehicle, schedule, null, auto, false, false);
        }

        static EventSchedule Jittered()
        {
            var stages = new[]
            {
                new EventStage { engineCount = 9, pc = 97, pcRated = 97 },
                new EventStage { engineCount = 1, pc = 97, pcRated = 97 },
            };
            var vehicle = new EventVehicle { avionicsTier = "basic", safetyFactor = 1.0, canStage = true };
            for (ulong s = 1; s < 400; s++)
            {
                var schedule = EventModel.Roll(11, s, stages, vehicle, false);
                if (schedule.SepScale(1) < 1) return schedule;
            }
            Assert.Fail("no staging jitter in the first seeds");
            return null;
        }
    }
}
