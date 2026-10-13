using System;
using NUnit.Framework;

namespace Kestrel.Vehicle.Tests
{
    public class LandingLayoutTests
    {
        [Test]
        public void FourLegsReachThePadOutsideTheSkirt()
        {
            float r = 1.85f, aft = 2.4f;
            var legs = LandingLayout.Legs(r, aft);
            int struts = 0, feet = 0;
            for (int i = 0; i < legs.Count; i++)
            {
                var p = legs[i];
                Assert.That(p.Y, Is.GreaterThan(-0.01f));
                float axis = (float)Math.Sqrt(p.AxisX * p.AxisX + p.AxisY * p.AxisY + p.AxisZ * p.AxisZ);
                Assert.That(axis, Is.EqualTo(1f).Within(1e-4f));
                if (p.Kind == LandingLayout.Foot)
                {
                    feet++;
                    float radial = (float)Math.Sqrt(p.X * p.X + p.Z * p.Z);
                    Assert.That(radial, Is.GreaterThan(r * 1.4f));
                    Assert.That(p.Y, Is.LessThan(0.1f));
                }
                else struts++;
            }
            Assert.That(struts, Is.EqualTo(8));
            Assert.That(feet, Is.EqualTo(4));
            Assert.That(LandingLayout.LegDrawCalls(legs), Is.EqualTo(1));
            Assert.That(LandingLayout.LegTriangles(legs, 0), Is.LessThan(400));
        }

        [Test]
        public void GridFinsAreOneLatticeOutsideTheBooster()
        {
            float r = 1.85f;
            var fins = LandingLayout.Fins(r, 40f);
            Assert.That(fins.Count, Is.EqualTo(4));
            for (int i = 0; i < fins.Count; i++)
            {
                float radial = (float)Math.Sqrt(fins[i].X * fins[i].X + fins[i].Z * fins[i].Z);
                Assert.That(radial, Is.EqualTo(r).Within(1e-3f));
                Assert.That(fins[i].Y, Is.LessThan(40f));
                Assert.That(fins[i].Span, Is.GreaterThan(r * 0.4f));
                float axis = (float)Math.Sqrt(fins[i].AxisX * fins[i].AxisX + fins[i].AxisZ * fins[i].AxisZ);
                Assert.That(axis, Is.EqualTo(1f).Within(1e-4f));
            }
            int one = FinMesh.TriangleCount(0);
            Assert.That(one, Is.GreaterThan(40));
            Assert.That(one * 4, Is.LessThan(2000));
            Assert.That(FinMesh.DrawCalls(4), Is.EqualTo(1));
            Assert.That(FinMesh.TriangleCount(2), Is.LessThan(FinMesh.TriangleCount(0)));
            var mesh = FinMesh.Build(0);
            for (int i = 0; i < mesh.Positions.Count; i++)
                Assert.That(float.IsNaN(mesh.Positions[i]), Is.False);
        }
    }
}
