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
        readonly Dictionary<DesignerV3Tab, float> tabScroll = new Dictionary<DesignerV3Tab, float>();
        readonly CultureInfo inv = CultureInfo.InvariantCulture;
        float heldScroll = -1f;

        static DesignerV3Panel mounted;

        DesignerV3Tab tab = DesignerV3Tab.Engines;
        string flowStep = "Engines";
        bool engineer;
        /// <summary>Landscape web (LANDSCAPE_WEB_BRIEF §3): four Simple tabs, More… for the rest, three gauges, one launch reason.</summary>
        bool landscape, more;
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
        /// <summary>Open, collapsed, or Guided vs All parts changed. SliceApp hides its own launch reason while the
        /// free designer's launch row shows it, so it re-checks on every switch, not only on the next edit.</summary>
        public event Action ViewChanged;
        int shownView = -1;

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
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1;
            root.style.flexShrink = 1;
            root.style.minHeight = 0;
            root.style.overflow = Overflow.Hidden;

            strip = ui.Row(0f);
            strip.name = "sticky-strip";
            strip.tooltip = "Live flight numbers.";
            strip.style.width = Length.Percent(100);
            strip.style.overflow = Overflow.Visible;
            strip.style.flexWrap = Wrap.Wrap;
            strip.RegisterCallback<GeometryChangedEvent>(_ => LayoutStrip());
            strip.style.flexShrink = 0;
            root.Add(strip);

            deltaRow = ui.Row(0f);
            deltaRow.name = "delta-row";
            deltaRow.style.width = Length.Percent(100);
            deltaRow.style.display = DisplayStyle.None;
            deltaRow.style.flexShrink = 0;
            root.Add(deltaRow);

            tabs = ui.Row(2f);
            tabs.name = "tab-bar";
            tabs.style.flexWrap = Wrap.Wrap;
            tabs.style.width = Length.Percent(100);
            tabs.style.flexShrink = 0;
            root.Add(tabs);

            scroll = new ScrollView(ScrollViewMode.Vertical) { name = "designer-scroll" };
            scroll.style.flexGrow = 1;
            scroll.style.flexShrink = 1;
            scroll.style.minHeight = ui.U(DesignerV3Layout.MinContentPt);

            tutorialBanner = ui.Column();
            tutorialBanner.name = "tutorial-banner";
            scroll.Add(tutorialBanner);

            stepRail = ui.Row(2f);
            stepRail.name = "step-rail";
            stepRail.style.flexWrap = Wrap.Wrap;
            stepRail.style.width = Length.Percent(100);
            scroll.Add(stepRail);

            goalLabel = ui.Text("", HangarUi.BodyPt, bold: true);
            goalLabel.name = "step-goal";
            goalLabel.style.whiteSpace = WhiteSpace.Normal;
            goalLabel.style.marginLeft = 8;
            goalLabel.style.marginRight = 8;
            scroll.Add(goalLabel);

            repairLabel = ui.Text("", HangarUi.SmallPt, HangarUi.Amber);
            repairLabel.name = "repair-note";
            repairLabel.style.whiteSpace = WhiteSpace.Normal;
            repairLabel.style.marginLeft = 8;
            repairLabel.style.marginRight = 8;
            repairLabel.style.display = DisplayStyle.None;
            scroll.Add(repairLabel);

            budgetRow = ui.Column();
            budgetRow.name = "dv-budget";
            budgetRow.style.width = Length.Percent(100);
            scroll.Add(budgetRow);

            riskRow = ui.Column();
            riskRow.name = "risk-chips";
            riskRow.style.width = Length.Percent(100);
            riskRow.style.overflow = Overflow.Hidden;
            scroll.Add(riskRow);

            content = ui.Column();
            content.name = "tab-content";
            scroll.Add(content);
            root.Add(scroll);

            confirmRow = ui.Row(2f);
            confirmRow.name = "designer-launch";
            confirmRow.style.flexShrink = 0;
            // Whole buttons wrap to the next row; the reason line takes a full row. In the 278 pt
            // readout column the reason was squeezed to one character per line.
            confirmRow.style.flexWrap = Wrap.Wrap;
            root.Add(confirmRow);
            root.RegisterCallback<GeometryChangedEvent>(_ => DesignerChrome.Contain(root));

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
                // B-10: the guided Review step launches only through the gate. Seeded fair-odds reds pass the gate
                // and keep their Launch anyway confirm.
                // F1: a soft block (predicted failure) also flies; the stepper's Review step already asked.
                Launch = () => { if (Gate(settle: true).CanLaunch) LaunchRequested?.Invoke(); },
                JumpRisk = JumpToRisk,
                LaunchArmed = () => launchConfirmArmed,
                SetLaunchArmed = v => launchConfirmArmed = v,
                Tab = () => tab,
                HideWhenGuided = new[] { tutorialBanner, strip, riskRow, tabs, scroll, confirmRow, stepRail, goalLabel, repairLabel, deltaRow, budgetRow },
                Rebuild = () => Rebuild(),
                Column = () => webColumn,
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
            NotifyView();
        }

        public void SetScale(float upp)
        {
            if (Mathf.Approximately(upp, ui.Upp)) return;
            ui.Upp = upp;
            heldScroll = -1f;
            BuildTabs();
            Rebuild();
        }

        /// <summary>SliceApp flips this with the landscape web shell. Portrait keeps the phone designer.</summary>
        public void SetLandscape(bool on)
        {
            if (landscape == on) return;
            landscape = on;
            more = false;
            if (!engineer && Array.IndexOf(DesignerV3Tabs.Visible(false, landscape, true), tab) < 0) tab = DesignerV3Tab.Engines;
            BuildTabs();
            Rebuild();
        }

        public bool Landscape => landscape;

        Vector2 webColumn;

        /// <summary>The stepper root in points inside the landscape web parts column (zero off the web column).
        /// The guided stepper sizes its tiles and decides where the check card goes from this.</summary>
        public void SetWebColumn(float widthPt, float heightPt)
        {
            var next = widthPt > 1f && heightPt > 1f ? new Vector2(widthPt, heightPt) : Vector2.zero;
            if ((next - webColumn).sqrMagnitude < 1f) return;
            webColumn = next;
            stepper?.Sync();
        }

        void SetMore(bool open)
        {
            if (more == open) return;
            more = open;
            BuildTabs();
            RefreshChrome();
        }

        public void SetContentMax(float units)
        {
            float minPx = ui.U(DesignerV3Layout.MinContentPt);
            scroll.style.minHeight = minPx;
            scroll.style.flexGrow = 1;
            scroll.style.flexShrink = 1;
            if (units > 0f && units < minPx) units = 0f;
            scroll.style.maxHeight = units > 0f ? units : StyleKeyword.Null;
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
            RememberScroll();
            heldScroll = tabScroll.TryGetValue(tab, out var saved) ? saved : 0f;
            BuildTabs();
            Rebuild();
        }

        public void Show(DesignerV3Tab next)
        {
            if (!engineer && landscape && Array.IndexOf(DesignerV3Tabs.Visible(false, true, true), next) >= 0)
            {
                // Landscape Simple keeps Payload; Stages and Landing open More… so the jump lands on a visible tab.
                if (Array.IndexOf(DesignerV3Tabs.LandscapeSimple, next) < 0 && !more) { more = true; BuildTabs(); }
            }
            else if (!engineer && Array.IndexOf(DesignerV3Tabs.Simple, next) < 0)
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
            RememberScroll();
            tab = next;
            launchConfirmArmed = false;
            heldScroll = tabScroll.TryGetValue(tab, out var saved) ? saved : 0f;
            TabChanged?.Invoke(tab);
            Rebuild();
        }

        void RememberScroll()
        {
            if (scroll == null) return;
            float y = scroll.scrollOffset.y;
            if (float.IsNaN(y) || float.IsInfinity(y) || y < 0f) y = 0f;
            tabScroll[tab] = y;
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
            heldScroll = -1f;
            RebuildTutorial();
            Rebuild();
        }

        void OnChanged()
        {
            launchConfirmArmed = false;
            if (tutorialOn) tutorialIndex = DesignerV3Tutorial.Advance(tutorialSteps, tutorialIndex);
            float y = scroll != null ? scroll.scrollOffset.y : 0f;
            heldScroll = float.IsNaN(y) || float.IsInfinity(y) || y < 0f ? 0f : y;
            Rebuild();
        }

        void RebuildTutorial()
        {
            double alt = 0;
            try { alt = Readout.Quick(C, D).targetAlt; } catch (Exception) { alt = 0; }
            tutorialSteps = DesignerV3Tutorial.ForDesign(
                D.Str("missionId"),
                alt,
                () => D.Obj("payload")?.Str("orbit"),
                () => session.Sheet != null ? Readout.Quick(C, D).liftoffTWR : 0,
                () => Readout.Quick(C, D).margin,
                () => D.Obj("aero")?.Bool("fins") ?? false,
                () => { var r = Readout.Quick(C, D); return !r.fairingFits.HasValue || r.fairingFits.Value; },
                // F1: KH-2's 100 km apogee is a hop, not "Low Earth Orbit at 200 km".
                Kestrel.Core.Flight.MissionAim.IsSuborbital(C, D.Obj("payload")?.Str("orbit")));
            tutorialIndex = DesignerV3Tutorial.Advance(tutorialSteps, 0);
        }

        void BuildTabs()
        {
            tabs.Clear();
            tabButtons.Clear();
            foreach (var t in DesignerV3Tabs.Visible(engineer, landscape, more))
            {
                var captured = t;
                var b = ui.Btn(DesignerV3Tabs.Label(t, engineer, landscape), () => Show(captured), DesignerV3Tabs.VoiceOver(t), minWidthPt: 56f);
                b.name = "tab-" + t;
                tabButtons[t] = b;
                tabs.Add(b);
            }
            if (landscape && !engineer)
            {
                // One disclosure: the other Simple tabs and the Engineer switch live behind More….
                var disclose = ui.Btn(more ? "Less" : DesignerV3Tabs.MoreLabel, () => SetMore(!more),
                    more ? "Hide the extra tabs" : "Show more tabs and the Engineer view", minWidthPt: 56f);
                disclose.name = "tab-more";
                tabs.Add(disclose);
                if (!more) return;
            }
            var mode = ui.Btn(engineer ? "Engineer" : "Simple", () => SetEngineer(!engineer),
                engineer ? "Switch to Simple view" : "Switch to Engineer view", minWidthPt: 72f);
            mode.name = "mode-toggle";
            tabs.Add(mode);
        }

        void Rebuild()
        {
            float keep = heldScroll;
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
            if (scroll != null)
            {
                if (keep >= 0f) RestoreScroll(keep);
                else scroll.scrollOffset = new Vector2(0f, 0f);
            }
            heldScroll = -1f;
            DesignerChrome.Apply(root);
            NotifyView();
        }

        /// <summary>The free designer (its own launch row) is on screen.</summary>
        public bool ShowsLaunchRow => Open && !sheetCollapsed && stepper != null && !stepper.IsGuided;

        void NotifyView()
        {
            int view = (Open ? 1 : 0) | (sheetCollapsed ? 2 : 0) | (stepper != null && stepper.IsGuided ? 4 : 0);
            if (view == shownView) return;
            shownView = view;
            ViewChanged?.Invoke();
        }

        void RestoreScroll(float y)
        {
            if (scroll == null || y < 0f || float.IsNaN(y) || float.IsInfinity(y)) return;
            var bar = scroll.verticalScroller;
            if (bar != null && bar.highValue < y) bar.highValue = y;
            scroll.scrollOffset = new Vector2(scroll.scrollOffset.x, y);
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
            var show = step;
            row.Add(ui.Btn("Show me", () =>
            {
                Show(show.tab);
                if (!string.IsNullOrEmpty(show.field)) FieldFocused?.Invoke(show.field);
            }, "Jump to the tutorial tab"));
            row.Add(ui.Btn("Skip tour", () => SetTutorial(false), "Hide the guided tour"));
            tutorialBanner.Add(row);
            tutorialBanner.tooltip = "Tutorial step " + (tutorialIndex + 1) + " of " + tutorialSteps.Count;
        }

        void BuildStrip()
        {
            strip.Clear();
            var readout = Readout.Quick(C, D);
            bool gauges = landscape && !engineer;
            foreach (var c in gauges ? DesignerV3Strip.Gauges(readout) : DesignerV3Strip.Cells(readout, engineer))
            {
                var cell = ui.Column();
                cell.name = "strip-" + c.id;
                cell.style.flexGrow = 1;
                cell.style.flexShrink = 1;
                cell.style.flexBasis = new StyleLength(new Length(gauges ? 30 : 46, LengthUnit.Percent));
                cell.style.minWidth = new StyleLength(new Length(gauges ? 28 : 40, LengthUnit.Percent));
                cell.style.overflow = Overflow.Visible;
                cell.style.alignItems = Align.Center;
                var label = ui.Text(c.label, HangarUi.SmallPt, HangarUi.Dim);
                label.name = "strip-label-" + c.id;
                label.style.whiteSpace = WhiteSpace.NoWrap;
                label.style.overflow = Overflow.Visible;
                cell.Add(label);
                var v = ui.Text(c.value, HangarUi.SmallPt, StateColor(c.state), bold: true);
                v.name = "strip-value-" + c.id;
                v.style.unityTextAlign = TextAnchor.MiddleCenter;
                v.style.whiteSpace = WhiteSpace.NoWrap;
                v.style.overflow = Overflow.Visible;
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
            var chip = ui.Text("Change: " + lastDelta, HangarUi.SmallPt, HangarUi.ChipOn, bold: true);
            chip.name = "delta-chip";
            chip.style.whiteSpace = WhiteSpace.Normal;
            chip.style.flexGrow = 1;
            chip.style.overflow = Overflow.Visible;
            chip.tooltip = "Change from the last edit. This is not the liftoff TWR.";
            deltaRow.Add(chip);
            LayoutStrip();
        }

        void LayoutStrip()
        {
            if (strip == null) return;
            float w = strip.resolvedStyle.width;
            if (float.IsNaN(w) || w < 8f) w = root.resolvedStyle.width;
            bool narrow = !(w > 420f);
            strip.style.flexWrap = narrow ? Wrap.Wrap : Wrap.NoWrap;
            strip.style.overflow = Overflow.Visible;
            foreach (var child in strip.Children())
            {
                child.style.flexBasis = narrow
                    ? new StyleLength(new Length(46, LengthUnit.Percent))
                    : new StyleLength(0f);
                child.style.minWidth = narrow
                    ? new StyleLength(new Length(40, LengthUnit.Percent))
                    : new StyleLength(0f);
                child.style.overflow = Overflow.Visible;
            }
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
            if (landscape && !engineer)
            {
                // Landscape Simple: no warning list. The launch row carries the one reason and its fix.
                riskRow.style.display = DisplayStyle.None;
                return;
            }
            var readout = Readout.Quick(C, D);
            var notices = DesignerNotices.Collect(session.Advisories, readout.risks);
            if (notices.Count == 0)
            {
                riskRow.style.display = DisplayStyle.None;
                return;
            }
            riskRow.style.display = DisplayStyle.Flex;
            riskRow.style.flexDirection = FlexDirection.Column;
            riskRow.style.width = Length.Percent(100);
            for (int i = 0; i < notices.Count; i++)
            {
                var notice = notices[i];
                var card = ui.Column();
                card.name = "risk-" + (string.IsNullOrEmpty(notice.code) ? "x" : notice.code);
                card.style.overflow = Overflow.Visible;
                card.style.marginBottom = 4;
                card.style.width = Length.Percent(100);
                string body = notice.text;
                if (engineer && !string.IsNullOrEmpty(notice.detail) && notice.detail != notice.text && !ShownText.Jargon(notice.detail))
                    body = notice.text + " " + notice.detail;
                var text = ui.Text(body, HangarUi.SmallPt, Color.white);
                text.name = "risk-text-" + i;
                text.style.whiteSpace = WhiteSpace.Normal;
                text.style.overflow = Overflow.Visible;
                text.style.backgroundColor = notice.level == "red" ? HangarUi.Red : HangarUi.Amber;
                text.style.paddingLeft = 8;
                text.style.paddingRight = 8;
                text.style.paddingTop = 6;
                text.style.paddingBottom = 6;
                text.tooltip = body;
                card.Add(text);
                var captured = notice;
                var fix = ui.Btn("Fix it", () => FixNotice(captured), "Fix it. " + captured.text, minWidthPt: 44f);
                fix.name = "risk-fix-" + (string.IsNullOrEmpty(notice.code) ? "x" : notice.code);
                card.Add(fix);
                riskRow.Add(card);
            }
        }

        void FixNotice(DesignerNotice notice)
        {
            if (notice == null) return;
            if (!string.IsNullOrEmpty(notice.tab))
                Show(DesignerV3Tabs.FromRiskTab(notice.tab));
            var risks = Readout.Quick(C, D).risks;
            if (risks != null)
            {
                for (int i = 0; i < risks.Count; i++)
                {
                    var risk = risks[i];
                    if (risk == null) continue;
                    if (DesignerNotices.Key(risk.kind) == DesignerNotices.Key(notice.code))
                    {
                        Repair(risk);
                        return;
                    }
                }
            }
        }

        /// <summary>B-10: the launch gate on the live readout. <paramref name="settle"/> finishes a stale reference
        /// ascent first, so an explicit press is never refused for "Checking…".</summary>
        public LaunchGateResult Gate(bool settle = false)
        {
            if (settle && Readout.Quick(C, D).maxQStale) FinishReference();
            return LaunchGate.Evaluate(session.Report, Readout.Quick(C, D));
        }

        Label GateLine(string text)
        {
            var why = ui.Text(text, HangarUi.SmallPt);
            why.name = "launch-gate-reason";
            why.style.whiteSpace = WhiteSpace.Normal;
            // Its own full row above the buttons, so it wraps by words in a narrow column.
            why.style.flexBasis = Length.Percent(100);
            why.style.width = Length.Percent(100);
            why.style.flexShrink = 0;
            why.style.flexGrow = 1;
            // 100% plus the theme's 3 px label margins ran 6 px past the row's right edge.
            why.style.marginLeft = 0f;
            why.style.marginRight = 0f;
            return why;
        }

        void BuildLaunchRow()
        {
            confirmRow.Clear();
            var gate = Gate();
            if (gate.state == LaunchGateState.Blocked)
            {
                // One plain reason and the tab that fixes it (LANDSCAPE_WEB_BRIEF B-10). Never "Ready".
                // F1: only a hard (validation) block disables Launch. A predicted failure asks once, then flies.
                Button off;
                if (gate.CanLaunch)
                {
                    off = ui.Btn(DesignerV3Risk.LaunchButtonLabel(true, launchConfirmArmed), () =>
                    {
                        if (DesignerV3Risk.NeedsConfirm(true, launchConfirmArmed)) { launchConfirmArmed = true; BuildLaunchRow(); return; }
                        LaunchRequested?.Invoke();
                    }, DesignerV3Risk.ConfirmTitle + ". " + gate.Line, minWidthPt: 120f);
                    if (!launchConfirmArmed) off.style.backgroundColor = HangarUi.Amber;
                }
                else
                {
                    off = ui.Btn("Launch", () => { }, "Launch unavailable. " + gate.Line, minWidthPt: 120f);
                    off.SetEnabled(false);
                }
                off.name = "launch-btn";
                confirmRow.Add(GateLine(gate.Line));
                confirmRow.Add(Word(off));
                if (!string.IsNullOrEmpty(gate.tab))
                {
                    var target = DesignerV3Tabs.FromRiskTab(gate.tab);
                    var fix = ui.Btn("Fix it", () => Show(target), "Open " + DesignerV3Tabs.Label(target, engineer, landscape), minWidthPt: 72f);
                    fix.name = "launch-gate-fix";
                    confirmRow.Add(Word(fix));
                }
                return;
            }
            if (gate.state == LaunchGateState.Checking)
            {
                // The reference ascent is still running. The line says so; a press finishes the check first
                // and launches only if the gate then passes.
                var check = ui.Btn("Launch", () =>
                {
                    var settled = Gate(settle: true);
                    bool ready = settled.Ready;
                    BuildLaunchRow();
                    // Seeded fair-odds reds still show their Launch anyway confirm on the rebuilt row.
                    if (ready && !DesignerV3Strip.HasRedRisk(Readout.Quick(C, D))) LaunchRequested?.Invoke();
                }, "Checking the test flight first", minWidthPt: 120f);
                check.name = "launch-btn";
                confirmRow.Add(GateLine(LaunchGate.Checking));
                confirmRow.Add(Word(check));
                return;
            }
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
            confirmRow.Add(Word(b));
        }

        /// <summary>A launch-row button keeps its words on one line and wraps as a whole.</summary>
        static Button Word(Button b)
        {
            b.style.flexShrink = 0;
            b.style.whiteSpace = WhiteSpace.NoWrap;
            return b;
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
            var props = new List<HangarUi.Choice>();
            string currentFuel = Stage0()?.Str("propellant");
            foreach (var id in HangarEditor.PropellantChoices(C, currentFuel))
            {
                string label = id;
                if (C.PropellantById.TryGetValue(id, out var p) && !string.IsNullOrEmpty(p.label)) label = p.label;
                props.Add(new HangarUi.Choice(id, label, label));
            }
            var prop = ui.Chips("Propellant", props, () => Stage0()?.Str("propellant"),
                id => Do(d => HangarEditor.SetPropellant(C, d, PartRef.Stage(0), id)), refreshers);
            prop.name = "field-propellant";
            content.Add(prop);
            var mixRow = ui.Stepper("Mixture offset", () => (Stage0()?.Num("mixtureOffsetPct") ?? 0).ToString("0.0", inv) + "%",
                () => engineer ? "±10%. Beyond ±6% risks instability." : "Keep near zero for a clean burn",
                (dir, key) => Do(d => DesignerKnobs.NudgeMixture(d, 0, dir), key),
                End, "mix", refreshers);
            mixRow.name = "field-mixtureOffsetPct";
            content.Add(mixRow);
            AttachDelta("mixtureOffsetPct", d => DesignerKnobs.NudgeMixture(d, 0, 1));
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
            var engineChips = ui.Chips("Engine", engines, () => Stage0()?.Str("engine"), id => Do(d => HangarEditor.SetEngine(C, d, p, id)), refreshers);
            engineChips.name = "field-engine";
            content.Add(engineChips);
            var count = ui.Stepper("Count", () => ((int)(Stage0()?.Num("engineCount") ?? 1)).ToString(inv),
                () => null,
                (dir, key) => Do(d => HangarEditor.SetEngineCount(d, p, (int)(HangarParts.Body(d, p)?.Num("engineCount") ?? 1) + dir), key),
                End, "eng.count", refreshers, null,
                dir =>
                {
                    int n = (int)(Stage0()?.Num("engineCount") ?? 1) + dir;
                    return n >= DesignerLimits.Active.engineCountMin && n <= DesignerLimits.Active.engineCountMax;
                });
            count.name = "field-engineCount";
            content.Add(count);
            var patterns = new List<HangarUi.Choice>();
            foreach (var pl in DesignerLimits.Active.patterns)
                patterns.Add(new HangarUi.Choice(pl.id, pl.id));
            var cluster = ui.Chips("Cluster", patterns, () => Stage0()?.Obj("cluster")?.Str("pattern") ?? "auto",
                id => Do(d => HangarEditor.SetClusterPattern(d, p, id)), refreshers);
            cluster.name = "field-cluster";
            content.Add(cluster);
            if (engineer)
            {
                var pc = ui.Stepper("Chamber pressure", () => DesignerV3Edits.ShownPc(C, D, 0).ToString("0", inv) + " bar",
                    () => "Higher pc: more thrust and Isp, overpressure risk",
                    (dir, key) => Do(d => DesignerKnobs.NudgePc(C, d, 0, dir), key),
                    End, "eng.pc", refreshers);
                pc.name = "field-pc";
                content.Add(pc);
                var eps = ui.Stepper("Expansion ratio", () => DesignerV3Edits.ShownExpansion(C, D, 0).ToString("0", inv),
                    () => "Bigger ε: better vacuum, worse pad",
                    (dir, key) => Do(d => DesignerKnobs.NudgeExpansion(C, d, 0, dir), key),
                    End, "eng.eps", refreshers);
                eps.name = "field-expansionRatio";
                content.Add(eps);
                var gimbal = ui.Stepper("Gimbal", () => DesignerV3Edits.ShownGimbal(C, D, 0).ToString("0.0", inv) + "°",
                    () => "Control authority vs mass",
                    (dir, key) => Do(d => DesignerKnobs.NudgeGimbal(C, d, 0, dir), key),
                    End, "eng.gimbal", refreshers);
                gimbal.name = "field-gimbal";
                content.Add(gimbal);
                var throttle = ui.Stepper("Min throttle", () => (DesignerKnobs.ShownThrottle(C, D, 0) * 100).ToString("0", inv) + "%",
                    () => "How far the engines can ease off",
                    (dir, key) => Do(d => DesignerKnobs.NudgeThrottle(C, d, 0, dir), key),
                    End, "eng.thr", refreshers);
                throttle.name = "field-throttle";
                content.Add(throttle);
            }
        }

        void BuildTanks()
        {
            var p = PartRef.Stage(0);
            content.Add(ui.Section(engineer ? "Stage 1 tank" : "Fuel tank"));
            var fuel = Readout.Quick(C, D);
            Teach(DesignerCoach.WhyTanks(fuel.totalDeltaV, fuel.required), "tanks");
            var fuelRow = ui.Stepper("Fuel load", () => HangarFormat.Tons(Stage0()?.Num("propMass") ?? 0),
                () => engineer ? null : "More fuel goes further but weighs more",
                (dir, key) => Do(d => HangarEditor.SetPropMass(d, p, HangarFormat.StepProp(HangarParts.Body(d, p)?.Num("propMass") ?? 0, dir)), key),
                End, "tank.prop", refreshers, null,
                dir => HangarFormat.PropCanStep(Stage0()?.Num("propMass") ?? 0, dir));
            fuelRow.name = "field-propMass";
            content.Add(fuelRow);
            if (engineer)
            {
                var pressure = ui.Stepper("Pressure", () => DesignerKnobs.ShownPressure(D, 0).ToString("0.0", inv) + " bar",
                    () => "Hoop stress sets wall mass",
                    (dir, key) => Do(d => DesignerKnobs.NudgePressure(d, 0, dir), key),
                    End, "tank.p", refreshers);
                pressure.name = "field-pressure";
                content.Add(pressure);
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
            {
                var add = ui.Btn("+ Stage on top", () => Do(d => HangarEditor.AddStage(C, d, n)), "Add a stage");
                add.name = "field-addStage";
                content.Add(add);
            }
            if (n > DesignerLimits.Active.stagesMin)
            {
                var remove = ui.Btn("Remove top stage", () => Do(d => HangarEditor.RemoveStage(d, n - 1)), "Remove uppermost stage");
                remove.name = "field-removeStage";
                content.Add(remove);
            }
            var counts = new List<HangarUi.Choice>();
            foreach (var k in DesignerLimits.Active.strapOnCounts)
                counts.Add(new HangarUi.Choice(k.ToString(inv), k == 0 ? "None" : k.ToString(inv)));
            var boosters = ui.Chips(engineer ? "Strap-ons" : "Side boosters", counts,
                () => HangarParts.BoosterCount(D).ToString(inv),
                id => Do(d => HangarEditor.SetBoosterCount(C, d, int.Parse(id, inv))), refreshers);
            boosters.name = "field-strapOns";
            content.Add(boosters);
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
            var fins = ui.Switch("Fins", "Steady in the air", () => D.Obj("aero")?.Bool("fins") ?? false,
                on => Do(d => HangarEditor.SetFins(d, on)), refreshers);
            fins.name = "field-fins";
            content.Add(fins);
            if (D.Obj("aero")?.Bool("fins") == true)
            {
                var finCount = ui.Stepper("Fin count", () => ((int)(D.Obj("aero")?.Num("finCount", 4) ?? 4)).ToString(inv), null,
                    (dir, key) => Do(d => HangarEditor.SetFinCount(d, (int)(d.Obj("aero")?.Num("finCount", 4) ?? 4) + dir), key),
                    End, "fins.count", refreshers);
                finCount.name = "field-finCount";
                content.Add(finCount);
                var span = ui.Stepper("Fin size", () => (D.Obj("aero")?.Num("finSpanM", 1) ?? 1).ToString("0.0", inv) + " m",
                    () => "Bigger fins hold the point up",
                    (dir, key) => Do(d => HangarEditor.SetFinSpan(d, (d.Obj("aero")?.Num("finSpanM", 1) ?? 1) + dir * DesignerLimits.Active.finSpanStep), key),
                    End, "fins.span", refreshers);
                span.name = "field-finSpan";
                content.Add(span);
            }
            var noses = new List<HangarUi.Choice>();
            foreach (var id in DesignerKnobs.Noses)
                noses.Add(new HangarUi.Choice(id, HangarFormat.Cap(id)));
            var nose = ui.Chips("Nose", noses, () => D.Obj("aero")?.Str("nose") ?? "ogive",
                id => Do(d => DesignerKnobs.SetNose(d, id)), refreshers);
            nose.name = "field-nose";
            content.Add(nose);
            var fairLen = ui.Stepper("Fairing length", () => HangarFormat.Metres(D.Obj("fairing")?.Num("length") ?? 0),
                () => "Room for the cargo",
                (dir, key) => Do(d => HangarEditor.SetFairingLength(d, (d.Obj("fairing")?.Num("length") ?? 1) + dir * 0.5), key),
                End, "fair.l", refreshers);
            fairLen.name = "field-fairing";
            content.Add(fairLen);
            var fairDia = ui.Stepper("Fairing width", () => HangarFormat.Metres(D.Obj("fairing")?.Num("diameter") ?? 0),
                () => "Wide enough for the cargo",
                (dir, key) => Do(d => HangarEditor.SetDiameter(d, PartRef.Fairing, (d.Obj("fairing")?.Num("diameter") ?? 1) + dir * 0.1), key),
                End, "fair.d", refreshers);
            fairDia.name = "field-fairingDiameter";
            content.Add(fairDia);
            var maxQ = ui.Stepper("Max-Q throttle", () => Kestrel.Core.Designer.Stepper.MaxQThrottle.Label(D),
                () => "Ease off while the air pushes hardest",
                (dir, key) => Do(d => Kestrel.Core.Designer.Stepper.MaxQThrottle.Nudge(d, dir), key),
                End, "g.q", refreshers);
            maxQ.name = "field-maxQThrottle";
            content.Add(maxQ);
        }

        void BuildStructure()
        {
            content.Add(ui.Section(engineer ? "Structure" : "Strength"));
            var wall = Readout.Quick(C, D);
            Teach(DesignerCoach.WhyMaxQ(wall.maxQ_kPa, wall.qLim_kPa), "max-q");
            var mats = new List<HangarUi.Choice>();
            foreach (var m in C.Materials) mats.Add(new HangarUi.Choice(m.id, m.label));
            var material = ui.Chips("Material", mats, () => Stage0()?.Str("material"),
                id => Do(d => HangarEditor.SetMaterial(C, d, PartRef.Stage(0), id)), refreshers);
            material.name = "field-material";
            content.Add(material);
            var safety = ui.Stepper("Safety factor", () => D.Num("safetyFactor", 1.4).ToString("0.00", inv),
                () => "Stronger structure weighs more",
                (dir, key) => Do(d => HangarEditor.SetSafetyFactor(d, d.Num("safetyFactor", 1.4) + dir * DesignerLimits.Active.safetyStep), key),
                End, "sf", refreshers);
            safety.name = "field-safetyFactor";
            content.Add(safety);
            var avionics = ui.Chips("Avionics", Ids(HangarEditor.AvionicsTiers), () => D.Obj("avionics")?.Str("tier"),
                id => Do(d => HangarEditor.SetAvionics(d, id)), refreshers);
            avionics.name = "field-avionics";
            content.Add(avionics);
        }

        void BuildGuidance()
        {
            content.Add(ui.Section("Guidance profile"));
            var guide = Readout.Quick(C, D);
            Teach(DesignerCoach.WhyGLimit(guide.gLimit) + " " + DesignerCoach.WhyMaxQ(guide.maxQ_kPa, guide.qLim_kPa), "max-q");
            var kickAlt = ui.Stepper("Pitch kick alt", () => DesignerKnobs.ShownGuidance(D, "pitchKickAlt").ToString("0", inv) + " m",
                () => "Start of the gravity turn",
                (dir, key) => Do(d => DesignerKnobs.NudgeGuidance(d, "pitchKickAlt", dir), key),
                End, "g.alt", refreshers);
            kickAlt.name = "field-pitchKickAlt";
            content.Add(kickAlt);
            var kick = ui.Stepper("Pitch kick", () => DesignerKnobs.ShownGuidance(D, "pitchKickDeg").ToString("0.0", inv) + "°",
                () => null,
                (dir, key) => Do(d => DesignerKnobs.NudgeGuidance(d, "pitchKickDeg", dir), key),
                End, "g.kick", refreshers);
            kick.name = "field-pitchKickDeg";
            content.Add(kick);
            var qThrottle = ui.Stepper("Max-Q throttle", () => Kestrel.Core.Designer.Stepper.MaxQThrottle.Label(D),
                () => "Throttle bucket through max-Q",
                (dir, key) => Do(d => DesignerKnobs.NudgeGuidance(d, "maxQThrottle", dir), key),
                End, "g.q", refreshers);
            qThrottle.name = "field-maxQThrottle";
            content.Add(qThrottle);
            var gLimit = ui.Stepper("g limit", () => DesignerKnobs.ShownGuidance(D, "gLimit").ToString("0.0", inv) + " g",
                () => "Autopilot caps sensed acceleration",
                (dir, key) => Do(d => DesignerKnobs.NudgeGuidance(d, "gLimit", dir), key),
                End, "g.g", refreshers);
            gLimit.name = "field-gLimit";
            content.Add(gLimit);
        }

        void BuildRecovery()
        {
            content.Add(ui.Section(engineer ? "Recovery" : "Landing"));
            var land = Readout.Quick(C, D);
            Teach(DesignerCoach.WhyLanding(land.totalDeltaV, land.required), "landing");
            var legsRow = ui.Switch("Landing legs", null, () => D.Obj("landing")?.Bool("legs") ?? false,
                on => Do(d => HangarEditor.SetLegs(d, on)), refreshers);
            legsRow.name = "field-legs";
            content.Add(legsRow);
            content.Add(ui.Switch("Grid fins", "Steer on the way down", () => D.Obj("landing")?.Bool("gridFins") ?? false,
                on => Do(d => HangarEditor.SetGridFins(d, on)), refreshers));
            var modes = new List<HangarUi.Choice>
            {
                new HangarUi.Choice("expendable", "Expend"),
                new HangarUi.Choice("rtls", "Return to pad"),
                new HangarUi.Choice("barge", "Haven barge"),
            };
            var recovery = ui.Chips(engineer ? "Recovery mode" : "Where it lands", modes,
                () => D.Str("recoveryMode") ?? "expendable",
                id => Do(d => DesignerV3Edits.SetRecoveryMode(d, id)), refreshers);
            recovery.name = "field-recoveryMode";
            content.Add(recovery);
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
            var payloadType = ui.Chips("Type", payloads, () => D.Obj("payload")?.Str("type"),
                id => Do(d => HangarEditor.SetPayloadType(C, d, id)), refreshers);
            payloadType.name = "field-payload";
            content.Add(payloadType);
            var orbits = new List<HangarUi.Choice>();
            foreach (var o in C.Orbits.Values) orbits.Add(new HangarUi.Choice(o.id, o.label ?? o.id));
            var orbitChips = ui.Chips("Orbit", orbits, () => D.Obj("payload")?.Str("orbit"),
                id => Do(d => HangarEditor.SetOrbit(C, d, id)), refreshers);
            orbitChips.name = "field-orbit";
            content.Add(orbitChips);
            var mass = ui.Stepper("Mass", () => HangarFormat.Kg(D.Obj("payload")?.Num("mass") ?? 0), null,
                (dir, key) => Do(d => HangarEditor.SetPayloadMass(d, (d.Obj("payload")?.Num("mass") ?? 0) + dir * 100), key),
                End, "pl.mass", refreshers);
            mass.name = "field-payloadMass";
            content.Add(mass);
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
                string painted = D.Obj("livery")?.Str("name");
                if (string.IsNullOrEmpty(painted)) painted = D.Str("name") ?? "";
                var nameField = new TextField { value = painted, maxLength = HangarEditor.VehicleNameMax };
                nameField.label = "Vehicle name";
                nameField.name = "field-name";
                nameField.isDelayed = true;
                nameField.style.minHeight = ui.U(HangarUi.TargetPt);
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
            goalLabel.text = ShownText.Screen(DesignerCoach.Goal(flowStep));
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
                // Simple mode has no Cargo tab. Show() maps Payload onto Stages there, and the
                // step chip stays on Mission so the rail does not look like it did nothing.
                Show(DesignerV3Tab.Payload);
                if (!engineer)
                {
                    flowStep = "Mission";
                    BuildFlow();
                }
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
                string dvNote = result != null && result.Solved
                    ? "Added propellant or a stage so the speed covers this trip."
                    : PlayerText.ForPlayer(result != null ? result.Reason : "");
                NoteRepair(dvNote);
                return;
            }
            Show(next);
            Edit made = Edit.None;
            string said = null;
            Do(d =>
            {
                var check = new Kestrel.Core.Designer.Stepper.StepCheck
                {
                    Step = Kestrel.Core.Designer.Stepper.DesignerStep.Look,
                    Code = string.IsNullOrEmpty(kind) ? "fix" : kind,
                    Gate = Kestrel.Core.Designer.Stepper.StepGate.Amber,
                };
                var result = Kestrel.Core.Designer.Stepper.StepperFix.Apply(C, d, check);
                made = result.Edit;
                said = result.Instruction;
                if (!made.changed) made = DesignerRepair.Apply(C, d, kind);
                return made;
            });
            string note;
            if (made.changed && !string.IsNullOrEmpty(made.from) && !string.IsNullOrEmpty(made.to))
                note = made.what + " " + made.from + " → " + made.to;
            else if (!string.IsNullOrEmpty(said))
                note = said;
            else if (kind == "overg" || kind == "payload-g")
                note = "The g limit is already under the payload rating.";
            else
                note = "That control is already at the safer setting.";
            NoteRepair(note);
        }

        void Repair(FlightRisk risk)
        {
            if (risk == null) return;
            JumpToRisk(risk);
            ApplyReportedFix(risk.kind, risk.tab);
        }

        void NoteRepair(string note)
        {
            repairLabel.text = ShownText.Screen(note ?? "");
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
                { chip.text = ShownText.Screen(lastDelta); return; }
                chip.text = ShownText.Screen(DesignerV3Delta.Chip(C, D, preview));
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
                    BuildLaunchRow();
                    stepper?.RefreshLive();
                    session.RefreshDerived();
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
                session.RefreshDerived();
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
