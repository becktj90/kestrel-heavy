using System.Collections.Generic;
using System.Linq;
using Kestrel.Kit.Contracts;
using NUnit.Framework;

namespace Kestrel.Pad.Tests
{
    public class TowerAnchorTests
    {
        static readonly PadFit Heavy = new PadFit(70f, 2.5f);

        [TestCase(PadArchetype.Tower)] [TestCase(PadArchetype.TowerStrongback)]
        public void EveryLevelHasFourFaceAnchorsAtDeckHeight(PadArchetype t)
        {
            var L = PadGenerator.Generate(3, t, Heavy); var tp = L.Tower;
            for (int l = 0; l < tp.Levels; l++)
                foreach (var f in new[] { "s", "e", "n", "w" })
                {
                    var a = L.Anchors.Get(TowerAnchors.LevelId(l, f));
                    Assert.That(a.Level, Is.EqualTo(l)); Assert.That(a.Face, Is.EqualTo(f));
                    Assert.That(a.Position.Y, Is.EqualTo((l + 1) * tp.LevelHeight).Within(1e-4f));
                }
            Assert.That(L.Anchors.Get(TowerAnchors.LevelId(0, "s")).Position.Z, Is.EqualTo(tp.NearFaceZ).Within(1e-4f));
        }

        [Test]
        public void VehicleFaceAnchorsPointTowardTheVehicle()
        {
            var a = PadGenerator.Generate(5, PadArchetype.Tower, Heavy).Anchors.Get(TowerAnchors.LevelId(2, "s"));
            // rotate local +Z by the quaternion: z' = (2(xz+wy), 2(yz-wx), 1-2(xx+yy))
            var q = a.Rotation;
            float fx = 2 * (q.X * q.Z + q.W * q.Y), fz = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            Assert.That(fz, Is.EqualTo(-1f).Within(1e-4f)); Assert.That(fx, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void CrewArmAnchorExistsAndSitsBelowTheVehicleTip()
        {
            var L = PadGenerator.Generate(7, PadArchetype.Tower, Heavy);
            Assert.That(L.Anchors.TryGet(TowerAnchors.CrewArm, out var c), Is.True);
            Assert.That(c.Position.Y, Is.LessThan(Heavy.VehicleHeight)); Assert.That(c.Position.Y, Is.GreaterThan(Heavy.VehicleHeight * 0.5f));
            Assert.That(c.Level, Is.EqualTo(L.Tower.CrewLevel));
            Assert.That(L.Graph.Contains("tower.crewarm"), Is.True);
            Assert.That(L.Members.Any(m => m.ParentId == "tower.crewarm" && m.Kind == MemberKind.ArmChord), Is.True);
        }

        [Test]
        public void EverySwingArmHasAnAnchorAtItsPivot()
        {
            var L = PadGenerator.Generate(2, PadArchetype.TowerStrongback, Heavy);
            Assert.That(L.Arms, Is.Not.Empty);
            foreach (var s in L.Arms)
            {
                var a = L.Anchors.Get(TowerAnchors.ArmId(s.AttachId));
                Assert.That(a.Position.X, Is.EqualTo(s.Pivot.X)); Assert.That(a.Position.Y, Is.EqualTo(s.Pivot.Y)); Assert.That(a.Position.Z, Is.EqualTo(s.Pivot.Z));
                Assert.That(a.Face, Is.EqualTo("s"));
            }
        }

        [Test]
        public void IdsAreUniqueAndUnknownIdThrows()
        {
            var A = PadGenerator.Generate(4, PadArchetype.Tower, Heavy).Anchors;
            Assert.That(A.All.Select(a => a.Id).Distinct().Count(), Is.EqualTo(A.Count));
            Assert.That(A.Contains(TowerAnchors.CapTop), Is.True);
            Assert.Throws<KeyNotFoundException>(() => A.Get("arm.nope"));
            Assert.That(A.TryGet("arm.nope", out _), Is.False);
        }

        [Test]
        public void SameSeedSameAnchors()
        {
            var a = PadGenerator.Generate(9, PadArchetype.Tower, Heavy).Anchors.All;
            var b = PadGenerator.Generate(9, PadArchetype.Tower, Heavy).Anchors.All;
            Assert.That(a.Select(x => x.ToString()), Is.EqualTo(b.Select(x => x.ToString())));
        }

        [Test]
        public void OnFaceNearPicksNearestDeck()
        {
            var tp = new TowerParams { LevelHeight = 6f, Levels = 10 };
            var A = new TowerAnchors(tp);
            Assert.That(A.OnFaceNear(13f).Id, Is.EqualTo("level.01.s"));   // deck at 12 m
            Assert.That(A.OnFaceNear(1f).Id, Is.EqualTo("level.00.s"));
            Assert.That(A.OnFaceNear(999f).Id, Is.EqualTo("level.09.s"));
            Assert.That(A.Contains(TowerAnchors.CrewArm), Is.False);
        }

        [Test]
        public void RailPadHasNoAnchorsAndTowerPadsHaveMasts()
        {
            Assert.That(PadGenerator.Generate(1, PadArchetype.Rail, new PadFit(15f, 0.6f)).Anchors, Is.Null);
            var L = PadGenerator.Generate(1, PadArchetype.Tower, Heavy);
            int masts = L.Graph.Query(ElementCategory.Structure, "lightning-mast").Count;
            Assert.That(masts, Is.InRange(2, 3));
            foreach (var m in L.Members.Where(m => m.ParentId != null && m.ParentId.StartsWith("mast.")))
            {
                float r = (float)System.Math.Sqrt(m.A.X * m.A.X + m.A.Z * m.A.Z);
                Assert.That(r, Is.GreaterThan(L.DeckRadius), m.Id);
            }
        }
    }
}
