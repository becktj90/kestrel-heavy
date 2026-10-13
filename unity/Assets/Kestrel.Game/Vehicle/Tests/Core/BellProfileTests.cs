// Engine-free: Unity EditMode (Kestrel.Vehicle.Tests) and dotnet (Tests/Dotnet~).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Vehicle.Tests
{
    public class BellProfileTests
    {
        static BellSpec Spec(string id, EngineCycleKind cycle, float eps, float diameter, ulong seed = 42) => new BellSpec
        {
            EngineId = id, Cycle = cycle, ExpansionRatio = eps, ExitDiameter = diameter, VehicleSeed = seed
        };

        [Test]
        public void EveryCatalogEngineHasAMonotonicBellWithTheRightThroat()
        {
            foreach (var e in CatalogEngines())
            {
                var c = BellProfile.Build(e, 0);
                Assert.That(c.Wall.Count, Is.GreaterThanOrEqualTo(6), e.EngineId);
                Assert.That(c.Wall[0].R, Is.EqualTo(e.ExitRadius).Within(1e-4), e.EngineId + " exit");
                float throatR = e.ExitRadius / (float)Math.Sqrt(e.ExpansionRatio);
                Assert.That(c.ThroatRadius, Is.EqualTo(throatR).Within(1e-4), e.EngineId + " throat");
                // The injector face closes at r = 0. The throat is the minimum of the wall above that.
                float minWall = c.Wall.Where(p => p.R > 1e-3f).Min(p => p.R);
                Assert.That(minWall, Is.EqualTo(throatR).Within(1e-3), e.EngineId + " throat on the wall");
                for (int i = 1; i < c.Wall.Count; i++)
                    Assert.That(c.Wall[i].Y, Is.GreaterThanOrEqualTo(c.Wall[i - 1].Y - 1e-5f), e.EngineId + " Y");
                Assert.That(c.Length, Is.GreaterThan(c.ThroatY), e.EngineId);
                Assert.That(c.ThroatY, Is.LessThan(c.Length - 1e-3f), e.EngineId + " chamber above the throat");
                Assert.That(c.Wall[c.Wall.Count - 1].R, Is.EqualTo(0f).Within(1e-4f), "injector closes");
            }
        }

        [Test]
        public void LengthGrowsWithExpansionAtFixedDiameter()
        {
            var small = BellProfile.Build(Spec("a", EngineCycleKind.GasGenerator, 16f, 1.2f), 0);
            var big = BellProfile.Build(Spec("b", EngineCycleKind.GasGenerator, 80f, 1.2f), 0);
            // Nozzle length (exit to throat) grows with ε. The chamber shrinks as the throat shrinks, so the
            // full stack length is not the check the spec describes.
            Assert.That(big.ThroatY, Is.GreaterThan(small.ThroatY * 1.1f));
        }

        [Test]
        public void DifferentCycleOrExpansionChangesTheNormalisedShape()
        {
            var engines = CatalogEngines();
            int compared = 0;
            for (int i = 0; i < engines.Count; i++)
            for (int j = i + 1; j < engines.Count; j++)
            {
                var a = engines[i]; var b = engines[j];
                if (a.Cycle == b.Cycle && Math.Abs(a.ExpansionRatio - b.ExpansionRatio) < 0.05f) continue;
                compared++;
                float dev = MaxNormalisedDeviation(BellProfile.Build(a, 0), BellProfile.Build(b, 0));
                Assert.That(dev, Is.GreaterThanOrEqualTo(0.03f), $"{a.EngineId} vs {b.EngineId} dev {dev:F3}");
            }
            Assert.That(compared, Is.GreaterThan(20));
        }

        [Test]
        public void SameSeedSharesOneMeshKeyAcrossSlots()
        {
            var spec = Spec("gg-kerolox", EngineCycleKind.GasGenerator, 16f, 0.92f, seed: 7);
            var again = BellProfile.Build(spec, 0);
            var other = BellProfile.Build(Spec("gg-kerolox", EngineCycleKind.GasGenerator, 16f, 0.92f, seed: 8), 0);
            Assert.That(BellProfile.Build(spec, 0).MeshKey, Is.EqualTo(again.MeshKey));
            Assert.That(again.MeshKey, Is.Not.EqualTo(other.MeshKey));
            var p0 = BellVariation.Place(spec, 0, 0);
            var p1 = BellVariation.Place(spec, 0, 1);
            Assert.That(p0.RollDeg, Is.Not.EqualTo(p1.RollDeg).Within(0.01f));
            Assert.That(Math.Abs(p0.GimbalXDeg), Is.LessThanOrEqualTo(0.4f + 1e-4f));
            Assert.That(BellProfile.MeshKey(spec, 0, again.TubeCount, again.BandDepth), Is.EqualTo(again.MeshKey));
        }

        [Test]
        public void TubeCountDividesRadialSegments()
        {
            var c = BellProfile.Build(Spec("gg-kerolox", EngineCycleKind.GasGenerator, 16f, 0.92f), 0);
            Assert.That(BellLod.Radial(0) % c.TubeCount, Is.EqualTo(0));
            Assert.That(c.TubeCount, Is.GreaterThan(1));
        }

        [Test]
        public void MeshHasNoNaNsAndMatchesItsTriangleCount()
        {
            var c = BellProfile.Build(Spec("gg-kerolox", EngineCycleKind.GasGenerator, 16f, 0.92f), 0);
            var mesh = BellMesh.Build(c, 0);
            Assert.That(mesh.VertexCount, Is.GreaterThan(0));
            Assert.That(mesh.TriangleCount, Is.EqualTo(BellMesh.TriangleCount(c, 0)));
            Assert.That(mesh.Normals.Count, Is.EqualTo(mesh.Positions.Count));
            foreach (var p in mesh.Positions) Assert.That(float.IsNaN(p), Is.False);
            foreach (var n in mesh.Normals) Assert.That(float.IsNaN(n), Is.False);
        }

        [Test]
        public void HeaviestClusterStaysInsideTheLowTriangleCap()
        {
            var sea = BellProfile.Build(Spec("gg-kerolox", EngineCycleKind.GasGenerator, 16f, 0.92f), 0);
            var vac = BellProfile.Build(Spec("gg-kerolox-vac", EngineCycleKind.GasGenerator, 165f, 2.4f), 0);
            int tris = BellMesh.TriangleCount(sea, 0) * 9 + BellMesh.TriangleCount(vac, 0) * 2;
            int draws = BellMesh.DrawCalls(2);
            Assert.That(tris, Is.LessThanOrEqualTo(BellLod.TriangleCap(0)), "tris " + tris);
            Assert.That(draws, Is.LessThanOrEqualTo(BellLod.DrawCallCap(0)));
            // LOD1 drops the inner wall, so it is cheaper than the hero bell.
            Assert.That(BellMesh.TriangleCount(sea, 1), Is.LessThan(BellMesh.TriangleCount(sea, 0)));
        }

        static float MaxNormalisedDeviation(BellContour a, BellContour b)
        {
            float max = 0f;
            const int n = 32;
            for (int i = 0; i <= n; i++)
            {
                float u = i / (float)n;
                float ra = RadiusAt(a, u * a.Length) / a.ExitRadius;
                float rb = RadiusAt(b, u * b.Length) / b.ExitRadius;
                max = Math.Max(max, Math.Abs(ra - rb));
            }
            return max;
        }

        static float RadiusAt(BellContour c, float y)
        {
            var w = c.Wall;
            if (y <= w[0].Y) return w[0].R;
            for (int i = 1; i < w.Count; i++)
            {
                if (w[i].Y < y) continue;
                float dy = w[i].Y - w[i - 1].Y;
                float t = dy < 1e-6f ? 0f : (y - w[i - 1].Y) / dy;
                return w[i - 1].R + (w[i].R - w[i - 1].R) * t;
            }
            return w[w.Count - 1].R;
        }

        public static List<BellSpec> CatalogEngines()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            string path = null;
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, "data", "engines.json");
                if (File.Exists(candidate)) { path = candidate; break; }
                dir = dir.Parent;
            }
            Assert.That(path, Is.Not.Null, "data/engines.json");
            var list = new List<BellSpec>();
            var root = MiniJson.ParseObject(File.ReadAllText(path));
            foreach (JsonObject eng in root.Arr("engines"))
            {
                string id = eng.Str("id");
                string cycle = eng.Str("cycle");
                Assert.That(BellSpec.TryParseCycle(cycle, out var kind), Is.True, cycle);
                float eps = (float)eng.Num("expansionRatio");
                float diameter = (float)eng.Num("diameter");
                var spec = Spec(id, kind, eps, diameter, seed: 42);
                Assert.That(spec.Validate(), Is.Empty, id);
                list.Add(spec);
            }
            Assert.That(list.Count, Is.GreaterThanOrEqualTo(15));
            return list;
        }
    }
}
