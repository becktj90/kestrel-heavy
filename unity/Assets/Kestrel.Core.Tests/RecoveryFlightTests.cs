// Guidance and touchdown scoring for the Haven recovery. The auto-assist replay is
// bit-identical. The reserve is a payload cost, not a free tank.
using System;
using Kestrel.Core.Flight;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class RecoveryFlightTests
    {
        static RecoveryCraft BargeCraft(double reserveKg)
        {
            return new RecoveryCraft
            {
                DryKg = 18000,
                ReserveKg = reserveKg,
                ThrustVacN = 520000,
                ThrustSlN = 470000,
                IspSec = 311,
                DiameterM = 3.7,
                Cd = 0.52,
                GridFins = true,
                Legs = true,
                LegsKind = "heavy",
                Mode = "barge",
            };
        }

        [Test]
        public void Guidance_IsDeterministic_AndManualStickReplacesTheLaw()
        {
            var craft = BargeCraft(8000);
            var body = RecoverySim.Begin(craft, 4000, 18000, 160, 30);
            var auto = RecoveryGuidance.Command(body, craft, RecoveryStick.Assisted);
            for (int i = 0; i < 12; i++)
            {
                var again = RecoveryGuidance.Command(body, craft, RecoveryStick.Assisted);
                Assert.AreEqual(auto.Pitch, again.Pitch, 0);
                Assert.AreEqual(auto.Throttle, again.Throttle, 0);
                Assert.AreEqual(auto.Phase, again.Phase);
                Assert.AreEqual(auto.Fins, again.Fins);
                Assert.AreEqual(auto.Legs, again.Legs);
            }
            var hand = RecoveryGuidance.Command(body, craft, new RecoveryStick { Auto = false, Throttle = 0.4, PitchRad = 0.2 });
            Assert.AreEqual(0.2, hand.PitchRad(), 1e-12);
            Assert.AreEqual(0.4, hand.Throttle, 0);
            Assert.AreEqual(auto.Fins, hand.Fins, "fins still follow the altitude gate");
        }

        [Test]
        public void Barge_SitsWhereTheBoostbackImpulseLands()
        {
            var craft = BargeCraft(8000);
            var body = RecoverySim.Begin(craft, 5000, 22000, 170, 20);
            RecoveryFrames.Split(body.X, body.Y, body.Vx, body.Vy, out double east, out double radial);
            double cut = RecoveryModel.BargeFactor * Math.Abs(east);
            Assert.AreEqual(0.3 * Math.Abs(east), cut, 1e-9);
            Assert.Greater(Math.Abs(body.BargeArc), 1000, "Haven is downrange of the pad");
            var rtls = craft;
            rtls.Mode = "rtls";
            var home = RecoverySim.Begin(rtls, 5000, 22000, 170, 20);
            Assert.AreEqual(0, home.BargeArc, 0);
            Assert.IsFalse(RecoverySim.Offered("expendable"));
            Assert.IsTrue(RecoverySim.Offered("barge"));
        }

        [Test]
        public void AutoAssist_LandsOnHaven_AndReplaysBitIdentical()
        {
            var craft = BargeCraft(9000);
            var stick = RecoveryStick.Assisted;
            var a = RecoverySim.Fly(craft, 4500, 20000, 150, 25, stick);
            var b = RecoverySim.Fly(craft, 4500, 20000, 150, 25, stick);
            Assert.AreEqual(a.End.Arc, b.End.Arc, 0);
            Assert.AreEqual(a.End.Sink, b.End.Sink, 0);
            Assert.AreEqual(a.End.TiltDeg, b.End.TiltDeg, 0);
            Assert.AreEqual(a.MissM, b.MissM, 0);
            Assert.AreEqual(a.Cause, b.Cause);
            if (a.Cause != null)
                Assert.Fail(Dump(a) + "\n" + Trace(a));
            Assert.Less(Math.Abs(a.MissM), RecoverySim.DeckRadiusM);
            Assert.LessOrEqual(a.Sink, craft.VLeg);
            Assert.LessOrEqual(a.TiltDeg, RecoverySim.TiltLimitDeg);
            Assert.IsNull(a.Failure);
            Assert.IsTrue(a.Medal == "gold" || a.Medal == "silver", a.Medal + " " + a.GradeName);
            Assert.Greater(a.End.Legs01, 0.92);
            Assert.Greater(a.End.Fins, 0.5);
        }

        [Test]
        public void EmptyReserve_IsOutOfFuel_AndHasNoDeckMedal()
        {
            var craft = BargeCraft(0);
            var flown = RecoverySim.Fly(craft, 4500, 20000, 150, 25, RecoveryStick.Assisted);
            Assert.AreEqual(RecoveryScore.Fuel, flown.Cause, Dump(flown));
            Assert.IsNotNull(flown.Failure);
            Assert.AreEqual("out-of-fuel", flown.Failure.cause);
            Assert.That(flown.Failure.kid, Does.Contain("m/s"));
            Assert.That(flown.Failure.engineer, Does.Contain("m"));
            Assert.IsNull(flown.Medal);
        }

        [Test]
        public void Score_NamesSpeedTiltAndMiss_AndKeepsTheMedalShelf()
        {
            var craft = BargeCraft(1000);
            var tune = RecoveryScore.HavenTune();
            var deck = Sample(1.2, 2, 8, 0.2, false, RecoveryPhase.Down, 1);
            var soft = RecoveryScore.Judge(deck, craft, null);
            Assert.IsNull(soft.Cause);
            Assert.AreEqual("gold", soft.Medal);
            Assert.AreEqual(MedalShelf.Evaluate(tune, "deck", 1.2, 8, craft.VLeg, RecoverySim.DeckRadiusM, false).Medal, soft.Medal);

            var fast = Sample(9, 1, 4, 400, false, RecoveryPhase.Down, 1);
            var hard = RecoveryScore.Judge(fast, craft, null);
            Assert.AreEqual(RecoveryScore.Fast, hard.Cause);
            Assert.AreEqual("bronze", hard.Medal, "a deck hit that is too fast is salvage, and salvage keeps bronze");
            Assert.That(hard.Failure.engineer, Does.Contain("9.00"));

            var tip = Sample(1.0, 12, 5, 400, false, RecoveryPhase.Down, 1);
            var tipped = RecoveryScore.Judge(tip, craft, null);
            Assert.AreEqual(RecoveryScore.Tilt, tipped.Cause);
            Assert.AreEqual("bronze", tipped.Medal);

            var water = Sample(6, 3, 80, 400, false, RecoveryPhase.Down, 1);
            var missed = RecoveryScore.Judge(water, craft, null);
            Assert.AreEqual(RecoveryScore.Miss, missed.Cause);
            Assert.IsNull(missed.Medal);
            Assert.AreEqual("structural-maxq", RecoveryViews.Boom(missed.Cause, missed.Sink, craft.VLeg));
            Assert.AreEqual("leg-collapse", RecoveryViews.Boom(RecoveryScore.Tilt, 1, craft.VLeg));
        }

        [Test]
        public void Callout_QuotesTheFlownNumbers()
        {
            var marks = new[]
            {
                new RecoveryMark { Phase = RecoveryPhase.Coast, T = 0, Alt = 20000, Speed = 160 },
                new RecoveryMark { Phase = RecoveryPhase.Boostback, T = 0.1, Alt = 19800, Speed = 155 },
                new RecoveryMark { Phase = RecoveryPhase.Landing, T = 40, Alt = 900, Speed = 70 },
                new RecoveryMark { Phase = RecoveryPhase.Down, T = 70, Alt = 0, Speed = 2, Sink = 1.4, TiltDeg = 2.5, MissM = 11 },
            };
            var mid = Sample(0, 0, 0, 100, false, RecoveryPhase.Boostback, 0);
            mid.T = 0.4;
            var call = RecoveryViews.Callout(mid, marks);
            Assert.AreEqual("BOOSTBACK", call.Title);
            Assert.AreEqual("haven-burn", call.NarrationId);
            Assert.That(call.Line, Does.Contain("T+0.1"));
            Assert.That(call.Line, Does.Contain("19.8km"));
            Assert.That(call.Line, Does.Contain("155"));
            var done = Sample(1.4, 2.5, 11, 0, false, RecoveryPhase.Down, 1);
            done.T = 70.2;
            var touch = RecoveryViews.Callout(done, marks);
            Assert.AreEqual("TOUCHDOWN  1.4 m/s  2.5°  11 m", touch.Line);
            var pic = RecoveryViews.Of(done, marks, null, 4, true);
            Assert.IsTrue(pic.Dust);
            Assert.IsFalse(pic.Splash);
            Assert.IsTrue(pic.Follow);
            Assert.IsNull(pic.BoomKind);
        }

        [Test]
        public void Reserve_CostsPayload_AndZeroReserveDoesNot()
        {
            var cat = Golden.Catalog;
            var stock = cat.Preset("stock").Clone();
            stock.recoveryMode = "barge";
            var held = RecoveryTradeoff.Measure(cat, stock);
            Assert.Greater(stock.stages[0].landingPropMass, 1000);
            Assert.Greater(held.PayloadCostKg, 50, held.Engineer);
            Assert.Less(held.DvWithMps, held.DvWithoutMps);
            Assert.That(held.Kid, Does.Contain("kg of cargo"));
            Assert.That(held.Engineer, Does.Contain("m/s"));

            var open = stock.Clone();
            open.stages[0].landingPropMass = 0;
            open.recoveryMode = "expendable";
            var free = RecoveryTradeoff.Measure(cat, open);
            Assert.AreEqual(0, free.PayloadCostKg, 0);
            Assert.That(free.Kid, Does.Contain("Expendable"));

            var light = stock.Clone();
            light.stages[0].landingPropMass = 2000;
            var heavier = stock.Clone();
            heavier.stages[0].landingPropMass = 20000;
            Assert.Greater(RecoveryTradeoff.Measure(cat, heavier).PayloadCostKg, RecoveryTradeoff.Measure(cat, light).PayloadCostKg);
        }

        [Test]
        public void Report_NamesTheRecoveryCauses_AndOptionalMissKeepsTheGoal()
        {
            Assert.AreEqual("Landing fuel ran out", FlightDebrief.TitleOf(RecoveryScore.Fuel));
            Assert.AreEqual("Too fast to land", FlightDebrief.TitleOf(RecoveryScore.Fast));
            Assert.AreEqual("Tilted touchdown", FlightDebrief.TitleOf(RecoveryScore.Tilt));
            Assert.AreEqual("Missed Haven", FlightDebrief.TitleOf(RecoveryScore.Miss));
            var missed = new FailureEvent { kind = RecoveryScore.Miss, value = 80, limit = 36, t = 90, kid = "wide", engineer = "80 m" };
            Assert.That(FlightDebrief.NumberLine(missed, null, false), Does.Contain("80"));
            Assert.That(FlightDebrief.NumberLine(missed, null, false), Does.Contain("36"));
            var facts = new FlightFacts
            {
                Passed = true,
                LandingRequired = false,
                Failure = missed,
                ApoKm = 200,
                GoalKm = 200,
            };
            var card = FlightDebrief.Compose(null, null, facts, NarrationPool.Empty, 1, null);
            Assert.IsTrue(card.Passed, "an orbit that did not ask for the booster still counts");
            Assert.AreEqual(RecoveryScore.Miss, card.Kind);
            Assert.That(card.Numbers, Does.Contain("80"));
            facts.LandingRequired = true;
            facts.Passed = false;
            var required = FlightDebrief.Compose(null, null, facts, NarrationPool.Empty, 1, null);
            Assert.IsFalse(required.Passed);
            Assert.AreEqual("Missed Haven", required.CauseTitle);
            FlightDebrief.ClearLatest();
        }

        [Test]
        public void StockCore_BecomesAHavenCraft_OnlyWhenRecoveryIsAskedFor()
        {
            var cat = Golden.Catalog;
            var spent = cat.Preset("stock").Clone();
            spent.recoveryMode = "expendable";
            Assert.IsFalse(RecoveryCrafts.TryFrom(cat, spent, out _));
            var home = spent.Clone();
            home.recoveryMode = "barge";
            Assert.IsTrue(RecoveryCrafts.TryFrom(cat, home, out var craft), "the stock core can come back to Haven");
            Assert.Greater(craft.DryKg, 1000);
            Assert.Greater(craft.ThrustVacN, 10000);
            double held = home.stages[0].landingPropMass;
            if (held > home.stages[0].propMass) held = home.stages[0].propMass;
            Assert.AreEqual(held, craft.ReserveKg, 1e-6);
            Assert.AreEqual("barge", craft.Mode);
            home.stages.RemoveAt(home.stages.Count - 1);
            while (home.stages.Count > 1) home.stages.RemoveAt(home.stages.Count - 1);
            Assert.IsFalse(RecoveryCrafts.TryFrom(cat, home, out _), "a stage that never separates has no recovery");
        }

        static RecoverySample Sample(double sink, double tilt, double miss, double prop, bool dry, RecoveryPhase phase, double legs)
        {
            return new RecoverySample
            {
                Sink = sink,
                TiltDeg = tilt,
                MissM = miss,
                Prop = prop,
                FuelEmpty = dry || prop <= 1e-6,
                Phase = phase,
                Legs01 = legs,
                T = 80,
            };
        }

        static string Trace(RecoveryVerdict v)
        {
            var sb = new System.Text.StringBuilder();
            var marks = v.Marks;
            if (marks == null) return "";
            for (int i = 0; i < marks.Length; i++)
            {
                var m = marks[i];
                sb.Append(m.Phase).Append(" t=").Append(m.T.ToString("0.0"))
                    .Append(" alt=").Append(m.Alt.ToString("0"))
                    .Append(" spd=").Append(m.Speed.ToString("0"))
                    .Append(" sink=").Append(m.Sink.ToString("0"))
                    .Append(" miss=").Append(m.MissM.ToString("0"))
                    .Append(" prop=").Append(m.Prop.ToString("0"))
                    .Append('\n');
            }
            return sb.ToString();
        }

        static string Dump(RecoveryVerdict v)
        {
            var s = v.End;
            return (v.Cause ?? "clean") + " grade=" + v.GradeName + " medal=" + v.Medal
                + " sink=" + s.Sink.ToString("0.00") + " tilt=" + s.TiltDeg.ToString("0.0")
                + " miss=" + s.MissM.ToString("0") + " alt=" + s.Alt.ToString("0")
                + " prop=" + s.Prop.ToString("0") + " t=" + s.T.ToString("0.0")
                + " phase=" + s.Phase + " fins=" + s.Fins.ToString("0.00") + " legs=" + s.Legs01.ToString("0.00");
        }
    }

    static class RecoveryCommandTest
    {
        public static double PitchRad(this RecoveryCommand cmd) => cmd.Pitch;
    }
}
