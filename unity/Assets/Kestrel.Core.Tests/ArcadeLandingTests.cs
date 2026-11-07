// DECISIONS #79: the player flies the final landing burn. Every landing mission starter (KH-6, 7, 10, 13, 15, 19) is
// handed over from its own FlightBatch separation state (the state BoosterLanding.FlyPlanned flies), then flown at the
// phone's 60 fps frame through ArcadeLandingSession by a scripted pilot that only reads the HUD readout. A good pilot
// lands Good or Perfect; no input and a too-late burn crash. The input is a RecoveryStick into the same RecoverySim.Step
// the autopilot uses, so these are the real dynamics. Also: the multipliers, the hand-over, the cue, slow motion and
// the autopilot path (Auto-assist keeps today's landing, bit for bit).
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class ArcadeLandingTests
    {
        const double Frame = 1.0 / 60.0;

        sealed class Handover
        {
            public CampaignMission Mission;
            public Design Design;
            public double Down, Alt, Speed, Pitch;
        }

        static Dictionary<int, Handover> states;
        static Catalog catalog;
        static ArcadeLandingTune fileTune;

        static ArcadeLandingTune Tune => fileTune ??= ArcadeLandingTune.Load(Golden.Root);

        /// <summary>The starter's batch separation (one FlightBatch flight per mission, cached for the fixture).</summary>
        static Handover State(int index)
        {
            if (states == null)
            {
                states = new Dictionary<int, Handover>();
                catalog = Catalog.LoadFromRepo(Golden.Root);
            }
            if (states.TryGetValue(index, out var h)) return h;
            var hangar = HangarData.LoadFromRepo(Golden.Root);
            var campaign = Campaign.Load(Golden.Root);
            var mission = campaign.Mission(index);
            var design = Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, mission).Design);
            var flight = FlightBatch.Fly(catalog, mission, design, campaign.Seeds[0]);
            Assert.IsFalse(double.IsNaN(flight.separationDownM), "KH-" + index + " separates");
            h = new Handover
            {
                Mission = mission,
                Design = design,
                Down = flight.separationDownM,
                Alt = flight.separationAltKm * 1000.0,
                Speed = flight.separationSpeedMps,
                Pitch = flight.separationPitchDeg,
            };
            states[index] = h;
            return h;
        }

        [TearDown]
        public void Drop() => RecoveryLog.Clear();

        static ArcadeLandingSession Start(Handover h, bool autoAssist, ArcadeLandingTune tune = null)
        {
            var landing = BoosterLanding.Arm(catalog, h.Design);
            Assert.AreEqual(BoosterFate.Planned, landing.Fate);
            landing.Watch(100, landing.CoreSerial, h.Down, h.Alt, h.Speed, h.Pitch, true, false);
            Assert.IsTrue(landing.Watch(100.02, landing.CoreSerial + 1, 0, 0, 0, 0, false, false), "separation starts the landing");
            return new ArcadeLandingSession(landing, tune ?? Tune, autoAssist);
        }

        delegate ArcadeInput PilotFn(ArcadeLandingSession s);

        sealed class Flown
        {
            public string Log;
            public ArcadeReadout AtHandover;
            public bool SawLightNow, SawLate, SawHandoverCall, SawSlowMo;
            public double SlowWallSec;
        }

        static Flown Fly(ArcadeLandingSession s, PilotFn pilot)
        {
            var f = new Flown();
            int frames = 0;
            bool wasHanded = false;
            while (!s.Landing.Touched && frames++ < 200000)
            {
                if (s.PlayerFlying) s.Input = pilot(s);
                s.Step(Frame);
                if (s.HandedOver && !wasHanded)
                {
                    wasHanded = true;
                    f.AtHandover = s.Readout;
                    f.SawHandoverCall = s.CallTitle == ArcadeLandingSession.HandoverTitle && s.CallVisible;
                }
                if (s.Readout.Cue == LightCue.LightNow) f.SawLightNow = true;
                if (s.Readout.Cue == LightCue.Late) f.SawLate = true;
                if (s.Scale.Scale < 1) { f.SawSlowMo = true; f.SlowWallSec += Frame; }
            }
            Assert.IsTrue(s.Landing.Touched, "touches down");
            var v = s.Landing.Verdict;
            var r = f.AtHandover;
            f.Log = "handover alt " + r.AltM.ToString("0") + " sink " + r.SinkMps.ToString("0") + " stop " + r.StopAltM.ToString("0")
                + " light in " + r.LightInS.ToString("0.0") + " s off " + r.OffsetM.ToString("0") + " prop " + r.PropKg.ToString("0")
                + " | " + (v.Cause ?? "clean") + " " + v.GradeName + " sink " + v.Sink.ToString("0.00") + " miss " + v.MissM.ToString("0.0")
                + " tilt " + v.TiltDeg.ToString("0.0") + " prop " + v.PropLeft.ToString("0") + (s.Score != null ? " pts " + s.Score.Points : "");
            return f;
        }

        /// <summary>
        /// A competent pilot who reads only the HUD: waits for LIGHT NOW, then rides 75% of the full-slider stopping curve
        /// (T/W max is on the slider), leans toward the deck centre with the drift it reads, stands upright for the last
        /// metres and sets down at about a metre a second.
        /// </summary>
        static ArcadeInput GoodPilot(ArcadeLandingSession s)
        {
            var r = s.Readout;
            var input = s.Input;
            const double g = 9.81;
            bool lit = r.Throttle > 0.005;
            if (!lit && r.Cue != LightCue.LightNow && r.Cue != LightCue.Late) return ArcadeInput.Idle;
            double aMax = (r.TwMax - 1) * g;
            if (aMax < 1) aMax = 1;
            bool high = r.AltM > 20;
            double vT = high ? Math.Sqrt(2 * 0.75 * aMax * (r.AltM - 12)) + 1.0 : 0.8 + (r.AltM / 20) * 1.5;
            double tw = 1 + ((high ? 0.75 * aMax : 0) + 1.5 * (r.SinkMps - vT)) / g;
            if (tw > r.TwMax) tw = r.TwMax;
            if (tw < 0.1) tw = 0.1;
            input.Throttle = s.SliderForTw(tw);
            // The drift that closes the offset by touchdown, capped; lean for it; nearly upright for the last metres.
            double tGo = r.SinkMps > 2 ? Math.Max(3, r.AltM / r.SinkMps) : 6;
            double want = -r.OffsetM / tGo;
            double cap = r.AltM > 150 ? 12 : r.AltM > 40 ? 4 : 1.5;
            if (want > cap) want = cap;
            if (want < -cap) want = -cap;
            double steer = (want - r.DriftMps) / 4.0;
            double lim = r.AltM > 60 ? 1 : r.AltM > 15 ? 0.35 : 0.15;
            if (steer > lim) steer = lim;
            if (steer < -lim) steer = -lim;
            input.Steer = steer;
            return input;
        }

        static ArcadeInput NoInput(ArcadeLandingSession s) => ArcadeInput.Idle;

        /// <summary>Ignores LIGHT NOW and TOO LATE until the booster is under 70% of its stopping height, then floors the slider upright.</summary>
        static ArcadeInput TooLatePilot(ArcadeLandingSession s)
        {
            var r = s.Readout;
            if (r.Throttle > 0.005 || r.BurnRatio < 0.7) return new ArcadeInput { Throttle = 1, Steer = 0 };
            return ArcadeInput.Idle;
        }

        [TestCase(6)] [TestCase(7)] [TestCase(10)] [TestCase(13)] [TestCase(15)] [TestCase(19)]
        public void GoodPilot_LandsGoodOrPerfect_OnTheMissionsSite(int index)
        {
            var h = State(index);
            var s = Start(h, false);
            Assert.AreEqual(ArcadePilot.Player, s.Pilot);
            Assert.AreEqual(h.Mission.LandingMode, s.Landing.Craft.Mode, "Haven for a barge mission, the pad for RTLS");
            var f = Fly(s, GoodPilot);
            string log = "KH-" + index + " " + s.Landing.Craft.Mode + " " + f.Log;
            TestContext.WriteLine(log);
            Assert.IsTrue(s.HandedOver, log);
            // A safe hand-over: no higher than the cap, the player gets a lead before the cue, and the stage is falling.
            Assert.LessOrEqual(s.HandoverAltM, Tune.HandoverMaxAltM, log);
            Assert.Greater(f.AtHandover.LightInS, 1.0, "time to read the HUD before LIGHT NOW: " + log);
            Assert.Greater(f.AtHandover.SinkMps, 20, log);
            Assert.IsTrue(f.SawHandoverCall, "YOUR BURN callout: " + log);
            Assert.IsTrue(f.SawLightNow, "LIGHT NOW cue: " + log);
            Assert.IsFalse(f.SawLate, "a good pilot is never late: " + log);
            Assert.IsTrue(f.SawSlowMo, "slow motion over the deck: " + log);

            var v = s.Landing.Verdict;
            Assert.IsNull(v.Cause, log);
            Assert.IsTrue(MissionScore.IsGood(v.GradeName), log);
            Assert.IsTrue(MissionScore.IsGood(BoosterLanding.MissionGrade(h.Mission, h.Design, v.GradeName)), "legs and the right site: " + log);
            Assert.IsTrue(MissionScore.IsGood(BoosterLanding.ReportGrade(null, v)), "the card quotes it: " + log);
            Assert.AreSame(v, RecoveryLog.Current, "the report reads the player's landing: " + log);
            Assert.Greater(s.Score.Points, 0, log);
            Assert.IsTrue(s.Score.Clean, log);
            Assert.IsTrue(s.Splash.Good, log);
            Assert.AreEqual(v.GradeName.ToUpperInvariant(), s.Splash.Title, log);
            Assert.IsTrue(s.CallVisible, "the splash stays up: " + log);
        }

        [TestCase(6)] [TestCase(7)] [TestCase(10)] [TestCase(13)] [TestCase(15)] [TestCase(19)]
        public void NoInputPilot_Crashes(int index)
        {
            var s = Start(State(index), false);
            var f = Fly(s, NoInput);
            string log = "KH-" + index + " " + f.Log;
            TestContext.WriteLine(log);
            var v = s.Landing.Verdict;
            Assert.AreEqual(RecoveryScore.Fast, v.Cause, log);
            Assert.IsFalse(MissionScore.IsGood(v.GradeName), log);
            Assert.IsFalse(s.Score.Clean, log);
            Assert.AreEqual(1, s.Score.Multiplier, 0, "no multipliers on a crash: " + log);
            Assert.IsTrue(f.SawLate, "TOO LATE shows: " + log);
            Assert.AreEqual("TOO HARD", s.Splash.Title, log);
        }

        [TestCase(6)] [TestCase(7)] [TestCase(10)] [TestCase(13)] [TestCase(15)] [TestCase(19)]
        public void TooLatePilot_Crashes(int index)
        {
            var s = Start(State(index), false);
            var f = Fly(s, TooLatePilot);
            string log = "KH-" + index + " " + f.Log;
            TestContext.WriteLine(log);
            var v = s.Landing.Verdict;
            Assert.IsNotNull(v.Cause, log);
            Assert.IsFalse(MissionScore.IsGood(v.GradeName), log);
            Assert.Greater(v.PropLeft, 0, "it lit, it was just too late: " + log);
            Assert.Less(v.PropLeft, s.HandoverPropKg, "the engine burned: " + log);
        }

        [TestCase(6)] [TestCase(7)] [TestCase(13)]
        public void AutoAssist_KeepsTheAutopilotLanding_BitForBit(int index)
        {
            // Kid play / Auto-assist: the session never hands over, and the landing is exactly FlyPlanned's (the batch judge).
            var h = State(index);
            var s = Start(h, true);
            Assert.AreEqual(ArcadePilot.Autopilot, s.Pilot);
            Assert.IsFalse(s.SkipStopsAtHandover);
            var f = Fly(s, (x) => { Assert.Fail("the player is never asked on Auto-assist"); return default; });
            Assert.IsFalse(s.HandedOver, f.Log);
            Assert.AreEqual(1, s.Scale.Scale, 0, "no slow motion on the autopilot");
            var planned = BoosterLanding.FlyPlanned(catalog, h.Design, h.Down, h.Alt, h.Speed, h.Pitch);
            var v = s.Landing.Verdict;
            Assert.AreEqual(planned.GradeName, v.GradeName, f.Log);
            Assert.AreEqual(planned.Sink, v.Sink, 0, f.Log);
            Assert.AreEqual(planned.MissM, v.MissM, 0, f.Log);
            Assert.AreEqual(planned.T, v.T, 0, f.Log);
            Assert.AreEqual(planned.PropLeft, v.PropLeft, 0, f.Log);
            Assert.IsTrue(MissionScore.IsGood(v.GradeName), f.Log);
        }

        [Test]
        public void SlowMotion_OnlyPacesTheFixedSteps()
        {
            // The same (input-free) landing fed at slow motion and at realtime lands bit-identical: the scale changes how
            // much time is fed, never the step. Slow motion takes more real frames.
            var h = State(7);
            var slow = Start(h, false);
            var a = Fly(slow, NoInput);
            RecoveryLog.Clear();
            var flat = ArcadeLandingTune.Load(Golden.Root);
            flat.SlowMoScale = 1;
            var real = Start(h, false, flat);
            var b = Fly(real, NoInput);
            Assert.IsTrue(a.SawSlowMo, a.Log);
            Assert.IsFalse(b.SawSlowMo, b.Log);
            var va = slow.Landing.Verdict; var vb = real.Landing.Verdict;
            Assert.AreEqual(vb.T, va.T, 0);
            Assert.AreEqual(vb.Sink, va.Sink, 0);
            Assert.AreEqual(vb.MissM, va.MissM, 0);
            Assert.AreEqual(vb.PropLeft, va.PropLeft, 0);
        }

        [Test]
        public void TimeScale_SlowsTheLastMetresAndTheTouchdownHold()
        {
            var t = ArcadeLandingTune.Default;
            Assert.AreEqual(1, ArcadeLanding.TimeScale(ArcadePilot.Autopilot, true, 5, -1, t).Scale, 0, "autopilot: realtime");
            Assert.AreEqual(1, ArcadeLanding.TimeScale(ArcadePilot.Player, false, 5, -1, t).Scale, 0, "before the hand-over: realtime");
            Assert.AreEqual(1, ArcadeLanding.TimeScale(ArcadePilot.Player, true, t.SlowMoAltM + 1, -1, t).Scale, 0);
            Assert.AreEqual(t.SlowMoScale, ArcadeLanding.TimeScale(ArcadePilot.Player, true, t.SlowMoAltM - 1, -1, t).Scale, 0);
            Assert.AreEqual(t.SlowMoScale, ArcadeLanding.TimeScale(ArcadePilot.Player, true, 0, 0.2, t).Scale, 0, "touchdown hold");
            Assert.AreEqual(1, ArcadeLanding.TimeScale(ArcadePilot.Player, true, 0, t.SlowMoHoldSec + 0.01, t).Scale, 0, "then realtime");
            Assert.Less(t.SlowMoScale, 1);
            Assert.Greater(t.SlowMoScale, 0);
        }

        [Test]
        public void Skip_RunsToTheHandover_AndNeverPastIt()
        {
            // A "next stage" skip may fast-forward the autopilot's boostback and entry, but stops at the player's burn.
            var s = Start(State(7), false);
            Assert.IsTrue(s.SkipStopsAtHandover);
            Assert.IsTrue(s.RunToHandover());
            Assert.IsTrue(s.HandedOver);
            Assert.IsFalse(s.SkipStopsAtHandover);
            Assert.IsFalse(s.Landing.Touched, "the burn is still the player's");
            Assert.AreNotEqual(RecoveryPhase.Down, s.Landing.Body.Phase);
            Assert.Greater(s.Readout.LightInS, 1.0, "still the full lead before LIGHT NOW");
            double t = s.Landing.Body.T;
            Assert.IsTrue(s.RunToHandover(), "a second skip does nothing");
            Assert.AreEqual(t, s.Landing.Body.T, 0);
            var f = Fly(s, GoodPilot);
            Assert.IsTrue(MissionScore.IsGood(s.Landing.Verdict.GradeName), f.Log);
        }

        [Test]
        public void AscentOver_RunsToTheHandover_OnlyTheAutopilotSettles()
        {
            // t466: an ascent that ends first settles the booster at once on the autopilot. The player's burn is kept.
            var p = Start(State(10), false);
            Assert.IsFalse(p.Conclude(true, false));
            Assert.IsTrue(p.HandedOver);
            Assert.IsFalse(p.Landing.Touched);
            RecoveryLog.Clear();
            var a = Start(State(10), true);
            Assert.IsTrue(a.Conclude(true, false));
            Assert.IsTrue(a.Landing.Touched);
            RecoveryLog.Clear();
            var lost = Start(State(10), false);
            Assert.IsFalse(lost.Conclude(true, true), "a lost ascent leaves the booster alone");
            Assert.IsFalse(lost.HandedOver);
        }

        [Test]
        public void Pilot_PlayerOnlyFliesAPlannedLandingWithoutAssist()
        {
            Assert.AreEqual(ArcadePilot.Player, ArcadeLanding.Pilot(BoosterFate.Planned, "barge", false));
            Assert.AreEqual(ArcadePilot.Player, ArcadeLanding.Pilot(BoosterFate.Planned, "rtls", false), "RTLS flies the same burn onto the pad");
            Assert.AreEqual(ArcadePilot.Autopilot, ArcadeLanding.Pilot(BoosterFate.Planned, "barge", true), "Auto-assist watches");
            Assert.AreEqual(ArcadePilot.Autopilot, ArcadeLanding.Pilot(BoosterFate.Expendable, "barge", false), "expendable is unchanged");
            Assert.AreEqual(ArcadePilot.Autopilot, ArcadeLanding.Pilot(BoosterFate.None, "barge", false));
            Assert.AreEqual(ArcadePilot.Autopilot, ArcadeLanding.Pilot(BoosterFate.Planned, "expendable", false));
        }

        [Test]
        public void Handover_NeverInBoostbackOrEntry_AndAlwaysOnTheLandingBurn()
        {
            var craft = new RecoveryCraft
            {
                DryKg = 20000, ReserveKg = 40000, ThrustVacN = 5600000, ThrustSlN = 5070000, IspSec = 311,
                DiameterM = 3.7, Cd = 0.5, GridFins = true, Legs = true, LegsKind = "light", Mode = "barge",
            };
            var t = ArcadeLandingTune.Default;
            var body = RecoverySim.Begin(craft, 5000, 2000, 0, -250);
            body.Phase = RecoveryPhase.Entry;
            Assert.IsFalse(ArcadeLanding.HandoverDue(body, craft, t));
            body.Phase = RecoveryPhase.Boostback;
            Assert.IsFalse(ArcadeLanding.HandoverDue(body, craft, t));
            body.Phase = RecoveryPhase.Descent;
            Assert.IsTrue(ArcadeLanding.HandoverDue(body, craft, t), "2 km and falling 250 m/s");
            var high = RecoverySim.Begin(craft, 5000, t.HandoverMaxAltM + 500, 0, -250);
            high.Phase = RecoveryPhase.Descent;
            Assert.IsFalse(ArcadeLanding.HandoverDue(high, craft, t), "over the cap");
            var rising = RecoverySim.Begin(craft, 5000, 2000, 0, 40);
            rising.Phase = RecoveryPhase.Descent;
            Assert.IsFalse(ArcadeLanding.HandoverDue(rising, craft, t), "climbing");
            high.Phase = RecoveryPhase.Landing;
            Assert.IsTrue(ArcadeLanding.HandoverDue(high, craft, t), "the autopilot opened its burn: hand over now");
        }

        [Test]
        public void SuicideCurve_TheCueAndTheSliderMath()
        {
            var craft = new RecoveryCraft { DryKg = 20000, ThrustSlN = 5070000, ThrustVacN = 5600000, IspSec = 311, Mode = "barge", Legs = true, LegsKind = "light" };
            var t = ArcadeLandingTune.Default;
            double g = Kestrel.Core.Physics.Gravity.Accel(Kestrel.Core.Physics.Gravity.R_EARTH);
            // T/W max 3.5 (the engine could give 25): net 2.5 g, so 250 m/s needs 250^2 / (2 * 2.5 g) metres.
            Assert.AreEqual(t.MaxTw, ArcadeLanding.TwMax(craft, 20000, 0, t), 1e-12);
            Assert.AreEqual(250.0 * 250.0 / (2 * 2.5 * g), ArcadeLanding.StopAlt(craft, 20000, 250, 0, 0, t), 1e-6);
            double stop = ArcadeLanding.StopAlt(craft, 20000, 250, 0, 0, t);
            Assert.AreEqual(LightCue.Wait, ArcadeLanding.Cue(stop * (t.LightNowRatio + 0.1), 250, 0, stop, 2, t));
            Assert.AreEqual(LightCue.LightNow, ArcadeLanding.Cue(stop * (t.LightNowRatio - 0.05), 250, 0, stop, 2, t));
            Assert.AreEqual(LightCue.Burning, ArcadeLanding.Cue(stop * 1.1, 250, 0.3, stop, 2, t));
            Assert.AreEqual(LightCue.Late, ArcadeLanding.Cue(stop * 0.9, 250, 0, stop, 2, t));
            Assert.AreEqual(LightCue.Late, ArcadeLanding.Cue(stop * 0.9, 250, 1, stop, 2, t), "lit is still late under the curve");
            Assert.AreNotEqual(LightCue.Late, ArcadeLanding.Cue(0.3, 1.0, 0.2, 1.0, 2, t), "a slow settle is never late");
            // The slider: off under the cutoff, linear T/W up to MaxTw, turned into the engine throttle for this mass.
            Assert.AreEqual(0, ArcadeLanding.TwCommand(t.CutoffInput * 0.5, t), 0);
            Assert.AreEqual(t.MaxTw, ArcadeLanding.TwCommand(1, t), 0);
            Assert.AreEqual(t.MaxTw * 0.5, ArcadeLanding.TwCommand(0.5, t), 1e-12);
            double hover = ArcadeLanding.EngineThrottle(1, craft, 20000, 0);
            Assert.AreEqual(20000 * g / craft.ThrustSlN, hover, 1e-9, "T/W 1 is a hover");
            Assert.AreEqual(1, ArcadeLanding.EngineThrottle(99, craft, 20000, 0), 0, "never past full");
            Assert.AreEqual(0.5, ArcadeLanding.SliderFor(ArcadeLanding.EngineThrottle(ArcadeLanding.TwCommand(0.5, t), craft, 20000, 0), craft, 20000, 0, t), 1e-9);
            // Steering: + leans the thrust east (pitch under π/2), capped at MaxTiltDeg.
            Assert.AreEqual(Math.PI / 2, ArcadeLanding.PitchRad(0, t), 0);
            Assert.AreEqual(Math.PI / 2 - t.MaxTiltDeg * Math.PI / 180, ArcadeLanding.PitchRad(1, t), 1e-12);
            Assert.AreEqual(Math.PI / 2 + t.MaxTiltDeg * Math.PI / 180, ArcadeLanding.PitchRad(-3, t), 1e-12);
            var stick = ArcadeLanding.Stick(new ArcadeInput { Throttle = 0.5, Steer = 0 }, craft, 20000, 0, t);
            Assert.IsFalse(stick.Auto, "a manual stick: the law keeps only fins, legs and phase");
        }

        static RecoveryVerdict Touch(double miss, double sink, double prop, string cause = null, string grade = "Perfect")
            => new RecoveryVerdict { MissM = miss, Sink = sink, PropLeft = prop, Cause = cause, GradeName = grade, Attempted = true };

        [Test]
        public void Multipliers_ComputeAsSpecified()
        {
            var t = ArcadeLandingTune.Default;
            // Bullseye (≤3 m) x2, feather (≤0.6 m/s) x2, fuel miser (≥80% of the handed propellant) x1.5 on a Perfect: 6000.
            var best = ArcadeLanding.Score(Touch(2.0, 0.5, 900), 1000, t);
            Assert.AreEqual(2.0, best.CentreX, 0); Assert.AreEqual("BULLSEYE", best.CentreCall);
            Assert.AreEqual(2.0, best.SoftX, 0); Assert.AreEqual("FEATHER", best.SoftCall);
            Assert.AreEqual(1.5, best.FuelX, 0); Assert.AreEqual("FUEL MISER", best.FuelCall);
            Assert.AreEqual(6000, best.Points);
            Assert.AreEqual(0.9, best.Fuel01, 1e-12);
            // Next tiers: centre ≤8 m x1.5, soft ≤1.0 m/s x1.5, fuel ≥65% x1.25 on a Good (600): 1687.5 -> 1688.
            var mid = ArcadeLanding.Score(Touch(-6.0, 0.9, 700, null, "Good"), 1000, t);
            Assert.AreEqual(1.5, mid.CentreX, 0); Assert.AreEqual(1.5, mid.SoftX, 0); Assert.AreEqual(1.25, mid.FuelX, 0);
            Assert.AreEqual(1688, mid.Points);
            // Third tiers: on the paint ≤15 m x1.2, smooth ≤1.5 m/s x1.2, little fuel x1, Hard (250): 360.
            var low = ArcadeLanding.Score(Touch(14, 1.4, 100, null, "Hard"), 1000, t);
            Assert.AreEqual(1.2, low.CentreX, 0); Assert.AreEqual(1.2, low.SoftX, 0); Assert.AreEqual(1, low.FuelX, 0);
            Assert.AreEqual(360, low.Points);
            // Outside every tier: the base alone.
            var plain = ArcadeLanding.Score(Touch(30, 1.9, 0, null, "Good"), 1000, t);
            Assert.AreEqual(1, plain.Multiplier, 0);
            Assert.AreEqual(600, plain.Points);
            // A crash keeps its base and gets no multiplier, however centred; no grade scores nothing.
            var crash = ArcadeLanding.Score(Touch(0.5, 9, 900, RecoveryScore.Fast, "Salvage"), 1000, t);
            Assert.IsFalse(crash.Clean);
            Assert.AreEqual(1, crash.Multiplier, 0);
            Assert.AreEqual(100, crash.Points);
            Assert.AreEqual(0, ArcadeLanding.Score(Touch(400, 30, 0, RecoveryScore.Miss, null), 1000, t).Points);
            // The points never change the grade.
            Assert.AreEqual("Good", mid.Grade);
            Assert.AreEqual("+1688  CENTRE x1.5  SOFT x1.5  FUEL LEFT x1.25", ArcadeLandingSession.Bonus(mid));
        }

        [Test]
        public void Splash_NamesTheGradeOrTheCause()
        {
            Assert.AreEqual("PERFECT", ArcadeLanding.Splash(Touch(2, 0.8, 10), "barge").Title);
            Assert.IsTrue(ArcadeLanding.Splash(Touch(2, 0.8, 10), "barge").Good);
            Assert.AreEqual("HARD", ArcadeLanding.Splash(Touch(2, 1.9, 10, null, "Hard"), "rtls").Title);
            Assert.IsFalse(ArcadeLanding.Splash(Touch(2, 1.9, 10, null, "Hard"), "rtls").Good);
            Assert.AreEqual("TOO HARD", ArcadeLanding.Splash(Touch(2, 9, 10, RecoveryScore.Fast, "Salvage"), "barge").Title);
            Assert.AreEqual("MISSED", ArcadeLanding.Splash(Touch(90, 1, 10, RecoveryScore.Miss, null), "barge").Title);
            Assert.AreEqual("OUT OF FUEL", ArcadeLanding.Splash(Touch(2, 9, 0, RecoveryScore.Fuel, "Salvage"), "barge").Title);
            Assert.AreEqual("TIPPED", ArcadeLanding.Splash(Touch(2, 1, 10, RecoveryScore.Tilt, "Salvage"), "barge").Title);
            StringAssert.Contains("pad", ArcadeLanding.Splash(Touch(2, 0.8, 10), "rtls").Line);
            StringAssert.Contains("Haven", ArcadeLanding.Splash(Touch(2, 0.8, 10), "barge").Line);
        }

        [Test]
        public void Deck_SwellsAtSea_PadStaysPut()
        {
            var t = ArcadeLandingTune.Default;
            double maxHeave = 0, maxRoll = 0;
            for (double s = 0; s < 60; s += 0.1)
            {
                var d = ArcadeLanding.Deck(s, "barge", t);
                maxHeave = Math.Max(maxHeave, Math.Abs(d.HeaveM));
                maxRoll = Math.Max(maxRoll, Math.Abs(d.RollDeg));
                var pad = ArcadeLanding.Deck(s, "rtls", t);
                Assert.AreEqual(0, pad.HeaveM, 0);
                Assert.AreEqual(0, pad.RollDeg, 0);
            }
            Assert.Greater(maxHeave, t.DeckHeaveM * 0.5);
            Assert.LessOrEqual(maxHeave, t.DeckHeaveM + 1e-9);
            Assert.Greater(maxRoll, t.DeckRollDeg * 0.5);
            Assert.LessOrEqual(maxRoll, t.DeckRollDeg + 1e-9);
        }

        [Test]
        public void Tune_DefaultIsTheDataFile()
        {
            // The device falls back on Default when the synced TextAsset is missing; it must be the shipped file.
            Assert.AreEqual(ArcadeLandingTune.Default.Describe(), Tune.Describe());
            var o = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "arcade-landing.json")));
            Assert.AreEqual(1, o.Num("version"), 0);
            Assert.AreEqual(ArcadeLandingTune.Default.Describe(), ArcadeLandingTune.FromJson(new JsonObject()).Describe(), "missing keys keep the default");
        }

        [Test]
        public void HudCells_FitTheNarrowestPhone()
        {
            // Three cells a row on a 320 pt SE (the narrowest the HUD is laid out for), key over value.
            float cellW = (320f - 2f * FlightHudLayout.SideMarginPt - 2f * FlightHudLayout.CellGapPt) / 3f - 2f * FlightHudLayout.CellPadPt;
            foreach (string key in ArcadeHud.Labels)
                Assert.Less(FlightHudLayout.TextWidth(key, FlightHudLayout.LabelFontPt), cellW, key);
            foreach (string value in ArcadeHud.ValueSamples)
                Assert.Less(FlightHudLayout.TextWidth(value, FlightHudLayout.ValueFontPt), cellW, value);
            float line = 320f - 2f * FlightHudLayout.SideMarginPt;
            foreach (string cue in ArcadeHud.CueSamples)
                Assert.Less(FlightHudLayout.TextWidth(cue, ArcadeHud.CueFontPt), line, cue);
            foreach (string word in ArcadeHud.SplashSamples)
                Assert.Less(FlightHudLayout.TextWidth(word, ArcadeHud.SplashFontPt), line, word);
            foreach (string title in new[] { ArcadeLandingSession.HandoverTitle, ArcadeLandingSession.LightNowTitle, ArcadeLandingSession.LateTitle, ArcadeLandingSession.LegsTitle })
                Assert.Less(FlightHudLayout.TextWidth(title, ArcadeHud.TitleFontPt), line, title);
            // The line under the title wraps (white-space normal) into at most two lines.
            Assert.LessOrEqual(FlightHudLayout.WrapLines(ArcadeHud.BonusSample, line, ArcadeHud.LineFontPt), 2, ArcadeHud.BonusSample);
            Assert.LessOrEqual(FlightHudLayout.WrapLines(ArcadeLandingSession.HandoverLine, line, ArcadeHud.LineFontPt), 2);
            // The cells read a live sample: both screen directions name the side the picture shows.
            var r = new ArcadeReadout { AltM = 120, SinkMps = 12.3, OffsetM = 7.6, LeanDeg = -2.5, TiltDeg = 2.5, Prop01 = 0.42, PropKg = 8400, Tw = 1.25, Throttle = 0.1 };
            var right = ArcadeHud.Cells(r, true, "barge", 2);
            Assert.AreEqual(ArcadeHud.Count, right.Length);
            Assert.AreEqual("R 8 m", right[2].Value);
            Assert.AreEqual("L 2.5°", right[3].Value);
            Assert.AreEqual("1.25", right[5].Value);
            var left = ArcadeHud.Cells(r, false, "rtls", 2);
            Assert.AreEqual("L 8 m", left[2].Value);
            StringAssert.Contains("pad", left[2].Spoken);
            Assert.AreEqual("42%", right[4].Value);
            Assert.AreEqual("LIGHT NOW", ArcadeHud.CueText(new ArcadeReadout { Cue = LightCue.LightNow }));
            Assert.AreEqual("LIGHT IN 3.2 s", ArcadeHud.CueText(new ArcadeReadout { Cue = LightCue.Wait, LightInS = 3.21 }));
            ArcadeHud.Gauge(new ArcadeReadout { AltM = 1500, StopAltM = 1000 }, 3000, ArcadeLandingTune.Default, out double a01, out double s01, out double l01);
            Assert.AreEqual(0.5, a01, 1e-12);
            Assert.AreEqual(1000.0 / 3000.0, s01, 1e-12);
            Assert.AreEqual(1000.0 * ArcadeLandingTune.Default.LightNowRatio / 3000.0, l01, 1e-12);
        }
    }
}
