using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FlightGhostTests
    {
        [SetUp]
        public void Clear() => FlightGhost.Clear();

        [Test]
        public void Capture_StoresFailAlong_AndTryForMatchesMission()
        {
            var plan = new TrajectoryPlan { Progress = 0.42f, Marks = new[]
            {
                new TrajectoryMark { Along = 0.1f, Passed = true },
                new TrajectoryMark { Along = 0.4f, Passed = true },
            }};
            FlightGhost.Capture("m3", plan, true);
            Assert.IsTrue(FlightGhost.Has);
            Assert.IsTrue(FlightGhost.TryFor("m3", out float fail, out float peak));
            Assert.AreEqual(0.42f, fail, 1e-4f);
            Assert.GreaterOrEqual(peak, 0.4f);
            Assert.IsFalse(FlightGhost.TryFor("m4", out _, out _));
        }

        [Test]
        public void CommitLive_UsesRememberedPlan()
        {
            var plan = new TrajectoryPlan { Progress = 0.7f };
            FlightGhost.RememberLive("m1", plan);
            FlightGhost.CommitLive(false);
            Assert.IsTrue(FlightGhost.TryFor("m1", out float fail, out float peak));
            Assert.IsTrue(float.IsNaN(fail));
            Assert.AreEqual(0.7f, peak, 1e-4f);
        }
    }
}
