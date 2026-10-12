using System.Linq;
using Kestrel.Kit;
using Kestrel.Kit.Contracts;
using NUnit.Framework;

namespace Kestrel.Pad.Tests
{
    public class PadGeneratorTests
    {
        static readonly PadFit Heavy = new PadFit(70f, 2.5f);

        [TestCase(PadArchetype.Rail)] [TestCase(PadArchetype.Tower)] [TestCase(PadArchetype.TowerStrongback)]
        public void SameSeedSamePad(PadArchetype t)
        {
            var a = PadGenerator.Generate(42, t, Heavy); var b = PadGenerator.Generate(42, t, Heavy);
            Assert.That(a.Hash(), Is.EqualTo(b.Hash()));
            Assert.That(a.Members.Count, Is.EqualTo(b.Members.Count));
            Assert.That(a.Graph.Count, Is.EqualTo(b.Graph.Count));
        }

        [Test]
        public void SeedVariesLayout()
        {
            var hashes = Enumerable.Range(1, 8).Select(s => PadGenerator.Generate(s, PadArchetype.Tower, Heavy).Hash()).Distinct().Count();
            Assert.That(hashes, Is.GreaterThan(4));
        }

        [Test]
        public void ArchetypesHaveTheirHardware()
        {
            var rail = PadGenerator.Generate(1, PadArchetype.Rail, new PadFit(15f, 0.6f));
            Assert.That(rail.Tower, Is.Null); Assert.That(rail.Arms, Is.Empty);
            Assert.That(rail.Graph.Contains("rail"), Is.True);
            var tower = PadGenerator.Generate(1, PadArchetype.Tower, Heavy);
            Assert.That(tower.Graph.Contains("tower"), Is.True); Assert.That(tower.Graph.Contains("strongback"), Is.False);
            Assert.That(tower.Arms.Count, Is.EqualTo(3));
            var heavy = PadGenerator.Generate(1, PadArchetype.TowerStrongback, Heavy);
            Assert.That(heavy.Graph.Contains("strongback"), Is.True);
            Assert.That(heavy.Tower.Width, Is.GreaterThan(PadGenerator.Generate(1, PadArchetype.Tower, Heavy).Tower.Width));
        }

        [Test]
        public void ArchetypeFollowsCorePadRule()
        {
            Assert.That(PadGenerator.ArchetypeFor("pad.rail", false), Is.EqualTo(PadArchetype.Rail));
            Assert.That(PadGenerator.ArchetypeFor("pad.tower", false), Is.EqualTo(PadArchetype.Tower));
            Assert.That(PadGenerator.ArchetypeFor("pad.tower", true), Is.EqualTo(PadArchetype.TowerStrongback));
        }

        [TestCase(30f, 1.6f)] [TestCase(70f, 2.5f)] [TestCase(120f, 5f)]
        public void TowerClearsVehicleAndOvertopsIt(float h, float r)
        {
            var L = PadGenerator.Generate(7, PadArchetype.TowerStrongback, new PadFit(h, r));
            Assert.That(L.Tower.Height, Is.GreaterThanOrEqualTo(h));
            Assert.That(L.Tower.NearFaceZ - r, Is.EqualTo(6f).Within(1e-3f));
            foreach (var m in L.Members)
            {
                // nothing but arms may enter the vehicle cylinder (+0.5 m)
                foreach (var p in new[] { m.A, m.B })
                    Assert.That(p.X * p.X + p.Z * p.Z, Is.GreaterThan((r + 0.5f) * (r + 0.5f)).Or.LessThan(-1f), m.Id);
            }
        }

        [Test]
        public void ArmsReachDefaultAttachPoints()
        {
            var L = PadGenerator.Generate(3, PadArchetype.Tower, Heavy);
            foreach (var p in PadGenerator.DefaultAttachPoints(Heavy, PadArchetype.Tower))
            {
                var spec = L.Arms.Single(a => a.AttachId == p.Id);
                var sol = SwingArmSolver.Solve(spec, p.Pose.Position);
                Assert.That(sol.Ok, Is.True, sol.Reason);
                Assert.That(sol.YawDeg, Is.EqualTo(180f).Within(1f).Or.EqualTo(-180f).Within(1f));
            }
        }

        [Test]
        public void DefaultAttachTimelineValidatesAgainstContract()
        {
            var pts = PadGenerator.DefaultAttachPoints(Heavy, PadArchetype.Tower);
            Assert.That(UmbilicalTimeline.Validate(pts, UmbilicalTimeline.Build(pts)), Is.Empty);
        }

        [TestCase(PadArchetype.Rail)] [TestCase(PadArchetype.Tower)] [TestCase(PadArchetype.TowerStrongback)]
        public void WorstCaseInsideHighAndLowBudgets(PadArchetype t)
        {
            var L = PadGenerator.Generate(9, t, new PadFit(120f, 5f));
            var led = new BudgetLedger(); PadPerf.Estimate(L, led);
            Assert.That(led.Within(KitBudgets.Low), Is.True, led.Report(KitBudgets.Low));
            Assert.That(led.DrawCalls, Is.LessThanOrEqualTo(12), led.Report(KitBudgets.High));
            Assert.That(led.Triangles, Is.LessThan(40_000), led.Report(KitBudgets.High));
        }

        [Test]
        public void ToPadAppliesRotationThenTranslation()
        {
            var yaw90 = new Quat(0, 0.70710678f, 0, 0.70710678f);
            var p = PadGenerator.ToPad(new LocalPose(new Vec3(1, 2, 3), yaw90), new Vec3(0, 0, 1));
            Assert.That(p.X, Is.EqualTo(2f).Within(1e-4f)); Assert.That(p.Y, Is.EqualTo(2f).Within(1e-4f)); Assert.That(p.Z, Is.EqualTo(3f).Within(1e-4f));
        }
    }

    public class ElementGraphTests
    {
        [Test]
        public void GraphIsAForestWithUniqueIdsAndKnownParents()
        {
            var g = new ElementGraph(); g.Add("pad", ElementCategory.Site);
            Assert.Throws<System.ArgumentException>(() => g.Add("pad", ElementCategory.Site));
            Assert.Throws<System.ArgumentException>(() => g.Add("x", ElementCategory.Member, "missing"));
            Assert.Throws<System.ArgumentException>(() => g.Add("", ElementCategory.Member));
        }

        [Test]
        public void TowerGraphIsQueryable()
        {
            var L = PadGenerator.Generate(5, PadArchetype.Tower, new PadFit(70f, 2.5f));
            var g = L.Graph;
            int levels = L.Tower.Levels;
            Assert.That(g.Query(ElementCategory.Level, "level:0").Count, Is.EqualTo(1));
            Assert.That(g.Query(ElementCategory.Member, "column").Count, Is.EqualTo(levels * 4));
            Assert.That(g.Query(ElementCategory.Member, "brace", "tower.l00", "face:s").Count, Is.EqualTo(2));
            Assert.That(g.Query(ElementCategory.SwingArm).Count, Is.EqualTo(L.Arms.Count));
            Assert.That(g.PathTo("tower.l03.col.ne"), Is.EqualTo(new[] { "pad", "tower", "tower.l03", "tower.l03.col.ne" }));
            Assert.That(g.Parent("tower.l03").Id, Is.EqualTo("tower"));
            // everything but pad + tower + the free-standing lightning masts (D2: mast.N hang off pad, not the tower)
            int masts = g.Query(ElementCategory.Structure, "lightning-mast").Count;
            Assert.That(masts, Is.GreaterThan(0));
            Assert.That(g.Query(ElementCategory.Structure, "lightning-mast").All(e => e.ParentId == "pad"), Is.True);
            Assert.That(g.Descendants("tower").Count(), Is.EqualTo(g.Count - 2 - masts));
            foreach (var e in g.All) if (e.ParentId != null) Assert.That(g.Contains(e.ParentId), e.Id);
            // every built member has a graph element with the same id
            foreach (var m in L.Members.Where(m => m.ParentId == null || !m.ParentId.StartsWith("mast."))) Assert.That(g.Get(m.Id)?.Category, Is.EqualTo(m.Kind == MemberKind.Deck ? ElementCategory.Deck : ElementCategory.Member));
        }

        [Test]
        public void PickReturnsSmallestContainingElement()
        {
            var L = PadGenerator.Generate(5, PadArchetype.Tower, new PadFit(70f, 2.5f));
            var col = L.Graph.Get("tower.l02.col.ne");
            Assert.That(L.Graph.PickAt(col.Center).Id, Is.EqualTo("tower.l02.col.ne"));
            Assert.That(L.Graph.PickAt(col.Center, ElementCategory.Level).Id, Is.EqualTo("tower.l02"));
        }
    }

    public class SwingArmMotionTests
    {
        [Test]
        public void RetractEasesAndClamps()
        {
            Assert.That(SwingArmSolver.RetractProgress(-1, 8), Is.EqualTo(0));
            Assert.That(SwingArmSolver.RetractProgress(4, 8), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(SwingArmSolver.RetractProgress(9, 8), Is.EqualTo(1));
        }

        [Test]
        public void SolveRejectsOutOfReach()
        {
            var s = new SwingArmSpec { Pivot = new Vec3(0, 10, 10), MaxLength = 8, MaxVerticalOffset = 1 };
            Assert.That(SwingArmSolver.Solve(s, new Vec3(0, 10, 0)).Ok, Is.False);
            Assert.That(SwingArmSolver.Solve(s, new Vec3(0, 13, 4)).Ok, Is.False);
            Assert.That(SwingArmSolver.Solve(s, new Vec3(0, 10.5f, 4)).Ok, Is.True);
        }

        [Test]
        public void TrussMemberCountMatchesFormula()
        {
            foreach (var len in new[] { 3f, 7.5f, 16f })
                Assert.That(SwingArmTruss.Build("a", len, 1.6f, 1.8f).Count, Is.EqualTo(SwingArmTruss.MemberCount(len)));
        }
    }
}
