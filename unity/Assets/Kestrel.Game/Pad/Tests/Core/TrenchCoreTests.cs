// Engine-free tests for the D3 trench, deflector and deck-detail builders.
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Kestrel.Pad.Tests
{
    public class TrenchCoreTests
    {
        static readonly PadFit Heavy = new PadFit(70f, 2.5f, 6f);
        static readonly string[] Banned = { "nasa", "lc-", "slc", "cape", "kourou", "baikonur", "starbase", "vosto", "pad 39", "39a" };

        [TestCase(PadArchetype.Rail)]
        [TestCase(PadArchetype.Tower)]
        [TestCase(PadArchetype.TowerStrongback)]
        public void GrammarRangesAndDeterminism(PadArchetype type)
        {
            TrenchSpec first = null;
            for (int seed = 1; seed <= 24; seed++)
            {
                var a = TrenchGrammar.Sample(seed, type, Heavy, 18f);
                var b = TrenchGrammar.Sample(seed, type, Heavy, 18f);
                if (first == null) first = a;
                Assert.That(a.WidthFactor, Is.EqualTo(b.WidthFactor).Within(1e-5f));
                Assert.That(a.BearingDeg, Is.EqualTo(b.BearingDeg).Within(1e-5f));
                Assert.That(a.Width / a.ClusterDiameter, Is.InRange(1.4f, 4.05f), $"seed {seed} width factor");
                Assert.That(a.FaceAngleDeg, Is.InRange(29.5f, 60f), $"seed {seed} face");
                Assert.That(a.FlareDeg, Is.InRange(9.5f, 20.1f));
                Assert.That(a.ExitLength / 70f, Is.InRange(0.95f, 2.55f));
                Assert.That(a.PondCount, Is.InRange(1, 2));
                Assert.That(a.PondWidth / 70f, Is.InRange(0.29f, 0.61f));
                Assert.That(a.DrainCount, Is.InRange(2, 4));
                Assert.That(Math.Abs(a.SlopeY(0f) - a.ApexY), Is.LessThan(1e-4f));
                Assert.That(a.SlopeY(a.WedgeRun), Is.EqualTo(a.FloorY).Within(1e-3f));
                float rise = a.ApexY - a.FloorY;
                float ang = (float)(Math.Atan(rise / a.WedgeRun) * 180.0 / Math.PI);
                Assert.That(ang, Is.EqualTo(a.FaceAngleDeg).Within(0.15f), $"seed {seed}");
                if (type == PadArchetype.Rail)
                {
                    Assert.That(a.TwoSided, Is.False);
                    Assert.That(a.Deflector, Is.EqualTo(DeflectorKind.Ramp));
                    Assert.That(a.Depth / 70f, Is.LessThan(0.12f), "rail trench is the shallow exception");
                    Assert.That(a.SpareDeflector, Is.False);
                }
                else
                {
                    Assert.That(a.TwoSided, Is.True);
                    Assert.That(a.Deflector, Is.EqualTo(DeflectorKind.Wedge));
                    Assert.That(a.Depth / 70f, Is.InRange(0.12f, 0.25f));
                    Assert.That(a.BearingDeg, Is.InRange(84f, 96f), "exit runs out the side, not at the tower");
                    Assert.That(TrenchGrammar.ClearsStructures(a, Heavy, type, 18f), Is.True, $"seed {seed}");
                    Assert.That(TrenchGrammar.InPlumeSector(a, 0f, Heavy.VehicleRadius + Heavy.Clearance, Heavy.VehicleHeight), Is.False);
                }
                Assert.That(TrenchGrammar.Mouths(a).Count, Is.EqualTo(a.TwoSided ? 2 : 1));
            }
            var other = TrenchGrammar.Sample(99, type, Heavy, 18f);
            Assert.That(other.BearingDeg, Is.Not.EqualTo(first.BearingDeg).Within(1e-3f)
                .Or.Not.EqualTo(first.Width).Within(1e-3f));
        }

        [Test]
        public void HeavyAsksForAWiderTrenchThanTower()
        {
            int wider = 0;
            for (int seed = 1; seed <= 16; seed++)
            {
                var tower = TrenchGrammar.Sample(seed, PadArchetype.Tower, Heavy, 18f);
                var heavy = TrenchGrammar.Sample(seed, PadArchetype.TowerStrongback, Heavy, 18f);
                // Same RNG stream up to the factor remap: heavy's requested factor is the higher band.
                if (!tower.ClearanceClamped && !heavy.ClearanceClamped)
                    Assert.That(heavy.Width, Is.GreaterThan(tower.Width), $"seed {seed}");
                if (heavy.Width + 1e-3f >= tower.Width) wider++;
            }
            Assert.That(wider, Is.GreaterThanOrEqualTo(10));
        }

        [Test]
        public void PlumeSectorFollowsTheExitAndNotTheTower()
        {
            var s = TrenchGrammar.Sample(4, PadArchetype.Tower, Heavy, 18f);
            s.ToWorld(s.ExitLength * 0.5f, 0f, out float x, out float z);
            Assert.That(TrenchGrammar.InPlumeSector(s, x, z, Heavy.VehicleHeight), Is.True);
            Assert.That(TrenchGrammar.InPlumeSector(s, 0f, 40f, Heavy.VehicleHeight), Is.False);
        }

        [Test]
        public void MouthsSitOnTheExitBearing()
        {
            foreach (var type in new[] { PadArchetype.Rail, PadArchetype.Tower, PadArchetype.TowerStrongback })
            {
                var s = TrenchGrammar.Sample(6, type, Heavy, 16f);
                foreach (var m in TrenchGrammar.Mouths(s, 16f))
                {
                    float dist = (float)Math.Sqrt(m.X * m.X + m.Z * m.Z);
                    Assert.That(dist, Is.EqualTo(s.ExitLength).Within(0.05f));
                    float along = m.X * m.DirX + m.Z * m.DirZ;
                    Assert.That(along, Is.GreaterThan(0.9f), "outward dir points away from the pad");
                    Assert.That(m.Width, Is.GreaterThan(s.Width));
                }
            }
        }

        [Test]
        public void MeshesCoverTrenchDeflectorAndDeckDetail()
        {
            foreach (var type in new[] { PadArchetype.Rail, PadArchetype.Tower, PadArchetype.TowerStrongback })
                for (int seed = 1; seed <= 6; seed++)
                {
                    var layout = PadGenerator.Generate(seed, type, type == PadArchetype.Rail ? new PadFit(16f, 0.7f, 6f) : Heavy);
                    var s = layout.Trench;
                    Assert.That(s, Is.Not.Null);
                    float deck = TrenchGrammar.SurfaceRadius(layout);
                    var built = TrenchMeshBuilder.Build(s, deck);
                    Assert.That(built.DrawCalls, Is.EqualTo(4), $"{type} {seed}");
                    Assert.That(built.Triangles, Is.LessThan(20000), $"{type} {seed} tris {built.Triangles}");
                    Assert.That(built.Concrete.TriangleCount, Is.GreaterThan(20));
                    Assert.That(built.Refractory.TriangleCount, Is.GreaterThan(0));
                    Assert.That(built.Galvanized.TriangleCount, Is.GreaterThan(0));
                    Assert.That(built.Markings.TriangleCount, Is.GreaterThan(4));
                    AssertNormals(built.Concrete);
                    AssertNormals(built.Refractory);
                    AssertHasApex(s, built.Refractory);
                    AssertFloorIsBelowDeck(s, built.Concrete);
                    AssertPlatesMissTheSlot(s, deck, built);
                    AssertNames(layout);
                }
        }

        [Test]
        public void GroundCutMatchesHoleAreaAndFacesUp()
        {
            var s = TrenchGrammar.Sample(3, PadArchetype.Tower, Heavy, 18f);
            var holes = new List<IList<Vec2>> { TrenchGrammar.GroundOpening(s) };
            foreach (var p in TrenchGrammar.PondPolys(s)) holes.Add(p);
            // Plane-local: a 200 m trench on a 20 km-per-unit ground is a small hole. Use the same
            // metre space here; Frame is scale-free.
            var mesh = PolyCut.Frame(500f, holes, TrenchSpec.GradeY);
            Assert.That(mesh.TriangleCount, Is.GreaterThan(4));
            float ny = 0f; int n = 0;
            for (int i = 0; i < mesh.VertexCount; i++) { ny += mesh.Normals[i * 3 + 1]; n++; }
            Assert.That(ny / n, Is.GreaterThan(0.99f));
            float area = MeshArea(mesh);
            float outer = 1000f * 1000f;
            float hole = Math.Abs(PolyCut.SignedArea(holes[0]));
            foreach (var p in TrenchGrammar.PondPolys(s)) hole += Math.Abs(PolyCut.SignedArea(p));
            Assert.That(area, Is.EqualTo(outer - hole).Within(outer * 0.002f), $"mesh {area} outer-hole {outer - hole}");
            // A point in the channel waist is not covered: every ground triangle's centroid stays out of the hole.
            AssertNoCentroidIn(mesh, holes[0]);
        }

        [Test]
        public void DeckLidHasTheSlotAndFacesUp()
        {
            var s = TrenchGrammar.Sample(8, PadArchetype.TowerStrongback, Heavy, 20f);
            var built = TrenchMeshBuilder.Build(s, 20f);
            // Origin is the cluster: it must fall in the opening, so no slab triangle covers it.
            Assert.That(Covers(built.Concrete, 0f, 0.05f, 0f), Is.False, "slab must stay open under the plume");
        }

        [Test]
        public void DetailStaysInsideTheLowDrawBudget()
        {
            foreach (var type in new[] { PadArchetype.Rail, PadArchetype.Tower, PadArchetype.TowerStrongback })
            {
                var L = PadGenerator.Generate(9, type, new PadFit(120f, 5f));
                var led = new Kestrel.Kit.BudgetLedger();
                PadPerf.Estimate(L, led);
                Assert.That(led.DrawCalls, Is.LessThanOrEqualTo(16), led.Report(Kestrel.Kit.KitBudgets.Low));
                Assert.That(led.Triangles, Is.LessThan(40000), led.Report(Kestrel.Kit.KitBudgets.Low));
                Assert.That(led.Within(Kestrel.Kit.KitBudgets.Low), Is.True, led.Report(Kestrel.Kit.KitBudgets.Low));
            }
        }

        static void AssertNormals(PadMeshData m)
        {
            for (int i = 0; i < m.VertexCount; i++)
            {
                float x = m.Normals[i * 3], y = m.Normals[i * 3 + 1], z = m.Normals[i * 3 + 2];
                float len = (float)Math.Sqrt(x * x + y * y + z * z);
                Assert.That(len, Is.EqualTo(1f).Within(1e-3f));
            }
        }

        static void AssertHasApex(TrenchSpec s, PadMeshData refractory)
        {
            float best = float.MaxValue;
            for (int i = 0; i < refractory.VertexCount; i++)
            {
                float x = refractory.Positions[i * 3], y = refractory.Positions[i * 3 + 1], z = refractory.Positions[i * 3 + 2];
                s.ToFrame(x, z, out float u, out float v);
                if (Math.Abs(y - s.ApexY) > 0.08f) continue;
                best = Math.Min(best, Math.Abs(u) + Math.Abs(v) * 0.01f);
            }
            Assert.That(best, Is.LessThan(0.15f), "deflector ridge passes under the cluster");
        }

        static void AssertFloorIsBelowDeck(TrenchSpec s, PadMeshData concrete)
        {
            float minY = float.MaxValue;
            for (int i = 0; i < concrete.VertexCount; i++)
                minY = Math.Min(minY, concrete.Positions[i * 3 + 1]);
            Assert.That(minY, Is.LessThan(-0.2f));
            Assert.That(minY, Is.LessThanOrEqualTo(s.FloorY + 0.05f));
        }

        static void AssertPlatesMissTheSlot(TrenchSpec s, float deck, PadDetailBuild built)
        {
            var hole = TrenchGrammar.DeckOpening(s, deck);
            // Plate centres are not stored. A concrete vertex exactly at y=0 is the slab; plate tops sit near 0.09.
            // Sample a few points along the slot centreline and require they are not inside a plate box by
            // checking no vertex cluster sits on the centreline at plate height.
            int onCentre = 0;
            for (int i = 0; i < built.Concrete.VertexCount; i++)
            {
                float x = built.Concrete.Positions[i * 3], y = built.Concrete.Positions[i * 3 + 1], z = built.Concrete.Positions[i * 3 + 2];
                if (y < 0.04f || y > 0.1f) continue;
                if (PolyCut.Contains(hole, x, z)) onCentre++;
            }
            Assert.That(onCentre, Is.EqualTo(0), "plates stay out of the trench slot");
        }

        static void AssertNames(PadLayout L)
        {
            foreach (var e in L.Graph.All)
            {
                string n = (e.DisplayName ?? "").ToLowerInvariant();
                foreach (var b in Banned) Assert.That(n, Does.Not.Contain(b), e.Id);
            }
            Assert.That(L.Graph.Contains("pad.trench"), Is.True);
            Assert.That(L.Graph.Contains("pad.trench.deflector"), Is.True);
            Assert.That(L.Graph.Contains("pad.deck.plates"), Is.True);
            Assert.That(L.Graph.Contains("pad.deck.grating"), Is.True);
            Assert.That(L.Graph.Contains("pad.deck.drains"), Is.True);
            Assert.That(L.Graph.Contains("pad.deck.markings"), Is.True);
            Assert.That(L.Graph.Get("pad.trench").ParentId, Is.EqualTo("pad"));
            Assert.That(L.Graph.Parent("pad.trench").Id, Is.Not.EqualTo("tower"));
        }

        static float MeshArea(PadMeshData m)
        {
            double a = 0;
            for (int i = 0; i + 2 < m.Triangles.Count; i += 3)
            {
                int ia = m.Triangles[i], ib = m.Triangles[i + 1], ic = m.Triangles[i + 2];
                float ax = m.Positions[ia * 3], az = m.Positions[ia * 3 + 2];
                float bx = m.Positions[ib * 3], bz = m.Positions[ib * 3 + 2];
                float cx = m.Positions[ic * 3], cz = m.Positions[ic * 3 + 2];
                a += Math.Abs((bx - ax) * (cz - az) - (bz - az) * (cx - ax)) * 0.5;
            }
            return (float)a;
        }

        static void AssertNoCentroidIn(PadMeshData m, IList<Vec2> hole)
        {
            for (int i = 0; i + 2 < m.Triangles.Count; i += 3)
            {
                int ia = m.Triangles[i], ib = m.Triangles[i + 1], ic = m.Triangles[i + 2];
                float x = (m.Positions[ia * 3] + m.Positions[ib * 3] + m.Positions[ic * 3]) / 3f;
                float z = (m.Positions[ia * 3 + 2] + m.Positions[ib * 3 + 2] + m.Positions[ic * 3 + 2]) / 3f;
                Assert.That(PolyCut.Contains(hole, x, z), Is.False, $"triangle centroid {x},{z} landed in the trench");
            }
        }

        static bool Covers(PadMeshData m, float x, float y, float z)
        {
            for (int i = 0; i + 2 < m.Triangles.Count; i += 3)
            {
                int ia = m.Triangles[i], ib = m.Triangles[i + 1], ic = m.Triangles[i + 2];
                float ay = m.Positions[ia * 3 + 1], by = m.Positions[ib * 3 + 1], cy = m.Positions[ic * 3 + 1];
                if (Math.Abs(ay - y) > 0.2f || Math.Abs(by - y) > 0.2f || Math.Abs(cy - y) > 0.2f) continue;
                var a = new Vec2(m.Positions[ia * 3], m.Positions[ia * 3 + 2]);
                var b = new Vec2(m.Positions[ib * 3], m.Positions[ib * 3 + 2]);
                var c = new Vec2(m.Positions[ic * 3], m.Positions[ic * 3 + 2]);
                if (PolyCut.Cross(a, b, new Vec2(x, z)) >= -1e-3f && PolyCut.Cross(b, c, new Vec2(x, z)) >= -1e-3f && PolyCut.Cross(c, a, new Vec2(x, z)) >= -1e-3f)
                    return true;
                if (PolyCut.Cross(a, b, new Vec2(x, z)) <= 1e-3f && PolyCut.Cross(b, c, new Vec2(x, z)) <= 1e-3f && PolyCut.Cross(c, a, new Vec2(x, z)) <= 1e-3f)
                    return true;
            }
            return false;
        }
    }
}
