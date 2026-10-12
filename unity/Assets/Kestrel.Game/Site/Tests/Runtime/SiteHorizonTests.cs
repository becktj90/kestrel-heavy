// Locks the terrain sphere to the chase camera so the coast stays on the horizon the camera already frames.
using Kestrel.Slice;
using NUnit.Framework;

namespace Kestrel.Site.Tests
{
    public class SiteHorizonTests
    {
        [Test]
        public void CurvatureMatchesChaseCameraEarth()
        {
            Assert.That(SiteHorizon.EarthRadius, Is.EqualTo(ChaseCamera.EarthRadius));
            float h = PadGround.MinCameraClearance;
            float horizon = SiteHorizon.Distance(h);
            Assert.That(SiteHorizon.WaterRadius, Is.GreaterThan(horizon));
            Assert.That(Coastline.ShoreX(0f), Is.LessThan(horizon), "the coast is in front of the pad-level horizon");
            Assert.That(Coastline.ShoreX(0f), Is.GreaterThan(0f), "sea is downrange of the pad");
        }
    }
}
