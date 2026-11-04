// DECISIONS #77 (UX review #416 item 5, fun review PR3): the max-Q green sits on the flight's own peak, a design with no
// max-Q setting is never offered the call, and the count card follows the one predictor verdict.
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class Pr3VerdictTests
    {
        const double Lim = 35000;

        static AscentSample At(double t, double q) => new AscentSample
        {
            T = t, AltM = 2000 + 200 * t, SpeedMps = 60, QPa = q, // under the pitch call's 70 m/s: max-Q alone
            G = 2, ThrustN = 8e6, MassKg = 4e5, Thrusting = true, PropFraction = 0.8,
        };

        /// <summary>q climbing to <paramref name="peak"/> at t = 60 s and falling after (a parabola 20 s wide each side).</summary>
        static double Q(double t, double peak) => peak * System.Math.Max(0, 1 - System.Math.Pow((t - 60) / 20.0, 2));

        [Test]
        public void LowPeakFlight_TheGreenIsReachable_AndATapThereEarnsGold()
        {
            // Peak at 0.62 of the line: the old fixed band (0.70..0.96 of the line) was unreachable, so the call could only
            // time out to Bronze. On the flight's own peak the green is there and the window waits for it.
            double peak = 0.62 * Lim;
            bool oldGreen = false;
            var v = new AscentVehicle { QLimPa = Lim, ThrottleMin = 0.4, MaxQSet = true, MaxQRaw = 70 };
            var d = AscentDirector.Start(v, null, null, false, false, false);
            bool tapped = false, sawEarly = false;
            for (double t = 20; t < 100 && !tapped; t += 0.05)
            {
                double q = Q(t, peak);
                oldGreen |= AscentDecisions.GradeMaxQ(q, Lim, 1, 8) == AscentGrade.Perfect;
                d.Advance(At(t, q), 0.05);
                foreach (var p in d.OpenPrompts())
                {
                    if (p.Call != AscentCall.MaxQ) continue;
                    if (!p.InGreen) { sawEarly = true; continue; }
                    Assert.IsTrue(d.Choose(AscentCall.MaxQ, true));
                    tapped = true;
                }
            }
            Assert.IsFalse(oldGreen, "the old band never went green on this flight");
            Assert.IsTrue(sawEarly, "climbing hard is not green yet");
            Assert.IsTrue(tapped, "the green came before the window ran out");
            Assert.AreEqual(AscentGrade.Perfect, d.GradeOf(AscentCall.MaxQ));
            Assert.AreEqual(AscentMedal.Gold, d.Score().Medal);
            Assert.AreEqual(0, d.Command.EngageMaxQCap, 1e-12, "the design's own law flies its cap; no director dip");
        }

        [Test]
        public void TappingWhileQStillClimbsHard_IsEarly_AndAutoAssistWaitsForThePeak()
        {
            double peak = 0.9 * Lim;
            var v = new AscentVehicle { QLimPa = Lim, ThrottleMin = 0.4, MaxQSet = true, MaxQRaw = 70 };
            var early = AscentDirector.Start(v, null, null, false, false, false);
            var auto = AscentDirector.Start(v, null, null, true, false, false);
            double autoAt = double.NaN;
            for (double t = 20; t < 100; t += 0.05)
            {
                var s = At(t, Q(t, peak));
                if (!early.IsDone(AscentCall.MaxQ))
                {
                    early.Advance(s, 0.05);
                    if (early.IsOpen(AscentCall.MaxQ)) Assert.IsTrue(early.Choose(AscentCall.MaxQ, true));
                }
                bool wasOpen = auto.IsOpen(AscentCall.MaxQ);
                auto.Advance(s, 0.05);
                if (wasOpen && auto.IsDone(AscentCall.MaxQ) && double.IsNaN(autoAt)) autoAt = t;
            }
            Assert.AreEqual(AscentGrade.Good, early.GradeOf(AscentCall.MaxQ), "the first open frame is still climbing hard");
            Assert.AreEqual(AscentGrade.Perfect, auto.GradeOf(AscentCall.MaxQ), "auto-assist takes it at the top");
            Assert.Greater(autoAt, 50, "not at the arming line (t ≈ 47.5 s)");
            Assert.LessOrEqual(autoAt, 60.05, "by the peak");
        }

        [Test]
        public void PeakGrade_Pins()
        {
            Assert.AreEqual(AscentGrade.Good, AscentDecisions.GradeMaxQPeak(20000, 20000, Lim, 1, 8, false), "rising hard");
            Assert.AreEqual(AscentGrade.Perfect, AscentDecisions.GradeMaxQPeak(20000, 20000, Lim, 1, 8, true), "at the top");
            Assert.AreEqual(AscentGrade.Perfect, AscentDecisions.GradeMaxQPeak(17500, 20000, Lim, 1, 8, true), "within 15% after");
            Assert.AreEqual(AscentGrade.Late, AscentDecisions.GradeMaxQPeak(16000, 20000, Lim, 1, 8, true), "fell off the peak");
            Assert.AreEqual(AscentGrade.Late, AscentDecisions.GradeMaxQPeak(20000, 20000, Lim, 8, 8, true), "window out");
            Assert.AreEqual(AscentGrade.Late, AscentDecisions.GradeMaxQPeak(36000, 36000, Lim, 1, 8, true), "past the line");
            Assert.IsTrue(AscentDecisions.AtMaxQPeak(19000, 20000, -50));
            Assert.IsTrue(AscentDecisions.AtMaxQPeak(20000, 20000, 0.01 * 20000));
            Assert.IsFalse(AscentDecisions.AtMaxQPeak(20000, 20000, 0.05 * 20000));
        }

        [Test]
        public void NoMaxQSetting_TheCallIsNeverOffered_AndTheThrottleNeverDips()
        {
            var plain = new AscentVehicle { QLimPa = Lim, ThrottleMin = 0.4 };
            foreach (bool auto in new[] { true, false })
            {
                var d = AscentDirector.Start(plain, null, null, auto, false, false);
                for (double t = 20; t < 100; t += 0.05)
                {
                    d.Advance(At(t, Q(t, 0.95 * Lim)), 0.05);
                    Assert.IsFalse(d.IsOpen(AscentCall.MaxQ));
                    Assert.IsFalse(d.Choose(AscentCall.MaxQ, true));
                    Assert.AreEqual(0, d.Command.EngageMaxQCap, 1e-12);
                    Assert.AreEqual(1, d.Command.ThrottleScale, 1e-12);
                }
                Assert.IsFalse(d.IsDone(AscentCall.MaxQ), "nothing to grade, nothing lost");
            }
        }

        [Test]
        public void CountCard_FollowsTheOneVerdict()
        {
            var pass = FlightVerdict.Pass();
            var miss = FlightVerdict.Fail("low");
            // Sheet says short; the predictor's flight passes: the card says it reaches, never "short".
            var a = WebcastBoard.Pulse(false, 0, 0, 7600, -89, 0, 35, "LEO", 0, pass);
            Assert.AreEqual("Test flight reaches LEO", a.Detail);
            var b = WebcastBoard.Pulse(false, 0, 0, 7600, 420, 0, 35, "LEO", 0, miss);
            Assert.AreEqual("Test flight misses LEO", b.Detail);
            var c = WebcastBoard.Pulse(false, 0, 0, 7600, 420, 0, 35, "LEO", 0, FlightVerdict.Checking);
            StringAssert.Contains("m/s to spare for LEO", c.Detail, "unknown keeps the sheet line");
            // In flight below 40% of the speed: "short" only when the verdict does not pass.
            Assert.AreEqual(WebcastBoard.VerdictShort, WebcastBoard.Pulse(true, 30000, 1000, 7600, 0, 1000, 35, "LEO", 0, null).Verdict);
            Assert.AreEqual(WebcastBoard.VerdictShort, WebcastBoard.Pulse(true, 30000, 1000, 7600, 0, 1000, 35, "LEO", 0, miss).Verdict);
            Assert.AreNotEqual(WebcastBoard.VerdictShort, WebcastBoard.Pulse(true, 30000, 1000, 7600, 0, 1000, 35, "LEO", 0, pass).Verdict);
            Assert.AreEqual(FlightVerdict.InFlightMedalLine, "Flying the plan.");
        }
    }
}
