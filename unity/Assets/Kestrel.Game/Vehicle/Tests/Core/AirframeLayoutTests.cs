using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Kestrel.Vehicle.Tests
{
    public class AirframeLayoutTests
    {
        [Test]
        public void InterstageHasRingsAndStringersOutsideTheCone()
        {
            float y0 = 12f, y1 = 16f, r0 = 1.8f, r1 = 1.2f;
            var poses = AirframeLayout.Interstage(y0, y1, r0, r1);
            Assert.That(Count(poses, AirframeLayout.JointRing), Is.EqualTo(2));
            Assert.That(Count(poses, AirframeLayout.Stringer), Is.EqualTo(AirframeLayout.StringerCount));
            for (int i = 0; i < poses.Count; i++)
            {
                var p = poses[i];
                float axis = (float)Math.Sqrt(p.AxisX * p.AxisX + p.AxisY * p.AxisY + p.AxisZ * p.AxisZ);
                Assert.That(axis, Is.EqualTo(1f).Within(1e-4f));
                if (p.Kind != AirframeLayout.Stringer) continue;
                float radial = (float)Math.Sqrt(p.X * p.X + p.Z * p.Z);
                Assert.That(radial, Is.GreaterThan(Math.Min(r0, r1)));
                Assert.That(p.Y, Is.GreaterThan(y0));
                Assert.That(p.Y, Is.LessThan(y1));
            }
            var lower = poses.Find(p => p.Kind == AirframeLayout.JointRing && p.Y < (y0 + y1) * 0.5f);
            Assert.That(lower.Radius, Is.GreaterThan(r0));
        }

        [Test]
        public void RacewaySitsOnTheTowerSideOfTheTank()
        {
            var run = AirframeLayout.RacewayRun(0f, 0f, 0f, 1.85f, 4f, 20f);
            Assert.That(run.Kind, Is.EqualTo(AirframeLayout.Raceway));
            Assert.That(run.X, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(run.Z, Is.GreaterThan(1.85f));
            Assert.That(run.Length, Is.EqualTo(20f).Within(1e-4f));
            Assert.That(run.Y, Is.EqualTo(14f).Within(1e-3f));
            Assert.That(AirframeLayout.RacewayClearance(1.85f, run, 0f, 0f), Is.GreaterThan(0.02f));

            var strap = AirframeLayout.RacewayRun(3.2f, 0f, -1.1f, 0.7f, 1.2f, 8f);
            Assert.That(strap.X, Is.EqualTo(3.2f).Within(1e-4f));
            Assert.That(strap.Z, Is.GreaterThan(-1.1f + 0.7f));
        }

        [Test]
        public void TwoInterstagesAndFourRacewaysStayOneBatch()
        {
            var all = new List<StrutPose>();
            all.AddRange(AirframeLayout.Interstage(10f, 14f, 1.8f, 1.6f));
            all.AddRange(AirframeLayout.Interstage(30f, 33f, 1.6f, 1.5f));
            for (int i = 0; i < 4; i++) all.Add(AirframeLayout.RacewayRun(i * 0.1f, 0f, 0f, 1.5f, 2f, 12f));
            int tris = AirframeLayout.TriangleCount(all, 0);
            Assert.That(tris, Is.LessThan(2000));
            Assert.That(AirframeLayout.DrawCalls(all, 0), Is.EqualTo(1));
            Assert.That(AirframeLayout.TriangleCount(all, 2), Is.LessThan(tris));
            Assert.That(AirframeLayout.DrawCalls(all, 2), Is.EqualTo(1));
            Assert.That(AirframeLayout.Draws(AirframeLayout.Stringer, 2), Is.False);
            Assert.That(AirframeLayout.Draws(AirframeLayout.Raceway, 2), Is.True);
        }

        static int Count(List<StrutPose> poses, string kind)
        {
            int n = 0;
            for (int i = 0; i < poses.Count; i++) if (poses[i].Kind == kind) n++;
            return n;
        }
    }
}
