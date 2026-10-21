using System;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Timeline;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class TimelineDiffTests
    {
        [Test]
        public void UnchangedDesign_HasAnEmptyDiff()
        {
            var cat = Golden.Catalog;
            // A design whose prediction reaches its target (the scout preset), so an unchanged copy has no problems at all. The old
            // sounding-solid template used to be this design, but it only reached 100 km on the vacuum Kepler apoapsis (113.8 km).
            // It is now the solid test fixture, whose prediction fails on its own (see the test below).
            var d = Preset("scout");
            var again = d.Clone();
            var diff = TimelineDiff.Compare(cat, d, again, 1);
            var twice = TimelineDiff.Compare(cat, d, again, 1);
            Assert.AreEqual(diff.Canonical(), twice.Canonical());
            Assert.AreEqual(0, diff.added.Count);
            Assert.AreEqual(0, diff.removed.Count);
            Assert.AreEqual(0, diff.shifted.Count);
            Assert.AreEqual(0, diff.problems.Count, diff.Canonical());
            AssertGolden("identity", diff);
        }

        [Test]
        public void UnchangedFailingDesign_HasNoEventDiff_AndOnlyItsOwnFailedPrediction()
        {
            var cat = Golden.Catalog;
            // #314: the solid sounding fixture. The prediction flies the played checks, so it breaks up over 20 g at T+0.6 s.
            var d = Golden.Fixture(Golden.SolidSoundingFixture);
            var diff = TimelineDiff.Compare(cat, d, d.Clone(), 1);
            Assert.AreEqual(diff.Canonical(), TimelineDiff.Compare(cat, d, d.Clone(), 1).Canonical());
            Assert.AreEqual(0, diff.added.Count);
            Assert.AreEqual(0, diff.removed.Count);
            Assert.AreEqual(0, diff.shifted.Count);
            Assert.AreEqual(1, diff.problems.Count, diff.Canonical());
            Assert.AreEqual(TimelineDiff.FailedPrediction, diff.problems[0].kind);
            StringAssert.DoesNotStartWith("The edit loses the target.", diff.problems[0].explainWhy, "nothing was edited");
            StringAssert.Contains("Apogee 0.6 km, target 100.0 km", diff.problems[0].explainWhy);
        }

        [Test]
        public void FairingRemoval_RemovesTheEvent_AndFlagsMissingHardware()
        {
            var cat = Golden.Catalog;
            var before = LoadTemplate("hydrogen-upper.json");
            var after = before.Clone();
            after.fairingDiameter = 0;
            after.fairingLength = 0;
            var diff = TimelineDiff.Compare(cat, before, after, 1);
            var fairing = Find(diff.removed, "stg.fairing");
            Assert.IsNotNull(fairing, diff.Canonical());
            StringAssert.StartsWith("\u2212 ", fairing.label);
            StringAssert.Contains("no fairing", fairing.explainWhy);
            var flag = FindProblem(diff, TimelineDiff.MissingHardware, "fairing");
            Assert.IsNotNull(flag, diff.Canonical());
            StringAssert.Contains("fairing", flag.explainWhy);
            StringAssert.Contains("stg.fairing", flag.explainWhy);
            AssertExplained(diff);
            AssertOrdered(diff);
            AssertGolden("fairing", diff);
        }

        [Test]
        public void UpperEngineSwap_DropsRestart_AndExplainsTheLostBurns()
        {
            var cat = Golden.Catalog;
            var before = LoadTemplate("hydrogen-upper.json");
            var after = before.Clone();
            after.stages[1].engine = "sc-kerolox";
            after.stages[1].propellant = "kerolox";
            var diff = TimelineDiff.Compare(cat, before, after, 1);
            Assert.IsNotNull(Find(diff.removed, "stg.ses.s2.1"), diff.Canonical());
            Assert.IsNotNull(Find(diff.removed, "stg.ses.s2.2"), diff.Canonical());
            Assert.IsNotNull(Find(diff.added, "stg.light.s2"), diff.Canonical());
            var flag = FindProblem(diff, TimelineDiff.LostRestart, "s2");
            Assert.IsNotNull(flag, diff.Canonical());
            StringAssert.Contains("exp-hydrolox", flag.explainWhy);
            StringAssert.Contains("sc-kerolox", flag.explainWhy);
            StringAssert.Contains("restarts", flag.explainWhy);
            StringAssert.Contains("stg.ses.s2.2", flag.explainWhy);
            AssertExplained(diff);
            AssertOrdered(diff);
            AssertGolden("restart", diff);
        }

        [Test]
        public void PropellantCut_FlagsAFailedPrediction_WithTheLoss()
        {
            var cat = Golden.Catalog;
            var before = Preset("scout");
            var after = before.Clone();
            after.stages[0].propMass = 1500;
            after.stages[1].propMass = 200;
            var beforeP = LaunchPredictor.Predict(cat, before);
            var afterP = LaunchPredictor.Predict(cat, after);
            Assert.IsTrue(beforeP.Success, beforeP.Text);
            Assert.IsFalse(afterP.Success, afterP.Text);
            var diff = TimelineDiff.Compare(cat, before, after, 1);
            var flag = FindProblem(diff, TimelineDiff.FailedPrediction, "scout");
            Assert.IsNotNull(flag, diff.Canonical());
            StringAssert.StartsWith("The edit loses the target.", flag.explainWhy);
            StringAssert.Contains("does not reach", flag.explainWhy);
            Assert.Greater(diff.shifted.Count, 0);
            foreach (var s in diff.shifted)
            {
                Assert.AreEqual(s.afterNominal - s.beforeNominal, s.deltaSeconds);
                StringAssert.Contains("shifts", s.explainWhy);
            }
            AssertExplained(diff);
            AssertOrdered(diff);
            AssertGolden("prediction", diff);
        }

        [Test]
        public void BrokenPrecondition_FlagsCausality_AndKeepsTheRemovedEvent()
        {
            var cat = Golden.Catalog;
            var design = LoadTemplate("hydrogen-upper.json");
            var mission = new Mission { mode = "standard" };
            var pad = Pad.For(cat, design);
            var before = TimelineGenerator.Generate(cat, design, pad, mission, 1);
            var after = TimelineGenerator.Generate(cat, design, pad, mission, 1);
            Assert.IsNotNull(FindEvent(before, "stg.seco.s2.2"));
            after.events.RemoveAll(e => e.id == "stg.seco.s2.2");
            var pred = LaunchPredictor.Predict(cat, design, "standard", 1);
            var diff = TimelineDiff.Compare(before, after, pred, pred, 1);
            Assert.IsNotNull(Find(diff.removed, "stg.seco.s2.2"));
            var failed = FindProblem(diff, TimelineDiff.FailedPrediction, design.id);
            if (pred.Success) Assert.IsNull(failed);
            else
            {
                Assert.IsNotNull(failed, pred.Text);
                StringAssert.DoesNotStartWith("The edit loses the target.", failed.explainWhy);
            }
            bool causality = false;
            foreach (var p in diff.problems)
            {
                if (p.kind != TimelineDiff.Causality) continue;
                causality = true;
                StringAssert.Contains("precondition", p.explainWhy);
                StringAssert.Contains("stg.seco.s2.2", p.explainWhy);
            }
            Assert.IsTrue(causality, diff.Canonical());
            AssertExplained(diff);
            AssertGolden("causality", diff);
        }

        static void AssertGolden(string name, DiffReport diff)
        {
            string dir = Path.Combine(Golden.Root, "tests", "vectors", "timeline-diff");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, name + ".txt");
            string text = diff.Canonical();
            if (Environment.GetEnvironmentVariable("KESTREL_WRITE_TIMELINE_DIFF") == "1")
                File.WriteAllText(path, text);
            Assert.IsTrue(File.Exists(path), "missing golden " + path);
            Assert.AreEqual(File.ReadAllText(path), text, name);
        }

        static void AssertExplained(DiffReport diff)
        {
            foreach (var c in diff.added) Assert.IsFalse(string.IsNullOrEmpty(c.explainWhy), c.id);
            foreach (var c in diff.removed) Assert.IsFalse(string.IsNullOrEmpty(c.explainWhy), c.id);
            foreach (var c in diff.shifted) Assert.IsFalse(string.IsNullOrEmpty(c.explainWhy), c.id);
            foreach (var p in diff.problems) Assert.IsFalse(string.IsNullOrEmpty(p.explainWhy), p.kind + " " + p.id);
        }

        static void AssertOrdered(DiffReport diff)
        {
            AssertSorted(diff.added);
            AssertSorted(diff.removed);
            AssertSorted(diff.shifted);
        }

        static void AssertSorted(System.Collections.Generic.List<DiffChange> rows)
        {
            for (int i = 1; i < rows.Count; i++)
            {
                int n = rows[i - 1].nominal.CompareTo(rows[i].nominal);
                if (n == 0) n = string.CompareOrdinal(rows[i - 1].id, rows[i].id);
                Assert.LessOrEqual(n, 0, rows[i].id);
            }
        }

        static DiffChange Find(System.Collections.Generic.List<DiffChange> rows, string id)
        {
            foreach (var c in rows) if (c.id == id) return c;
            return null;
        }

        static DiffProblem FindProblem(DiffReport diff, string kind, string id)
        {
            foreach (var p in diff.problems) if (p.kind == kind && p.id == id) return p;
            return null;
        }

        static MissionEvent FindEvent(Kestrel.Core.Timeline.Timeline t, string id)
        {
            foreach (var e in t.events) if (e.id == id) return e;
            return null;
        }

        static Design Preset(string id)
        {
            foreach (var p in Golden.Catalog.Presets)
                if (p.id == id) return p.Clone();
            Assert.Fail("missing preset " + id);
            return null;
        }

        static Design LoadTemplate(string file)
        {
            string path = Path.Combine(Golden.Root, "web", "js", "data", "templates", file);
            return Design.FromJson(MiniJson.ParseObject(File.ReadAllText(path)));
        }
    }
}
