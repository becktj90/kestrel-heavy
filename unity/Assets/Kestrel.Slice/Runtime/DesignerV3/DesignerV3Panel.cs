// Portrait designer v3 panel (§5–§6). Composes over HangarSession; does not edit HangarPanel.
// Sticky strip, 10-tab bar (Simple subset / Engineer full), delta chips, risk chips, Launch anyway,
// and the guided 200 km LEO tutorial.
// The guided stepper is one hook (AttachStepper): same design, All parts keeps these tabs.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core;
using Kestrel.Core.Codex;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Game.Guide;
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
        readonly VisualElement root, strip, riskRow, tabs, content, confirmRow, tutorialBanner, stepRail, deltaRow, budgetRow;
        readonly Label goalLabel, repairLabel;
        readonly ScrollView scroll;
        readonly List<Action> refreshers = new List<Action>();
        readonly Dictionary<DesignerV3Tab, Button> tabButtons = new Dictionary<DesignerV3Tab, Button>();
        readonly CultureInfo inv = CultureInfo.InvariantCulture;

        static DesignerV3Panel mounted;

        DesignerV3Tab tab = DesignerV3Tab.Engines;
        string flowStep = "Engines";
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

            stepRail = ui.Row(2f);
            stepRail.name = "step-rail";
            stepRail.style.flexWrap = Wrap.Wrap;
            stepRail.style.width = Length.Percent(100);
            root.Add(stepRail);

            goalLabel = ui.Text("", HangarUi.BodyPt, bold: true);
            goalLabel.name = "step-goal";
            goalLabel.style.whiteSpace = WhiteSpace.Normal;
            goalLabel.style.marginLeft = 8;
            goalLabel.style.marginRight = 8;
            root.Add(goalLabel);

            repairLabel = ui.Text("", HangarUi.SmallPt, HangarUi.Amber);
            repairLabel.name = "repair-note";
            repairLabel.style.whiteSpace = WhiteSpace.Normal;
            repairLabel.style.marginLeft = 8;
            repairLabel.style.marginRight = 8;
            repairLabel.style.display = DisplayStyle.None;
            root.Add(repairLabel);

            strip = ui.Row(0f);
            strip.name = "sticky-strip";
            strip.tooltip = "Live flight numbers.";
            strip.style.width = Length.Percent(100);
            strip.style.overflow = Overflow.Hidden;
            strip.style.flexWrap = Wrap.NoWrap;
            root.Add(strip);

            deltaRow = ui.Row(0f);
            deltaRow.name = "delta-row";
            deltaRow.style.width = Length.Percent(100);
            deltaRow.style.display = DisplayStyle.None;
            root.Add(deltaRow);

            budgetRow = ui.Column();
            budgetRow.name = "dv-budget";
            budgetRow.style.width = Length.Percent(100);
            root.Add(budgetRow);

            riskRow = ui.Column();
            riskRow.name = "risk-chips";
            riskRow.style.width = Length.Percent(100);
            riskRow.style.overflow = Overflow.Hidden;
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
                HideWhenGuided = new[] { tutorialBanner, strip, riskRow, tabs, scroll, confirmRow, stepRail, goalLabel, repairLabel, deltaRow, budgetRow },
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
            if (GuideSession.Engine != null) GuideSession.Engine.SetEngineer(on);
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
            flowStep = FlowName(next);
            if (tab == next)
            {
                BuildFlow();
                return;
            }
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
            BuildFlow();
            BuildStrip();
            BuildBudget();
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
                cell.style.flexShrink = 1;
                cell.style.flexBasis = 0;
                cell.style.minWidth = 0;
                cell.style.overflow = Overflow.Hidden;
                cell.style.alignItems = Align.Center;
                var label = ui.Text(c.label, HangarUi.SmallPt, HangarUi.Dim);
                label.style.whiteSpace = WhiteSpace.NoWrap;
                label.style.overflow = Overflow.Hidden;
                cell.Add(label);
                var v = ui.Text(c.value, HangarUi.SmallPt, StateColor(c.state), bold: true);
                v.name = "strip-value-" + c.id;
                v.style.unityTextAlign = TextAnchor.MiddleCenter;
                v.style.whiteSpace = WhiteSpace.Normal;
                v.style.overflow = Overflow.Hidden;
                v.style.width = Length.Percent(100);
                cell.Add(v);
                cell.tooltip = c.label + " " + c.value + (c.hint != null ? ", " + c.hint : "");
                strip.Add(cell);
            }
            deltaRow.Clear();
            if (string.IsNullOrEmpty(lastDelta))
            {
                deltaRow.style.display = DisplayStyle.None;
                return;
            }
            deltaRow.style.display = DisplayStyle.Flex;
            var chip = ui.Text(lastDelta, HangarUi.SmallPt, HangarUi.ChipOn, bold: true);
            chip.name = "delta-chip";
            chip.style.whiteSpace = WhiteSpace.Normal;
            chip.style.flexGrow = 1;
            chip.style.overflow = Overflow.Hidden;
            chip.tooltip = "Change from the last edit.";
            deltaRow.Add(chip);
        }

        void BuildBudget()
        {
            budgetRow.Clear();
            var readout = Readout.Quick(C, D);
            double have = readout.totalDeltaV;
            double need = readout.required;
            double span = Math.Max(have, Math.Max(need, 1));
            var caption = ui.Text(DesignerCoach.BudgetLine(have, need), HangarUi.SmallPt);
            caption.name = "dv-budget-label";
            caption.style.whiteSpace = WhiteSpace.Normal;
            budgetRow.Add(caption);
            var track = new VisualElement { name = "dv-budget-track" };
            track.style.height = 8;
            track.style.width = Length.Percent(100);
            track.style.backgroundColor = HangarUi.ChipOff;
            track.style.overflow = Overflow.Hidden;
            var fill = new VisualElement { name = "dv-budget-fill" };
            float frac = (float)Math.Min(1, Math.Max(0, have / span));
            fill.style.width = Length.Percent(frac * 100f);
            fill.style.height = Length.Percent(100);
            fill.style.backgroundColor = have + 1 >= need ? HangarUi.Green : HangarUi.Amber;
            track.Add(fill);
            budgetRow.Add(track);
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
            riskRow.style.flexDirection = FlexDirection.Column;
            foreach (var risk in chips)
            {
                var r = risk;
                var card = ui.Column();
                card.name = "risk-" + (r.kind ?? "x");
                card.style.overflow = Overflow.Hidden;
                card.style.marginBottom = 4;
                card.style.width = Length.Percent(100);
                var text = ui.Text(DesignerV3Risk.ChipLabel(r, engineer), HangarUi.SmallPt, Color.white);
                text.style.whiteSpace = WhiteSpace.Normal;
                text.style.overflow = Overflow.Hidden;
                text.style.backgroundColor = DesignerV3Risk.LevelState(r.level) == CellState.Bad ? HangarUi.Red : HangarUi.Amber;
                text.style.paddingLeft = 8;
                text.style.paddingRight = 8;
                text.style.paddingTop = 6;
                text.style.paddingBottom = 6;
                text.tooltip = DesignerV3Risk.ChipTooltip(r);
                card.Add(text);
                var fix = ui.Btn("Fix it", () => Repair(r), "Fix it. " + DesignerV3Risk.ChipTooltip(r), minWidthPt: 44f);
                fix.name = "risk-fix-" + (r.kind ?? "x");
                card.Add(fix);
                riskRow.Add(card);
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
            var mix = Readout.Quick(C, D);
            Teach(DesignerCoach.WhyTanks(mix.totalDeltaV, mix.required), "prop");
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
            var push = Readout.Quick(C, D);
            double isp = 0, thrust = 0;
            var engId = Stage0()?.Str("engine");
            if (engId != null && C.EngineById.TryGetValue(engId, out var eng)) { isp = eng.ispVac; thrust = eng.thrustVac; }
            Teach(DesignerCoach.WhyEngines(push.liftoffTWR, isp, thrust), "engines");
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
            var fuel = Readout.Quick(C, D);
            Teach(DesignerCoach.WhyTanks(fuel.totalDeltaV, fuel.required), "tanks");
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
            var stack = Readout.Quick(C, D);
            Teach(DesignerCoach.WhyStages(D.Arr("stages")?.Count ?? 0, stack.totalDeltaV) + " " + DesignerCoach.WhyMaxQ(stack.maxQ_kPa, stack.qLim_kPa), "stages");
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
            Teach(DesignerCoach.WhyStability(Readout.Quick(C, D).stabilityCal), "stability");
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
            var wall = Readout.Quick(C, D);
            Teach(DesignerCoach.WhyMaxQ(wall.maxQ_kPa, wall.qLim_kPa), "max-q");
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
            var guide = Readout.Quick(C, D);
            Teach(DesignerCoach.WhyGLimit(guide.gLimit) + " " + DesignerCoach.WhyMaxQ(guide.maxQ_kPa, guide.qLim_kPa), "max-q");
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
            var land = Readout.Quick(C, D);
            Teach(DesignerCoach.WhyLanding(land.totalDeltaV, land.required), "landing");
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
            var job = Readout.Quick(C, D);
            Teach(DesignerCoach.WhyMission(job.orbitLabel, job.required), "orbit");
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
            Teach("Paint does not change Isp, thrust, TWR, or delta-v.", null);
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

        void BuildFlow()
        {
            stepRail.Clear();
            foreach (var step in DesignerCoach.Flow)
            {
                var captured = step;
                var b = ui.Btn(step, () => ShowStep(captured), DesignerCoach.Goal(step), minWidthPt: 44f);
                b.name = "step-" + step.ToLowerInvariant();
                b.style.flexShrink = 1;
                b.style.minWidth = 44;
                ui.SetChip(b, step == flowStep);
                stepRail.Add(b);
            }
            goalLabel.text = DesignerCoach.Goal(flowStep);
        }

        void ShowStep(string step)
        {
            if (step == "Launch")
            {
                flowStep = "Launch";
                BuildFlow();
                return;
            }
            if (step == "Mission")
            {
                flowStep = "Mission";
                if (tab != DesignerV3Tab.Payload)
                {
                    tab = DesignerV3Tab.Payload;
                    launchConfirmArmed = false;
                    TabChanged?.Invoke(tab);
                    Rebuild();
                }
                else BuildFlow();
                return;
            }
            Show(TabOf(step));
        }

        static DesignerV3Tab TabOf(string step)
        {
            switch (step)
            {
                case "Engines": return DesignerV3Tab.Engines;
                case "Tanks": return DesignerV3Tab.Tanks;
                case "Stages": return DesignerV3Tab.Stages;
                case "Landing": return DesignerV3Tab.Recovery;
                case "Look": return DesignerV3Tab.Look;
                default: return DesignerV3Tab.Engines;
            }
        }

        static string FlowName(DesignerV3Tab next)
        {
            switch (next)
            {
                case DesignerV3Tab.Payload: return "Mission";
                case DesignerV3Tab.Engines: return "Engines";
                case DesignerV3Tab.Tanks: return "Tanks";
                case DesignerV3Tab.Stages: return "Stages";
                case DesignerV3Tab.Recovery: return "Landing";
                case DesignerV3Tab.Look: return "Look";
                default: return "Engines";
            }
        }

        public void ApplyReportedFix(string kind, string tabId)
        {
            var next = DesignerV3Tabs.FromRiskTab(tabId);
            if (DesignerRepair.NeedsDeltaV(kind))
            {
                Show(next);
                var result = session.FixDeltaV();
                string note = result != null && result.Solved
                    ? "Added propellant or a stage so the speed covers this trip."
                    : PlayerText.ForPlayer(result != null ? result.Reason : "");
                NoteRepair(note);
                return;
            }
            Show(next);
            Edit made = Edit.None;
            Do(d =>
            {
                made = DesignerRepair.Apply(C, d, kind);
                return made;
            });
            string said = made.changed
                ? made.what + " is now " + made.to + "."
                : (kind == "overg" || kind == "payload-g")
                    ? "The g limit is already under the payload rating."
                    : "That control is already at the safer setting.";
            NoteRepair(said);
        }

        void Repair(FlightRisk risk)
        {
            if (risk == null) return;
            JumpToRisk(risk);
            ApplyReportedFix(risk.kind, risk.tab);
        }

        void NoteRepair(string note)
        {
            repairLabel.text = note ?? "";
            repairLabel.style.display = string.IsNullOrEmpty(repairLabel.text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void Teach(string why, string concept)
        {
            var line = ui.Text(why ?? "", HangarUi.SmallPt, HangarUi.Dim);
            line.name = "why-line";
            line.style.whiteSpace = WhiteSpace.Normal;
            content.Add(line);
            string id = DesignerCoach.CodexId(concept);
            if (string.IsNullOrEmpty(id)) return;
            var more = ui.Btn("Learn more", () => ToggleCodex(id), "Codex note on " + id, minWidthPt: 44f);
            more.name = "learn-" + id;
            content.Add(more);
        }

        void ToggleCodex(string id)
        {
            var existing = content.Q("codex-open");
            if (existing != null && (existing.userData as string) == id)
            {
                existing.RemoveFromHierarchy();
                return;
            }
            if (existing != null) existing.RemoveFromHierarchy();
            var note = ui.Text(DesignerCoach.CodexBlurb(LoadCodex(), id, engineer), HangarUi.SmallPt);
            note.name = "codex-open";
            note.userData = id;
            note.style.whiteSpace = WhiteSpace.Normal;
            content.Add(note);
        }

        CodexBook LoadCodex()
        {
            if (codex != null) return codex;
            try
            {
                var asset = Resources.Load<TextAsset>("KestrelData/codex");
                if (asset != null && !string.IsNullOrEmpty(asset.text))
                    codex = CodexBook.Parse(asset.text);
            }
            catch (Exception e) { Debug.LogWarning("[DesignerV3] codex: " + e.Message); }
            return codex;
        }

        CodexBook codex;

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
