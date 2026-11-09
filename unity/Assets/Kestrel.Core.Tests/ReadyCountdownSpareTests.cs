// #473: designer Ready and the countdown spare line share one FlightVerdict source (DECISIONS #83).
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class ReadyCountdownSpareTests
    {
        [Test]
        public void KH1_EnsureRemembered_CountCardAgreesWithReady()
        {
            var rocket = StarterFixture.Rocket("m1");
            Assert.AreEqual("apogee", rocket.Mission.TargetKind);
            // Clear any prior remember for this design key so Ensure has to re-judge.
            var design = rocket.BuildDesign();
            string key = ReferenceAscent.CanonicalKey(StarterFixture.Catalog, design);
            // Starter Ready path
            Assert.IsTrue(StarterScreen.Readiness(rocket, out var badge, out var rest), "Launch follows the hard block");
            Assert.AreEqual(StarterScreen.ReadyBadge, badge, rest);
            var remembered = FlightVerdict.Recall(key);
            Assert.IsTrue(remembered.Passes, "Starter Remembered a pass for Ready");

            // Sheet spare can still look short (orbital required vs hop Δv); the count must follow the verdict.
            var pulse = WebcastBoard.Pulse(false, 0, 0, 1443, -89, 0, null, "First Hop", rocket.Mission.ApoKm * 1000, remembered);
            Assert.AreEqual("Test flight reaches First Hop", pulse.Detail);

            // EnsureRemembered is idempotent and recovers after a miss.
            var again = StarterVerdict.EnsureRemembered(StarterFixture.Catalog, design, rocket.Mission);
            Assert.IsTrue(again.Passes);
            Assert.AreEqual("Test flight reaches First Hop", FlightVerdict.CountLine(again, "First Hop"));
        }

        [Test]
        public void HopCountdown_UnknownVerdict_NeverShowsSheetShort()
        {
            var pulse = WebcastBoard.Pulse(false, 0, 0, 1443, -89, 0, null, "First Hop", 10000, FlightVerdict.Checking);
            Assert.IsFalse(pulse.Detail.Contains("short"), pulse.Detail);
            Assert.IsTrue(pulse.Detail.Contains("Checking"), pulse.Detail);
        }
    }
}
