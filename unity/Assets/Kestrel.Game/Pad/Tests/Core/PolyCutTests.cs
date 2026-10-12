// Ear clipping must never throw on the deck slab, including collinear and duplicate vertices from bridged holes.
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Kestrel.Pad.Tests
{
    public class PolyCutTests
    {
        static float TriArea(List<Vec2> v, List<int> t)
        {
            double a = 0;
            for (int i = 0; i + 2 < t.Count; i += 3)
                a += 0.5 * Math.Abs(PolyCut.Cross(v[t[i]], v[t[i + 1]], v[t[i + 2]]));
            return (float)a;
        }

        [Test]
        public void DegenerateRingDoesNotThrowAndKeepsArea()
        {
            // Square with a duplicate vertex, a collinear run and a hole whose edge is collinear with the bridge.
            var outer = new List<Vec2> { new Vec2(-10, -10), new Vec2(0, -10), new Vec2(0, -10), new Vec2(5, -10), new Vec2(10, -10),
                                         new Vec2(10, 10), new Vec2(-10, 10), new Vec2(-10, 0) };
            var hole = new List<Vec2> { new Vec2(-2, -2), new Vec2(-2, 2), new Vec2(2, 2), new Vec2(2, 0), new Vec2(2, -2) };
            var verts = new List<Vec2>(); var tris = new List<int>();
            Assert.DoesNotThrow(() => PolyCut.Triangulate(outer, new List<IList<Vec2>> { hole }, verts, tris));
            Assert.That(TriArea(verts, tris), Is.EqualTo(400f - 16f).Within(400f * 0.02f));
        }

        [Test]
        public void DeckSlabAndGroundCutTriangulateForManySeedsAndFits()
        {
            var fits = new List<PadFit>();
            for (float h = 12f; h <= 110f; h += 7.3f)
                for (float r = 0.5f; r <= 7.5f; r += 0.61f) fits.Add(new PadFit(h, r, 6f));
            foreach (var type in new[] { PadArchetype.Rail, PadArchetype.Tower, PadArchetype.TowerStrongback })
                foreach (var fit in fits)
                    for (int seed = 0; seed < 12; seed++)
                    {
                        var layout = PadGenerator.Generate(seed, type, fit);
                        if (layout.Trench == null) continue;
                        var t = layout.Trench;
                        Assert.DoesNotThrow(() => TrenchMeshBuilder.Build(t, TrenchGrammar.SurfaceRadius(layout)), $"slab {type} seed {seed} r {fit.VehicleRadius}");
                        // Ground plane cut: trench opening plus flame ponds, as PadDetailLink does.
                        var holes = new List<IList<Vec2>> { TrenchGrammar.GroundOpening(t) };
                        foreach (var pond in TrenchGrammar.PondPolys(t)) holes.Add(pond);
                        Assert.DoesNotThrow(() => PolyCut.Frame(500f, holes, 0f), $"ground {type} seed {seed} h {fit.VehicleHeight} r {fit.VehicleRadius}");
                    }
        }
    }
}
