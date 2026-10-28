// Engine-free coverage tests for the apron terrace ring (TrenchMeshBuilder.Apron).
// The tower-pad capture showed wide bare-ground wedges beside each trench mouth: whole 28-gon
// segments plus a whole neighbour were dropped, so the gap was far wider than the channel.
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Kestrel.Pad.Tests
{
    public class PadApronCoverageTests
    {
        const float TopY = -0.80f, Width = 8f;
        // The apron is cut on the 28-gon chords, so mid-chord it sits this fraction of the radius inside the circle.
        static readonly float ChordInset = (float)Math.Cos(Math.PI / 28.0);

        struct Tri { public float Ax, Az, Bx, Bz, Cx, Cz; }

        static List<Tri> ApronTriangles(PadMeshData concrete, float deck)
        {
            var list = new List<Tri>();
            for (int t = 0; t < concrete.TriangleCount; t++)
            {
                var p = new float[9];
                bool ok = true;
                for (int k = 0; k < 3; k++)
                {
                    int i = concrete.Triangles[t * 3 + k];
                    float x = concrete.Positions[i * 3], y = concrete.Positions[i * 3 + 1], z = concrete.Positions[i * 3 + 2];
                    float r = (float)Math.Sqrt(x * x + z * z);
                    if (Math.Abs(y - TopY) > 1e-3f || r < deck * ChordInset - 0.05f || r > deck + Width + 0.05f) ok = false;
                    p[k * 3] = x; p[k * 3 + 1] = y; p[k * 3 + 2] = z;
                }
                if (ok) list.Add(new Tri { Ax = p[0], Az = p[2], Bx = p[3], Bz = p[5], Cx = p[6], Cz = p[8] });
            }
            return list;
        }

        // Barycentric inside test with a margin: margin > 0 requires strictly interior, < 0 allows the boundary.
        static bool Inside(Tri t, float x, float z, float eps)
        {
            float d = (t.Bz - t.Cz) * (t.Ax - t.Cx) + (t.Cx - t.Bx) * (t.Az - t.Cz);
            if (Math.Abs(d) < 1e-9f) return false;
            float a = ((t.Bz - t.Cz) * (x - t.Cx) + (t.Cx - t.Bx) * (z - t.Cz)) / d;
            float b = ((t.Cz - t.Az) * (x - t.Cx) + (t.Ax - t.Cx) * (z - t.Cz)) / d;
            float c = 1f - a - b;
            return a > eps && b > eps && c > eps;
        }

        static float DistToPoly(IList<Vec2> poly, float x, float z)
        {
            float best = float.MaxValue;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                float ex = poly[i].X - poly[j].X, ez = poly[i].Y - poly[j].Y;
                float len2 = ex * ex + ez * ez;
                float u = len2 < 1e-9f ? 0f : Math.Max(0f, Math.Min(1f, ((x - poly[j].X) * ex + (z - poly[j].Y) * ez) / len2));
                float dx = x - (poly[j].X + ex * u), dz = z - (poly[j].Y + ez * u);
                best = Math.Min(best, (float)Math.Sqrt(dx * dx + dz * dz));
            }
            return best;
        }

        static IEnumerable<(PadArchetype type, PadFit fit, int seed)> Pads()
        {
            foreach (var type in new[] { PadArchetype.Rail, PadArchetype.Tower, PadArchetype.TowerStrongback })
                foreach (var fit in new[] { new PadFit(16f, 0.7f, 6f), new PadFit(48f, 1.6f, 6f), new PadFit(70f, 2.5f, 6f), new PadFit(120f, 5f, 6f) })
                    for (int seed = 1; seed <= 5; seed++)
                        yield return (type, fit, seed);
        }

        [Test]
        public void ApronNeverCoversTheTrenchChannelAndNeverOverlapsItself()
        {
            int checkedPads = 0;
            foreach (var (type, fit, seed) in Pads())
            {
                var layout = PadGenerator.Generate(seed, type, fit);
                var s = layout.Trench;
                if (s == null) continue;
                float deck = Math.Max(8f, TrenchGrammar.SurfaceRadius(layout));
                var tris = ApronTriangles(TrenchMeshBuilder.Build(s, TrenchGrammar.SurfaceRadius(layout)).Concrete, deck);
                string id = $"{type} seed {seed} h {fit.VehicleHeight}";
                Assert.That(tris.Count, Is.GreaterThan(0), $"{id}: no apron");
                var hole = TrenchGrammar.GroundOpening(s);
                for (float a = 0f; a < 360f; a += 0.25f)
                    foreach (float r in new[] { deck + 0.3f, deck + Width * 0.5f, (deck + Width) * ChordInset - 0.1f })
                    {
                        float x = r * (float)Math.Cos(a * Math.PI / 180.0), z = r * (float)Math.Sin(a * Math.PI / 180.0);
                        int hits = 0;
                        foreach (var t in tris) if (Inside(t, x, z, 1e-4f)) hits++;
                        Assert.That(hits, Is.LessThanOrEqualTo(1), $"{id}: coplanar apron overlap at {a} deg r {r}");
                        if (hits > 0)
                            Assert.That(PolyCut.Contains(hole, x, z), Is.False, $"{id}: apron bridges the trench channel at {a} deg r {r}");
                    }
                checkedPads++;
            }
            Assert.That(checkedPads, Is.GreaterThan(20));
        }

        [Test]
        public void ApronCoversTheFullRingExceptAtTheTrenchMouths()
        {
            foreach (var (type, fit, seed) in Pads())
            {
                var layout = PadGenerator.Generate(seed, type, fit);
                var s = layout.Trench;
                if (s == null) continue;
                float deck = Math.Max(8f, TrenchGrammar.SurfaceRadius(layout));
                var tris = ApronTriangles(TrenchMeshBuilder.Build(s, TrenchGrammar.SurfaceRadius(layout)).Concrete, deck);
                var hole = TrenchGrammar.GroundOpening(s);
                string id = $"{type} seed {seed} h {fit.VehicleHeight}";
                // A bare spot may sit this far from the grade opening: the buffer piece each side plus one
                // piece of quantisation, plus the lateral drift of the channel edge across the 8 m terrace
                // (a piece is dropped if the opening touches any part of it, and the edge is oblique to the ring). Whole 28-gon
                // segments (the old cut) are 4x the chord term and fail this on any large pad.
                float outerChord = 2f * (deck + Width) * (float)Math.Sin(Math.PI / (28.0 * TrenchMeshBuilder.ApronSub));
                float allowed = 2f * outerChord + 1f + Width;
                for (float a = 0f; a < 360f; a += 0.25f)
                    foreach (float r in new[] { deck + 0.3f, deck + Width * 0.5f, (deck + Width) * ChordInset - 0.1f })
                    {
                        float x = r * (float)Math.Cos(a * Math.PI / 180.0), z = r * (float)Math.Sin(a * Math.PI / 180.0);
                        bool covered = false;
                        foreach (var t in tris) if (Inside(t, x, z, -1e-4f)) { covered = true; break; }
                        if (covered) continue;
                        float d = PolyCut.Contains(hole, x, z) ? 0f : DistToPoly(hole, x, z);
                        Assert.That(d, Is.LessThanOrEqualTo(allowed),
                            $"{id}: bare apron {d:F1} m from the trench opening at {a} deg r {r:F1} (allowed {allowed:F1})");
                    }
            }
        }

        [Test]
        public void TowerPadTrenchMouthsLeaveOnlyTheChannelAndABufferBare()
        {
            // The pad framed by PadTowerCaptureTests (tower, seed 1, 48 m vehicle). Whole 28-gon segments
            // left 154 deg of the ring bare (now 103) for two channels that only span about 2 x 34 deg.
            var layout = PadGenerator.Generate(1, PadArchetype.Tower, new PadFit(48f, 1.6f, 6f));
            float deck = Math.Max(8f, TrenchGrammar.SurfaceRadius(layout));
            var tris = ApronTriangles(TrenchMeshBuilder.Build(layout.Trench, TrenchGrammar.SurfaceRadius(layout)).Concrete, deck);
            float r = deck + Width * 0.5f;
            int bare = 0, steps = 1440;
            for (int i = 0; i < steps; i++)
            {
                float a = (float)(2 * Math.PI * i / steps);
                float x = r * (float)Math.Cos(a), z = r * (float)Math.Sin(a);
                bool covered = false;
                foreach (var t in tris) if (Inside(t, x, z, -1e-4f)) { covered = true; break; }
                if (!covered) bare++;
            }
            float bareDeg = bare * 360f / steps;
            Assert.That(bareDeg, Is.LessThanOrEqualTo(120f), $"bare ring {bareDeg:F0} deg");
        }
    }
}
