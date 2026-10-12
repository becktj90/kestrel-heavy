using Kestrel.Slice;
using NUnit.Framework;

namespace Kestrel.Game.Tests
{
    // P0 fix 3: plume was a thin string of orange blobs (no core, no expansion, no ground cloud).
    public class PlumeShapeTests
    {
        [Test]
        public void WidensMonotonicallyWithAltitude()
        {
            float prev = 0;
            for (int km = 0; km <= 80; km += 5)
            {
                float pa = (float)System.Math.Exp(-km / 7.0);
                var s = PlumeShape.Compute(1f, pa, 3f);
                Assert.That(s.Width, Is.GreaterThanOrEqualTo(prev - 1e-4f), $"{km} km");
                prev = s.Width;
            }
            Assert.That(PlumeShape.Compute(1f, 1f, 3f).Width, Is.EqualTo(3f * 0.7f).Within(0.3f), "sea level ~ pinched column");
            Assert.That(prev, Is.EqualTo(3f * 6f).Within(1e-3f), "vacuum caps at 6 D");
        }

        [Test]
        public void LengthGrowsWithThrottleAndDiamondsOnlyLow()
        {
            Assert.That(PlumeShape.Compute(1f, 1f, 3f).Length, Is.GreaterThan(PlumeShape.Compute(0.2f, 1f, 3f).Length));
            Assert.That(PlumeShape.Compute(1f, 1f, 3f).Length, Is.EqualTo(3f * 16f).Within(1e-3f));
            Assert.That(PlumeShape.Compute(1f, 1f, 3f).Diamonds, Is.EqualTo(7));
            Assert.That(PlumeShape.Compute(1f, 0.01f, 3f).Diamonds, Is.EqualTo(0));
        }

        [TestCase(0), TestCase(1), TestCase(2), TestCase(3)]
        public void SplitNeverExceedsPresetBudget(int level)
        {
            int budget = PlumeShape.BudgetForLevel(level);
            var b = PlumeShape.Split(budget, level > 0);
            Assert.That(b.Total, Is.LessThanOrEqualTo(budget));
            Assert.That(b.Core, Is.GreaterThan(0)); Assert.That(b.Body, Is.GreaterThan(0)); Assert.That(b.Cloud, Is.GreaterThan(0));
            if (level == 0) Assert.That(b.Diamonds, Is.EqualTo(0), "Low = basic plume");
        }

        [Test]
        public void GroundCloudFullAtPadAndGoneAfterClearingTower()
        {
            const float tower = 80f;
            Assert.That(PlumeShape.GroundCloud(1f, 0f, tower), Is.EqualTo(1f));
            Assert.That(PlumeShape.GroundCloud(1f, tower * 0.9f, tower), Is.EqualTo(1f));
            Assert.That(PlumeShape.GroundCloud(1f, tower * 2.5f, tower), Is.EqualTo(0f));
            Assert.That(PlumeShape.GroundCloud(0f, 0f, tower), Is.EqualTo(0f), "no cloud before ignition");
        }
    }
}
