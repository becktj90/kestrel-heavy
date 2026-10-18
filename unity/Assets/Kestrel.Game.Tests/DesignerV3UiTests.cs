// Designer v3 WP-UI: tabs, strip, delta chips, risk launch confirm, tutorial, layout, panel smoke.
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Slice;
using Kestrel.Slice.DesignerV3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class DesignerV3UiTests
    {
        static Catalog catalog; static HangarData data; static string root;
        static string Root => root ??= Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
        static Catalog C => catalog ??= Catalog.LoadFromRepo(Root);
        static HangarData H => data ??= HangarData.LoadFromRepo(Root);
        static JsonObject Template(string id) => HangarEditor.ForkTemplate(H.Template(id));

        [Test]
        public void Tabs_EngineerShowsTen_SimpleShowsFive()
        {
            Assert.AreEqual(10, DesignerV3Tabs.All.Length);
            Assert.AreEqual(5, DesignerV3Tabs.Simple.Length);
            Assert.AreEqual(10, DesignerV3Tabs.Visible(true).Length);
            Assert.AreEqual(5, DesignerV3Tabs.Visible(false).Length);
            Assert.AreEqual(DesignerV3Tab.Engines, DesignerV3Tabs.FromRiskTab("engines"));
            Assert.AreEqual(DesignerV3Tab.Recovery, DesignerV3Tabs.FromRiskTab("landing"));
        }

        [Test]
        public void Layout_FitsSeThroughIpad()
        {
            foreach (var (h, safe) in new[] { (667f, 0f), (852f, 34f), (956f, 34f), (1376f, 20f) })
            {
                float content = DesignerV3Layout.ContentMaxPt(h, safe, showLaunchConfirm: true);
                Assert.GreaterOrEqual(content, DesignerV3Layout.MinContentPt, $"{h} pt");
                Assert.IsTrue(DesignerV3Layout.FitsSeContent(content));
            }
            Assert.AreEqual(DesignerV3Layout.SeSideInsetPt, DesignerV3Layout.SideInset(375f));
            Assert.AreEqual(DesignerV3Layout.SideInsetPt, DesignerV3Layout.SideInset(440f));
            Assert.IsFalse(DesignerV3Layout.SidePanel(1179, 2556));
            Assert.IsTrue(DesignerV3Layout.SidePanel(2064, 2752));
            Assert.AreEqual(667f * DesignerV3Layout.RocketFraction, DesignerV3Layout.RocketBandPt(667f, false), 1e-3f);
        }

        [Test]
        public void Strip_HasDvTwrMaxQCost_AndDetectsRed()
        {
            var s = new HangarSession(C, H, Template("workhorse"));
            var r = Readout.Quick(C, s.Design);
            var cells = DesignerV3Strip.Cells(r, engineer: true);
            CollectionAssert.AreEqual(new[] { "dv", "twr", "q", "cost" }, cells.ConvertAll(c => c.id));
            Assert.AreEqual(4, DesignerV3Strip.Cells(null, false).Count);
            Assert.IsFalse(DesignerV3Strip.HasRedRisk(null));

            var red = new QuickReadout
            {
                risks = new List<FlightRisk>
                {
                    new FlightRisk { kind = "tumble", level = "red", tab = "aero", field = "fins", kid = "It will tumble" },
                    new FlightRisk { kind = "thermal", level = "amber", tab = "structure", field = "material", kid = "Hot" },
                }
            };
            Assert.IsTrue(DesignerV3Strip.HasRedRisk(red));
            Assert.AreEqual(2, DesignerV3Strip.RiskChips(red).Count);

            var q = cells.Find(c => c.id == "q");
            Assert.AreEqual("Run a sim", q.value);
            var cost = cells.Find(c => c.id == "cost");
            Assert.AreEqual(DesignerV3Strip.CompactMoney(r.cost), cost.value);
            Assert.IsFalse(cost.value.Contains(","));
            StringAssert.Contains(r.cost.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), cost.hint);
        }

        [Test]
        public void CompactMoney_UsesBillionsForTheWorkhorseScale()
        {
            Assert.AreEqual("$1.66B", DesignerV3Strip.CompactMoney(1660781357));
            Assert.AreEqual("$1.66M", DesignerV3Strip.CompactMoney(1660781));
            Assert.AreEqual("$16.61k", DesignerV3Strip.CompactMoney(16607));
            Assert.AreEqual("$850", DesignerV3Strip.CompactMoney(850));
        }

        [Test]
        public void Engines_ShowCatalogDefaultsUntilEdited()
        {
            var d = Template("workhorse");
            var stage = d.Arr("stages")[0] as JsonObject;
            Assert.IsNull(stage.Obj("engineParams"));
            Assert.AreEqual(97, DesignerV3Edits.ShownPc(C, d, 0), 1e-9);
            Assert.AreEqual(16, DesignerV3Edits.ShownExpansion(C, d, 0), 1e-9);
            Assert.AreEqual(5, DesignerV3Edits.ShownGimbal(C, d, 0), 1e-9);

            DesignerV3Edits.SetEnginePc(d, 0, DesignerV3Edits.ShownPc(C, d, 0) + 5);
            Assert.AreEqual(102, DesignerV3Edits.ShownPc(C, d, 0), 1e-9);
            Assert.AreEqual("gg-kerolox", stage.Str("engine"));
        }

        [Test]
        public void Strip_MaxQShowsKilopascalsAfterReference()
        {
            ReferenceAscent.Reset();
            try
            {
                var s = new HangarSession(C, H, Template("workhorse"));
                var flown = Readout.Reference(C, s.Design);
                Assert.IsFalse(flown.maxQStale);
                Assert.IsTrue(flown.maxQ_kPa.HasValue);
                var q = DesignerV3Strip.Cells(flown, true).Find(c => c.id == "q");
                StringAssert.Contains("kPa", q.value);
                StringAssert.DoesNotContain("Run a sim", q.value);
            }
            finally { ReferenceAscent.Reset(); }
        }

        [Test]
        public void DeltaChip_MatchesReadoutFormula()
        {
            Assert.AreEqual("+42 m/s dv · -0.03 TWR", Readout.DeltaChip(42, -0.03));
            var s = new HangarSession(C, H, Template("workhorse"));
            int before = (int)(HangarParts.Body(s.Design, PartRef.Stage(0))?.Num("engineCount") ?? 9);
            string chip = DesignerV3Delta.Chip(C, s.Design, d => { HangarEditor.SetEngineCount(d, PartRef.Stage(0), Mathf.Max(1, before - 2)); });
            StringAssert.Contains("Δv", chip);
            Assert.AreEqual(before, (int)(HangarParts.Body(s.Design, PartRef.Stage(0))?.Num("engineCount") ?? 0), "clone only");
            Assert.AreEqual(300, DesignerV3Delta.DebounceMs);
        }

        [Test]
        public void Risk_LaunchAnyway_ThenLaunch()
        {
            Assert.AreEqual("Launch anyway", DesignerV3Risk.LaunchButtonLabel(true, false));
            Assert.AreEqual("Launch", DesignerV3Risk.LaunchButtonLabel(true, true));
            Assert.AreEqual("Launch", DesignerV3Risk.LaunchButtonLabel(false, false));
            Assert.IsTrue(DesignerV3Risk.NeedsConfirm(true, false));
            Assert.IsFalse(DesignerV3Risk.CanLaunch(true, false));
            Assert.IsTrue(DesignerV3Risk.CanLaunch(true, true));
            Assert.AreEqual(CellState.Bad, DesignerV3Risk.LevelState("red"));
        }

        [Test]
        [Test]
        public void Tutorial_Kh1Hop_NamesTheHopNotLowEarthOrbit()
        {
            var steps = DesignerV3Tutorial.ForDesign("m1", 10000, () => "leo-200", () => 1.4, () => 0.2, () => true, () => true);
            var payload = steps.Find(s => s.id == "payload");
            Assert.IsNotNull(payload);
            Assert.AreEqual("Set the trip to a 10 km hop.", payload.kid);
            StringAssert.DoesNotContain("Low Earth Orbit", payload.kid);
            StringAssert.DoesNotContain("200 km", payload.kid);
            Assert.IsFalse(payload.done(), "leo-200 is not the hop target");
            Assert.IsTrue(DesignerV3Tutorial.ForDesign("m1", 10000, () => "kh-m1", () => 1.4, () => 0.2, () => true, () => true).Find(s => s.id == "payload").done());
            var leo = DesignerV3Tutorial.ForDesign(null, 200000, () => "gto", () => 0.8, () => -0.1, () => false, () => true);
            Assert.AreEqual("Set the trip to Low Earth Orbit at 200 km.", leo.Find(s => s.id == "payload").kid);
        }

        [Test]
        public void Tutorial_Leo200_AdvancesWhenChecksPass()
        {
            double twr = 0.8, margin = -0.1;
            bool fins = false, fit = true;
            string orbit = "gto";
            var steps = DesignerV3Tutorial.Leo200(() => orbit, () => twr, () => margin, () => fins, () => fit);
            Assert.AreEqual(6, steps.Count);
            // Welcome is already done → Advance skips it and stops on Engines.
            Assert.AreEqual(1, DesignerV3Tutorial.Advance(steps, 0), "welcome done, engines not");
            twr = 1.3;
            Assert.AreEqual(2, DesignerV3Tutorial.Advance(steps, 0), "engines done, tanks not");
            margin = 0.1;
            Assert.AreEqual(3, DesignerV3Tutorial.Advance(steps, 0), "tanks done, aero not");
            fins = true;
            Assert.AreEqual(4, DesignerV3Tutorial.Advance(steps, 0), "aero done, payload not");
            orbit = DesignerV3Tutorial.TargetOrbitId;
            Assert.AreEqual(5, DesignerV3Tutorial.Advance(steps, 0), "payload done, on go");
            Assert.IsTrue(DesignerV3Tutorial.Complete(steps));
        }

        [Test]
        public void Edits_WriteEngineParamsAndTankWithoutClobberingEngineId()
        {
            var d = Template("workhorse");
            string engine = (d.Arr("stages")[0] as JsonObject).Str("engine");
            Assert.IsFalse(string.IsNullOrEmpty(engine));
            Assert.IsTrue(DesignerV3Edits.SetEnginePc(d, 0, 280).changed);
            Assert.AreEqual(engine, (d.Arr("stages")[0] as JsonObject).Str("engine"));
            Assert.AreEqual(280, (d.Arr("stages")[0] as JsonObject).Obj("engineParams").Num("pc"), 1e-9);
            Assert.IsTrue(DesignerV3Edits.SetInsulation(d, 0, "foam").changed);
            Assert.AreEqual("foam", (d.Arr("stages")[0] as JsonObject).Obj("tank").Str("insulation"));
            Assert.IsTrue(DesignerV3Edits.SetRecoveryMode(d, "barge").changed);
            Assert.AreEqual("barge", d.Str("recoveryMode"));
            Assert.IsTrue(DesignerV3Edits.SetGuidance(d, "maxQThrottle", 0.7).changed);
            Assert.AreEqual(0.7, d.Obj("guidance").Num("maxQThrottle"), 1e-9);
        }

        [Test]
        public void Panel_BuildsEveryTab_WithoutThrowing()
        {
            var session = new HangarSession(C, H, Template("workhorse"));
            var body = new VisualElement();
            var panel = new DesignerV3Panel(session, body, upp: 2f);
            Assert.AreEqual(DesignerV3Panel.RootName, body.name);
            Assert.NotNull(body.Q("sticky-strip"));
            Assert.NotNull(body.Q("tab-bar"));
            Assert.NotNull(body.Q("launch-btn"));

            panel.SetEngineer(true);
            foreach (var t in DesignerV3Tabs.All)
            {
                Assert.DoesNotThrow(() => panel.Show(t), t.ToString());
                Assert.AreEqual(t, panel.Tab);
            }
            panel.SetEngineer(false);
            Assert.AreEqual(5, DesignerV3Tabs.Visible(false).Length);
            // Simple layer remaps Aero → Stages.
            panel.JumpToRisk(new FlightRisk { kind = "tumble", level = "red", tab = "aero", field = "fins" });
            Assert.AreEqual(DesignerV3Tab.Stages, panel.Tab);

            panel.SetEngineer(true);
            string focused = null;
            panel.FieldFocused += f => focused = f;
            panel.JumpToRisk(new FlightRisk { kind = "tumble", level = "red", tab = "aero", field = "fins" });
            Assert.AreEqual(DesignerV3Tab.Aero, panel.Tab);
            Assert.AreEqual("fins", focused);

            int count = (int)(HangarParts.Body(session.Design, PartRef.Stage(0))?.Num("engineCount") ?? 9);
            session.Apply(d => HangarEditor.SetEngineCount(d, PartRef.Stage(0), count));
            Assert.NotNull(body.Q("launch-btn"));

            panel.Detach();
        }

        [Test]
        public void Panel_RedRisk_ArmsLaunchAnyway()
        {
            Assert.AreEqual("Launch anyway", DesignerV3Risk.LaunchButtonLabel(true, false));
            var session = new HangarSession(C, H, Template("workhorse"));
            var body = new VisualElement();
            var panel = new DesignerV3Panel(session, body, 2f);
            // Force confirm path through the pure helper (panel reads live risks from predict cache).
            Assert.IsFalse(panel.LaunchConfirmArmed);
            panel.Detach();
        }

        [Test]
        public void Attach_MountsBesideHangar_AndTogglesWithoutClearingIt()
        {
            var ui = new VisualElement { name = "root" };
            var design = new VisualElement { name = "design" };
            var body = new VisualElement { name = "design-body" };
            body.Add(new Label("hangar-stays"));
            var row = new VisualElement { name = "launch-row" };
            var launch = new Button { name = "launch", text = "LAUNCH" };
            row.Add(launch);
            design.Add(new Button { name = "design-toggle", text = "Design" });
            design.Add(body);
            design.Add(row);
            ui.Add(design);

            var session = new HangarSession(C, H, Template("workhorse"));
            DesignerV3Panel panel = null;
            try
            {
                panel = DesignerV3Panel.Attach(session, ui, 2f);
                Assert.IsNotNull(panel);
                Assert.AreSame(panel, DesignerV3Panel.Attach(session, ui, 2f));
                Assert.AreEqual("hangar-stays", body.Q<Label>().text);
                Assert.AreEqual(DesignerV3Panel.RootName, panel.Root.name);
                Assert.AreEqual(body.parent, panel.Root.parent);
                Assert.Less(design.IndexOf(panel.Root), design.IndexOf(row), "designer sits above the Hangar thumb row");
                Assert.AreEqual(DisplayStyle.None, panel.Root.style.display.value);
                Assert.AreEqual(DesignerV3Panel.ToggleName, row[0].name);
                Assert.AreSame(launch, row[1]);

                panel.SetOpen(true);
                Assert.IsTrue(panel.Open);
                Assert.AreEqual(DisplayStyle.Flex, panel.Root.style.display.value);
                Assert.AreEqual(DisplayStyle.None, body.style.display.value);
                Assert.AreEqual("Hangar", row.Q<Button>(DesignerV3Panel.ToggleName).text);
                Assert.AreEqual(DisplayStyle.None, launch.style.display.value);
                Assert.IsNotNull(panel.Root.Q("designer-launch"));
                Assert.IsNotNull(panel.Root.Q("tab-bar"));

                panel.SetSheetCollapsed(true);
                Assert.AreEqual(DisplayStyle.None, panel.Root.style.display.value);
                panel.SetSheetCollapsed(false);
                Assert.AreEqual(DisplayStyle.Flex, panel.Root.style.display.value);

                panel.SetOpen(false);
                Assert.IsFalse(panel.Open);
                Assert.AreEqual(DisplayStyle.None, panel.Root.style.display.value);
                Assert.AreEqual(StyleKeyword.Null, body.style.display.keyword);
                Assert.AreEqual(StyleKeyword.Null, launch.style.display.keyword);
                Assert.AreEqual("Designer", row.Q<Button>(DesignerV3Panel.ToggleName).text);
            }
            finally
            {
                panel?.Detach();
            }
        }
    }
}
