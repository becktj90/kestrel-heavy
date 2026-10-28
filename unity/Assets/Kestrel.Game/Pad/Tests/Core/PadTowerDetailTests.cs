// EditMode, engine-free. Owner feedback on an iPhone recording (KH-1 First Hop): "The tower needs more detail like an
// actual tower on a launch pad." Small stacks (stages under 3 m across) get the Rail pad, and its rail on two yellow
// legs read as one flat grey slab. The rail now rides a compact service tower built by the same TowerLayout as the
// big pads, and every tower gets plank work platforms, a stair core (plus an elevator shaft when wide enough) and a
// lightning spike. These pin the geometry for every stack size, First Hop to the heavy, and the PadPerf ceilings.
using System;
using System.Collections.Generic;
using System.Linq;
using Kestrel.Kit;
using Kestrel.Kit.Contracts;
using NUnit.Framework;

namespace Kestrel.Pad.Tests
{
    public class PadTowerDetailTests
    {
        // (name, height, widest radius, pad): the designer's range, First Hop (4.5 m hopper) to the 120 m heavy.
        static readonly (string Name, float H, float R, PadArchetype Type)[] Stacks =
        {
            ("First Hop", 4.5f, 0.9f, PadArchetype.Rail), ("KH-1 tall", 9f, 0.8f, PadArchetype.Rail),
            ("starter", 14f, 0.45f, PadArchetype.Rail), ("Tiny Orbit", 22f, 0.6f, PadArchetype.Rail),
            ("rail 36", 36f, 1.2f, PadArchetype.Rail), ("rail 60", 60f, 1.4f, PadArchetype.Rail),
            ("tower 48", 48f, 1.6f, PadArchetype.Tower), ("tower 70", 70f, 2.5f, PadArchetype.Tower),
            ("strap-ons 90", 90f, 3.6f, PadArchetype.TowerStrongback), ("heavy 120", 120f, 5f, PadArchetype.TowerStrongback),
        };
        static readonly int[] Seeds = { 1, 2, 3, 5, 8, 13 };

        static IEnumerable<(string Where, PadLayout L, float H, float R)> Pads()
        {
            foreach (var s in Stacks)
                foreach (var seed in Seeds)
                    yield return (s.Name + " " + s.Type + " seed " + seed, PadGenerator.Generate(seed, s.Type, new PadFit(s.H, s.R)), s.H, s.R);
        }

        [Test]
        public void EveryPadHasATowerThatOvertopsTheStack()
        {
            foreach (var (where, L, h, r) in Pads())
            {
                var tp = L.Tower;
                Assert.That(tp, Is.Not.Null, where + ": no service tower");
                Assert.That(L.Graph.Contains(TowerLayout.TowerId), Is.True, where);
                Assert.That(tp.Levels, Is.GreaterThanOrEqualTo(2), where);
                Assert.That(tp.Height, Is.GreaterThanOrEqualTo(h), where + " levels top out under the nose");
                if (L.Archetype == PadArchetype.Rail)
                {
                    Assert.That(tp.Height, Is.GreaterThanOrEqualTo(Math.Max(h + 1.5f, h * 1.08f) - 1e-3f), where + " margin over the nose");
                    Assert.That(tp.LevelHeight, Is.InRange(2.4f, PadGenerator.RailTowerMaxLevel + 1e-3f), where + " level pitch");
                    Assert.That(tp.Width, Is.InRange(3f, 6f), where + " compact footprint");
                    Assert.That(L.Anchors, Is.Null, where + ": rail pads keep their umbilicals on the rail");
                    Assert.That(L.Arms, Is.Empty, where);
                }
                // Lightning spike above the cap, the cap above the last deck.
                var spike = L.Members.Single(m => m.Id == "tower.cap.spike");
                Assert.That(Math.Min(spike.A.Y, spike.B.Y), Is.EqualTo(tp.Height + tp.CapHeight).Within(1e-3f), where);
                Assert.That(Math.Max(spike.A.Y, spike.B.Y), Is.GreaterThan(tp.Height + tp.CapHeight + 1f), where);
            }
        }

        [Test]
        public void EveryLevelIsALatticeWithAWorkPlatformAndAFlightOfStairs()
        {
            foreach (var (where, L, _, _) in Pads())
            {
                var tp = L.Tower;
                var ids = new HashSet<string>(L.Members.Select(m => m.Id));
                for (int l = 0; l < tp.Levels; l++)
                {
                    string lid = $"tower.l{l:00}";
                    foreach (var c in new[] { "sw", "se", "ne", "nw" }) Assert.That(ids.Contains($"{lid}.col.{c}"), where + " " + lid + " leg " + c);
                    foreach (var f in new[] { "s", "e", "n", "w" })
                    {
                        Assert.That(ids.Contains($"{lid}.br.{f}.0") && ids.Contains($"{lid}.br.{f}.1"), where + " " + lid + " X-brace " + f);
                        Assert.That(ids.Contains($"{lid}.girt.{f}"), where + " " + lid + " girt " + f);
                    }
                    var planks = L.Members.Where(m => m.ParentId == lid && m.Kind == MemberKind.Deck).ToList();
                    Assert.That(planks.Count, Is.GreaterThanOrEqualTo(2), where + " " + lid + " platform");
                    foreach (var p in planks)
                    {
                        Assert.That(p.A.Y, Is.EqualTo((l + 1) * tp.LevelHeight).Within(1e-3f), where + " plank on the deck line");
                        Assert.That(Math.Abs(p.A.X), Is.LessThanOrEqualTo(tp.Width * 0.5f), where + " plank inside the legs");
                        Assert.That(p.A.X - TowerLayout.DeckW * 0.5f, Is.GreaterThan(TowerLayout.StairX(tp) + TowerLayout.DeckW * 0.5f - 1e-3f), where + " plank clear of the stairwell");
                    }
                    var stair = L.Members.Single(m => m.Id == $"tower.core.stair.{l:00}");
                    Assert.That(stair.B.Y - stair.A.Y, Is.EqualTo(tp.LevelHeight).Within(1e-3f), where + " flight climbs one level");
                    Assert.That(stair.A.X, Is.EqualTo(stair.B.X), where + " flight runs along z (plank lies flat)");
                }
                bool lift = L.Members.Any(m => m.Id.StartsWith("tower.core.lift."));
                Assert.That(lift, Is.EqualTo(tp.Width >= TowerParams.ElevatorMinWidth), where + " elevator shaft on wide towers only");
            }
        }

        [Test]
        public void NoMemberIsNaNOrDegenerate()
        {
            foreach (var (where, L, _, _) in Pads())
            {
                foreach (var m in L.Members)
                {
                    foreach (var v in new[] { m.A.X, m.A.Y, m.A.Z, m.B.X, m.B.Y, m.B.Z })
                        Assert.That(float.IsNaN(v) || float.IsInfinity(v), Is.False, where + " " + m.Id);
                    Assert.That(m.Length, Is.GreaterThan(0.05f), where + " " + m.Id + " is a point");
                }
                foreach (var e in L.Graph.All)
                    Assert.That(float.IsNaN(e.Center.X + e.Center.Y + e.Center.Z + e.Size.X + e.Size.Y + e.Size.Z), Is.False, where + " " + e.Id);
            }
        }

        [Test]
        public void AccessArmReachesTheStackWithoutEnteringIt()
        {
            foreach (var (where, L, h, r) in Pads())
            {
                var tp = L.Tower;
                Assert.That(tp.CrewLevel, Is.InRange(0, tp.Levels - 1), where);
                float y = (tp.CrewLevel + 1) * tp.LevelHeight;
                Assert.That(y, Is.LessThan(h), where + " arm under the nose");
                Assert.That(y, Is.GreaterThan(h * 0.4f), where + " arm in the upper stack");
                var arm = L.Members.Where(m => m.ParentId == "tower.crewarm").ToList();
                Assert.That(arm, Is.Not.Empty, where + " access arm");
                float tipZ = arm.Min(m => Math.Min(m.A.Z, m.B.Z));
                if (L.Archetype == PadArchetype.Rail)
                {
                    // Beside the stack, abreast of its axis, never across the rail (x = 0).
                    Assert.That(tipZ, Is.EqualTo(0f).Within(0.05f), where + " reaches abreast of the stack");
                    float inner = arm.Min(m => Math.Min(m.A.X, m.B.X));
                    Assert.That(inner, Is.GreaterThanOrEqualTo(r + PadGenerator.RailArmGap - 1e-3f), where + " inner chord off the skin line");
                }
                else Assert.That(tipZ - r, Is.EqualTo(PadFit.ArmSkinGap).Within(0.05f), where + " tip stops at the skin gap");
                foreach (var m in arm)
                    for (int i = 0; i <= 10; i++)
                    {
                        float t = i / 10f, x = m.A.X + (m.B.X - m.A.X) * t, z = m.A.Z + (m.B.Z - m.A.Z) * t;
                        Assert.That(Math.Sqrt(x * x + z * z), Is.GreaterThan(r + 0.3f), where + " " + m.Id + " enters the stack");
                    }
            }
        }

        [Test]
        public void RailRidesTheTowerOnTies()
        {
            foreach (var (where, L, h, r) in Pads())
            {
                if (L.Archetype != PadArchetype.Rail) continue;
                var tp = L.Tower;
                Assert.That(L.RailTop.Z + PadGenerator.RailTowerGap, Is.EqualTo(tp.NearFaceZ).Within(1e-3f), where + " tower face just behind the rail top");
                var ties = L.Members.Where(m => m.Id.StartsWith("rail.tie.")).ToList();
                Assert.That(ties.Count, Is.GreaterThanOrEqualTo(Math.Min(tp.Levels, 2)), where + " ties");
                foreach (var t in ties)
                {
                    Assert.That(UmbilicalRigGap(t.A, L.RailBase, L.RailTop), Is.LessThan(1e-3f), where + " " + t.Id + " starts on the rail");
                    Assert.That(t.B.Z, Is.EqualTo(tp.NearFaceZ).Within(1e-3f), where + " " + t.Id + " ends on the tower face");
                    Assert.That(t.A.Y, Is.EqualTo(t.B.Y).Within(1e-4f), where + " level tie");
                }
                // The old A-frame legs are gone: nothing stands on the deck between the rail and the tower.
                Assert.That(L.Members.Any(m => m.Id.StartsWith("rail.leg.")), Is.False, where);
            }
        }

        static float UmbilicalRigGap(Vec3 p, Vec3 a, Vec3 b)
        {
            float dx = b.X - a.X, dy = b.Y - a.Y, dz = b.Z - a.Z;
            float len2 = dx * dx + dy * dy + dz * dz;
            float t = Math.Max(0f, Math.Min(1f, ((p.X - a.X) * dx + (p.Y - a.Y) * dy + (p.Z - a.Z) * dz) / len2));
            float x = a.X + dx * t - p.X, y = a.Y + dy * t - p.Y, z = a.Z + dz * t - p.Z;
            return (float)Math.Sqrt(x * x + y * y + z * z);
        }

        [Test]
        public void RailTrenchNeverRunsUnderTheTower()
        {
            foreach (var s in Stacks.Where(s => s.Type == PadArchetype.Rail))
                for (int seed = 1; seed <= 24; seed++)
                {
                    var L = PadGenerator.Generate(seed, s.Type, new PadFit(s.H, s.R));
                    var tr = L.Trench;
                    foreach (var m in L.Members.Where(m => m.Id.StartsWith("tower.") && m.Kind == MemberKind.Column && Math.Min(m.A.Y, m.B.Y) < 0.01f))
                    {
                        tr.ToFrame(m.A.X, m.A.Z, out float u, out float v);
                        // The one-sided ramp channel runs u = 0 (back wall) to the exit (TrenchMeshBuilder); 0.5 m covers
                        // half a column and a footing.
                        float back = tr.TwoSided ? -tr.ExitLength : 0f;
                        bool inSlot = u > back - 0.5f && u < tr.ExitLength + 0.5f && Math.Abs(v) < tr.HalfWidth(u) + 0.5f;
                        Assert.That(inSlot, Is.False, s.Name + " seed " + seed + " " + m.Id + " stands in the trench (bearing " + tr.BearingDeg.ToString("0") + ")");
                    }
                    Assert.That(TrenchGrammar.InPlumeSector(tr, 0f, L.Tower.StandOff, s.H), Is.False, s.Name + " seed " + seed + " tower in the plume lane");
                }
        }

        /// <summary>Tower + rail steel only (PadPerf.StructureTriangles), worst case with the camera at the pad.</summary>
        /// <summary>Measured at seed 1: First Hop 1.9k, Tiny Orbit 4.9k, rail 60 m 13.3k; tower 48 m 7.6k, heavy 120 m 17.5k.</summary>
        public const int RailStructureTrisMax = 16_000, TowerStructureTrisMax = 22_000;

        [Test]
        public void StructureStaysInsideItsTriangleCeilingAndEveryTierBudget()
        {
            foreach (var (where, L, _, _) in Pads())
            {
                long tris = PadPerf.StructureTriangles(L);
                int max = L.Archetype == PadArchetype.Rail ? RailStructureTrisMax : TowerStructureTrisMax;
                Assert.That(tris, Is.LessThanOrEqualTo(max), where + " tower steel");
                var led = new BudgetLedger(); PadPerf.Estimate(L, led);
                foreach (var q in new[] { KitBudgets.Low, KitBudgets.Medium, KitBudgets.High })
                    Assert.That(led.Within(q), Is.True, where + " q" + q + " " + led.Report(q));
                Assert.That(led.DrawCalls, Is.LessThanOrEqualTo(14), where + " " + led.Report(KitBudgets.High));
                if (L.Seed == 1) TestContext.WriteLine($"{where}: tower steel {tris} tris, {L.Members.Count} members; pad {led.Report(KitBudgets.Low)}");
            }
        }
    }
}
