// Guided stepper shell: portrait layout, copy fallback, step stats, and the All-parts switch.
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Slice.DesignerV3;
using Kestrel.Slice.DesignerV3.Stepper;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class DesignerStepperUiTests
    {
        static Catalog catalog;
        static HangarData data;
        static string root;
        static string Root => root ??= Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
        static Catalog C => catalog ??= Catalog.LoadFromRepo(Root);
        static HangarData H => data ??= HangarData.LoadFromRepo(Root);
        static JsonObject Template(string id) => HangarEditor.ForkTemplate(H.Template(id));

        [SetUp]
        public void FreshCopy() => StepperCopy.Reset();

        [Test]
        public void Layout_FitsSeThroughIpad_PortraitOnly()
        {
            Assert.AreEqual(112f, StepperLayout.RocketBandPt);
            Assert.AreEqual(52f, StepperLayout.NavButtonPt);
            Assert.GreaterOrEqual(StepperLayout.SegmentMinPt, 96f);
            Assert.GreaterOrEqual(StepperLayout.NavButtonPt, StepperLayout.MinTargetPt);
            Assert.AreEqual(StepperLayout.SeSideInsetPt, StepperLayout.SideInset(375f));
            Assert.AreEqual(StepperLayout.SideInsetPt, StepperLayout.SideInset(768f));

            foreach (var (w, h, top, bottom) in new[]
            {
                (375f, 667f, 20f, 0f),
                (390f, 844f, 47f, 34f),
                (430f, 932f, 59f, 34f),
                (768f, 1024f, 24f, 20f),
                (1024f, 1366f, 24f, 20f),
            })
            {
                var frame = StepperLayout.Solve(w, h, top, bottom, 0f, 0f);
                Assert.IsTrue(frame.Portrait, w + "x" + h);
                Assert.IsTrue(frame.Fits, w + "x" + h);
                Assert.GreaterOrEqual(frame.ScrollPt, StepperLayout.MinScrollPt);
            }

            var landscape = StepperLayout.Solve(844f, 390f, 0f, 0f, 47f, 34f);
            Assert.IsFalse(landscape.Portrait);
            Assert.IsFalse(landscape.Fits);

            var seSafe = StepperLayout.Solve(375f, 667f, 47f, 34f, 8f, 8f);
            Assert.IsTrue(seSafe.Fits);
            Assert.Less(seSafe.ScrollPt, StepperLayout.Solve(375f, 667f, 0f, 0f, 0f, 0f).ScrollPt);
        }

        [Test]
        public void Copy_FileOverridesPlaceholders_AndBadJsonFallsBack()
        {
            var missing = StepperCopy.Parse(null);
            Assert.IsNull(missing.FileText("step.engines.why.kid"));
            StringAssert.Contains("push", missing.Resolve("step.engines.why.kid").ToLowerInvariant());
            Assert.AreEqual("", missing.Resolve("not.a.real.id"));

            var flat = StepperCopy.Parse("{\"strings\":{\"step.engines.why.kid\":{\"en\":\"Push comes from the engines.\"}}}");
            Assert.AreEqual("Push comes from the engines.", flat.Resolve("step.engines.why.kid"));
            Assert.AreEqual("Push comes from the engines.", flat.FileText("step.engines.why.kid"));

            var nested = StepperCopy.Parse(@"{
                ""steps"": { ""tanks"": { ""goal"": ""Reach the speed."", ""why"": { ""kid"": ""Fuel is heavy."", ""engineer"": ""Rocket equation."" } } },
                ""checks"": { ""dv-margin"": { ""kid"": ""Short on speed."", ""engineer"": ""Margin under zero."" } }
            }");
            Assert.AreEqual("Fuel is heavy.", nested.Resolve("step.tanks.why.kid"));
            Assert.AreEqual("Rocket equation.", nested.FileText("step.tanks.why.engineer"));
            Assert.AreEqual("Reach the speed.", nested.FileText("step.tanks.goal"));
            Assert.AreEqual("Short on speed.", nested.FileText("check.dv-margin.kid"));

            var list = StepperCopy.Parse("{\"steps\":[{\"id\":\"launch\",\"whyKid\":\"Read the risks.\"}]}");
            Assert.AreEqual("Read the risks.", list.Resolve("step.review.why.kid"));

            var bad = StepperCopy.Parse("{not json");
            Assert.IsNull(bad.FileText("step.engines.why.kid"));
            Assert.IsFalse(string.IsNullOrEmpty(bad.Resolve("step.look.why.kid")));
            Assert.DoesNotThrow(() => StepperCopy.Load());
        }

        [Test]
        public void Stats_AreLimitedToTheStep()
        {
            var design = Template("workhorse");
            var readout = Readout.Quick(C, design);
            var engines = StepperStats.Build(DesignerStep.Engines, readout, design, C);
            CollectionAssert.AreEqual(new[] { "liftoffTWR", "engineCount", "mixtureOffsetPct" }, engines.ConvertAll(s => s.Key));
            var tanks = StepperStats.Build(DesignerStep.Tanks, readout, design, C);
            CollectionAssert.AreEqual(new[] { "margin", "totalDeltaV", "maxQFraction", "safetyFactor" }, tanks.ConvertAll(s => s.Key));
            Assert.IsFalse(tanks.Exists(s => s.Key == "liftoffTWR"));
            Assert.AreEqual(4, StepperStats.Build(DesignerStep.Review, readout, design, C).Count);
            var blank = StepperStats.Build(DesignerStep.Review, null, design, C);
            Assert.AreEqual("—", blank.Find(s => s.Key == "cost").Value);
            Assert.AreEqual("—", blank.Find(s => s.Key == "maxQ").Value);
        }

        [Test]
        public void Stylesheet_DeclaresTheStepStates()
        {
            string path = Path.Combine(Root, "unity", "Assets", "UI Toolkit", "DesignerV3", "Stepper.uss");
            Assert.IsTrue(File.Exists(path));
            string text = File.ReadAllText(path);
            StringAssert.Contains(".designer-stepper", text);
            StringAssert.Contains(".stepper-seg--current", text);
            StringAssert.Contains(".stepper-seg--done", text);
            StringAssert.Contains(".stepper-forward--amber", text);
            StringAssert.Contains(".stepper-forward--fix", text);
            StringAssert.Contains("white-space: nowrap", text);
            StringAssert.Contains("#stepper-next", text);
            StringAssert.Contains(".stepper-portrait", text);
        }

        [Test]
        public void LegacyDesign_StaysOnAllParts_UntilGuidedSteps()
        {
            int prev = PlayerPrefs.GetInt(DesignerV3Panel.EngineerPref, 0);
            var session = new HangarSession(C, H, Template("workhorse"));
            var body = new VisualElement();
            var panel = new DesignerV3Panel(session, body, 2f);
            try
            {
                Assert.IsFalse(panel.Stepper.IsGuided);
                Assert.IsNull(session.Design.Obj(StepperProgress.Field));
                Assert.IsNotNull(body.Q("tab-bar"));
                Assert.IsNotNull(body.Q("launch-btn"));
                Assert.IsNotNull(body.Q<Button>("stepper-guided"));
                Assert.AreEqual(DisplayStyle.None, panel.Stepper.Root.style.display.value);
                Assert.AreEqual("STEP 1 OF 7", StepperCatalog.Header(DesignerStep.Mission));

                panel.SetEngineer(false);
                panel.Stepper.EnterGuided();
                Assert.IsTrue(panel.Stepper.IsGuided);
                Assert.AreEqual(DesignerStep.Mission, panel.Stepper.Step);
                Assert.AreEqual("guided", session.Design.Obj(StepperProgress.Field).Str("view"));
                Assert.AreEqual(DisplayStyle.None, body.Q("tab-bar").style.display.value);
                Assert.AreEqual(DisplayStyle.None, body.Q("step-rail").style.display.value);
                Assert.AreEqual(DisplayStyle.None, body.Q("step-goal").style.display.value);
                Assert.AreEqual(DisplayStyle.None, body.Q("dv-budget").style.display.value);
                Assert.AreEqual(DisplayStyle.Flex, panel.Stepper.Root.style.display.value);
                Assert.AreEqual("STEP 1 OF 7", panel.Stepper.Root.Q<Label>("stepper-count").text);
                Assert.IsNull(panel.Stepper.Root.Q("stepper-rocket"));
                Assert.IsNull(panel.Stepper.Root.Q("stepper-why"));
                Assert.IsNull(panel.Stepper.Root.Q("stepper-goal"));
                var check = panel.Stepper.Root.Q("stepper-check");
                var stepScroll = panel.Stepper.Root.Q("stepper-scroll");
                Assert.IsNotNull(check);
                Assert.Less(panel.Stepper.Root.IndexOf(check), panel.Stepper.Root.IndexOf(stepScroll));
                var recovery = panel.Stepper.Root.Q<Button>("stepper-seg-5");
                StringAssert.Contains("Recovery", recovery.text);
                StringAssert.DoesNotContain("\n", recovery.text);
                Assert.AreEqual(WhiteSpace.NoWrap, recovery.style.whiteSpace.value);
                Assert.GreaterOrEqual(recovery.style.minWidth.value.value, StepperLayout.SegmentMinPt * 2f - 0.1f);
                Assert.IsFalse(panel.Stepper.Root.Q<Button>("stepper-back").enabledSelf);
                Assert.IsFalse(panel.Stepper.Root.Q<Button>("stepper-seg-7").enabledSelf);
                Assert.AreEqual(104f, panel.Stepper.Root.Q<Button>("stepper-forward").style.minHeight.value.value, 0.1f);

                panel.SetEngineer(true);
                StringAssert.Contains("payload", panel.Stepper.Root.Q<Label>("stepper-goal-body").text.ToLowerInvariant());
                Assert.IsFalse(string.IsNullOrEmpty(panel.Stepper.Root.Q<Label>("stepper-why-kid").text));
                Assert.IsFalse(string.IsNullOrEmpty(panel.Stepper.Root.Q<Label>("stepper-why-engineer").text));
                Assert.IsNotNull(panel.Stepper.Root.Q("stepper-rocket"));
                Assert.AreEqual(224f, panel.Stepper.Root.Q("stepper-rocket").style.minHeight.value.value, 0.1f);

                panel.Stepper.ApplySafeArea(47f, 34f, 0f, 0f);
                Assert.AreEqual(94f, panel.Stepper.Root.style.paddingTop.value.value, 0.1f);
                Assert.AreEqual(68f, panel.Stepper.Root.style.paddingBottom.value.value, 0.1f);

                panel.Stepper.ShowAllParts();
                Assert.IsFalse(panel.Stepper.IsGuided);
                Assert.AreEqual("all-parts", session.Design.Obj(StepperProgress.Field).Str("view"));
                var tabs = body.Q("tab-bar").style.display;
                Assert.IsTrue(tabs.keyword == StyleKeyword.Null || tabs.value != DisplayStyle.None);
                var rail = body.Q("step-rail").style.display;
                Assert.IsTrue(rail.keyword == StyleKeyword.Null || rail.value != DisplayStyle.None);
                var goal = body.Q("step-goal").style.display;
                Assert.IsTrue(goal.keyword == StyleKeyword.Null || goal.value != DisplayStyle.None);
                var budget = body.Q("dv-budget").style.display;
                Assert.IsTrue(budget.keyword == StyleKeyword.Null || budget.value != DisplayStyle.None);
                Assert.IsNotNull(body.Q<Button>("stepper-guided"));
                Assert.IsNotNull(body.Q("launch-btn"));
            }
            finally
            {
                panel.Detach();
                PlayerPrefs.SetInt(DesignerV3Panel.EngineerPref, prev);
            }
        }

        [Test]
        public void Guided_JumpOnlyVisitsOpenSteps_AndBothViewsEditOneDesign()
        {
            int prev = PlayerPrefs.GetInt(DesignerV3Panel.EngineerPref, 0);
            var session = new HangarSession(C, H, Template("workhorse"));
            var panel = new DesignerV3Panel(session, new VisualElement(), 2f);
            try
            {
                panel.SetEngineer(true);
                panel.Stepper.EnterGuided();
                Assert.AreEqual(StepGate.Pass, panel.Stepper.Gate);
                Assert.IsFalse(panel.Stepper.PressJump(DesignerStep.Look));
                Assert.AreEqual(DesignerStep.Mission, panel.Stepper.Step);

                Assert.IsTrue(panel.Stepper.PressForward());
                Assert.AreEqual(DesignerStep.Engines, panel.Stepper.Step);
                StringAssert.Contains("push", panel.Stepper.Root.Q<Label>("stepper-why-kid").text.ToLowerInvariant());
                StringAssert.Contains("x 7", panel.Stepper.Root.Q<Label>("field-engineCount-value").text);

                session.Apply(d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), 8));
                Assert.AreEqual(DesignerStep.Engines, panel.Stepper.Step);
                StringAssert.Contains("x 8", panel.Stepper.Root.Q<Label>("field-engineCount-value").text);
                Assert.AreEqual(8, (int)((session.Design.Arr("stages")[0] as JsonObject).Num("engineCount")));

                Assert.IsTrue(panel.Stepper.PressJump(DesignerStep.Mission));
                Assert.AreEqual(DesignerStep.Mission, panel.Stepper.Step);
                Assert.IsTrue(panel.Stepper.Root.Q<Button>("stepper-seg-2").enabledSelf);
                Assert.IsFalse(panel.Stepper.PressJump(DesignerStep.Look));

                panel.Stepper.ShowAllParts();
                Assert.AreEqual(8, (int)((session.Design.Arr("stages")[0] as JsonObject).Num("engineCount")));
                Assert.IsNotNull(panel.Root.Q("tab-" + DesignerV3Tab.Engines));
            }
            finally
            {
                panel.Detach();
                PlayerPrefs.SetInt(DesignerV3Panel.EngineerPref, prev);
            }
        }

        [Test]
        public void Guided_RedFixIt_AmberNextAnyway_AndEngineerMore()
        {
            int prev = PlayerPrefs.GetInt(DesignerV3Panel.EngineerPref, 0);
            var session = new HangarSession(C, H, Template("workhorse"));
            var panel = new DesignerV3Panel(session, new VisualElement(), 2f);
            try
            {
                panel.SetEngineer(true);
                panel.Stepper.EnterGuided();
                Assert.IsTrue(panel.Stepper.PressForward());
                Assert.IsNotNull(panel.Stepper.Root.Q("stepper-more"));
                Assert.AreEqual(DisplayStyle.None, panel.Stepper.Root.Q("stepper-more-body").style.display.value);
                panel.Stepper.ToggleMore();
                Assert.IsTrue(panel.Stepper.MoreOpen);
                Assert.AreEqual(DisplayStyle.Flex, panel.Stepper.Root.Q("stepper-more-body").style.display.value);
                Assert.IsNotNull(panel.Stepper.Root.Q("field-pc"));

                panel.SetEngineer(false);
                Assert.IsNull(panel.Stepper.Root.Q("stepper-more"));
                Assert.IsNull(panel.Stepper.Root.Q("field-pc"));
                Assert.IsNotNull(panel.Stepper.Root.Q("field-propellant"));

                session.Apply(d => DesignerV3Edits.SetMixtureOffset(d, 0, 8));
                Assert.AreEqual(StepGate.Amber, panel.Stepper.Gate);
                Assert.AreEqual("Fix it", panel.Stepper.Root.Q<Button>("stepper-forward").text);
                Assert.IsTrue(panel.Stepper.Root.Q<Button>("stepper-forward").ClassListContains("stepper-forward--fix"));
                var anyway = panel.Stepper.Root.Q<Button>("stepper-next");
                Assert.AreEqual("Next anyway", anyway.text);
                Assert.AreEqual(DisplayStyle.Flex, anyway.style.display.value);
                Assert.IsTrue(anyway.ClassListContains("stepper-forward--amber"));
                panel.Stepper.PressForward();
                Assert.AreEqual(DesignerStep.Engines, panel.Stepper.Step);
                Assert.IsNotNull(panel.Stepper.Root.Q("stepper-confirm"));
                panel.Stepper.PressForward();
                Assert.AreEqual(DesignerStep.Tanks, panel.Stepper.Step);

                panel.Stepper.PressJump(DesignerStep.Engines);
                session.Apply(d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), 1));
                Assert.AreEqual(StepGate.Red, panel.Stepper.Gate);
                Assert.AreEqual("Fix it", panel.Stepper.Root.Q<Button>("stepper-forward").text);
                Assert.IsTrue(panel.Stepper.Root.Q<Button>("stepper-forward").ClassListContains("stepper-forward--fix"));
                Assert.AreEqual(DisplayStyle.None, panel.Stepper.Root.Q("stepper-next").style.display.value);
                int engines = (int)((session.Design.Arr("stages")[0] as JsonObject).Num("engineCount"));
                Assert.IsTrue(panel.Stepper.PressFix());
                Assert.AreEqual(DesignerStep.Engines, panel.Stepper.Step);
                Assert.Greater((int)((session.Design.Arr("stages")[0] as JsonObject).Num("engineCount")), engines);
                Assert.AreNotEqual("twr-low", StepperModel.Evaluate(C, session.Design)[DesignerStep.Engines].Code);
            }
            finally
            {
                panel.Detach();
                PlayerPrefs.SetInt(DesignerV3Panel.EngineerPref, prev);
            }
        }

        [Test]
        public void Guided_FuelEditKeepsStepScrollAndFocus()
        {
            int prev = PlayerPrefs.GetInt(DesignerV3Panel.EngineerPref, 0);
            var session = new HangarSession(C, H, Template("workhorse"));
            var panel = new DesignerV3Panel(session, new VisualElement(), 2f);
            try
            {
                panel.SetEngineer(false);
                panel.Stepper.EnterGuided();
                Assert.IsTrue(panel.Stepper.PressForward());
                if (panel.Stepper.Gate == StepGate.Amber) panel.Stepper.PressForward();
                Assert.IsTrue(panel.Stepper.PressForward());
                Assert.AreEqual(DesignerStep.Tanks, panel.Stepper.Step);

                var scroll = panel.Stepper.Root.Q<ScrollView>("stepper-scroll");
                var fuel = panel.Stepper.Root.Q("field-propMass");
                Assert.IsNotNull(scroll);
                Assert.IsNotNull(fuel);
                if (scroll.verticalScroller != null) scroll.verticalScroller.highValue = 4000f;
                panel.Stepper.NoteFocus("propMass");
                panel.Stepper.ScrollOffset = 180f;
                Assert.IsTrue(fuel.ClassListContains("stepper-focus"));

                double mass = (session.Design.Arr("stages")[0] as JsonObject).Num("propMass");
                session.Apply(d => HangarEditor.SetPropMass(d, PartRef.Stage(0), mass + 1000));

                var scrollAfter = panel.Stepper.Root.Q<ScrollView>("stepper-scroll");
                var fuelAfter = panel.Stepper.Root.Q("field-propMass");
                Assert.AreSame(scroll, scrollAfter);
                Assert.AreSame(fuel, fuelAfter);
                Assert.AreEqual(DesignerStep.Tanks, panel.Stepper.Step);
                Assert.AreEqual(180f, panel.Stepper.ScrollOffset, 0.01f);
                Assert.AreEqual(180f, scrollAfter.scrollOffset.y, 0.01f);
                Assert.IsTrue(fuelAfter.ClassListContains("stepper-focus"));
                Assert.AreEqual("propMass", panel.Stepper.FocusedField);
            }
            finally
            {
                panel.Detach();
                PlayerPrefs.SetInt(DesignerV3Panel.EngineerPref, prev);
            }
        }

        [Test]
        public void Guided_MaxQCheckIsPlainAndFixItStaysOnTheStep()
        {
            int prev = PlayerPrefs.GetInt(DesignerV3Panel.EngineerPref, 0);
            DesignerLimits.Load(Root);
            ReferenceAscent.Reset();
            var session = new HangarSession(C, H, Template("workhorse"));
            var panel = new DesignerV3Panel(session, new VisualElement(), 2f);
            try
            {
                panel.SetEngineer(false);
                panel.Stepper.EnterGuided();
                Assert.IsTrue(panel.Stepper.PressForward());
                if (panel.Stepper.Gate == StepGate.Amber) panel.Stepper.PressForward();
                Assert.IsTrue(panel.Stepper.PressForward());
                Assert.AreEqual(DesignerStep.Tanks, panel.Stepper.Step);

                var readout = StepperModel.Evaluate(C, session.Design).Readout;
                Assert.IsTrue(readout.qLim_kPa.HasValue);
                ReferenceAscent.Store(ReferenceAscent.CanonicalKey(C, session.Design), readout.qLim_kPa.Value * 1000.0 * 1.05, 11000, 3);
                panel.Stepper.Sync();
                Assert.AreEqual("maxq-high", StepperModel.Evaluate(C, session.Design)[DesignerStep.Tanks].Code);
                string body = panel.Stepper.Root.Q<Label>("stepper-check-body").text;
                string extra = panel.Stepper.Root.Q<Label>("stepper-check-extra").text;
                StringAssert.Contains("air", body.ToLowerInvariant());
                StringAssert.DoesNotContain("q_lim", body);
                StringAssert.DoesNotContain("q_lim", extra);
                StringAssert.Contains("kPa", extra);
                Assert.AreEqual("Fix it", panel.Stepper.Root.Q<Button>("stepper-forward").text);
                Assert.AreEqual(DisplayStyle.Flex, panel.Stepper.Root.Q("stepper-next").style.display.value);

                float scrollBefore = panel.Stepper.ScrollOffset;
                bool cleared = panel.Stepper.PressFix();
                Assert.AreEqual(DesignerStep.Tanks, panel.Stepper.Step);
                string note = panel.Stepper.Root.Q<Label>("stepper-check-body").text;
                StringAssert.DoesNotContain("q_lim", note);
                StringAssert.DoesNotContain("Lower the Max-Q", note);
                if (cleared)
                {
                    Assert.AreNotEqual("maxq-high", StepperModel.Evaluate(C, session.Design)[DesignerStep.Tanks].Code);
                    StringAssert.Contains(" to ", note);
                }
                else
                    Assert.That(note, Does.Match(@"\d"));
                Assert.AreEqual(scrollBefore, panel.Stepper.ScrollOffset, 0.01f);
            }
            finally
            {
                panel.Detach();
                ReferenceAscent.Reset();
                PlayerPrefs.SetInt(DesignerV3Panel.EngineerPref, prev);
            }
        }
    }
}
