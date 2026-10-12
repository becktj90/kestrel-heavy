using Kestrel.Slice;
using NUnit.Framework;

namespace Kestrel.Game.Tests
{
    // P0-3 follow-up (#119 Unity capture: thin orange blobs, no core, faint/missing ground cloud).
    // Sizes are checked relative to the vehicle height: the plume and vehicle sit at the same camera distance,
    // so world-size ratios equal screen-height ratios on every device.
    public class PlumeLayoutTests
    {
        const float H = 70f, R = 6f, ClusterD = 3f, Deck = 15f;   // slice heavy: 70 m, boosters, 9-engine core

        [TestCase(0), TestCase(1), TestCase(2), TestCase(3)]
        public void BodyWideContinuousAndCoreBigAtLiftoff(int level)
        {
            var L = PlumeShape.ComputeLayout(1f, 1f, ClusterD, R, H, Deck);
            var b = PlumeShape.Split(PlumeShape.BudgetForLevel(level), level > 0);
            Assert.That(L.Length / H, Is.GreaterThanOrEqualTo(0.6f), "plume length vs vehicle screen height");
            Assert.That(L.BodyMin * PlumeShape.SpriteSolidFraction / H, Is.GreaterThanOrEqualTo(0.12f), "solid body width vs vehicle height");
            Assert.That(L.CoreSize * PlumeShape.SpriteSolidFraction, Is.GreaterThanOrEqualTo(1.2f * L.VisualD), "core wider than engine section");
            Assert.That(L.VisualD, Is.GreaterThanOrEqualTo(1.5f * R), "plume keyed to vehicle width, not one nozzle");
            Assert.That(PlumeShape.BodyOverlap(L, b.Body), Is.GreaterThanOrEqualTo(2f), $"level {level}: body must overlap (no blobs)");
        }

        [TestCase(0), TestCase(1), TestCase(2), TestCase(3)]
        public void GroundCloudDenseWideAndReadable(int level)
        {
            var L = PlumeShape.ComputeLayout(1f, 1f, ClusterD, R, H, Deck);
            var b = PlumeShape.Split(PlumeShape.BudgetForLevel(level), level > 0);
            Assert.That(L.CloudRadius / Deck, Is.InRange(2f, 4f), "cloud spans 2-4x deck radius");
            Assert.That(2f * L.CloudRadius / H, Is.GreaterThanOrEqualTo(1f), "cloud at least as wide as the vehicle is tall");
            Assert.That(L.CloudSizeMax * PlumeShape.SpriteSolidFraction / H, Is.GreaterThanOrEqualTo(0.3f), "billows read at phone size");
            Assert.That(L.CloudHeight, Is.GreaterThan(0f), "spawns above the deck top");
            Assert.That(b.Cloud, Is.GreaterThanOrEqualTo(PlumeShape.MinCloud), "Low/iPad keeps a dense cloud");
            Assert.That(PlumeShape.CloudCoverage(L, b.Cloud), Is.GreaterThanOrEqualTo(0.9f), $"level {level}: cloud disc covered");
        }

        [Test]
        public void StillWidensWithAltitude()
        {
            var sea = PlumeShape.ComputeLayout(1f, 1f, ClusterD, R, H, Deck);
            var high = PlumeShape.ComputeLayout(1f, 0.01f, ClusterD, R, H, Deck);
            Assert.That(high.Width, Is.GreaterThan(1.5f * sea.Width));
        }
    }
}
