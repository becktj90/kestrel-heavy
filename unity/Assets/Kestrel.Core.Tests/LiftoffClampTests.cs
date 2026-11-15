// DECISIONS #100 "Release the clamps": the gauge, the grading, the handoff gate, the medal credit and the haptic
// envelope. The flown trajectory never depends on the release (the sim starts at its own t = 0 at full thrust).
using System.Collections.Generic;
using Kestrel.Core.Feel;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Designer;
using Kestrel.Core.Physics;
using Kestrel.Core.Timeline;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class LiftoffClampTests
    {
        static ClampRelease Standard(double twr = 1.3) => new ClampRelease(new ClampTuning(), false, twr);
        static ClampRelease Forgiving(double twr = 1.3) => new ClampRelease(new ClampTuning(), true, twr);

        [Test]
        public void Tuning_FileMatchesTheCompiledDefaults()
        {
            var file = LiftoffTuning.Load(Golden.Root);
            var d = new LiftoffTuning();
            Assert.AreEqual(d.Clamps.IgnitionS, file.Clamps.IgnitionS, 1e-12);
            Assert.AreEqual(d.Clamps.Standard.GreenBeforeS, file.Clamps.Standard.GreenBeforeS, 1e-12);
            Assert.AreEqual(d.Clamps.Standard.GreenAfterS, file.Clamps.Standard.GreenAfterS, 1e-12);
            Assert.AreEqual(d.Clamps.Forgiving.GreenBeforeS, file.Clamps.Forgiving.GreenBeforeS, 1e-12);
            Assert.AreEqual(d.Clamps.Forgiving.GreenAfterS, file.Clamps.Forgiving.GreenAfterS, 1e-12);
            Assert.AreEqual(d.Clamps.GaugeGreenFrom, file.Clamps.GaugeGreenFrom, 1e-12);
            Assert.AreEqual(d.Clamps.EarlyHesitateS, file.Clamps.EarlyHesitateS, 1e-12);
            Assert.AreEqual(d.Clamps.EarlySettleM, file.Clamps.EarlySettleM, 1e-12);
            var a = d.Haptics; var b = file.Haptics;
            foreach (var pair in new[]
            {
                (a.RumbleMinHz, b.RumbleMinHz), (a.RumbleMaxHz, b.RumbleMaxHz), (a.RumbleMinIntensity, b.RumbleMinIntensity),
                (a.RumbleMaxIntensity, b.RumbleMaxIntensity), (a.KickIntensity, b.KickIntensity), (a.KickEchoIntensity, b.KickEchoIntensity),
                (a.KickEchoS, b.KickEchoS), (a.KickTailIntensity, b.KickTailIntensity), (a.KickTailS, b.KickTailS),
                (a.HesitateIntensity, b.HesitateIntensity), (a.BuzzFromQ, b.BuzzFromQ), (a.BuzzHz, b.BuzzHz),
                (a.BuzzMinIntensity, b.BuzzMinIntensity), (a.BuzzMaxIntensity, b.BuzzMaxIntensity), (a.BuzzMaxS, b.BuzzMaxS),
            })
                Assert.AreEqual(pair.Item1, pair.Item2, 1e-12);
        }

        [Test]
        public void Tuning_MissingKeysKeepDefaults_AndANullFileIsTheDefaults()
        {
            var t = LiftoffTuning.FromJson(MiniJson.ParseObject("{\"clamps\":{\"standard\":{\"greenAfterS\":1.5}}}"));
            Assert.AreEqual(1.5, t.Clamps.Standard.GreenAfterS, 1e-12);
            Assert.AreEqual(0.6, t.Clamps.Standard.GreenBeforeS, 1e-12);
            Assert.AreEqual(-3.0, t.Clamps.IgnitionS, 1e-12);
            Assert.AreEqual(20, LiftoffTuning.FromJson(null).Haptics.BuzzHz, 1e-12);
        }

        [Test]
        public void Spool_RisesSmoothlyFromIgnition_ToFullThrustAtTheGreen()
        {
            var c = Standard();
            Assert.AreEqual(-3.0, c.IgnitionT, 1e-12);
            Assert.AreEqual(-0.6, c.GreenStart, 1e-12);
            Assert.AreEqual(0.9, c.GreenEnd, 1e-12);
            Assert.AreEqual(0, c.Thrust01(-3.5));
            Assert.AreEqual(ClampPhase.Idle, c.Phase(-3.5));
            double prev = 0;
            for (double t = -3.0; t <= -0.6; t += 0.05)
            {
                double f = c.Thrust01(t);
                Assert.That(f, Is.GreaterThanOrEqualTo(prev - 1e-12));
                Assert.That(f, Is.InRange(0.0, 1.0));
                prev = f;
            }
            Assert.AreEqual(1, c.Thrust01(-0.6), 1e-12);
            Assert.AreEqual(ClampPhase.Spooling, c.Phase(-1.0));
            Assert.AreEqual(ClampPhase.Green, c.Phase(-0.5));
            Assert.AreEqual(1.3, c.Twr(0), 1e-12, "full thrust reads the design's liftoff T/W");
            Assert.AreEqual("T/W 1.30", c.TwrText(0));
        }

        [Test]
        public void Needle_ReachesTheGreenAtFullThrust_AndTheTopWhenTheGreenCloses()
        {
            var c = Standard();
            Assert.AreEqual(c.GaugeGreenFrom, c.Needle(c.GreenStart), 1e-9);
            Assert.AreEqual(1, c.Needle(c.GreenEnd), 1e-9);
            double prev = -1;
            for (double t = -3.2; t <= 1.2; t += 0.02)
            {
                double n = c.Needle(t);
                Assert.That(n, Is.InRange(0.0, 1.0));
                Assert.That(n, Is.GreaterThanOrEqualTo(prev - 1e-12));
                Assert.AreEqual(n >= c.GaugeGreenFrom - 1e-9, c.InGreen(t) || t > c.GreenEnd, "needle in the band = the window is open, t=" + t);
                prev = n;
            }
        }

        [Test]
        public void CleanTap_InTheGreen_ReleasesNow_ForPerfectCredit()
        {
            var c = Standard();
            c.Step(-0.3);
            Assert.IsFalse(c.Released);
            Assert.IsTrue(c.Tap(-0.3));
            Assert.AreEqual(ClampOutcome.Clean, c.Outcome);
            Assert.AreEqual(AscentGrade.Perfect, c.Grade);
            Assert.IsTrue(c.Touched);
            Assert.IsTrue(c.LiftoffDue(-0.3), "the sim starts on the tap (before the nominal T-0)");
            Assert.IsFalse(c.Tap(-0.2), "one release per flight");
            Assert.AreEqual("Clean release!", c.Status(-0.2));
        }

        [Test]
        public void EarlyTap_IsHeldToFullThrustAndABeat_ForGoodCredit_AndSaysWhy()
        {
            var c = Standard(1.25);
            double tap = -2.0;
            Assert.That(c.Twr(tap), Is.LessThan(1.0), "this early the stack could not lift");
            Assert.IsTrue(c.Tap(tap));
            Assert.AreEqual(ClampOutcome.Early, c.Outcome);
            Assert.AreEqual(AscentGrade.Good, c.Grade);
            Assert.IsTrue(c.Touched);
            Assert.AreEqual(c.GreenStart + new ClampTuning().EarlyHesitateS, c.LiftoffAt, 1e-12);
            Assert.IsFalse(c.LiftoffDue(c.GreenStart), "it waits on the pad past full thrust: the hesitation");
            Assert.IsTrue(c.LiftoffDue(c.LiftoffAt));
            Assert.That(c.LiftoffAt, Is.LessThan(c.AutoAt), "an early tap never costs more than waiting");
            Assert.AreEqual("Too early: T/W under 1", c.Status(-1.5));
            Assert.That(c.SettleM(-1.5), Is.GreaterThan(0).And.LessThanOrEqualTo(0.3), "a few centimetres on the posts, no damage");
            Assert.AreEqual(0, c.SettleM(c.LiftoffAt), 1e-12);
            var hot = Standard(3.0);
            hot.Tap(-1.5);
            Assert.AreEqual("Too early: thrust not full", hot.Status(-1.4), "a stiff stack would lift, so the line says thrust, not T/W");
        }

        [Test]
        public void NoTap_Standard_AutoReleasesWhenTheGreenCloses_WithNoCredit()
        {
            var c = Standard();
            for (double t = -3; t < c.GreenEnd; t += 0.05) { c.Step(t); Assert.IsFalse(c.Released, "t=" + t); }
            Assert.IsTrue(c.HoldsAtZero(0.5), "the count holds at T-0 while the green is open");
            c.Step(c.GreenEnd + 1e-9);
            Assert.IsTrue(c.Released);
            Assert.AreEqual(ClampOutcome.Late, c.Outcome);
            Assert.AreEqual(AscentGrade.None, c.Grade);
            Assert.IsFalse(c.Touched);
            Assert.IsTrue(c.LiftoffDue(c.GreenEnd + 1e-9));
        }

        [Test]
        public void NoTap_KidOrAutoAssist_AutoReleasesExactlyAtTMinusZero()
        {
            var c = Forgiving();
            Assert.That(-c.GreenStart, Is.GreaterThan(-Standard().GreenStart), "a wider window before T-0");
            c.Step(-1e-9);
            Assert.IsFalse(c.Released);
            Assert.IsFalse(c.HoldsAtZero(-1e-9));
            c.Step(0);
            Assert.IsTrue(c.LiftoffDue(0), "today's timing: the sim starts at T-0");
            Assert.AreEqual(ClampOutcome.Auto, c.Outcome);
            Assert.AreEqual(AscentGrade.None, c.Grade);
            Assert.AreEqual("Auto release", c.Status(0));
        }

        [Test]
        public void SkipCount_ReleasesNow_WithNoCredit()
        {
            var c = Standard();
            c.Skip(-8);
            Assert.IsTrue(c.LiftoffDue(-8));
            Assert.AreEqual(ClampOutcome.Skipped, c.Outcome);
            Assert.AreEqual(AscentGrade.None, c.Grade);
            Assert.IsFalse(c.Tap(-0.2));
        }

        [Test]
        public void UnknownTwr_ReadsThrustOnly()
        {
            var c = new ClampRelease(new ClampTuning(), false, double.NaN);
            Assert.AreEqual("", c.TwrText(-1));
            Assert.AreEqual("Thrust 0%", c.Status(-3));
            Assert.That(c.Status(-1.0), Does.StartWith("Thrust "));
        }

        // ---- the handoff gate ----------------------------------------------------------------------

        static LaunchSequence Count()
        {
            var cat = StarterFixture.Catalog;
            var design = Design.FromJson(StarterFixture.Rocket("m3").BuildDesign());
            var tl = TimelineGenerator.Generate(cat, design, Pad.For(cat, design), new Mission { mode = "standard" }, 1);
            return LaunchSequence.Open(tl);
        }

        /// <summary>Walks the handoff at 60 Hz with a clamp; returns the count time the ascent started (NaN if never).</summary>
        static double Walk(ClampRelease clamp, double tapAt, out LiftoffHandoff handoff)
        {
            handoff = new LiftoffHandoff();
            var h = handoff;
            handoff.Enter(Count(), -4);
            double clampT = handoff.Sequence.Time;
            double started = double.NaN;
            handoff.ReleaseGate = () => clamp.LiftoffDue(clampT);
            bool tapped = false;
            for (int i = 0; i < 60 * 12 && double.IsNaN(started); i++)
            {
                double dt = 1.0 / 60.0;
                clampT = h.Counting ? h.Sequence.Time + dt : clampT + dt;
                clamp.Step(clampT);
                if (!tapped && clampT >= tapAt) { tapped = true; clamp.Tap(clampT); }
                double at = clampT;
                if (clamp.LiftoffDue(clampT) && h.Counting) h.Skip(() => { started = at; return true; });
                else h.Tick(dt, () => { started = at; return true; });
            }
            return started;
        }

        [Test]
        public void Gate_NoGateStartsAtTMinusZero_AndTheForgivingAutoReleaseMatchesIt()
        {
            var plain = new LiftoffHandoff();
            plain.Enter(Count(), -4);
            double t = plain.Sequence.Time, startedPlain = double.NaN;
            for (int i = 0; i < 60 * 8 && double.IsNaN(startedPlain); i++)
            {
                double at = t + 1.0 / 60.0;
                plain.Tick(1.0 / 60.0, () => { startedPlain = at; return true; });
                t = at;
            }
            double startedAuto = Walk(Forgiving(), double.PositiveInfinity, out var h);
            Assert.IsTrue(h.Ascending);
            Assert.AreEqual(startedPlain, startedAuto, 1.0 / 60.0 + 1e-9, "Auto-assist releases on the same frame as today");
        }

        [Test]
        public void Gate_HoldsTheCountAtZero_UntilTheStandardWindowCloses()
        {
            var clamp = Standard();
            double started = Walk(clamp, double.PositiveInfinity, out var h);
            Assert.IsTrue(h.Ascending);
            Assert.AreEqual(clamp.GreenEnd, started, 1.0 / 60.0 + 1e-9);
            Assert.AreEqual(ClampOutcome.Late, clamp.Outcome);
        }

        [Test]
        public void Gate_ACleanTapBeforeZero_StartsTheAscentOnTheTap()
        {
            var clamp = Standard();
            double started = Walk(clamp, -0.3, out var h);
            Assert.IsTrue(h.Ascending);
            Assert.AreEqual(-0.3, started, 1.0 / 60.0 + 1e-9);
            Assert.AreEqual(ClampOutcome.Clean, clamp.Outcome);
        }

        [Test]
        public void Gate_SkipNeverWaits_AndAThrowingGateDoesNotStrandThePad()
        {
            var h = new LiftoffHandoff();
            h.Enter(Count(), -4);
            h.ReleaseGate = () => false;
            bool started = false;
            h.Skip(() => started = true);
            Assert.IsTrue(started && h.Ascending, "Skip count ignores the clamps");
            var h2 = new LiftoffHandoff();
            h2.Enter(Count(), -0.01);
            h2.ReleaseGate = () => throw new System.InvalidOperationException("boom");
            bool s2 = false;
            for (int i = 0; i < 10 && !s2; i++) h2.Tick(1.0 / 60.0, () => s2 = true);
            Assert.IsTrue(s2, "a broken gate starts the ascent as before");
        }

        // ---- medal credit --------------------------------------------------------------------------

        [Test]
        public void Credit_OnlyATapCounts_OncePerFlight_AndFeedsTheExistingMedal()
        {
            var idle = AscentDirector.Start(new AscentVehicle { Stages = 1 }, null, null, false, false, false);
            Assert.AreEqual(AscentMedal.None, idle.Score().Medal, "a single-stage hop with no calls has no medal today");
            Assert.IsFalse(idle.CreditClampRelease(AscentGrade.None, false));
            Assert.IsFalse(idle.CreditClampRelease(AscentGrade.Perfect, false), "an auto-release is not the player's");
            Assert.AreEqual(AscentMedal.None, idle.Score().Medal, "untouched flights keep their medal");

            var clean = AscentDirector.Start(new AscentVehicle { Stages = 1 }, null, null, false, false, false);
            Assert.IsTrue(clean.CreditClampRelease(AscentGrade.Perfect, true));
            Assert.IsFalse(clean.CreditClampRelease(AscentGrade.Good, true), "once per flight");
            var s = clean.Score();
            Assert.AreEqual(1, s.Perfect);
            Assert.IsTrue(s.PlayerFlew);
            Assert.AreEqual(AscentMedal.Gold, s.Medal);

            var early = AscentDirector.Start(new AscentVehicle { Stages = 1 }, null, null, false, false, false);
            Assert.IsTrue(early.CreditClampRelease(AscentGrade.Good, true));
            Assert.AreEqual(AscentMedal.Silver, early.Score().Medal);
            Assert.AreEqual(idle.Command.ThrottleScale, clean.Command.ThrottleScale, 0, "the credit is no command");
            Assert.AreEqual(idle.Command.PitchOffsetRad, clean.Command.PitchOffsetRad, 0);
        }

        [Test]
        public void Credit_NeverChangesTheFlight_Kh1Hop()
        {
            var catalog = StarterFixture.Catalog;
            var mission = StarterFixture.Campaign.ById("m1");
            var design = Design.FromJson(StarterFixture.Rocket("m1").BuildDesign());
            var a = FlyHop(catalog, mission, design, AscentGrade.None);
            var b = FlyHop(catalog, mission, design, AscentGrade.Perfect);
            Assert.AreEqual(a.outcome, b.outcome);
            Assert.AreEqual(a.time, b.time, 0, "bit-identical flight");
            Assert.AreEqual(a.x, b.x, 0);
            Assert.AreEqual(a.y, b.y, 0);
            Assert.AreEqual(a.vy, b.vy, 0);
        }

        static SimResult FlyHop(Kestrel.Core.Data.Catalog catalog, CampaignMission mission, Design design, AscentGrade credit)
        {
            var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt, null);
            var opts = setup.Options;
            double target = MissionAim.IsApogeeHop(mission) ? mission.GuidanceAltM : 200000;
            var director = MissionFlightSetup.Director(catalog, design, null, null, FailureMonitor.StructuralQLimPa(catalog, design),
                FailureMonitor.GentleTier(design), target, FlightBatch.DesignHash(design), 1, true, false, false, null);
            if (credit != AscentGrade.None) director.CreditClampRelease(credit, true);
            var sim = new AscentSim(setup.Stages, opts);
            var directed = new DirectedFlight(sim, director, opts);
            for (int i = 0; i < 2000000 && !sim.Done; i++) directed.Step();
            return directed.Finish();
        }

        // ---- haptics -------------------------------------------------------------------------------

        static List<HapticPulse> Run(LiftoffHaptics h, double seconds, LiftoffHapticInput input, double dt = 1.0 / 60.0)
        {
            var list = new List<HapticPulse>();
            for (double t = 0; t < seconds; t += dt) h.Step(dt, input, list);
            return list;
        }

        [Test]
        public void Haptics_RumbleRampsWithThrust_AndStaysUnderTheTapticRate()
        {
            var h = new LiftoffHaptics(new HapticTuning());
            Assert.That(h.RumbleHz(1), Is.LessThanOrEqualTo(LiftoffHaptics.MaxHz));
            Assert.That(h.RumbleIntensity(1), Is.GreaterThan(h.RumbleIntensity(0.3)));
            Assert.That(h.RumbleHz(1), Is.GreaterThan(h.RumbleHz(0.1)));
            var low = Run(h, 1, new LiftoffHapticInput { Enabled = true, Spooling = true, Thrust01 = 0.2 });
            h.Reset();
            var high = Run(h, 1, new LiftoffHapticInput { Enabled = true, Spooling = true, Thrust01 = 1.0 });
            Assert.That(high.Count, Is.GreaterThan(low.Count));
            Assert.That(high.Count, Is.LessThanOrEqualTo(31));
            foreach (var p in high) Assert.That(p.Intensity, Is.InRange(0f, 1f));
            Assert.That(high[high.Count - 1].Intensity, Is.GreaterThan(low[low.Count - 1].Intensity));
        }

        [Test]
        public void Haptics_ReleaseIsAHardKick_ThenAnEchoAndATail()
        {
            var h = new LiftoffHaptics(new HapticTuning());
            var list = new List<HapticPulse>();
            h.Step(1.0 / 60.0, new LiftoffHapticInput { Enabled = true, LiftoffNow = true }, list);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(HapticStyle.Heavy, list[0].Style);
            Assert.AreEqual(1f, list[0].Intensity, 1e-6);
            for (int i = 0; i < 30; i++) h.Step(1.0 / 60.0, new LiftoffHapticInput { Enabled = true }, list);
            Assert.AreEqual(3, list.Count, "kick, echo, tail and nothing more");
            Assert.AreEqual(HapticStyle.Rigid, list[1].Style);
            Assert.That(list[2].Intensity, Is.LessThan(list[1].Intensity));
        }

        [Test]
        public void Haptics_BuzzOnlyNearMaxQ_AndIsCapped()
        {
            var h = new LiftoffHaptics(new HapticTuning());
            Assert.AreEqual(0, Run(h, 2, new LiftoffHapticInput { Enabled = true, QShare = 0.5 }).Count);
            var near = Run(h, 2, new LiftoffHapticInput { Enabled = true, QShare = 0.95 });
            Assert.That(near.Count, Is.InRange(30, 42));
            h.Reset();
            var all = Run(h, 30, new LiftoffHapticInput { Enabled = true, QShare = 1.0 });
            Assert.That(h.BuzzSeconds, Is.LessThanOrEqualTo(new HapticTuning().BuzzMaxS + 0.1));
            Assert.That(all.Count, Is.LessThanOrEqualTo((int)(new HapticTuning().BuzzMaxS * new HapticTuning().BuzzHz) + 2));
        }

        [Test]
        public void Haptics_SettingOffPlaysNothing_AndDefaultsOn()
        {
            var h = new LiftoffHaptics(new HapticTuning());
            var off = new LiftoffHapticInput { Enabled = false, Spooling = true, Thrust01 = 1, LiftoffNow = true, HesitatedNow = true, QShare = 1 };
            Assert.AreEqual(0, Run(h, 2, off).Count);
            Assert.IsTrue(LiftoffHaptics.On(_ => false, _ => 0), "no stored value is on (the Hangar default)");
            Assert.IsFalse(LiftoffHaptics.On(k => k == LiftoffHaptics.Pref, _ => 0));
            Assert.AreEqual("kh.hangar.haptics", LiftoffHaptics.Pref, "one switch for the Hangar and the flight");
        }

        [Test]
        public void Haptics_AnEarlyTapIsOneSoftClunk()
        {
            var h = new LiftoffHaptics(new HapticTuning());
            var list = new List<HapticPulse>();
            h.Step(1.0 / 60.0, new LiftoffHapticInput { Enabled = true, HesitatedNow = true }, list);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(HapticStyle.Soft, list[0].Style);
        }

        // ---- layout --------------------------------------------------------------------------------

        [Test]
        public void Layout_TheReleaseButtonIsAtLeast44Pt_AndEveryStatusFitsOnOneLineAtSe()
        {
            Assert.That(ClampGaugeLayout.ButtonPt, Is.GreaterThanOrEqualTo(FlightHudLayout.HitPt));
            foreach (float card in new[] { 375f - 2f * FlightHudLayout.SideMarginPt, 430f - 2f * FlightHudLayout.SideMarginPt, 820f - 2f * FlightHudLayout.SideMarginPt })
            {
                float room = card - 2f * CountdownStack.CardPadXPt;
                foreach (var s in ClampGaugeLayout.Statuses)
                {
                    var fit = ClampGaugeLayout.Fit(card, s, "T/W 1.30");
                    Assert.That(FlightHudLayout.BudgetWidth(s, fit.LinePt), Is.LessThanOrEqualTo(room - 2f), s + " at " + card);
                    Assert.That(fit.ButtonW, Is.LessThanOrEqualTo(room));
                    Assert.That(fit.ButtonW, Is.GreaterThanOrEqualTo(FlightHudLayout.BudgetWidth(ClampRelease.ReleaseText, ClampGaugeLayout.ButtonFontPt)));
                }
            }
        }
    }
}
