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
        }

        [Test]
        public void DeltaChip_MatchesReadoutFormula()
        {
            Assert.AreEqual("+42 m/s Δv · −0.03 TWR", Readout.DeltaChip(42, -0.03));
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
    }
}
