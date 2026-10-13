using System.Collections.Generic;
using NUnit.Framework;

namespace Kestrel.Vehicle.Tests
{
    public class LiveryLayoutTests
    {
        [Test]
        public void PanelRingsStepAlongTheBarrelAndClearTheSkin()
        {
            var marks = LiveryLayout.Tank(0f, 0f, 0f, 1.85f, 4f, 20f, true);
            int rings = 0, seams = 0, bands = 0, decals = 0;
            float prev = -1f;
            for (int i = 0; i < marks.Count; i++)
            {
                var m = marks[i];
                switch (m.Kind)
                {
                    case LiveryLayout.Ring:
                        rings++;
                        Assert.That(LiveryLayout.RingClearance(m, 1.85f), Is.GreaterThan(0.01f));
                        if (prev > 0f) Assert.That(m.Y - prev, Is.EqualTo(LiveryLayout.PanelPitch).Within(1e-3f));
                        prev = m.Y;
                        break;
                    case LiveryLayout.Seam: seams++; break;
                    case LiveryLayout.Band: bands++; break;
                    case LiveryLayout.Decal: decals++; break;
                }
            }
            Assert.That(rings, Is.EqualTo(8));
            Assert.That(seams, Is.EqualTo(2));
            Assert.That(bands, Is.EqualTo(1));
            Assert.That(decals, Is.EqualTo(3));
            Assert.That(LiveryMesh.Torus(0).TriangleCount, Is.EqualTo(LiveryMesh.TorusTriangles(0)));
            Assert.That(LiveryMesh.Box().TriangleCount, Is.EqualTo(12));
        }

        [Test]
        public void AccentIsABandAndBlocksNotAStripe()
        {
            var plain = LiveryLayout.Tank(0f, 0f, 0f, 1.5f, 2f, 12f, false);
            for (int i = 0; i < plain.Count; i++)
                Assert.That(LiveryLayout.Accent(plain[i].Kind), Is.False);
            var painted = LiveryLayout.Tank(1f, 0f, 2f, 1.5f, 2f, 12f, true);
            Assert.That(LiveryLayout.DrawCalls(painted, 0), Is.LessThanOrEqualTo(4));
            Assert.That(LiveryLayout.TriangleCount(painted, 0), Is.LessThan(3000));
            Assert.That(LiveryLayout.DrawCalls(painted, 2), Is.EqualTo(2));
            Assert.That(LiveryLayout.TriangleCount(painted, 2), Is.LessThan(LiveryLayout.TriangleCount(painted, 0)));
        }

        [Test]
        public void TwoTanksStayInsideASmallBudget()
        {
            var all = new List<LiveryMark>();
            all.AddRange(LiveryLayout.Tank(0f, 0f, 0f, 1.85f, 3f, 22f, true));
            all.AddRange(LiveryLayout.Tank(0f, 0f, 0f, 1.6f, 28f, 14f, false));
            Assert.That(LiveryLayout.TriangleCount(all, 0), Is.LessThan(6000));
            Assert.That(LiveryLayout.DrawCalls(all, 0), Is.LessThanOrEqualTo(4));
        }
    }
}
