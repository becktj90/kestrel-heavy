// EditMode tests for the Pier 7 terrain generator. Also run under dotnet (Tests/Dotnet~).
using System;
using NUnit.Framework;

namespace Kestrel.Site.Tests
{
    public class SiteGeneratorTests
    {
        CoastalSite Site() => SiteGenerator.Generate(SiteGenerator.Pier7Seed);

        [Test]
        public void SameSeedSameSite()
        {
            var a = SiteGenerator.Generate(7); var b = SiteGenerator.Generate(7);
            Assert.That(a.Hash(), Is.EqualTo(b.Hash()));
            Assert.That(a.Land(0).TriangleCount, Is.EqualTo(b.Land(0).TriangleCount));
            Assert.That(a.Water(0).Positions.Length, Is.EqualTo(b.Water(0).Positions.Length));
        }

        [Test]
        public void SeedChangesScrubAndBuildings()
        {
            var hashes = new System.Collections.Generic.HashSet<ulong>();
            for (int s = 1; s <= 6; s++) hashes.Add(SiteGenerator.Generate(s).Hash());
            Assert.That(hashes.Count, Is.GreaterThan(4));
        }

        [Test]
        public void PadSitsOnTheBreakwater()
        {
            Assert.That(Coastline.IsLand(0f, 0f), Is.True);
            Assert.That(Coastline.ShoreX(0f), Is.GreaterThan(Coastline.ApronRadius + 40f), "open water east of the apron");
            for (int i = 0; i < 24; i++)
            {
                double a = i / 24.0 * Math.PI * 2;
                float x = (float)(Math.Cos(a) * Coastline.ApronRadius * 0.98);
                float z = (float)(Math.Sin(a) * Coastline.ApronRadius * 0.98);
                Assert.That(Coastline.IsLand(x, z), Is.True, $"apron {x},{z}");
                Assert.That(Math.Abs(Coastline.Relief(x, z, 7)), Is.LessThan(0.08f));
            }
        }

        [Test]
        public void BeachSitsBetweenScrubAndWater()
        {
            float z = 0f, shore = Coastline.ShoreX(z);
            Assert.That(Coastline.IsBeach(shore - 10f, z), Is.True);
            Assert.That(Coastline.IsWater(shore + 8f, z), Is.True);
            Assert.That(Coastline.IsLand(shore - Coastline.BeachWidth - 30f, z), Is.True);
            Assert.That(Coastline.IsBeach(shore - Coastline.BeachWidth - 30f, z), Is.False);
            var land = Site().Land(0);
            int beach = 0;
            for (int i = 0; i < land.Colors.Length; i += 4)
                if (Close(land.Colors, i, Coastline.Beach)) beach++;
            Assert.That(beach, Is.GreaterThan(12), "beach vertices on the terrain");
        }

        [Test]
        public void ScrubPatchesAreLowAndCommon()
        {
            int land = 0, patch = 0;
            var rng = new SiteRng(3);
            for (int i = 0; i < 400; i++)
            {
                float x = rng.Range(-2500f, -200f), z = rng.Range(-2000f, 2000f);
                if (!Coastline.IsLand(x, z) || Coastline.IsBeach(x, z) || Coastline.IsApron(x, z)) continue;
                land++;
                if (Coastline.IsScrubPatch(x, z, 7)) patch++;
                float relief = Coastline.Relief(x, z, 7);
                Assert.That(relief, Is.LessThan(1.3f).And.GreaterThan(-0.2f));
            }
            Assert.That(land, Is.GreaterThan(200));
            Assert.That(patch / (float)land, Is.InRange(0.12f, 0.55f));
            Assert.That(CountColor(Site().Land(0), Coastline.ScrubPatch), Is.GreaterThan(40), "patches are on the mesh");
        }

        [Test]
        public void RoadStaysOnLandAndReachesTheApron()
        {
            var s = Site();
            Assert.That(s.RoadX.Length, Is.GreaterThan(20));
            float far = 0f;
            for (int i = 0; i < s.RoadX.Length; i++)
            {
                float x = s.RoadX[i], z = s.RoadZ[i];
                far = Math.Max(far, (float)Math.Sqrt(x * x + z * z));
                Assert.That(Coastline.IsLand(x, z), Is.True, $"center {x},{z}");
                Assert.That(s.Haven.Contains(x, z), Is.False);
            }
            Assert.That(far, Is.GreaterThan(2000f), "road comes from inland");
            float ex = s.RoadX[s.RoadX.Length - 1], ez = s.RoadZ[s.RoadZ.Length - 1];
            Assert.That(Math.Abs(ex + Coastline.ApronRadius), Is.LessThan(1f));
            Assert.That(ez, Is.EqualTo(0f).Within(0.01f));
            var road = CountColor(s.Land(0), Coastline.Road);
            Assert.That(road, Is.GreaterThan(20), "road ribbon is in the land mesh");
        }

        [Test]
        public void BuildingsAreDistantSilhouettesOnLand()
        {
            var s = Site();
            Assert.That(s.Buildings.Count, Is.InRange(8, 12));
            foreach (var b in s.Buildings)
            {
                Assert.That(Coastline.IsLand(b.X, b.Z), Is.True);
                Assert.That(Coastline.IsBeach(b.X, b.Z), Is.False);
                Assert.That(b.Distance, Is.GreaterThan(1000f));
                Assert.That(b.H, Is.InRange(6f, 24f));
                Assert.That(b.W, Is.InRange(8f, 40f));
                Assert.That(s.Haven.Contains(b.X, b.Z), Is.False);
                Assert.That(s.OnRoad(b.X, b.Z), Is.False);
            }
            int dark = CountColor(s.Land(0), Coastline.Building);
            Assert.That(dark, Is.GreaterThan(s.Buildings.Count * 8));
        }

        [Test]
        public void VegetationIsLowAndClearOfThePad()
        {
            var s = Site();
            Assert.That(s.Clumps.Count, Is.GreaterThanOrEqualTo(36));
            foreach (var c in s.Clumps)
            {
                Assert.That(c.Height, Is.InRange(0.4f, 1.3f));
                Assert.That(Coastline.IsLand(c.X, c.Z), Is.True);
                Assert.That(Coastline.IsApron(c.X, c.Z), Is.False);
                Assert.That(s.Haven.Contains(c.X, c.Z), Is.False);
                float d = (float)Math.Sqrt(c.X * c.X + c.Z * c.Z);
                Assert.That(d, Is.GreaterThan(90f));
            }
            Assert.That(SiteGenerator.ClumpCount(2), Is.LessThan(SiteGenerator.ClumpCount(0)));
        }

        [Test]
        public void HavenIsOpenWaterWithRoomForTheBarge()
        {
            var s = Site();
            Assert.That(s.Haven.Name, Is.EqualTo("Haven"));
            Assert.That(s.Haven.Area, Is.GreaterThan(200000f));
            Assert.That(s.Haven.CenterX, Is.GreaterThan(1500f), "downrange, at sea");
            for (int iz = -2; iz <= 2; iz++)
            for (int ix = -2; ix <= 2; ix++)
            {
                float x = s.Haven.CenterX + ix * s.Haven.HalfX * 0.8f;
                float z = s.Haven.CenterZ + iz * s.Haven.HalfZ * 0.8f;
                Assert.That(Coastline.IsWater(x, z), Is.True, $"{x},{z}");
                Assert.That(x - Coastline.ShoreX(z), Is.GreaterThan(400f));
            }
            foreach (var b in s.Buildings) Assert.That(s.Haven.Contains(b.X, b.Z), Is.False);
            foreach (var c in s.Clumps) Assert.That(s.Haven.Contains(c.X, c.Z), Is.False);
            var water = s.Water(1);
            Assert.That(water.ContainsXZ(s.Haven.CenterX, s.Haven.CenterZ), Is.True, "water mesh covers Haven");
            Assert.That(s.Land(1).ContainsXZ(s.Haven.CenterX, s.Haven.CenterZ), Is.False);
        }

        [Test]
        public void CoastStaysAShorelineOutToTheHorizon()
        {
            var s = Site();
            var land = s.Land(1); var water = s.Water(1);
            foreach (var z in new[] { 0f, 250f, -600f, 2800f, 4200f, -8000f, 25000f, -60000f, 180000f })
            {
                float shore = Coastline.ShoreX(z);
                Assert.That(land.ContainsXZ(shore - 18f, z), Is.True, $"land z={z} shore={shore}");
                Assert.That(water.ContainsXZ(shore + 25f, z), Is.True, $"water z={z} shore={shore}");
                Assert.That(water.ContainsXZ(shore - 200f, z), Is.False, $"no water inland z={z}");
            }
            Assert.That(land.ContainsXZ(0f, 0f), Is.True);
            Assert.That(water.ContainsXZ(0f, 0f), Is.False);
            Assert.That(land.ContainsXZ(-30000f, 10000f), Is.True, "inland plain");
            Assert.That(water.ContainsXZ(80000f, -2000f), Is.True, "open sea");
        }

        [Test]
        public void CurvatureDropsTheHorizon()
        {
            float h = 20000f;
            float d = SiteHorizon.Distance(h);
            float drop = SiteHorizon.Drop(d, 0f);
            float extra = h * h / (2f * SiteHorizon.EarthRadius);
            Assert.That(drop, Is.EqualTo(h + extra).Within(0.25f));
            var water = Site().Water(0);
            // A far vertex should sit on the parabola, not on a flat plane.
            float far = 0f, y = 0f;
            for (int i = 0; i < water.VertexCount; i++)
            {
                float x = water.Positions[i * 3], z = water.Positions[i * 3 + 2];
                float r = (float)Math.Sqrt(x * x + z * z);
                if (r > far) { far = r; y = water.Positions[i * 3 + 1]; }
            }
            Assert.That(far, Is.GreaterThan(SiteHorizon.Distance(100000f)), "water reaches the high-altitude horizon");
            Assert.That(y, Is.LessThan(-1000f), "far water is below sea level");
            float expected = -0.08f - SiteHorizon.Drop(far, 0f) - 0.12f;
            Assert.That(y, Is.EqualTo(expected).Within(5f));
        }

        [Test]
        public void LodGetsCoarserButKeepsTheHorizon()
        {
            var s = Site();
            int hi = SiteBudget.Triangles(s, 0), mid = SiteBudget.Triangles(s, 1), lo = SiteBudget.Triangles(s, 2);
            Assert.That(mid, Is.LessThan(hi));
            Assert.That(lo, Is.LessThan(mid));
            Assert.That(hi, Is.LessThan(SiteBudget.MaxTrisHigh));
            Assert.That(lo, Is.LessThan(SiteBudget.MaxTrisLow));
            Assert.That(s.Water(2).ContainsXZ(s.Haven.CenterX, s.Haven.CenterZ), Is.True);
            float maxR = 0f;
            var w = s.Water(2);
            for (int i = 0; i < w.VertexCount; i++)
            {
                float x = w.Positions[i * 3], z = w.Positions[i * 3 + 2];
                maxR = Math.Max(maxR, (float)Math.Sqrt(x * x + z * z));
            }
            Assert.That(maxR, Is.GreaterThan(SiteHorizon.WaterRadius * 0.95f));
        }

        [Test]
        public void BudgetIsTwoDrawsAndNoTextures()
        {
            var s = Site();
            var led = new Kestrel.Kit.BudgetLedger();
            SiteBudget.Estimate(s, 0, led);
            Assert.That(SiteBudget.DrawCalls, Is.EqualTo(2));
            Assert.That(led.DrawCalls, Is.EqualTo(2));
            Assert.That(led.ArtBytes, Is.EqualTo(0));
            Assert.That(led.Triangles, Is.LessThan(Kestrel.Kit.KitBudgets.PadTriangles(Kestrel.Kit.KitBudgets.Low) / 2));
        }

        [Test]
        public void TrenchOpeningDropsOnlyTheTrianglesItCovers()
        {
            float[] pos =
            {
                0, 0, 0,  4, 0, 0,  0, 0, 4,
                20, 0, 20,  24, 0, 20,  20, 0, 24
            };
            int[] idx = { 0, 1, 2, 3, 4, 5 };
            float[] hole = { 0.2f, 0.2f, 1.2f, 0.2f, 0.2f, 1.2f };
            var cut = SiteMesh.WithoutOpenings(pos, idx, new[] { hole });
            Assert.That(cut, Is.EqualTo(new[] { 3, 4, 5 }));
            Assert.That(SiteMesh.WithoutOpenings(pos, idx, null), Is.SameAs(idx));
        }

        [Test]
        public void GroundFacesUpAndIndicesAreInRange()
        {
            var land = Site().Land(0);
            Assert.That(land.AverageNormalY(), Is.GreaterThan(0.35f));
            Assert.That(land.Indices.Length % 3, Is.EqualTo(0));
            Assert.That(land.Colors.Length, Is.EqualTo(land.VertexCount * 4));
            int max = 0;
            for (int i = 0; i < land.Indices.Length; i++)
            {
                Assert.That(land.Indices[i], Is.InRange(0, land.VertexCount - 1));
                if (land.Indices[i] > max) max = land.Indices[i];
            }
            Assert.That(max, Is.LessThan(land.VertexCount));
        }

        static int CountColor(SiteMesh m, float[] rgba)
        {
            int n = 0;
            for (int i = 0; i < m.Colors.Length; i += 4)
                if (Close(m.Colors, i, rgba)) n++;
            return n;
        }

        static bool Close(float[] c, int i, float[] rgba) =>
            Math.Abs(c[i] - rgba[0]) < 0.02f && Math.Abs(c[i + 1] - rgba[1]) < 0.02f && Math.Abs(c[i + 2] - rgba[2]) < 0.02f;
    }
}
