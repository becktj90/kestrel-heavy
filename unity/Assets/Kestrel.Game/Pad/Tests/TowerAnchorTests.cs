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
            Assert.That(L.Graph.Query(ElementCategory.CrewArm).Count, Is.EqualTo(1));
            Assert.That(L.Graph.Query(ElementCategory.SwingArm).Count, Is.EqualTo(L.Arms.Count), "crew arm is not a swing arm");
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

        [Test]
        public void CrewAndSwingArmsEndAtTheRealSkin()
        {
            // overall half-width 6.19 m (side boosters), but the core facing the tower is only 2.5 m
            var fit = new PadFit(70f, 6.19f) { SkinRadiusAt = y => 2.5f };
            var L = PadGenerator.Generate(1, PadArchetype.Tower, fit);
            var crew = L.Members.Where(m => m.ParentId == "tower.crewarm" && m.Kind == MemberKind.ArmChord).ToList();
            Assert.That(crew, Is.Not.Empty);
            foreach (var c in crew) Assert.That(System.Math.Min(c.A.Z, c.B.Z), Is.EqualTo(2.5f + PadFit.ArmSkinGap).Within(1e-3f), c.Id);
            foreach (var p in PadGenerator.DefaultAttachPoints(fit, PadArchetype.Tower)) Assert.That(p.Pose.Position.Z, Is.EqualTo(2.5f).Within(1e-4f));
            foreach (var a in L.Arms)
                Assert.That(SwingArmSolver.Solve(a, new Vec3(0, a.Pivot.Y, 2.5f)).Ok, Is.True, a.AttachId);
            // no profile: falls back to the overall radius
            var plain = PadGenerator.Generate(1, PadArchetype.Tower, new PadFit(70f, 2.5f));
            foreach (var c in plain.Members.Where(m => m.ParentId == "tower.crewarm" && m.Kind == MemberKind.ArmChord))
                Assert.That(System.Math.Min(c.A.Z, c.B.Z), Is.EqualTo(2.5f + PadFit.ArmSkinGap).Within(1e-3f));
        }

        [TestCase(PadArchetype.Tower)] [TestCase(PadArchetype.TowerStrongback)]
        public void MastsStayOutOfTheDefaultCameraView(PadArchetype t)
        {
            double cb = PadGenerator.DefaultViewBearingDeg * System.Math.PI / 180;
            float cx = (float)System.Math.Sin(cb), cz = (float)System.Math.Cos(cb);
            for (int seed = 1; seed <= 12; seed++)
            {
                var L = PadGenerator.Generate(seed, t, Heavy);
                foreach (var e in L.Graph.Query(ElementCategory.Structure, "lightning-mast"))
                {
                    float r = (float)System.Math.Sqrt(e.Center.X * e.Center.X + e.Center.Z * e.Center.Z);
                    float dot = (e.Center.X * cx + e.Center.Z * cz) / r;
                    Assert.That(dot, Is.LessThan(0.5f), $"seed {seed} {e.Id}: within 60° of the camera bearing");
                    float b = (float)(System.Math.Atan2(e.Center.X, e.Center.Z) * 180 / System.Math.PI); b = (b + 360) % 360;
                    Assert.That(System.Math.Abs(b - 90) >= 25f && System.Math.Abs(b - 270) >= 25f, Is.True, $"{e.Id} in plume sector");
                }
            }
        }
    }
}
