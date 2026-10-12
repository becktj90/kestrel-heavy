using Kestrel.Slice;
using NUnit.Framework;

namespace Kestrel.Game.Tests
{
    // Regression: a fixed 90 m grey cube tower showed only its top end as a stray "grey box" at liftoff.
    public class PadTowerTests
    {
        [TestCase(40f, 2f)] [TestCase(70f, 6f)] [TestCase(120f, 9f)]
        public void TowerIsSizedToRocketAndClearOfIt(float h, float r)
        {
            var (x, z, height) = PadTower.Fit(h, r);
            Assert.That(height, Is.GreaterThanOrEqualTo(h).And.LessThanOrEqualTo(h * 1.15f), "top just above the fairing");
            Assert.That(-x - PadTower.MastWidth * 0.5f - r, Is.GreaterThanOrEqualTo(PadTower.Clearance - 1e-4f), "mast must not intersect the vehicle");
            Assert.That(z, Is.GreaterThan(0f), "behind the vehicle, away from the -z chase camera");
        }

        [Test]
        public void TowerIsDarkSteelNotDefaultGrey()
        {
            Assert.That(PadTower.Steel.maxColorComponent, Is.LessThan(0.3f));
        }
    }
}
