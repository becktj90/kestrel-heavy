// Portrait designer v3 panel (§5–§6). Composes over HangarSession; does not edit HangarPanel.
// Sticky strip, 10-tab bar (Simple subset / Engineer full), delta chips, risk chips, Launch anyway,
// and the guided 200 km LEO tutorial.
// The guided stepper is one hook (AttachStepper): same design, All parts keeps these tabs.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Slice.DesignerV3.Stepper;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice.DesignerV3
{
    public sealed class DesignerV3Panel
    {
        public const string RootName = "designer-v3";
        public const string ToggleName = "open-designer";
        public const string EngineerPref = "kh.designer.v3.engineer";
        public const string TutorialPref = "kh.designer.v3.tutorial";

        readonly HangarSession session;
        readonly HangarUi ui;
        readonly VisualElement root, strip, riskRow, tabs, content, confirmRow, tutorialBanner;
        readonly ScrollView scroll;
        readonly List<Action> refreshers = new List<Action>();
        readonly Dictionary<DesignerV3Tab, Button> tabButtons = new Dictionary<DesignerV3Tab, Button>();
        readonly CultureInfo inv = CultureInfo.InvariantCulture;

        static DesignerV3Panel mounted;

        DesignerV3Tab tab = DesignerV3Tab.Engines;
        bool engineer;
        bool launchConfirmArmed;
        bool tutorialOn;
        bool sheetCollapsed;
        Button toggle;
        DesignerStepperShell stepper;
        int tutorialIndex;
        List<TutorialStep> tutorialSteps;
        string lastDelta = "";
        double debounceUntil;
        ReferenceJob referenceJob;
        string referenceKey;

        public event Action<DesignerV3Tab> TabChanged;
        public event Action<bool> EngineerChanged;
        public event Action LaunchRequested;
        public event Action<FlightRisk> RiskJump;
        public event Action<string> FieldFocused;

        public bool Engineer => engineer;
        public bool Open { get; private set; }
        public DesignerV3Tab Tab => tab;
        public bool TutorialOn => tutorialOn;
        public bool LaunchConfirmArmed => launchConfirmArmed;
        public VisualElement Root => root;
        public DesignerStepperShell Stepper => stepper;
        public string LastDeltaChip => lastDelta;

        Catalog C => session.catalog;
        JsonObject D => session.Design;

        public DesignerV3Panel(HangarSession session, VisualElement body, float upp)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            ui = new HangarUi { Upp = upp };
            engineer = PlayerPrefs.GetInt(EngineerPref, 0) != 0;
            tutorialOn = PlayerPrefs.GetInt(TutorialPref, 1) != 0;
            root = body ?? new VisualElement();
            root.name = RootName;
            root.Clear();
            root.AddToClassList("designer-v3");

            tutorialBanner = ui.Column();
            tutorialBanner.name = "tutorial-banner";
            root.Add(tutorialBanner);

            strip = ui.Row(0f);
            strip.name = "sticky-strip";
            strip.tooltip = "Live flight numbers.";
            root.Add(strip);

            riskRow = ui.Row(2f);
            riskRow.name = "risk-chips";
            root.Add(riskRow);

            tabs = ui.Row(2f);
            tabs.name = "tab-bar";
            tabs.style.flexWrap = Wrap.Wrap;
            tabs.style.width = Length.Percent(100);
            root.Add(tabs);

            scroll = new ScrollView(ScrollViewMode.Vertical) { name = "designer-scroll" };
            scroll.style.flexGrow = 1;
            content = ui.Column();
            content.name = "tab-content";
            scroll.Add(content);
            root.Add(scroll);

            confirmRow = ui.Row(2f);
            confirmRow.name = "designer-launch";
            root.Add(confirmRow);

            BuildTabs();
            session.Changed += OnChanged;
            RebuildTutorial();
            AttachStepper();
            Rebuild();
        }

        /// <summary>Guided seven-step shell. All parts is the tab bar already built above. Both edit this session.</summary>
        void AttachStepper()
        {
            var bridge = new DesignerStepperBridge
            {
                Session = session,
                Ui = ui,
                Engineer = () => engineer,
                SetEngineer = SetEngineer,
                Apply = (op, key) => Do(op, key),
                EndGesture = End,
                Focus = f => FieldFocused?.Invoke(f),
                Launch = () => LaunchRequested?.Invoke(),
                JumpRisk = JumpToRisk,
                LaunchArmed = () => launchConfirmArmed,
                SetLaunchArmed = v => launchConfirmArmed = v,
                Tab = () => tab,
                HideWhenGuided = new[] { tutorialBanner, strip, riskRow, tabs, scroll, confirmRow },
                Rebuild = () => Rebuild(),
            };
            stepper = new DesignerStepperShell(bridge);
            root.Add(stepper.Root);
        }

        /// <summary>
        /// One registration from SliceApp, same shape as FailureHost.Attach.
        /// Mounts this panel in the design sheet, directly above the Hangar thumb row,
        /// and adds a Designer / Hangar toggle so the player can reach it without replacing Hangar G3.
        /// </summary>
        public static DesignerV3Panel Attach(HangarSession session, VisualElement ui, float upp)
        {
            if (session == null || ui == null) return null;
            var design = ui.Q("design");
            var body = design != null ? design.Q("design-body") : null;
            if (design == null || body == null) return null;

            if (mounted != null && ReferenceEquals(mounted.session, session) && ui.Q(RootName) == mounted.root)
            {
                mounted.SetScale(upp);
                return mounted;
            }
            if (mounted != null) mounted.Detach();

            var host = new VisualElement { name = RootName };
            host.style.display = DisplayStyle.None;
            host.style.flexShrink = 1;
            VisualElement chrome = null;
            foreach (var child in design.Children())
                if (child.name == "launch-row") { chrome = child; break; }
            if (chrome != null) design.Insert(design.IndexOf(chrome), host);
            else
            {
                int at = design.IndexOf(body);
                if (at >= 0) design.Insert(at + 1, host);
                else design.Add(host);
            }

            var panel = new DesignerV3Panel(session, host, upp);
            if (chrome != null && chrome.Q(ToggleName) == null)
            {
                var open = new Button(() => panel.SetOpen(!panel.Open))
                {
                    name = ToggleName,
                    text = "Designer",
                    tooltip = "Open the vehicle designer, next to the Hangar.",
                };
                open.AddToClassList("tool");
                var launch = chrome.Q("launch");
                if (launch != null) chrome.Insert(chrome.IndexOf(launch), open);
                else chrome.Add(open);
                panel.toggle = open;
            }
            panel.SetOpen(false);
            mounted = panel;
            return panel;
        }

        public void Detach()
        {
            session.Changed -= OnChanged;
            if (mounted != this) return;
            toggle?.RemoveFromHierarchy();
            var body = root != null && root.parent != null ? root.parent.Q("design-body") : null;
            if (body != null && body != root) body.style.display = StyleKeyword.Null;
            root?.RemoveFromHierarchy();
            mounted = null;
        }

        /// <summary>Show the designer in the Hangar sheet, or return the sheet to Hangar G3.</summary>
        public void SetOpen(bool on)
        {
            Open = on;
            ApplyVisibility();
        }

        /// <summary>The design sheet handle collapsed. Keeps the open/closed choice for when it expands.</summary>
        public void SetSheetCollapsed(bool collapsed)
        {
            sheetCollapsed = collapsed;
            ApplyVisibility();
        }

        void ApplyVisibility()
        {
            if (root == null) return;
            root.style.display = Open && !sheetCollapsed ? DisplayStyle.Flex : DisplayStyle.None;
            var body = root.parent != null ? root.parent.Q("design-body") : null;
            if (body != null && body != root)
                body.style.display = Open ? DisplayStyle.None : StyleKeyword.Null;
            if (toggle != null)
            {
                toggle.text = Open ? "Hangar" : "Designer";
                toggle.tooltip = Open
                    ? "Back to the Hangar."
                    : "Open the vehicle designer, next to the Hangar.";
                var launch = toggle.parent != null ? toggle.parent.Q<Button>("launch") : null;
                if (launch != null) launch.style.display = Open ? DisplayStyle.None : StyleKeyword.Null;
            }
        }

        public void SetScale(float upp)
        {
            if (Mathf.Approximately(upp, ui.Upp)) return;
            ui.Upp = upp;
            BuildTabs();
            Rebuild();
        }

        public void SetContentMax(float units)
        {
            scroll.style.maxHeight = units > 0 ? units : StyleKeyword.Null;
            stepper?.SetContentMax(units);
        }

        public void SetEngineer(bool on)
        {
            if (engineer == on) return;
            engineer = on;
            PlayerPrefs.SetInt(EngineerPref, on ? 1 : 0);
            if (!engineer && Array.IndexOf(DesignerV3Tabs.Simple, tab) < 0) tab = DesignerV3Tab.Engines;
            EngineerChanged?.Invoke(engineer);
            BuildTabs();
            Rebuild();
        }

        public void Show(DesignerV3Tab next)
        {
            if (!engineer && Array.IndexOf(DesignerV3Tabs.Simple, next) < 0)
            {
                // Kid layer: map engineer-only tabs onto the closest Simple tab.
                next = next switch
                {
                    DesignerV3Tab.Propellant => DesignerV3Tab.Tanks,
                    DesignerV3Tab.Aero => DesignerV3Tab.Stages,
                    DesignerV3Tab.Structure => DesignerV3Tab.Stages,
                    DesignerV3Tab.Guidance => DesignerV3Tab.Stages,
                    DesignerV3Tab.Payload => DesignerV3Tab.Stages,
                    _ => DesignerV3Tab.Engines,
                };
            }
            if (tab == next) return;
            tab = next;
            launchConfirmArmed = false;
            TabChanged?.Invoke(tab);
            Rebuild();
        }

        public void JumpToRisk(FlightRisk risk)
        {
            if (risk == null) return;
            Show(DesignerV3Tabs.FromRiskTab(risk.tab));
            if (!string.IsNullOrEmpty(risk.field)) FieldFocused?.Invoke(risk.field);
            RiskJump?.Invoke(risk);
        }

        public void SetTutorial(bool on)
        {
            tutorialOn = on;
            PlayerPrefs.SetInt(TutorialPref, on ? 1 : 0);
            RebuildTutorial();
            Rebuild();
        }

        void OnChanged()
        {
            launchConfirmArmed = false;
            if (tutorialOn) tutorialIndex = DesignerV3Tutorial.Advance(tutorialSteps, tutorialIndex);
            Rebuild();
        }

        void RebuildTutorial()
        {
            tutorialSteps = DesignerV3Tutorial.Leo200(
                () => D.Obj("payload")?.Str("orbit"),
                () => session.Sheet != null ? Readout.Quick(C, D).liftoffTWR : 0,
                () => Readout.Quick(C, D).margin,
                () => D.Obj("aero")?.Bool("fins") ?? false,
                () => { var r = Readout.Quick(C, D); return !r.fairingFits.HasValue || r.fairingFits.Value; });
            tutorialIndex = DesignerV3Tutorial.Advance(tutorialSteps, 0);
        }

        void BuildTabs()
        {
            tabs.Clear();
            tabButtons.Clear();
            foreach (var t in DesignerV3Tabs.Visible(engineer))
            {
                var captured = t;
                var b = ui.Btn(DesignerV3Tabs.Label(t, engineer), () => Show(captured), DesignerV3Tabs.VoiceOver(t), minWidthPt: 56f);
                b.name = "tab-" + t;
                tabButtons[t] = b;
                tabs.Add(b);
            }
            var mode = ui.Btn(engineer ? "Engineer" : "Simple", () => SetEngineer(!engineer),
                engineer ? "Switch to Simple view" : "Switch to Engineer view", minWidthPt: 72f);
            mode.name = "mode-toggle";
            tabs.Add(mode);
        }

        void Rebuild()
        {
            refreshers.Clear();
            content.Clear();
            BuildTutorialBanner();
            switch (tab)
            {
                case DesignerV3Tab.Propellant: BuildPropellant(); break;
                case DesignerV3Tab.Engines: BuildEngines(); break;
                case DesignerV3Tab.Tanks: BuildTanks(); break;
                case DesignerV3Tab.Stages: BuildStages(); break;
                case DesignerV3Tab.Aero: BuildAero(); break;
                case DesignerV3Tab.Structure: BuildStructure(); break;
                case DesignerV3Tab.Guidance: BuildGuidance(); break;
                case DesignerV3Tab.Recovery: BuildRecovery(); break;
                case DesignerV3Tab.Payload: BuildPayload(); break;
                case DesignerV3Tab.Look: BuildLook(); break;
            }
            RefreshChrome();
        }

        void RefreshChrome()
        {
            foreach (var kv in tabButtons) ui.SetChip(kv.Value, kv.Key == tab);
            BuildStrip();
            BuildRisks();
            BuildLaunchRow();
            foreach (var r in refreshers) r();
            stepper?.Sync();
        }

        void BuildTutorialBanner()
        {
            tutorialBanner.Clear();
            tutorialBanner.style.display = tutorialOn ? DisplayStyle.Flex : DisplayStyle.None;
            if (!tutorialOn || tutorialSteps == null || tutorialSteps.Count == 0) return;
            var step = tutorialSteps[Math.Min(tutorialIndex, tutorialSteps.Count - 1)];
            tutorialBanner.Add(ui.Text(engineer ? step.engineer : step.kid, HangarUi.BodyPt, bold: true));
            var row = ui.Row();
            row.Add(ui.Btn("Show me", () => Show(step.tab), "Jump to the tutorial tab"));
            if (!string.IsNullOrEmpty(step.field)) FieldFocused?.Invoke(step.field);
            row.Add(ui.Btn("Skip tour", () => SetTutorial(false), "Hide the guided tour"));
            tutorialBanner.Add(row);
            tutorialBanner.tooltip = "Tutorial step " + (tutorialIndex + 1) + " of " + tutorialSteps.Count;
        }

        void BuildStrip()
        {
            strip.Clear();
            var readout = Readout.Quick(C, D);
            foreach (var c in DesignerV3Strip.Cells(readout, engineer))
            {
                var cell = ui.Column();
                cell.name = "strip-" + c.id;
                cell.style.flexGrow = 1;
                cell.style.flexBasis = 0;
                cell.style.alignItems = Align.Center;
                cell.Add(ui.Text(c.label, HangarUi.SmallPt, HangarUi.Dim));
                var v = ui.Text(c.value, engineer ? HangarUi.BodyPt : HangarUi.TitlePt, StateColor(c.state), bold: true);
                v.name = "strip-value-" + c.id;
                v.style.unityTextAlign = TextAnchor.MiddleCenter;
                v.style.whiteSpace = WhiteSpace.NoWrap;
                cell.Add(v);
                cell.tooltip = c.label + " " + c.value + (c.hint != null ? ", " + c.hint : "");
                strip.Add(cell);
            }
            if (!string.IsNullOrEmpty(lastDelta))
            {
                var chip = ui.Text(lastDelta, HangarUi.SmallPt, HangarUi.ChipOn, bold: true);
                chip.name = "delta-chip";
                chip.tooltip = "Change from the last edit.";
                strip.Add(chip);
            }
        }

        void BuildRisks()
        {
            riskRow.Clear();
            var readout = Readout.Quick(C, D);
            var chips = DesignerV3Strip.RiskChips(readout);
            if (chips.Count == 0)
            {
                riskRow.style.display = DisplayStyle.None;
                return;
            }
            riskRow.style.display = DisplayStyle.Flex;
            foreach (var risk in chips)
            {
                var r = risk;
                var b = ui.Btn(DesignerV3Risk.ChipLabel(r, engineer), () => JumpToRisk(r), DesignerV3Risk.ChipTooltip(r), minWidthPt: 64f);
                b.name = "risk-" + (r.kind ?? "x");
                b.style.backgroundColor = DesignerV3Risk.LevelState(r.level) == CellState.Bad ? HangarUi.Red : HangarUi.Amber;
                riskRow.Add(b);
            }
        }

        void BuildLaunchRow()
        {
            confirmRow.Clear();
            var readout = Readout.Quick(C, D);
            bool red = DesignerV3Strip.HasRedRisk(readout);
            string label = DesignerV3Risk.LaunchButtonLabel(red, launchConfirmArmed);
            var b = ui.Btn(label, () =>
            {
                if (DesignerV3Risk.NeedsConfirm(red, launchConfirmArmed))
                {
                    launchConfirmArmed = true;
                    BuildLaunchRow();
                    return;
                }
                LaunchRequested?.Invoke();
            }, red ? DesignerV3Risk.ConfirmTitle : "Launch this design", minWidthPt: 120f);
            b.name = "launch-btn";
            if (red && !launchConfirmArmed) b.style.backgroundColor = HangarUi.Amber;
            confirmRow.Add(b);
        }

        static Color StateColor(CellState s) =>
            s == CellState.Bad ? HangarUi.Red : s == CellState.Warn ? HangarUi.Amber : s == CellState.Good ? HangarUi.Green : HangarUi.TextColor;

        void Do(Func<JsonObject, Edit> op, string key = null)
        {
            var before = Readout.Quick(C, D);
            try { session.Apply(op, key); }
            catch (Exception e) { Debug.LogWarning("[DesignerV3] edit failed: " + e.Message); return; }
            var after = Readout.Quick(C, D);
            lastDelta = DesignerV3Delta.FromReadouts(before, after);
            debounceUntil = Time.realtimeSinceStartupAsDouble + DesignerV3Delta.DebounceMs / 1000.0;
        }

        void End() => session.EndGesture();

        JsonObject Stage0() => D.Arr("stages")?[0] as JsonObject;

        // ---- tabs ----

        void BuildPropellant()
        {
            content.Add(ui.Section(engineer ? "Mixture" : "Fuel mix"));
            content.Add(ui.Stepper("Mixture offset", () => (Stage0()?.Num("mixtureOffsetPct") ?? 0).ToString("0.0", inv) + "%",
                () => engineer ? "±10%. Beyond ±6% risks instability." : "Keep near zero for a clean burn",
                (dir, key) => Do(d => DesignerV3Edits.SetMixtureOffset(d, 0, (StageNum(d, "mixtureOffsetPct") ?? 0) + dir), key),
                End, "mix", refreshers));
            AttachDelta("mixtureOffsetPct", d => DesignerV3Edits.SetMixtureOffset(d, 0, (StageNum(d, "mixtureOffsetPct") ?? 0) + 1));
        }

        void BuildEngines()
        {
            var p = PartRef.Stage(0);
            content.Add(ui.Section(engineer ? "Stage 1 engines" : "Engines (the push)"));
            var engines = new List<HangarUi.Choice>();
            foreach (var e in HangarEditor.EngineChoices(C, D, p))
                engines.Add(new HangarUi.Choice(e.id, HangarFormat.EngineChip(e)));
            content.Add(ui.Chips("Engine", engines, () => Stage0()?.Str("engine"), id => Do(d => HangarEditor.SetEngine(C, d, p, id)), refreshers));
            content.Add(ui.Stepper("Count", () => ((int)(Stage0()?.Num("engineCount") ?? 1)).ToString(inv),
                () => null,
                (dir, key) => Do(d => HangarEditor.SetEngineCount(d, p, (int)(HangarParts.Body(d, p)?.Num("engineCount") ?? 1) + dir), key),
                End, "eng.count", refreshers));
            if (engineer)
            {
                content.Add(ui.Stepper("Chamber pressure", () => DesignerV3Edits.ShownPc(C, D, 0).ToString("0", inv) + " bar",
                    () => "Higher pc: more thrust and Isp, overpressure risk",
                    (dir, key) => Do(d => DesignerV3Edits.SetEnginePc(d, 0, DesignerV3Edits.ShownPc(C, d, 0) + dir * 5), key),
                    End, "eng.pc", refreshers));
                content.Add(ui.Stepper("Expansion ratio", () => DesignerV3Edits.ShownExpansion(C, D, 0).ToString("0", inv),
                    () => "Bigger ε: better vacuum, worse pad",
                    (dir, key) => Do(d => DesignerV3Edits.SetExpansionRatio(d, 0, DesignerV3Edits.ShownExpansion(C, d, 0) + dir * 2), key),
                    End, "eng.eps", refreshers));
                content.Add(ui.Stepper("Gimbal", () => DesignerV3Edits.ShownGimbal(C, D, 0).ToString("0.0", inv) + "°",
                    () => "Control authority vs mass",
                    (dir, key) => Do(d => DesignerV3Edits.SetGimbal(d, 0, DesignerV3Edits.ShownGimbal(C, d, 0) + dir * 0.5), key),
                    End, "eng.gimbal", refreshers));
            }
        }

        void BuildTanks()
        {
            var p = PartRef.Stage(0);
            content.Add(ui.Section(engineer ? "Stage 1 tank" : "Fuel tank"));
            content.Add(ui.Stepper("Fuel load", () => HangarFormat.Tons(Stage0()?.Num("propMass") ?? 0),
                () => engineer ? null : "More fuel goes further but weighs more",
                (dir, key) => Do(d => HangarEditor.SetPropMass(d, p, HangarFormat.StepProp(HangarParts.Body(d, p)?.Num("propMass") ?? 0, dir)), key),
                End, "tank.prop", refreshers));
            if (engineer)
            {
                content.Add(ui.Stepper("Pressure", () => (Stage0()?.Obj("tank")?.Num("pressureBar") ?? 3).ToString("0.0", inv) + " bar",
                    () => "Hoop stress sets wall mass",
                    (dir, key) => Do(d => DesignerV3Edits.SetTankPressure(d, 0, ((d.Arr("stages")?[0] as JsonObject)?.Obj("tank")?.Num("pressureBar") ?? 3) + dir * 0.5), key),
                    End, "tank.p", refreshers));
                var ins = new List<HangarUi.Choice>
                {
                    new HangarUi.Choice("none", "None"), new HangarUi.Choice("foam", "Foam"), new HangarUi.Choice("mli", "MLI"),
                };
                content.Add(ui.Chips("Insulation", ins, () => Stage0()?.Obj("tank")?.Str("insulation") ?? "none",
                    id => Do(d => DesignerV3Edits.SetInsulation(d, 0, id)), refreshers));
                content.Add(ui.Switch("Common bulkhead", "Saves mass; watch Δp",
                    () => Stage0()?.Obj("tank")?.Bool("commonBulkhead") ?? false,
                    on => Do(d => DesignerV3Edits.SetCommonBulkhead(d, 0, on)), refreshers));
            }
        }

        void BuildStages()
        {
            content.Add(ui.Section("Stack"));
            int n = D.Arr("stages")?.Count ?? 0;
            content.Add(ui.Text(n + " core stage" + (n == 1 ? "" : "s"), HangarUi.BodyPt));
            if (n < DesignerLimits.Active.stagesMax)
                content.Add(ui.Btn("+ Stage on top", () => Do(d => HangarEditor.AddStage(C, d, n)), "Add a stage"));
            if (n > DesignerLimits.Active.stagesMin)
                content.Add(ui.Btn("Remove top stage", () => Do(d => HangarEditor.RemoveStage(d, n - 1)), "Remove uppermost stage"));
            var counts = new List<HangarUi.Choice>();
            foreach (var k in DesignerLimits.Active.strapOnCounts)
                counts.Add(new HangarUi.Choice(k.ToString(inv), k == 0 ? "None" : k.ToString(inv)));
            content.Add(ui.Chips(engineer ? "Strap-ons" : "Side boosters", counts,
                () => HangarParts.BoosterCount(D).ToString(inv),
                id => Do(d => HangarEditor.SetBoosterCount(C, d, int.Parse(id, inv))), refreshers));
            if (engineer)
            {
                var seps = new List<HangarUi.Choice>
                {
                    new HangarUi.Choice("cold", "Cold"), new HangarUi.Choice("hot", "Hot"), new HangarUi.Choice("springs", "Springs"),
                };
                content.Add(ui.Chips("Separation", seps, () => Stage0()?.Str("sepMode") ?? "cold",
                    id => Do(d => DesignerV3Edits.SetSepMode(d, 0, id)), refreshers));
            }
        }

        void BuildAero()
        {
            content.Add(ui.Section(engineer ? "Aerodynamics" : "Shape"));
            content.Add(ui.Switch("Fins", "Steady in the air", () => D.Obj("aero")?.Bool("fins") ?? false,
                on => Do(d => HangarEditor.SetFins(d, on)), refreshers));
            if (D.Obj("aero")?.Bool("fins") == true)
            {
                content.Add(ui.Stepper("Fin count", () => ((int)(D.Obj("aero")?.Num("finCount", 4) ?? 4)).ToString(inv), null,
                    (dir, key) => Do(d => HangarEditor.SetFinCount(d, (int)(d.Obj("aero")?.Num("finCount", 4) ?? 4) + dir), key),
                    End, "fins.count", refreshers));
            }
            var noses = new List<HangarUi.Choice>();
            foreach (var id in new[] { "ogive", "conic", "blunt", "von-karman", "biconic", "hammerhead" })
                noses.Add(new HangarUi.Choice(id, HangarFormat.Cap(id)));
            content.Add(ui.Chips("Nose", noses, () => D.Obj("aero")?.Str("nose") ?? "ogive",
                id => Do(d => HangarEditor.SetNose(d, id)), refreshers));
        }

        void BuildStructure()
        {
            content.Add(ui.Section(engineer ? "Structure" : "Strength"));
            var mats = new List<HangarUi.Choice>();
            foreach (var m in C.Materials) mats.Add(new HangarUi.Choice(m.id, m.label));
            content.Add(ui.Chips("Material", mats, () => Stage0()?.Str("material"),
                id => Do(d => HangarEditor.SetMaterial(C, d, PartRef.Stage(0), id)), refreshers));
            content.Add(ui.Stepper("Safety factor", () => D.Num("safetyFactor", 1.4).ToString("0.00", inv),
                () => "Stronger structure weighs more",
                (dir, key) => Do(d => HangarEditor.SetSafetyFactor(d, d.Num("safetyFactor", 1.4) + dir * DesignerLimits.Active.safetyStep), key),
                End, "sf", refreshers));
            content.Add(ui.Chips("Avionics", Ids(HangarEditor.AvionicsTiers), () => D.Obj("avionics")?.Str("tier"),
                id => Do(d => HangarEditor.SetAvionics(d, id)), refreshers));
        }

        void BuildGuidance()
        {
            content.Add(ui.Section("Guidance profile"));
            content.Add(ui.Stepper("Pitch kick alt", () => (D.Obj("guidance")?.Num("pitchKickAlt") ?? 100).ToString("0", inv) + " m",
                () => "Start of the gravity turn",
                (dir, key) => Do(d => DesignerV3Edits.SetGuidance(d, "pitchKickAlt", (d.Obj("guidance")?.Num("pitchKickAlt") ?? 100) + dir * 50), key),
                End, "g.alt", refreshers));
            content.Add(ui.Stepper("Pitch kick", () => (D.Obj("guidance")?.Num("pitchKickDeg") ?? 2).ToString("0.0", inv) + "°",
                () => null,
                (dir, key) => Do(d => DesignerV3Edits.SetGuidance(d, "pitchKickDeg", (d.Obj("guidance")?.Num("pitchKickDeg") ?? 2) + dir * 0.5), key),
                End, "g.kick", refreshers));
            content.Add(ui.Stepper("Max-Q throttle", () => ((D.Obj("guidance")?.Num("maxQThrottle") ?? 1) * 100).ToString("0", inv) + "%",
                () => "Throttle bucket through max-Q",
                (dir, key) => Do(d => DesignerV3Edits.SetGuidance(d, "maxQThrottle", Math.Max(0.4, Math.Min(1, (d.Obj("guidance")?.Num("maxQThrottle") ?? 1) + dir * 0.05))), key),
                End, "g.q", refreshers));
            content.Add(ui.Stepper("g limit", () => (D.Obj("guidance")?.Num("gLimit") ?? 4).ToString("0.0", inv) + " g",
                () => "Autopilot caps sensed acceleration",
                (dir, key) => Do(d => DesignerV3Edits.SetGuidance(d, "gLimit", (d.Obj("guidance")?.Num("gLimit") ?? 4) + dir * 0.5), key),
                End, "g.g", refreshers));
        }

        void BuildRecovery()
        {
            content.Add(ui.Section(engineer ? "Recovery" : "Landing"));
            content.Add(ui.Switch("Landing legs", null, () => D.Obj("landing")?.Bool("legs") ?? false,
                on => Do(d => HangarEditor.SetLegs(d, on)), refreshers));
            content.Add(ui.Switch("Grid fins", "Steer on the way down", () => D.Obj("landing")?.Bool("gridFins") ?? false,
                on => Do(d => HangarEditor.SetGridFins(d, on)), refreshers));
            var modes = new List<HangarUi.Choice>
            {
                new HangarUi.Choice("expendable", "Expend"),
                new HangarUi.Choice("rtls", "Return to pad"),
                new HangarUi.Choice("barge", "Haven barge"),
            };
            content.Add(ui.Chips(engineer ? "Recovery mode" : "Where it lands", modes,
                () => D.Str("recoveryMode") ?? "expendable",
                id => Do(d => DesignerV3Edits.SetRecoveryMode(d, id)), refreshers));
            if (engineer)
            {
                var legs = new List<HangarUi.Choice>
                {
                    new HangarUi.Choice("light", "Light (2 m/s)"), new HangarUi.Choice("heavy", "Heavy (4 m/s)"),
                };
                content.Add(ui.Chips("Legs", legs, () => D.Obj("landing")?.Str("legsKind") ?? "light",
                    id => Do(d => DesignerV3Edits.SetLegsKind(d, id)), refreshers));
            }
        }

        void BuildPayload()
        {
            content.Add(ui.Section(engineer ? "Payload" : "Cargo"));
            var payloads = new List<HangarUi.Choice>();
            foreach (var pl in C.Payloads) payloads.Add(new HangarUi.Choice(pl.id, pl.label ?? pl.id));
            content.Add(ui.Chips("Type", payloads, () => D.Obj("payload")?.Str("type"),
                id => Do(d => HangarEditor.SetPayloadType(C, d, id)), refreshers));
            var orbits = new List<HangarUi.Choice>();
            foreach (var o in C.Orbits.Values) orbits.Add(new HangarUi.Choice(o.id, o.label ?? o.id));
            content.Add(ui.Chips("Orbit", orbits, () => D.Obj("payload")?.Str("orbit"),
                id => Do(d => HangarEditor.SetOrbit(C, d, id)), refreshers));
            content.Add(ui.Stepper("Mass", () => HangarFormat.Kg(D.Obj("payload")?.Num("mass") ?? 0), null,
                (dir, key) => Do(d => HangarEditor.SetPayloadMass(d, (d.Obj("payload")?.Num("mass") ?? 0) + dir * 100), key),
                End, "pl.mass", refreshers));
        }

        void BuildLook()
        {
            content.Add(ui.Section("Livery"));
            var primary = D.Obj("livery")?.Str("primary") ?? "#f2f2f2";
            var accent = D.Obj("livery")?.Str("accent") ?? "#1ec8ff";
            content.Add(ui.Text("Primary " + primary, HangarUi.SmallPt, HangarUi.Dim));
            content.Add(ui.Text("Accent " + accent, HangarUi.SmallPt, HangarUi.Dim));
            content.Add(ui.Btn("Swap cyan accent", () => Do(d => HangarEditor.SetPaint(d, primary, "#1ec8ff")), "Set Beckify cyan accent"));
            if (engineer)
            {
                var nameField = new TextField { value = D.Str("name") ?? "", maxLength = 32 };
                nameField.label = "Vehicle name";
                nameField.isDelayed = true;
                nameField.RegisterValueChangedCallback(e => Do(d => HangarEditor.SetVehicleName(d, e.newValue)));
                content.Add(nameField);
            }
        }

        void AttachDelta(string field, Action<JsonObject> preview)
        {
            var chip = ui.Text("", HangarUi.SmallPt, HangarUi.ChipOn);
            chip.name = "delta-" + field;
            content.Add(chip);
            refreshers.Add(() =>
            {
                if (Time.realtimeSinceStartupAsDouble < debounceUntil && !string.IsNullOrEmpty(lastDelta))
                { chip.text = lastDelta; return; }
                chip.text = DesignerV3Delta.Chip(C, D, preview);
            });
        }

        static double? StageNum(JsonObject d, string key) => (d.Arr("stages")?[0] as JsonObject)?.Num(key);

        /// <summary>Advance the debounced max-Q ascent by one frame budget. SliceApp calls this from Update.</summary>
        public void TickReference()
        {
            try
            {
                if (!Readout.Quick(C, D).maxQStale) { referenceJob = null; return; }
                EnsureReferenceJob();
                if (referenceJob == null || referenceJob.Done) return;
                if (referenceJob.Step(ReferenceAscent.FrameBudgetMs))
                {
                    BuildStrip();
                    stepper?.RefreshLive();
                }
            }
            catch (Exception e) { Debug.LogWarning("[DesignerV3] max-Q sim: " + e.Message); referenceJob = null; }
        }

        /// <summary>Finish the reference ascent so the strip can show max-Q. Capture uses this; play steps it per frame.</summary>
        public void FinishReference()
        {
            try
            {
                if (!Readout.Quick(C, D).maxQStale) { BuildStrip(); return; }
                EnsureReferenceJob();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (referenceJob != null && !referenceJob.Done && sw.Elapsed.TotalMilliseconds < 2000)
                    referenceJob.Step(50);
                BuildStrip();
                stepper?.RefreshLive();
            }
            catch (Exception e) { Debug.LogWarning("[DesignerV3] max-Q sim: " + e.Message); }
        }

        void EnsureReferenceJob()
        {
            string key = ReferenceAscent.CanonicalKey(C, D);
            if (referenceJob != null && referenceKey == key) return;
            referenceKey = key;
            referenceJob = new ReferenceJob(C, D);
        }

        static List<HangarUi.Choice> Ids(IEnumerable<string> ids)
        {
            var list = new List<HangarUi.Choice>();
            foreach (var id in ids) list.Add(new HangarUi.Choice(id, HangarFormat.Cap(id)));
            return list;
        }
    }
}
