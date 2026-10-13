using System;
using NUnit.Framework;

namespace Kestrel.Vehicle.Tests
{
    public class FairingLayoutTests
    {
        [Test]
        public void RingsAndSeamsSitOutsideTheOgive()
        {
            float r = 2.2f, L = 12f, nose = 0.45f;
            var poses = FairingLayout.Build(40f, r, L, nose, false);
            int rings = 0, seams = 0;
            float shoulder = 40f + L * (1f - nose);
            bool shoulderHit = false, baseHit = false, noseRing = false;
            for (int i = 0; i < poses.Count; i++)
            {
                var p = poses[i];
                float axis = (float)Math.Sqrt(p.AxisX * p.AxisX + p.AxisY * p.AxisY + p.AxisZ * p.AxisZ);
                Assert.That(axis, Is.EqualTo(1f).Within(1e-4f));
                Assert.That(p.Y, Is.GreaterThan(39f));
                Assert.That(p.Y, Is.LessThan(40f + L));
                if (p.Kind == FairingLayout.Ring)
                {
                    rings++;
                    Assert.That(p.Radius, Is.GreaterThan(0.05f));
                    if (Math.Abs(p.Y - (40f + 0.0275f)) < 0.02f) { baseHit = true; Assert.That(p.Radius, Is.GreaterThan(r)); }
                    if (Math.Abs(p.Y - shoulder) < 0.02f) { shoulderHit = true; Assert.That(p.Radius, Is.GreaterThan(r)); }
                    if (p.Y > shoulder + 0.2f) { noseRing = true; Assert.That(p.Radius, Is.LessThan(r)); }
                }
                else seams++;
            }
            Assert.That(baseHit, Is.True);
            Assert.That(shoulderHit, Is.True);
            Assert.That(noseRing, Is.True);
            Assert.That(seams, Is.GreaterThanOrEqualTo(2));
            Assert.That(rings, Is.GreaterThanOrEqualTo(4));
            Assert.That(AirframeLayout.DrawCalls(poses, 0), Is.EqualTo(1));
            Assert.That(AirframeLayout.TriangleCount(poses, 0), Is.LessThan(500));
        }

        [Test]
        public void ConicNoseIsNarrowerThanTheOgiveAtTheSameStation()
        {
            float r = 2f;
            float ogive = FairingLayout.SectionRadius(r, 0.5f, false);
            float conic = FairingLayout.SectionRadius(r, 0.5f, true);
            Assert.That(ogive, Is.EqualTo(r * Math.Sqrt(0.75)).Within(1e-4));
            Assert.That(conic, Is.EqualTo(r * 0.5f).Within(1e-4));
            Assert.That(conic, Is.LessThan(ogive));
            var nose = FairingLayout.Build(10f, r, 8f, 0.4f, true);
            bool narrower = false;
            for (int i = 0; i < nose.Count; i++)
                if (nose[i].Kind == FairingLayout.Ring && nose[i].Y > 10f + 8f * 0.6f + 0.2f)
                    narrower = nose[i].Radius < FairingLayout.SectionRadius(r, 0.5f, false);
            Assert.That(narrower, Is.True);
        }
    }
}
