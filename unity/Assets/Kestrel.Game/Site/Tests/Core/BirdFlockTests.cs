// The flock is a scale ruler: gull-sized, beside the stack, and left behind once the rocket climbs away.
using System;
using NUnit.Framework;

namespace Kestrel.Site.Tests
{
    public class BirdFlockTests
    {
        [Test]
        public void AGullIsAboutAMetreAcross()
        {
            Assert.That(BirdFlock.WingSpan, Is.InRange(0.85f, 1.2f));
            Assert.That(70f / BirdFlock.WingSpan, Is.GreaterThan(50f), "a 70 m stack is dozens of gulls tall");
            var poses = new BirdPose[BirdFlock.Count];
            BirdFlock.Evaluate(1.2f, 0f, 0f, 1f, poses);
            var xyz = new float[BirdFlock.VertexCount * 3];
            for (int i = 0; i < BirdFlock.Count; i++)
                BirdFlock.WriteBird(poses[i], i * BirdFlock.VertsPerBird, xyz, new float[xyz.Length]);
            for (int i = 0; i < BirdFlock.Count; i++)
            {
                int left = (i * BirdFlock.VertsPerBird + 2) * 3;
                int right = (i * BirdFlock.VertsPerBird + 6) * 3;
                float dx = xyz[left] - xyz[right], dy = xyz[left + 1] - xyz[right + 1], dz = xyz[left + 2] - xyz[right + 2];
                float span = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                Assert.That(span, Is.InRange(BirdFlock.WingSpan * 0.9f, BirdFlock.WingSpan * 1.25f), "bird " + i);
            }
        }

        [Test]
        public void OnThePadTheyCircleSouthOfTheStack()
        {
            var poses = new BirdPose[BirdFlock.Count];
            BirdFlock.Evaluate(3f, 0f, 0f, 1f, poses);
            for (int i = 0; i < poses.Length; i++)
            {
                float r = (float)Math.Sqrt(poses[i].X * poses[i].X + poses[i].Z * poses[i].Z);
                Assert.That(r, Is.InRange(8f, 16f), "clear of the airframe, inside the chase frame");
                Assert.That(poses[i].Z, Is.LessThan(-4f), "south of the stack, off the tower");
                Assert.That(poses[i].Y, Is.InRange(6f, 60f));
            }
        }

        [Test]
        public void TheyRideTheEarlyClimbAndAreLeftAtThePad()
        {
            var low = new BirdPose[BirdFlock.Count];
            BirdFlock.Evaluate(4f, 40f, 80f, 1f, low);
            for (int i = 0; i < low.Length; i++)
            {
                float dx = low[i].X - 40f, dz = low[i].Z;
                float r = (float)Math.Sqrt(dx * dx + dz * dz);
                Assert.That(r, Is.InRange(8f, 16f));
                Assert.That(low[i].Y, Is.InRange(80f, 140f), "still alongside the climbing stack");
            }

            var high = new BirdPose[BirdFlock.Count];
            BirdFlock.Evaluate(4f, 5000f, 2000f, 1f, high);
            for (int i = 0; i < high.Length; i++)
            {
                Assert.That(high[i].Y, Is.LessThan(80f), "left behind at the pad");
                float r = (float)Math.Sqrt(high[i].X * high[i].X + high[i].Z * high[i].Z);
                Assert.That(r, Is.LessThan(20f));
            }
            Assert.That(BirdFlock.Alongside(0f), Is.EqualTo(1f));
            Assert.That(BirdFlock.Alongside(BirdFlock.LeaveAlt + 10f), Is.EqualTo(0f));
        }

        [Test]
        public void TheSameMomentPlacesTheSameBirds()
        {
            var a = new BirdPose[BirdFlock.Count];
            var b = new BirdPose[BirdFlock.Count];
            BirdFlock.Evaluate(12.5f, 15f, 40f, 0f, a);
            BirdFlock.Evaluate(12.5f, 15f, 40f, 0f, b);
            for (int i = 0; i < a.Length; i++)
            {
                Assert.That(a[i].X, Is.EqualTo(b[i].X));
                Assert.That(a[i].Y, Is.EqualTo(b[i].Y));
                Assert.That(a[i].Lift, Is.EqualTo(0f));
            }
        }
    }
}
