// DECISIONS #83 (UX review #416 item 5, fun review PR3): the max-Q green sits on the flight's own peak, a design with no
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
            // #473: hop with unknown verdict must never show the sheet's orbital "short" next to Ready.
            var hopCheck = WebcastBoard.Pulse(false, 0, 0, 1443, -89, 0, 35, "First Hop", 10000, FlightVerdict.Checking);
            StringAssert.DoesNotContain("short", hopCheck.Detail);
            StringAssert.Contains("Checking", hopCheck.Detail);
            var hopPass = WebcastBoard.Pulse(false, 0, 0, 1443, -89, 0, 35, "First Hop", 10000, pass);
            Assert.AreEqual("Test flight reaches First Hop", hopPass.Detail);
            // In flight the "short" cue reads the live spare (DECISIONS #90), not the speed share or the verdict:
            // a held live spare reads short even on a design the predictor passes, and a fine live spare reads on
            // track even at 13 % of the budget on a design the predictor fails. The verdict still drives the
            // count line above; it no longer gates the in-flight cue.
            Assert.AreEqual(WebcastBoard.VerdictShort, WebcastBoard.Pulse(true, 30000, 1000, 7600, 0, 1000, 35, "LEO", 0, pass, true).Verdict);
            Assert.AreEqual(WebcastBoard.VerdictShort, WebcastBoard.Pulse(true, 30000, 1000, 7600, 0, 1000, 35, "LEO", 0, miss, true).Verdict);
            Assert.AreEqual(WebcastBoard.VerdictShort, WebcastBoard.Pulse(true, 30000, 1000, 7600, 0, 1000, 35, "LEO", 0, null, true).Verdict);
            Assert.AreNotEqual(WebcastBoard.VerdictShort, WebcastBoard.Pulse(true, 30000, 1000, 7600, 0, 1000, 35, "LEO", 0, miss).Verdict);
            Assert.AreEqual(FlightVerdict.InFlightMedalLine, "Flying the plan.");
        }
    }
}
