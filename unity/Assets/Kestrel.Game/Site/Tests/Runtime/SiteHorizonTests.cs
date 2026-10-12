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

        [Test]
        public void FarClipAndHoldCoverThe13KmChaseHorizon()
        {
            float h = SiteHorizon.ChaseAltitude;
            float slant = SiteHorizon.Distance(h);
            Assert.That(SiteHorizon.ClipFor(h), Is.GreaterThan(slant));
            Assert.That(SiteView.FarClip, Is.GreaterThan(slant));
            Assert.That(ChaseCamera.HoldWeight(h), Is.EqualTo(1f));
            Assert.That(ChaseCamera.HoldFullAlt, Is.GreaterThanOrEqualTo(h));
        }
    }
}
