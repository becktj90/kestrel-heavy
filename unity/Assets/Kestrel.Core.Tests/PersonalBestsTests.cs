using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class PersonalBestsTests
    {
        [Test]
        public void Pass_ImprovesLowerCostAndHigherMargin()
        {
            var rec = new MissionMetrics();
            var first = PersonalBests.Apply(rec, new PbSnapshot(100, 5000, 0.05, 10, true));
            Assert.IsTrue(first.Any);
            Assert.AreEqual(100, rec.BestCostKc, 1e-9);
            var worse = PersonalBests.Apply(rec, new PbSnapshot(120, 6000, 0.02, 20, true));
            Assert.IsFalse(worse.Any);
            var better = PersonalBests.Apply(rec, new PbSnapshot(80, 4000, 0.08, 5, true));
            Assert.IsTrue(better.Cost);
            Assert.IsTrue(better.Mass);
            Assert.IsTrue(better.Margin);
            Assert.IsTrue(better.Landing);
            StringAssert.Contains("New personal best", better.Line);
        }

        [Test]
        public void Miss_DoesNotImprove()
        {
            var rec = new MissionMetrics { BestCostKc = 50 };
            var delta = PersonalBests.Apply(rec, new PbSnapshot(10, 1, 1, 0, false));
            Assert.IsFalse(delta.Any);
            Assert.AreEqual(50, rec.BestCostKc, 1e-9);
        }
    }
}
