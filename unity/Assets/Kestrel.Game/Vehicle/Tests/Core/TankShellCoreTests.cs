// Engine-free tests: run in Unity EditMode (Kestrel.Vehicle.Tests) and in dotnet (Tests/Dotnet~).
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Kestrel.Vehicle.Tests
{
    public class TankShellCoreTests
    {
        static TankShellSpec Spec(float r = 1.85f, float len = 9.6f, ulong seed = 42) => TankShellSpec.Default(r, len, seed);

        [Test]
        public void SpecValidation()
        {
            Assert.That(Spec().Validate(), Is.Empty);
            var bad = Spec(); bad.Radius = 0; bad.DomeRatio = 2;
            Assert.That(bad.Validate().Count, Is.EqualTo(2));
            Assert.Throws<ArgumentException>(() => TankLayout.Build(bad));
        }

        [Test]
        public void CircWeldsSplitBarrelIntoPanels()
        {
            var s = Spec(); var L = TankLayout.Build(s);
            Assert.That(L.BarrelSections, Is.EqualTo(4));                 // 9.6 / 2.4
            Assert.That(L.CircWelds.Count, Is.EqualTo(5));                 // includes both dome joints
            Assert.That(L.CircWelds.First().Y, Is.EqualTo(s.BarrelBottom).Within(1e-4));
            Assert.That(L.CircWelds.Last().Y, Is.EqualTo(s.BarrelTop).Within(1e-4));
            for (int i = 1; i < L.CircWelds.Count; i++)
                Assert.That(L.CircWelds[i].Y - L.CircWelds[i - 1].Y, Is.LessThanOrEqualTo(s.PanelLength + 1e-4));
        }

        [Test]
        public void TwoDomeTankHasEquatorWeldOnly()
        {
            var sp = Spec(1.2f, 0); sp.AftDome = DomeStyle.Ellipsoidal;
            var L = TankLayout.Build(sp);
            Assert.That(L.BarrelSections, Is.EqualTo(0));
            Assert.That(L.CircWelds.Count, Is.EqualTo(1));
            Assert.That(L.LongWelds, Is.Empty);
            Assert.That(L.Stiffeners, Is.Empty);
        }

        [Test]
        public void LongWeldsAreStaggeredAndPanelWidthRespected()
        {
            var s = Spec(); var L = TankLayout.Build(s);
            int m = (int)Math.Ceiling(2 * Math.PI * s.Radius / s.MaxPanelWidth);
            Assert.That(L.LongWelds.Count, Is.EqualTo(m * L.BarrelSections));
            var firstPerSection = Enumerable.Range(0, L.BarrelSections).Select(k => L.LongWelds[k * m].Azimuth).ToList();
            Assert.That(firstPerSection.Distinct().Count(), Is.EqualTo(L.BarrelSections), "seams must not line up section to section");
            Assert.That(2 * Math.PI * s.Radius / m, Is.LessThanOrEqualTo(s.MaxPanelWidth + 1e-4));
        }

        [Test]
        public void StiffenersKeepClearOfWelds()
        {
            var s = Spec(); var L = TankLayout.Build(s);
            Assert.That(L.Stiffeners, Is.Not.Empty);
            foreach (var r in L.Stiffeners)
            {
                Assert.That(r.Y, Is.GreaterThan(s.BarrelBottom).And.LessThan(s.BarrelTop));
                foreach (var w in L.CircWelds) Assert.That(Math.Abs(r.Y - w.Y), Is.GreaterThanOrEqualTo(s.WeldClearance - 1e-4));
            }
        }

        [Test]
        public void LayoutIsDeterministicPerSeed()
        {
            var a = TankLayout.Build(Spec(seed: 7)); var b = TankLayout.Build(Spec(seed: 7)); var c = TankLayout.Build(Spec(seed: 8));
            Assert.That(a.LongWelds.Select(x => x.Azimuth), Is.EqualTo(b.LongWelds.Select(x => x.Azimuth)));
            Assert.That(a.LongWelds.Select(x => x.Azimuth), Is.Not.EqualTo(c.LongWelds.Select(x => x.Azimuth)));
        }

        [Test]
        public void ProfileIsMonotonicWithPolesAndBarrel()
        {
            foreach (var aft in new[] { DomeStyle.Flat, DomeStyle.Ellipsoidal })
            foreach (var lod in new[] { 0, 1, 2 })
            {
                var s = Spec(); s.AftDome = aft;
                var p = TankProfile.Build(s, TankLayout.Build(s), TankLod.For(lod));
                Assert.That(p.First().R, Is.EqualTo(0f)); Assert.That(p.Last().R, Is.EqualTo(0f));
                Assert.That(p.First().Y, Is.EqualTo(s.ShellBottom).Within(1e-4)); Assert.That(p.Last().Y, Is.EqualTo(s.ShellTop).Within(1e-4));
                for (int i = 1; i < p.Count; i++)
                {
                    Assert.That(p[i].Y, Is.GreaterThanOrEqualTo(p[i - 1].Y - 1e-6), $"{aft} lod {lod} point {i}");
                    Assert.That(p[i].V, Is.GreaterThanOrEqualTo(p[i - 1].V));
                    Assert.That(Math.Sqrt(p[i].NR * p[i].NR + p[i].NY * p[i].NY), Is.EqualTo(1).Within(1e-4));
                }
                Assert.That(p.First().NY, Is.EqualTo(-1).Within(1e-4)); Assert.That(p.Last().NY, Is.EqualTo(1).Within(1e-4));
            }
        }

        [Test]
        public void DomesNeverBulgePastBarrelOrBelowFlatEnd()
        {
            // #106: the aft dome hung below the barrel into the engine bay and the equator stood proud of the body.
            foreach (var aft in new[] { DomeStyle.Flat, DomeStyle.Ellipsoidal })
            foreach (var lod in new[] { 0, 1, 2 })
            {
                var s = Spec(); s.AftDome = aft;
                var p = TankProfile.Build(s, TankLayout.Build(s), TankLod.For(lod));
                foreach (var q in p)
                {
                    Assert.That(q.R, Is.LessThanOrEqualTo(s.Radius + 1e-6f), "nothing outside the barrel radius");
                    bool onDome = q.Y < s.BarrelBottom - 1e-5f || q.Y > s.BarrelTop + 1e-5f;
                    if (onDome) Assert.That(q.R, Is.LessThanOrEqualTo(s.DomeRadius + 1e-6f), "dome inset, no overshoot");
                }
                if (aft == DomeStyle.Flat) Assert.That(p.Min(q => q.Y), Is.EqualTo(s.BarrelBottom).Within(1e-6), "flat aft end is flush");
            }
            var d = Spec();
            Assert.That(d.AftDome, Is.EqualTo(DomeStyle.Flat), "vehicle default: flush aft end");
            Assert.That(d.DomeRadius, Is.LessThan(d.Radius));
        }

        [Test]
        public void LatheWindingFacesOutwardAndUvAreMetres()
        {
            foreach (var aft in new[] { DomeStyle.Flat, DomeStyle.Ellipsoidal })
            {
                var s = Spec(); s.AftDome = aft; var L = TankLayout.Build(s);
                var prof = TankProfile.Build(s, L, TankLod.For(0));
                var md = LatheMesh.Build(prof, 48);
                Assert.That(md.TriangleCount, Is.EqualTo(LatheMesh.CountTriangles(prof, 48)));
                double cy = (s.ShellBottom + s.ShellTop) / 2;
                int outward = 0, checkedTris = 0;
                for (int t = 0; t < md.Triangles.Count; t += 3)
                {
                    var a = P(md, md.Triangles[t]); var b = P(md, md.Triangles[t + 1]); var c = P(md, md.Triangles[t + 2]);
                    var n = Cross(Sub(b, a), Sub(c, a)); if (Len(n) < 1e-9) continue;
                    var cen = ((a.x + b.x + c.x) / 3, (a.y + b.y + c.y) / 3 - cy, (a.z + b.z + c.z) / 3);
                    checkedTris++; if (n.x * cen.Item1 + n.y * cen.Item2 + n.z * cen.Item3 > 0) outward++;
                }
                Assert.That(checkedTris, Is.EqualTo(md.TriangleCount), "no degenerate triangles");
                Assert.That(outward, Is.EqualTo(checkedTris), "every triangle must face out of the tank");
                double maxU = 0; for (int i = 0; i < md.Uv.Count; i += 2) maxU = Math.Max(maxU, md.Uv[i]);
                Assert.That(maxU, Is.EqualTo(2 * Math.PI * s.Radius).Within(1e-3));
            }
        }

        [Test]
        public void CutawayRemovesExactlyTheSector()
        {
            var s = Spec(); var prof = TankProfile.Build(s, TankLayout.Build(s), TankLod.For(0));
            int full = LatheMesh.Build(prof, 48).TriangleCount, cut = LatheMesh.Build(prof, 48, 0, (float)(Math.PI / 2)).TriangleCount;
            Assert.That(cut, Is.EqualTo(full * 3 / 4));
        }

        [Test]
        public void LodsReduceTrianglesAndLod0FitsCap()
        {
            // #106 measured ~28k per tank at LOD0. Cap is 14k for every tank size, including long cores.
            foreach (var (r, len) in new[] { (1.85f, 9.6f), (1.85f, 30f), (2.6f, 42f), (1.2f, 3f), (0.9f, 60f) })
            {
                var L = TankLayout.Build(Spec(r, len));
                int t0 = TankBudget.Triangles(L, TankLod.For(0), out _, out int hw0);
                int t1 = TankBudget.Triangles(L, TankLod.For(1), out _, out _);
                int t2 = TankBudget.Triangles(L, TankLod.For(2), out _, out int hw2);
                Assert.That(t0, Is.LessThanOrEqualTo(TankLayout.Lod0TriangleCap), $"R{r} L{len}");
                Assert.That(t0, Is.GreaterThan(t1)); Assert.That(t1, Is.GreaterThan(t2)); Assert.That(hw2, Is.EqualTo(0));
                Assert.That(hw0, Is.GreaterThan(0), "close-up welds/rings kept");
                Assert.That(L.CircWelds.Count, Is.GreaterThanOrEqualTo(2)); Assert.That(L.LongWelds, Is.Not.Empty);
                Assert.That(L.Stiffeners.Count, Is.GreaterThanOrEqualTo(L.BarrelSections), $"R{r} L{len}: at least one ring per panel");
            }
            // A normal tank is not thinned: requested pitch kept.
            var n = TankLayout.Build(Spec());
            Assert.That(n.Fitted.StiffenerPitch, Is.EqualTo(Spec().StiffenerPitch));
            Assert.That(n.Stiffeners, Is.Not.Empty);
        }

        [Test]
        public void EstimateMatchesLatheExactly()
        {
            var s = Spec(); var L = TankLayout.Build(s);
            for (int lod = 0; lod < 3; lod++)
            {
                var tl = TankLod.For(lod);
                TankBudget.Triangles(L, tl, out int shell, out _);
                Assert.That(shell, Is.EqualTo(LatheMesh.Build(TankProfile.Build(s, L, tl), tl.RadialSegments).TriangleCount));
            }
            Assert.That(TankBudget.TubeTriangles(49, 4), Is.EqualTo(48 * 4 * 2));
        }

        [Test]
        public void IsogridRibsAndTileSeamless()
        {
            Assert.That(Isogrid.Height(0, 0, 1, 0.12, 0.06), Is.EqualTo(1));           // node
            double s = Isogrid.RowSpacing(1);
            Assert.That(Isogrid.Height(0.5, s / 3, 1, 0.12, 0.06), Is.EqualTo(0));     // triangle centroid = pocket
            const int W = 64, H = 111; var px = Isogrid.NormalTileRgba(W, H);
            Assert.That(px.Length, Is.EqualTo(W * H * 4));
            for (int y = 0; y < H; y++)
            {
                // Seamless wrap: left and right columns differ no more than adjacent interior columns do.
                int l = (y * W) * 4, r = (y * W + W - 1) * 4;
                Assert.That(Math.Abs(px[l] - px[r]), Is.LessThanOrEqualTo(80));
            }
            for (int i = 0; i < px.Length; i += 4) { Assert.That(px[i + 3], Is.EqualTo(255)); Assert.That(px[i + 2], Is.GreaterThan(127)); }
            Assert.That(Isogrid.Height(0.37, 0.21, 1, 0.12, 0.06), Is.EqualTo(Isogrid.Height(1.37, 0.21 + 2 * s, 1, 0.12, 0.06)).Within(1e-9));
        }

        [Test]
        public void CutawayEdgesHaveCellLengthAndCap()
        {
            var e = new List<(double u0, double v0, double u1, double v1)>();
            Assert.That(Isogrid.CutawayEdges(2.9, 9.6, 0.3, 3000, e), Is.True);
            Assert.That(e, Is.Not.Empty);
            foreach (var x in e) Assert.That(Math.Sqrt(Math.Pow(x.u1 - x.u0, 2) + Math.Pow(x.v1 - x.v0, 2)), Is.EqualTo(0.3).Within(1e-6));
            Assert.That(e.Select(x => (Math.Round(x.u0, 6), Math.Round(x.v0, 6), Math.Round(x.u1, 6), Math.Round(x.v1, 6))).Distinct().Count(), Is.EqualTo(e.Count));
            Assert.That(Isogrid.CutawayEdges(2.9, 9.6, 0.05, 3000, e), Is.False);
            Assert.That(e, Is.Empty);
        }

        static (double x, double y, double z) P(MeshData m, int i) => (m.Positions[i * 3], m.Positions[i * 3 + 1], m.Positions[i * 3 + 2]);
        static (double x, double y, double z) Sub((double x, double y, double z) a, (double x, double y, double z) b) => (a.x - b.x, a.y - b.y, a.z - b.z);
        static (double x, double y, double z) Cross((double x, double y, double z) a, (double x, double y, double z) b) => (a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        static double Len((double x, double y, double z) a) => Math.Sqrt(a.x * a.x + a.y * a.y + a.z * a.z);
    }
}
