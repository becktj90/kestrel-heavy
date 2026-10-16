// The flock is a scale ruler: gull-sized, south of the stack, flushed at liftoff, gone above the pad air.
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
            BirdFlock.Evaluate(2.4f, 0f, 0f, 0f, poses);
            var xyz = Mesh(poses);
            for (int i = 0; i < BirdFlock.Count; i++)
            {
                float span = Dist(xyz, Tip(i, BirdFlock.LeftTip), Tip(i, BirdFlock.RightTip));
                float full = BirdFlock.WingSpan * poses[i].Scale;
                Assert.That(span, Is.InRange(full * 0.90f, full * 1.08f), "glide span, bird " + i);
            }
        }

        [Test]
        public void TheOutlineIsAGullNotARectangle()
        {
            var poses = new BirdPose[BirdFlock.Count];
            BirdFlock.Evaluate(2.4f, 0f, 0f, 0f, poses);
            var xyz = Mesh(poses);
            int gulls = 0, slender = 0;
            for (int i = 0; i < BirdFlock.Count; i++)
            {
                if (poses[i].Slender > 0.5f) slender++;
                else gulls++;
                int rootL = Tip(i, 8), rootT = Tip(i, 16);
                int handL = Tip(i, 11), handT = Tip(i, 13);
                float root = Dist(xyz, rootL, rootT);
                float hand = Dist(xyz, handL, handT);
                Assert.That(hand, Is.LessThan(root * 0.75f), "the hand is narrower than the arm");
                float beak = Along(poses[i], xyz, Tip(i, BirdFlock.Beak));
                float tail = Along(poses[i], xyz, Tip(i, BirdFlock.TailTip));
                float shoulder = Along(poses[i], xyz, rootL);
                Assert.That(beak, Is.GreaterThan(shoulder + 0.08f), "a beak, ahead of the wing");
                Assert.That(tail, Is.LessThan(shoulder - 0.10f), "a tail, behind the wing");
                Assert.That(SpanOf(xyz, i), Is.GreaterThan(Math.Abs(beak - tail) * 1.6f), "wings longer than the body");
            }
            Assert.That(gulls, Is.GreaterThan(0));
            Assert.That(slender, Is.GreaterThan(0), "a few narrower, more swept birds in the same skein");
        }

        [Test]
        public void WingsFlapOutOfPhaseAndGlide()
        {
            int flapping = 0, gliding = 0;
            var at = Cruise(1.1f);
            for (int i = 0; i < at.Length; i++)
            {
                if (at[i].Glide > 0.8f) gliding++;
                if (at[i].Glide < 0.2f) flapping++;
            }
            Assert.That(flapping, Is.GreaterThan(1), "not every bird flaps together");
            Assert.That(gliding, Is.GreaterThan(0), "some hold a glide");

            int bird = -1;
            float lo = 1f, hi = -1f;
            for (int n = 0; n < 80 && bird < 0; n++)
            {
                float t = n * 0.1f;
                var poses = Cruise(t);
                for (int i = 0; i < poses.Length; i++)
                    if (poses[i].Glide < 0.05f) { bird = i; break; }
            }
            Assert.That(bird, Is.GreaterThanOrEqualTo(0));
            for (int n = 0; n < 80; n++)
            {
                var poses = Cruise(n * 0.08f);
                if (poses[bird].Glide > 0.2f) continue;
                if (poses[bird].Lift < lo) lo = poses[bird].Lift;
                if (poses[bird].Lift > hi) hi = poses[bird].Lift;
            }
            Assert.That(hi - lo, Is.GreaterThan(0.4f), "the hand travels through an upstroke and a downstroke");

            var a = Cruise(0.4f);
            bool differed = false;
            for (int i = 1; i < a.Length; i++)
                if (Math.Abs(a[i].WingTip - a[0].WingTip) > 0.05f) differed = true;
            Assert.That(differed, Is.True, "per-bird phase");

            var held = new BirdPose[BirdFlock.Count];
            BirdFlock.Evaluate(0.4f, 0f, 0f, 0f, held);
            BirdFlock.Evaluate(3.6f, 0f, 0f, 0f, a);
            for (int i = 0; i < held.Length; i++)
            {
                Assert.That(held[i].Lift, Is.EqualTo(0f));
                Assert.That(held[i].Glide, Is.EqualTo(1f));
                Assert.That(a[i].WingTip, Is.EqualTo(held[i].WingTip), "Reduce Motion keeps the glide pose");
            }
        }

        [Test]
        public void OnThePadTheyWorkTheSouthAirAsAFlock()
        {
            var poses = Cruise(3f);
            for (float t = 0f; t <= 24f; t += 0.5f)
            {
                var sample = Cruise(t);
                Assert.That(MinSeparation(sample), Is.GreaterThan(1.15f), "boid separation at t=" + t);
                for (int i = 0; i < sample.Length; i++)
                {
                    float r = Horiz(sample[i].X, sample[i].Z);
                    Assert.That(r, Is.InRange(7f, 48f), "clear of the airframe at t=" + t);
                    Assert.That(sample[i].Z, Is.LessThan(-6f), "south of the stack at t=" + t);
                    Assert.That(sample[i].Y, Is.InRange(6f, 26f), "t=" + t);
                    Assert.That(sample[i].Presence, Is.EqualTo(1f));
                }
            }
            float yawSpread = 0f, yawMax = 0f;
            for (int i = 0; i < poses.Length; i++)
            {
                float d = Math.Abs(Delta(poses[i].Yaw, poses[0].Yaw));
                yawSpread += d;
                if (d > yawMax) yawMax = d;
            }
            Assert.That(yawSpread / poses.Length, Is.LessThan(0.45f), "headings agree: one skein, not twelve orbits");
            Assert.That(yawMax, Is.GreaterThan(0.02f), "headings are not a single cloned yaw");
            bool varied = false;
            for (int i = 1; i < poses.Length; i++)
                if (Math.Abs(poses[i].Scale - poses[0].Scale) > 0.02f) varied = true;
            Assert.That(varied, Is.True);
        }

        [Test]
        public void IgnitionScattersThemAndLowAltitudeEndsThem()
        {
            Assert.That(BirdFlock.LeaveAlt, Is.LessThan(200f), "gone above low altitude, not carried into the climb");
            Assert.That(BirdFlock.Flush(0f), Is.EqualTo(0f));
            Assert.That(BirdFlock.Flush(BirdFlock.IgniteAlt + 20f), Is.EqualTo(1f));
            Assert.That(BirdFlock.Alongside(0f), Is.EqualTo(1f));
            Assert.That(BirdFlock.Alongside(BirdFlock.LeaveAlt + 10f), Is.EqualTo(0f));

            var calm = Cruise(4f);
            var flushed = new BirdPose[BirdFlock.Count];
            BirdFlock.Evaluate(4f, 0f, 18f, 1f, flushed);
            float calmR = 0f, flushR = 0f, calmY = 0f, flushY = 0f;
            for (int i = 0; i < calm.Length; i++)
            {
                calmR += Horiz(calm[i].X, calm[i].Z);
                flushR += Horiz(flushed[i].X, flushed[i].Z);
                calmY += calm[i].Y;
                flushY += flushed[i].Y;
                Assert.That(flushed[i].Glide, Is.EqualTo(0f), "a flush is all wingbeats");
                Assert.That(flushed[i].Presence, Is.EqualTo(1f));
            }
            Assert.That(flushR, Is.GreaterThan(calmR + 80f), "the skein bursts outward");
            Assert.That(flushY, Is.GreaterThan(calmY + 40f), "and climbs off the pad");

            var gone = new BirdPose[BirdFlock.Count];
            BirdFlock.Evaluate(4f, 5000f, 2000f, 1f, gone);
            var xyz = Mesh(gone);
            for (int i = 0; i < gone.Length; i++)
            {
                Assert.That(gone[i].Presence, Is.EqualTo(0f));
                Assert.That(gone[i].Y, Is.LessThan(80f), "left in the pad air, not riding the rocket");
                int a = Tip(i, 0), b = Tip(i, BirdFlock.LeftTip);
                Assert.That(Dist(xyz, a, b), Is.LessThan(0.01f), "a hidden bird has no silhouette to alias into a speck");
            }
        }

        [Test]
        public void DistanceScaleStaysLifeSizeUpCloseAndCapsFarAway()
        {
            Assert.That(BirdFlock.AppearScale(40f), Is.EqualTo(1f));
            Assert.That(BirdFlock.AppearScale(270f), Is.InRange(1.4f, 2f));
            Assert.That(BirdFlock.AppearScale(5000f), Is.EqualTo(2f));
            Assert.That(70f / (BirdFlock.WingSpan * BirdFlock.AppearScale(5000f)), Is.GreaterThan(30f));
        }

        [Test]
        public void TintsStaySkyFamilyNotBlack()
        {
            for (int k = 0; k < BirdFlock.VertsPerBird; k++)
            {
                BirdFlock.VertexColor(k, out float r, out float g, out float b);
                Assert.That(r, Is.GreaterThan(0.4f));
                Assert.That(g, Is.GreaterThan(0.4f));
                Assert.That(b, Is.GreaterThan(0.4f));
            }
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
                Assert.That(a[i].Z, Is.EqualTo(b[i].Z));
                Assert.That(a[i].Lift, Is.EqualTo(0f));
                Assert.That(a[i].Yaw, Is.EqualTo(b[i].Yaw));
            }
        }

        [Test]
        public void TheFlockCanBeDisabled()
        {
            bool prev = BirdFlock.Enabled;
            try
            {
                BirdFlock.Enabled = false;
                Assert.That(BirdFlock.Alongside(0f), Is.EqualTo(0f));
                var poses = new BirdPose[BirdFlock.Count];
                BirdFlock.Evaluate(2f, 0f, 0f, 1f, poses);
                var xyz = Mesh(poses);
                for (int i = 0; i < poses.Length; i++)
                {
                    Assert.That(poses[i].Presence, Is.EqualTo(0f));
                    Assert.That(Dist(xyz, Tip(i, 0), Tip(i, BirdFlock.LeftTip)), Is.LessThan(0.01f));
                }
            }
            finally
            {
                BirdFlock.Enabled = prev;
            }
        }

        static BirdPose[] Cruise(float time)
        {
            var poses = new BirdPose[BirdFlock.Count];
            BirdFlock.Evaluate(time, 0f, 0f, 1f, poses);
            return poses;
        }

        static float[] Mesh(BirdPose[] poses)
        {
            var xyz = new float[BirdFlock.VertexCount * 3];
            var nrm = new float[xyz.Length];
            for (int i = 0; i < poses.Length; i++)
                BirdFlock.WriteBird(poses[i], i * BirdFlock.VertsPerBird, xyz, nrm);
            return xyz;
        }

        static int Tip(int bird, int local) => bird * BirdFlock.VertsPerBird + local;

        static float Dist(float[] xyz, int a, int b)
        {
            int ia = a * 3, ib = b * 3;
            float dx = xyz[ia] - xyz[ib], dy = xyz[ia + 1] - xyz[ib + 1], dz = xyz[ia + 2] - xyz[ib + 2];
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        static float SpanOf(float[] xyz, int bird) =>
            Dist(xyz, Tip(bird, BirdFlock.LeftTip), Tip(bird, BirdFlock.RightTip));

        static float Along(BirdPose b, float[] xyz, int vert)
        {
            int o = vert * 3;
            float dx = xyz[o] - b.X, dz = xyz[o + 2] - b.Z;
            float fx = (float)Math.Sin(b.Yaw), fz = (float)Math.Cos(b.Yaw);
            return dx * fx + dz * fz;
        }

        static float Horiz(float x, float z) => (float)Math.Sqrt(x * x + z * z);

        static float MinSeparation(BirdPose[] poses)
        {
            float min = 1e9f;
            for (int i = 0; i < poses.Length; i++)
            for (int j = i + 1; j < poses.Length; j++)
            {
                float dx = poses[i].X - poses[j].X, dy = poses[i].Y - poses[j].Y, dz = poses[i].Z - poses[j].Z;
                float d = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (d < min) min = d;
            }
            return min;
        }

        static float Delta(float a, float b)
        {
            float d = b - a;
            while (d > Math.PI) d -= (float)(Math.PI * 2);
            while (d < -Math.PI) d += (float)(Math.PI * 2);
            return d;
        }
    }
}
