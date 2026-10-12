// Regressions from the #169 review: names through undo/redo, no-op finish reset, booster issue routing,
// tune on engine swap, interstage override on a new bottom stage, custom numeric names, non-finite input,
// tuned part rows, and surrogate-safe truncation.
using System;
using System.IO;
using Kestrel.Core.Designer;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class HangarRegressionTests
    {
        static Data.Catalog C => Golden.Catalog;
        static HangarData H => HangarFixtures.Data;
        static JsonObject Stage(JsonObject d, int i) => (JsonObject)d.Arr("stages")[i];

        [Test]
        public void SaveUnderANewName_SurvivesUndoRedo_AndStaysClean()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kh-reg-" + Guid.NewGuid().ToString("N"));
            try
            {
                var lib = new DesignLibrary(new NativeStore(dir, () => 1, () => 0.5));
                var s = new HangarSession(C, H, HangarFixtures.Template("workhorse"));
                s.Apply(d => HangarEditor.SetLegs(d, true));
                Assert.IsTrue(s.SaveTo(lib, "Brand New").ok);
                Assert.IsFalse(s.Dirty);
                s.Undo(); s.Redo();
                Assert.IsFalse(s.Dirty, "redo returns to exactly the saved document");
                Assert.AreEqual("Brand New", s.SavedName);
                Assert.AreEqual("Brand New", lib.Load(s.SavedId).Str("name"), "the slot carries the name");
            }
            finally { try { Directory.Delete(dir, true); } catch (IOException) { } }
        }

        [Test]
        public void ResettingAnUnsetFinish_DoesNotTouchTheDesign()
        {
            var d = HangarFixtures.Template("workhorse");
            d.Remove("look"); d.Order.Remove("look");
            string before = HangarSession.Snapshot(d);
            Assert.IsFalse(HangarEditor.SetFinish(H, HangarProgress.None, d, "tank", null).changed);
            Assert.AreEqual(before, HangarSession.Snapshot(d));
        }

        [Test]
        public void BoosterRuleIssues_RingTheBoosters()
        {
            var d = HangarFixtures.Template("heavy");
            Assert.AreEqual(PartRef.Boosters, Advisories.PartFor(new Issue { code = "cluster-count", tab = "engines" }, 2, d));
            Assert.AreEqual(PartRef.Boosters, Advisories.PartFor(new Issue { code = "nozzle-unknown", tab = "engines" }, 2, d));
            Assert.AreEqual(PartRef.Stage(1), Advisories.PartFor(new Issue { code = "cluster-count", tab = "engines", index = 1 }, 2, d));
            var so = d.Obj("strapOns");
            JsonMut.Set(so, "propellant", "hydrolox"); JsonMut.Set(so, "material", "composite");
            Assert.AreEqual(PartRef.Boosters, Advisories.PartFor(new Issue { code = "cryo-composite", tab = "propellants" }, 2, d));
            JsonMut.Set(Stage(d, 1), "propellant", "hydrolox"); JsonMut.Set(Stage(d, 1), "material", "composite");
            Assert.AreEqual(PartRef.Stage(1), Advisories.PartFor(new Issue { code = "cryo-composite", tab = "propellants" }, 2, d), "stages are checked first");
            Assert.AreEqual(PartRef.Stage(0), Advisories.PartFor(new Issue { code = "twr-low", tab = "engines" }, 2, d));

            // End to end: a booster set with the wrong engine count for its pattern.
            var s = new HangarSession(C, H, HangarFixtures.Template("heavy"));
            s.Apply(x => { JsonMut.Set(x.Obj("strapOns").Obj("cluster"), "pattern", "ring7"); return new Edit("test"); });
            var a = s.Advisories.Find(x => x.code == "cluster-count");
            Assert.IsNotNull(a);
            Assert.AreEqual(PartRef.Boosters, a.part);
        }

        [Test]
        public void SwappingTheEngine_ClearsItsTune()
        {
            var d = HangarFixtures.Template("workhorse");
            HangarEditor.SetTune(d, PartRef.Stage(0), 10, 5);
            Assert.IsNotNull(Stage(d, 0).Obj("tune"));
            HangarEditor.SetEngine(C, d, PartRef.Stage(0), "sc-kerolox");
            Assert.IsNull(Stage(d, 0).Obj("tune"));
            HangarEditor.SetTune(d, PartRef.Stage(0), 10, 5);
            HangarEditor.SetPropellant(C, d, PartRef.Stage(0), "methalox");
            Assert.IsNull(Stage(d, 0).Obj("tune"), "a propellant change that swaps the engine clears it too");
        }

        [Test]
        public void NewBottomStage_DropsItsInterstageOverride()
        {
            var d = HangarFixtures.Template("workhorse");
            HangarEditor.SetInterstageMass(d, 1, 5000);
            HangarEditor.MoveStage(d, 1, 0);
            Assert.IsFalse(Stage(d, 0).ContainsKey("interstageMass"), "moved to the bottom");
            var e = HangarFixtures.Template("workhorse");
            HangarEditor.SetInterstageMass(e, 1, 5000);
            HangarEditor.RemoveStage(e, 0);
            Assert.IsFalse(Stage(e, 0).ContainsKey("interstageMass"), "the stage below was removed");
            var f = HangarFixtures.Template("workhorse");
            JsonMut.Set(Stage(f, 0), "interstageMass", 1500d);
            HangarEditor.AddStage(C, f, 2);
            Assert.AreEqual(1500, Stage(f, 0).Num("interstageMass"), "a stage that stays at the bottom keeps its value");
        }

        [Test]
        public void CustomNumericNames_AreKept()
        {
            var d = HangarFixtures.Template("workhorse");
            HangarEditor.SetStageName(d, 0, "Stage 42");
            HangarEditor.Normalize(d);
            Assert.AreEqual("Stage 42", Stage(d, 0).Str("name"), "opening a design never renames it");
            HangarEditor.AddStage(C, d, 0);
            Assert.AreEqual("Stage 42", Stage(d, 1).Str("name"), "structural edits keep custom names");
            Assert.AreEqual("Stage 1", Stage(d, 0).Str("name"));
            var e = HangarFixtures.Template("workhorse");
            HangarEditor.SetStageName(e, 0, "Stage 1"); HangarEditor.SetStageName(e, 1, "Stage 2");
            HangarEditor.MoveStage(e, 1, 0);
            Assert.AreEqual("Stage 1", Stage(e, 0).Str("name"), "position defaults follow the position");
            Assert.AreEqual("Stage 2", Stage(e, 1).Str("name"));
        }

        [Test]
        public void NonFiniteInterstageMass_IsRejected_AndApplyRollsBack()
        {
            var d = HangarFixtures.Template("workhorse");
            Assert.IsFalse(HangarEditor.SetInterstageMass(d, 1, double.NaN).changed);
            Assert.IsFalse(HangarEditor.SetInterstageMass(d, 1, double.PositiveInfinity).changed);
            var s = new HangarSession(C, H, HangarFixtures.Template("workhorse"));
            string before = HangarSession.Snapshot(s.Design);
            Assert.Throws<InvalidOperationException>(() => s.Apply(x => { JsonMut.Set(x, "safetyFactor", double.NaN); return new Edit("poison"); }));
            Assert.AreEqual(before, HangarSession.Snapshot(s.Design), "a value that can't be saved never sticks");
        }

        [Test]
        public void PartRows_ShowTunedThrustAndIsp()
        {
            var d = HangarFixtures.Template("workhorse");
            var plain = DataSheet.Build(C, H, d).parts.Find(p => p.part == PartRef.Stage(0));
            HangarEditor.SetTune(d, PartRef.Stage(0), 10, -5);
            var tuned = DataSheet.Build(C, H, d).parts.Find(p => p.part == PartRef.Stage(0));
            Assert.AreEqual(plain.thrustSL_N * 1.10, tuned.thrustSL_N, 1e-6);
            Assert.AreEqual(plain.ispVac_s * 0.95, tuned.ispVac_s, 1e-9);
        }

        [Test]
        public void Truncation_NeverSplitsASurrogatePair()
        {
            string name = new string('a', 19) + "\U0001F680"; // 19 letters then a rocket emoji (2 UTF-16 units)
            string o = HangarEditor.CleanText(name, 20);
            Assert.AreEqual(new string('a', 19), o);
            foreach (char ch in o) Assert.IsFalse(char.IsSurrogate(ch));
            Assert.AreEqual("ab\U0001F680", HangarEditor.CleanText("ab\U0001F680", 4));
        }
    }
}
