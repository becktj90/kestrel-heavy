// Guided step shell. Header, progress, goal, controls, why, live stats, check, and the sticky nav.
// All parts keeps the existing ten tabs. Both views edit the same HangarSession design.
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using Kestrel.Slice;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice.DesignerV3.Stepper
{
    public sealed class DesignerStepperBridge
    {
        public HangarSession Session;
        public HangarUi Ui;
        public Func<bool> Engineer;
        public Action<bool> SetEngineer;
        public Action<Func<JsonObject, Edit>, string> Apply;
        public Action EndGesture;
        public Action<string> Focus;
        public Action Launch;
        public Action<FlightRisk> JumpRisk;
        public Func<bool> LaunchArmed;
        public Action<bool> SetLaunchArmed;
        public Func<DesignerV3Tab> Tab;
        public VisualElement[] HideWhenGuided;
        public Action Rebuild;

        public Catalog Catalog => Session.catalog;
        public JsonObject Design => Session.Design;
        public bool IsEngineer => Engineer != null && Engineer();
        public List<string> Palette => Session?.data?.palette;
    }

    public sealed class DesignerStepperShell
    {
        readonly DesignerStepperBridge bridge;
        readonly VisualElement root;
        readonly List<Action> refreshers = new List<Action>();
        readonly List<Label> statValues = new List<Label>();
        readonly List<StepStat> statCache = new List<StepStat>();

        StepperProgress progress;
        StepperReport report;
        string progressSnap = "\0";
        ScrollView scroll;
        float scrollMax = -1f;
        float savedScroll;
        bool holdScroll;
        bool moreOpen;
        bool confirmStep;
        bool wasMaxQStale = true;
        bool scrollFocus;
        bool keepInstruction;
        bool builtGuided;
        DesignerStep builtStep;
        bool builtEngineer;
        bool builtMore;
        bool builtConfirm;
        string focusField;
        string shownCode;
        string fixNote;
        VisualElement focusEl;
        Label checkBody;
        Label checkExtra;
        Button backButton;
        Button forwardButton;
        Button nextButton;
        readonly List<Button> segmentButtons = new List<Button>();

        public VisualElement Root => root;
        public bool IsGuided => progress != null && progress.View == StepperView.Guided;
        public DesignerStep Step => progress != null ? progress.Current : DesignerStep.Mission;
        public StepGate Gate => report != null ? report[Step].Gate : StepGate.Pass;
        public bool MoreOpen => moreOpen;
        public string FocusedField => focusField;

        /// <summary>Vertical offset of the step scroller, in panel pixels. An edit must not change it.</summary>
        public float ScrollOffset
        {
            get => scroll != null ? scroll.scrollOffset.y : savedScroll;
            set => ApplyScrollOffset(value);
        }

        public DesignerStepperShell(DesignerStepperBridge bridge)
        {
            this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            root = new VisualElement { name = "stepper-root" };
            root.AddToClassList("designer-stepper");
            root.AddToClassList("stepper-portrait");
            root.style.display = DisplayStyle.None;
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1;
            root.style.flexShrink = 1;
            if (PlayerPrefs.GetInt(HangarCamera.ReduceMotionPref, 0) != 0)
                root.AddToClassList("reduce-motion");
            var sheet = Resources.Load<StyleSheet>("UI/Stepper");
            if (sheet != null) root.styleSheets.Add(sheet);
        }

        public void SetContentMax(float units)
        {
            scrollMax = units;
            ApplyScrollMax();
        }

        /// <summary>Safe insets in points. The Hangar sheet already pads the device safe area, so the panel does not call this.</summary>
        public void ApplySafeArea(float topPt, float bottomPt, float leftPt, float rightPt)
        {
            var ui = bridge.Ui;
            root.style.paddingTop = ui.U(Math.Max(0f, topPt));
            root.style.paddingBottom = ui.U(Math.Max(0f, bottomPt));
            root.style.paddingLeft = ui.U(Math.Max(0f, leftPt));
            root.style.paddingRight = ui.U(Math.Max(0f, rightPt));
        }

        public void Sync()
        {
            try { SyncCore(); }
            catch (Exception e)
            {
                Debug.LogWarning("[Stepper] " + e);
                root.style.display = DisplayStyle.None;
                ClearHiddenChrome();
            }
        }

        void SyncCore()
        {
            EnsureProgress();
            var before = report;
            report = Evaluate();
            if (before != null && report != null)
                StepperModel.NoteEdit(progress, EditedStep(before), before, report);
            int guard = 0;
            bool settled = false;
            while (report != null && StepperModel.Settle(progress, report) && guard++ < StepperCatalog.Count)
                settled = true;
            if (settled) Persist();

            bool guided = IsGuided;
            root.style.display = guided ? DisplayStyle.Flex : DisplayStyle.None;
            if (!guided)
            {
                builtGuided = false;
                scroll = null;
                EnsureGuidedButton();
                return;
            }

            var check = report[progress.Current];
            bool confirmVisible = confirmStep && check.Gate == StepGate.Amber && progress.Current != DesignerStep.Review;
            bool same = builtGuided && scroll != null && !settled
                && builtStep == progress.Current
                && builtEngineer == bridge.IsEngineer
                && builtMore == moreOpen
                && builtConfirm == confirmVisible;
            if (same)
            {
                RefreshInPlace(check);
                HideLegacy();
                HighlightFocus(scrollFocus);
                scrollFocus = false;
                GuideBinder.Designer((int)Step, report, bridge.IsEngineer);
                wasMaxQStale = report.Readout == null || report.Readout.maxQStale;
                return;
            }

            float keep = scroll != null ? scroll.scrollOffset.y : savedScroll;
            bool restore = builtGuided && !settled && builtStep == progress.Current;
            root.Clear();
            refreshers.Clear();
            statValues.Clear();
            statCache.Clear();
            segmentButtons.Clear();
            focusEl = null;
            scroll = null;
            checkBody = null;
            checkExtra = null;
            backButton = null;
            forwardButton = null;
            nextButton = null;
            BuildGuided(check, confirmVisible);
            if (restore) ApplyScrollOffset(keep);
            else ApplyScrollOffset(0f);
            HideLegacy();
            HighlightFocus(scrollFocus);
            scrollFocus = false;
            foreach (var refresh in refreshers) refresh();
            wasMaxQStale = report.Readout == null || report.Readout.maxQStale;
            GuideBinder.Designer((int)Step, report, bridge.IsEngineer);
        }

        void RefreshInPlace(StepCheck check)
        {
            ConsumeNote(check);
            foreach (var refresh in refreshers) refresh();
            PaintStats(StepperStats.Build(progress.Current, report.Readout, bridge.Design, bridge.Catalog));
            UpdateCheck(check);
            UpdateNav(check);
            UpdateSegments();
        }

        public void RefreshLive()
        {
            if (!IsGuided || report == null) return;
            QuickReadout live = null;
            try { live = Readout.Quick(bridge.Catalog, bridge.Design); }
            catch (Exception) { return; }
            PaintStats(StepperStats.Build(Step, live, bridge.Design, bridge.Catalog));
            bool stale = live == null || live.maxQStale;
            if (wasMaxQStale && !stale) Sync();
            wasMaxQStale = stale;
        }

        public void EnterGuided()
        {
            EnsureProgress();
            progress.EnterGuided();
            Persist();
            if (bridge.Rebuild != null) bridge.Rebuild();
            else Sync();
        }

        public void ShowAllParts()
        {
            EnsureProgress();
            progress.ShowAllParts();
            Persist();
            ClearHiddenChrome();
            if (bridge.Rebuild != null) bridge.Rebuild();
            else Sync();
        }

        public bool PressForward()
        {
            if (!IsGuided || report == null) return false;
            var action = StepperModel.Forward(progress, report);
            if (action.FixIt) return false;
            if (progress.Current == DesignerStep.Review)
            {
                if (action.NeedsConfirm)
                {
                    if (bridge.SetLaunchArmed != null) bridge.SetLaunchArmed(true);
                    Sync();
                    return false;
                }
                if (action.Launches) bridge.Launch?.Invoke();
                return action.Launches;
            }
            if (action.NeedsConfirm && !confirmStep)
            {
                confirmStep = true;
                Sync();
                return false;
            }
            if (!StepperModel.TryAdvance(progress, report, action.NeedsConfirm)) return false;
            confirmStep = false;
            if (bridge.SetLaunchArmed != null) bridge.SetLaunchArmed(false);
            Persist();
            Sync();
            return true;
        }

        public bool PressBack()
        {
            if (!IsGuided || report == null) return false;
            confirmStep = false;
            if (!StepperModel.TryBack(progress, report)) return false;
            Persist();
            Sync();
            return true;
        }

        public bool PressJump(DesignerStep target)
        {
            if (!IsGuided || report == null) return false;
            if (report[target].Gate == StepGate.Skip) return false;
            if (!StepperModel.TryJump(progress, report, target)) return false;
            confirmStep = false;
            Persist();
            Sync();
            return true;
        }

        public void ToggleMore()
        {
            moreOpen = !moreOpen;
            Sync();
        }

        /// <summary>Apply one undoable change that clears the current warning, or name the control and the amount.</summary>
        public bool PressFix()
        {
            if (!IsGuided || report == null) return false;
            var check = report[progress.Current];
            if (!WantsFix(check)) return false;
            string code = check.Code;
            StepperFixResult result = null;
            try
            {
                if (bridge.Apply != null)
                    bridge.Apply(d =>
                    {
                        result = StepperFix.Apply(bridge.Catalog, d, check);
                        return result != null ? result.Edit : Edit.None;
                    }, null);
                else
                    result = StepperFix.Apply(bridge.Catalog, bridge.Design, check);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Stepper] " + e.Message);
                result = new StepperFixResult
                {
                    Field = check.Field,
                    Instruction = "Fix it could not change this design.",
                };
            }
            if (result == null) return false;
            fixNote = string.IsNullOrEmpty(result.Instruction) ? null : result.Instruction;
            keepInstruction = !string.IsNullOrEmpty(fixNote);
            shownCode = code;
            Sync();
            return result.Cleared;
        }

        public void FocusField(string field)
        {
            focusField = StepperControls.Canonical(field);
            if (StepperControls.IsEngineerField(focusField)) moreOpen = true;
            scrollFocus = true;
            Sync();
        }

        /// <summary>Remember which control the player is editing. Does not scroll.</summary>
        public void NoteFocus(string field)
        {
            focusField = StepperControls.Canonical(field ?? "");
            scrollFocus = false;
            HighlightFocus(false);
        }

        void BuildGuided(StepCheck check, bool confirmVisible)
        {
            var ui = bridge.Ui;
            var def = StepperCatalog.Get(progress.Current);
            bool engineer = bridge.IsEngineer;
            ConsumeNote(check);

            root.tooltip = "Guided designer. " + StepperCatalog.Header(progress.Current) + ". " + def.Title + ".";
            root.Add(Header(ui, def));
            root.Add(Progress(ui));
            if (!engineer) root.Add(CheckCard(ui, check));

            scroll = new ScrollView(ScrollViewMode.Vertical) { name = "stepper-scroll" };
            scroll.style.flexGrow = 1;
            scroll.style.flexShrink = 1;
            ApplyScrollMax();
            WatchScroll();
            scroll.RegisterCallback<PointerDownEvent>(OnScrollPointer, TrickleDown.TrickleDown);
            if (engineer)
            {
                scroll.Add(Rocket(ui, def));
                scroll.Add(Goal(ui, def));
            }
            var controls = ui.CardBox();
            controls.name = "stepper-controls";
            controls.Add(ui.Section(progress.Current == DesignerStep.Review ? "Risk check" : "Controls"));
            StepperControls.Build(progress.Current, engineer, moreOpen, ToggleMore, controls, bridge, refreshers, OnRisk);
            scroll.Add(controls);
            if (engineer) scroll.Add(Why(ui, def));
            scroll.Add(Stats(ui));
            if (engineer) scroll.Add(CheckCard(ui, check));
            root.Add(scroll);

            if (confirmVisible) root.Add(Confirm(ui));
            if (progress.Current == DesignerStep.Review)
                root.Add(AllPartsButton(ui, "stepper-all-parts-footer", "All parts"));
            root.Add(Nav(ui, check));

            builtGuided = true;
            builtStep = progress.Current;
            builtEngineer = engineer;
            builtMore = moreOpen;
            builtConfirm = confirmVisible;
        }

        VisualElement Header(HangarUi ui, StepDef def)
        {
            var box = ui.Column();
            box.name = "stepper-header";
            var top = ui.Row(0f);
            top.style.justifyContent = Justify.SpaceBetween;
            var count = ui.Text(StepperCatalog.Header(def.Step), HangarUi.SmallPt, HangarUi.Dim, bold: true);
            count.name = "stepper-count";
            top.Add(count);
            top.Add(DepthToggle(ui));
            box.Add(top);

            var titleRow = ui.Row(0f);
            titleRow.style.justifyContent = Justify.SpaceBetween;
            var title = ui.Text(def.Title, HangarUi.TitlePt + 4f, HangarUi.TextColor, bold: true);
            title.name = "stepper-title";
            title.style.flexShrink = 1;
            titleRow.Add(title);
            var all = ui.Btn("All parts", ShowAllParts, "Open every part at once. Same rocket.", minWidthPt: 72f);
            all.name = "stepper-all-parts";
            all.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            all.style.color = HangarUi.Dim;
            titleRow.Add(all);
            box.Add(titleRow);
            return box;
        }

        VisualElement DepthToggle(HangarUi ui)
        {
            bool engineer = bridge.IsEngineer;
            var row = ui.Row(0f);
            row.name = "stepper-depth";
            row.style.backgroundColor = HangarUi.ChipOff;
            ui.Radius(row.style, 16f);
            row.style.paddingLeft = ui.U(2f);
            row.style.paddingRight = ui.U(2f);
            var simple = ui.Btn("Simple", () => bridge.SetEngineer?.Invoke(false), "Fewer controls. Same steps.", minWidthPt: 64f);
            simple.name = "stepper-simple";
            var eng = ui.Btn("Engineer", () => bridge.SetEngineer?.Invoke(true), "More controls and units. Same steps.", minWidthPt: 72f);
            eng.name = "stepper-engineer";
            ui.SetChip(simple, !engineer);
            ui.SetChip(eng, engineer);
            row.Add(simple);
            row.Add(eng);
            return row;
        }

        VisualElement Progress(HangarUi ui)
        {
            var scroller = new ScrollView(ScrollViewMode.Horizontal) { name = "stepper-progress-scroll" };
            scroller.style.flexGrow = 0;
            scroller.style.flexShrink = 0;
            scroller.style.height = ui.U(StepperLayout.ProgressPt);
            scroller.style.minHeight = ui.U(HangarUi.TargetPt);
            scroller.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroller.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            var row = ui.Row(2f);
            row.name = "stepper-progress";
            row.style.flexWrap = Wrap.NoWrap;
            row.style.flexGrow = 0;
            row.style.flexShrink = 0;
            row.style.alignSelf = Align.FlexStart;
            var marks = StepperModel.Marks(progress, report);
            for (int i = 0; i < StepperCatalog.Count; i++)
            {
                var step = (DesignerStep)(i + 1);
                var def = StepperCatalog.Get(step);
                var button = new Button(() => PressJump(step));
                button.name = "stepper-seg-" + def.Number;
                button.AddToClassList("stepper-seg");
                button.style.flexGrow = 0;
                button.style.flexShrink = 0;
                button.style.minWidth = ui.U(StepperLayout.SegmentMinPt);
                button.style.height = ui.U(StepperLayout.ProgressPt);
                button.style.minHeight = ui.U(HangarUi.TargetPt);
                button.style.fontSize = ui.U(12f);
                button.style.unityTextAlign = TextAnchor.MiddleCenter;
                button.style.whiteSpace = WhiteSpace.NoWrap;
                button.style.paddingLeft = ui.U(8f);
                button.style.paddingRight = ui.U(8f);
                button.style.marginLeft = ui.U(1f);
                button.style.marginRight = ui.U(1f);
                ui.Radius(button.style, 8f);
                button.style.borderLeftWidth = ui.U(1.5f);
                button.style.borderRightWidth = ui.U(1.5f);
                button.style.borderTopWidth = ui.U(1.5f);
                button.style.borderBottomWidth = ui.U(1.5f);
                PaintSegmentButton(button, marks[i], def);
                segmentButtons.Add(button);
                row.Add(button);
            }
            scroller.Add(row);
            return scroller;
        }

        void PaintSegmentButton(Button button, StepMark mark, StepDef def)
        {
            string name = def.Step == DesignerStep.Review ? "Launch" : def.ShortName;
            button.text = Glyph(mark, def.Number) + " " + name;
            button.tooltip = SegmentTip(def, mark);
            button.RemoveFromClassList("stepper-seg--current");
            button.RemoveFromClassList("stepper-seg--done");
            button.RemoveFromClassList("stepper-seg--warn");
            button.RemoveFromClassList("stepper-seg--failed");
            button.RemoveFromClassList("stepper-seg--skipped");
            button.RemoveFromClassList("stepper-seg--open");
            button.RemoveFromClassList("stepper-seg--locked");
            button.AddToClassList(SegClass(mark));
            PaintSegment(button, mark);
            bool locked = mark == StepMark.Locked || mark == StepMark.Skipped;
            button.SetEnabled(!locked && mark != StepMark.Current);
            button.style.opacity = locked ? 0.45f : 1f;
        }

        void UpdateSegments()
        {
            var marks = StepperModel.Marks(progress, report);
            for (int i = 0; i < segmentButtons.Count && i < marks.Length; i++)
                PaintSegmentButton(segmentButtons[i], marks[i], StepperCatalog.Get((DesignerStep)(i + 1)));
        }

        VisualElement Rocket(HangarUi ui, StepDef def)
        {
            var band = ui.Column();
            band.name = "stepper-rocket";
            band.AddToClassList("stepper-rocket");
            band.style.height = ui.U(StepperLayout.RocketBandPt);
            band.style.minHeight = ui.U(StepperLayout.RocketBandPt);
            band.style.maxHeight = ui.U(StepperLayout.RocketBandPt);
            band.style.alignItems = Align.Center;
            band.style.justifyContent = Justify.Center;
            band.style.overflow = Overflow.Hidden;
            string hot = def.Highlight ?? "";
            var stack = ui.Column();
            stack.style.alignItems = Align.Center;
            stack.Add(Part(ui, 18f, 10f, hot == "payload" || hot == "fairing", "stepper-rocket-nose"));
            stack.Add(Part(ui, 28f, 36f, hot == "tanks" || hot == "body" || hot == "fairing", "stepper-rocket-body"));
            var engines = Part(ui, 22f, 14f, hot == "engines", "stepper-rocket-engines");
            stack.Add(engines);
            if (hot == "legs")
            {
                var legs = ui.Row(2f);
                legs.name = "stepper-rocket-legs";
                legs.Add(Part(ui, 4f, 12f, true, null));
                legs.Add(Part(ui, 4f, 12f, true, null));
                stack.Add(legs);
            }
            band.Add(stack);
            var caption = ui.Text(Caption(def), HangarUi.SmallPt, HangarUi.Dim, bold: true);
            caption.name = "stepper-rocket-caption";
            caption.style.unityTextAlign = TextAnchor.MiddleCenter;
            band.Add(caption);
            band.tooltip = caption.text;
            return band;
        }

        VisualElement Part(HangarUi ui, float w, float h, bool hot, string name)
        {
            var el = new VisualElement();
            if (!string.IsNullOrEmpty(name)) el.name = name;
            el.style.width = ui.U(w);
            el.style.height = ui.U(h);
            el.style.backgroundColor = hot ? HangarUi.ChipOn : new Color(0.85f, 0.9f, 0.95f, 0.85f);
            ui.Radius(el.style, 2f);
            el.style.marginTop = ui.U(1f);
            return el;
        }

        VisualElement Goal(HangarUi ui, StepDef def)
        {
            var card = ui.CardBox();
            card.name = "stepper-goal";
            card.style.borderLeftWidth = ui.U(3f);
            card.style.borderLeftColor = HangarUi.ChipOn;
            card.Add(ui.Section("Goal"));
            string goal = StepperCopy.Current.FileText(StepperCopy.GoalId(def.Step)) ?? def.Goal;
            var body = ui.Text(goal, HangarUi.BodyPt, HangarUi.TextColor, bold: true);
            body.name = "stepper-goal-body";
            body.style.whiteSpace = WhiteSpace.Normal;
            card.Add(body);
            card.tooltip = "Goal. " + goal;
            return card;
        }

        VisualElement Why(HangarUi ui, StepDef def)
        {
            var card = ui.CardBox();
            card.name = "stepper-why";
            card.Add(ui.Section("Why this matters"));
            string kid = StepperCopy.Current.Resolve(StepperCopy.WhyKidId(def.Step));
            string eng = StepperCopy.Current.Resolve(StepperCopy.WhyEngineerId(def.Step));
            var kidLabel = ui.Text(kid, HangarUi.BodyPt);
            kidLabel.name = "stepper-why-kid";
            kidLabel.style.whiteSpace = WhiteSpace.Normal;
            card.Add(kidLabel);
            var engLabel = ui.Text("⚙  " + eng, HangarUi.SmallPt, HangarUi.Dim);
            engLabel.name = "stepper-why-engineer";
            engLabel.style.whiteSpace = WhiteSpace.Normal;
            engLabel.style.marginTop = ui.U(4f);
            card.Add(engLabel);
            card.tooltip = kid + " " + eng;
            return card;
        }

        VisualElement Stats(HangarUi ui)
        {
            var row = ui.Row(4f);
            row.name = "stepper-stats";
            row.style.justifyContent = Justify.SpaceBetween;
            var cells = StepperStats.Build(progress.Current, report.Readout, bridge.Design, bridge.Catalog);
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = ui.Column();
                cell.name = "stat-" + cells[i].Key;
                cell.style.flexGrow = 1;
                cell.style.flexBasis = 0;
                cell.style.alignItems = Align.Center;
                cell.style.backgroundColor = HangarUi.Card;
                ui.Radius(cell.style, 10f);
                ui.Pad(cell.style, 6f, 4f);
                cell.style.marginLeft = ui.U(2f);
                cell.style.marginRight = ui.U(2f);
                cell.Add(ui.Text(cells[i].Label, HangarUi.SmallPt, HangarUi.Dim));
                var value = ui.Text(cells[i].Value, HangarUi.BodyPt, StateColor(cells[i].State), bold: true);
                value.name = "stat-value-" + cells[i].Key;
                value.style.unityTextAlign = TextAnchor.MiddleCenter;
                value.style.whiteSpace = WhiteSpace.Normal;
                cell.Add(value);
                cell.tooltip = cells[i].Label + " " + cells[i].Value + (cells[i].Hint != null ? ". " + cells[i].Hint : "");
                statValues.Add(value);
                statCache.Add(cells[i]);
                row.Add(cell);
            }
            return row;
        }

        VisualElement CheckCard(HangarUi ui, StepCheck check)
        {
            var card = ui.CardBox();
            card.name = "stepper-check";
            card.Add(ui.Section("Check before next"));
            checkBody = ui.Text("", HangarUi.BodyPt, HangarUi.TextColor, bold: true);
            checkBody.name = "stepper-check-body";
            checkBody.style.whiteSpace = WhiteSpace.Normal;
            card.Add(checkBody);
            checkExtra = ui.Text("", HangarUi.SmallPt, HangarUi.Dim);
            checkExtra.name = "stepper-check-extra";
            checkExtra.style.whiteSpace = WhiteSpace.Normal;
            checkExtra.style.marginTop = ui.U(4f);
            card.Add(checkExtra);
            UpdateCheck(check);
            return card;
        }

        void UpdateCheck(StepCheck check)
        {
            if (checkBody == null || check == null) return;
            string main = Headline(check);
            string extra = Detail(check, main);
            string mark = check.Gate == StepGate.Red ? "✕" : check.Gate == StepGate.Amber ? "!" : check.Gate == StepGate.Skip ? "–" : "✓";
            checkBody.text = mark + "  " + main;
            checkBody.style.color = StateColor(GateColor(check.Gate));
            checkExtra.text = extra ?? "";
            checkExtra.style.display = string.IsNullOrEmpty(extra) ? DisplayStyle.None : DisplayStyle.Flex;
            if (checkBody.parent != null) checkBody.parent.tooltip = "Check. " + main;
        }

        VisualElement Confirm(HangarUi ui)
        {
            var card = ui.CardBox();
            card.name = "stepper-confirm";
            card.Add(ui.Text("This step still has a warning. Go on anyway?", HangarUi.BodyPt, HangarUi.TextColor, bold: true));
            var row = ui.Row();
            var stay = ui.Btn("Stay", () => { confirmStep = false; Sync(); }, "Stay on this step", minWidthPt: 72f);
            stay.name = "stepper-stay";
            var go = ui.Btn("Go on", () => { confirmStep = true; PressForward(); }, "Open the next step with the warning", minWidthPt: 72f);
            go.name = "stepper-go";
            go.style.backgroundColor = HangarUi.Amber;
            go.style.color = HangarUi.Ink;
            row.Add(stay);
            row.Add(go);
            card.Add(row);
            return card;
        }

        VisualElement Nav(HangarUi ui, StepCheck check)
        {
            var row = ui.Row(4f);
            row.name = "stepper-nav";
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginTop = ui.U(4f);
            row.style.marginBottom = ui.U(4f);
            backButton = ui.Btn("<  Back", () => PressBack(), "Previous step", minWidthPt: 88f);
            backButton.name = "stepper-back";
            backButton.userData = (Action)(() => PressBack());
            StyleNav(ui, backButton, false);
            nextButton = ui.Btn("Next anyway", () => PressForward(), "Go on with the warning", minWidthPt: 120f);
            nextButton.name = "stepper-next";
            nextButton.style.flexGrow = 0;
            nextButton.style.flexShrink = 0;
            StyleNav(ui, nextButton, false);
            forwardButton = ui.Btn("Next", () =>
            {
                if (report != null && WantsFix(report[progress.Current])) PressFix();
                else PressForward();
            }, "Next step", minWidthPt: 120f);
            forwardButton.name = "stepper-forward";
            forwardButton.style.flexGrow = 1;
            StyleNav(ui, forwardButton, true);
            row.Add(backButton);
            row.Add(nextButton);
            row.Add(forwardButton);
            UpdateNav(check);
            return row;
        }

        void UpdateNav(StepCheck check)
        {
            if (backButton == null || forwardButton == null || nextButton == null || check == null) return;
            var backAction = StepperModel.Back(progress, report);
            var forward = StepperModel.Forward(progress, report);
            backButton.SetEnabled(backAction.Allowed);
            backButton.style.opacity = backAction.Allowed ? 1f : 0.4f;
            bool fix = WantsFix(check);
            bool review = progress.Current == DesignerStep.Review;
            bool showNext = fix && ((check.Gate == StepGate.Amber && !review) || (review && (forward.NeedsConfirm || forward.Label == StepperLaunch.LaunchAnywayLabel)));
            nextButton.text = review ? StepperLaunch.LaunchAnywayLabel : "Next anyway";
            nextButton.tooltip = review ? StepperLaunch.ConfirmTitle : "Go on with the warning.";
            nextButton.style.display = showNext ? DisplayStyle.Flex : DisplayStyle.None;
            nextButton.RemoveFromClassList("stepper-forward--go");
            nextButton.RemoveFromClassList("stepper-forward--fix");
            nextButton.AddToClassList("stepper-forward--amber");
            nextButton.style.backgroundColor = HangarUi.Amber;
            nextButton.style.color = HangarUi.Ink;
            nextButton.style.whiteSpace = WhiteSpace.NoWrap;

            forwardButton.text = fix ? "Fix it" : forward.Label;
            forwardButton.tooltip = fix
                ? (string.IsNullOrEmpty(fixNote) ? "Apply one change that clears this warning. " + (check.Kid ?? "") : fixNote)
                : ForwardTip(forward, check, null);
            forwardButton.style.whiteSpace = WhiteSpace.NoWrap;
            forwardButton.RemoveFromClassList("stepper-forward--go");
            forwardButton.RemoveFromClassList("stepper-forward--amber");
            forwardButton.RemoveFromClassList("stepper-forward--fix");
            if (fix)
            {
                forwardButton.AddToClassList("stepper-forward--fix");
                forwardButton.style.backgroundColor = HangarUi.Red;
                forwardButton.style.color = HangarUi.TextColor;
            }
            else if (forward.NeedsConfirm || check.Gate == StepGate.Amber)
            {
                forwardButton.AddToClassList("stepper-forward--amber");
                forwardButton.style.backgroundColor = HangarUi.Amber;
                forwardButton.style.color = HangarUi.Ink;
            }
            else
            {
                forwardButton.AddToClassList("stepper-forward--go");
                forwardButton.style.backgroundColor = HangarUi.ChipOn;
                forwardButton.style.color = HangarUi.Ink;
            }
        }

        VisualElement AllPartsButton(HangarUi ui, string name, string label)
        {
            var button = ui.Btn(label, ShowAllParts, "Open every part at once. Same rocket.", minWidthPt: 120f);
            button.name = name;
            button.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            button.style.color = HangarUi.ChipOn;
            return button;
        }

        void OnRisk(FlightRisk risk)
        {
            if (risk == null) return;
            var step = StepForRisk(risk.tab);
            if (step.HasValue && report != null && report[step.Value].Gate != StepGate.Skip
                && StepperModel.CanJump(progress, report, step.Value))
            {
                StepperModel.TryJump(progress, report, step.Value);
                Persist();
                focusField = StepperControls.Canonical(risk.field);
                if (StepperControls.IsEngineerField(focusField)) moreOpen = true;
                scrollFocus = true;
                Sync();
                return;
            }
            progress.ShowAllParts();
            Persist();
            ClearHiddenChrome();
            bridge.JumpRisk?.Invoke(risk);
        }

        void HighlightFocus(bool scrollTo)
        {
            if (focusEl != null) focusEl.RemoveFromClassList("stepper-focus");
            focusEl = FindField(focusField);
            if (focusEl == null) return;
            focusEl.AddToClassList("stepper-focus");
            focusEl.style.borderLeftWidth = bridge.Ui.U(2f);
            focusEl.style.borderRightWidth = bridge.Ui.U(2f);
            focusEl.style.borderTopWidth = bridge.Ui.U(2f);
            focusEl.style.borderBottomWidth = bridge.Ui.U(2f);
            focusEl.style.borderLeftColor = HangarUi.ChipOn;
            focusEl.style.borderRightColor = HangarUi.ChipOn;
            focusEl.style.borderTopColor = HangarUi.ChipOn;
            focusEl.style.borderBottomColor = HangarUi.ChipOn;
            if (!scrollTo || scroll == null) return;
            if (scroll.panel != null) scroll.ScrollTo(focusEl);
            savedScroll = scroll.scrollOffset.y;
            bridge.Focus?.Invoke(focusField);
        }

        void OnScrollPointer(PointerDownEvent e)
        {
            string field = FieldFrom(e.target as VisualElement, scroll);
            if (!string.IsNullOrEmpty(field)) focusField = field;
        }

        static string FieldFrom(VisualElement ve, VisualElement stop)
        {
            while (ve != null && ve != stop)
            {
                string name = ve.name;
                if (!string.IsNullOrEmpty(name) && name.StartsWith("field-", StringComparison.Ordinal))
                {
                    string field = name.Substring(6);
                    if (field.EndsWith("-value", StringComparison.Ordinal)) field = field.Substring(0, field.Length - 6);
                    return field;
                }
                ve = ve.parent;
            }
            return null;
        }

        void Reveal(string field)
        {
            string canon = StepperControls.Canonical(field);
            if (StepperControls.IsEngineerField(canon)) moreOpen = true;
            focusField = canon;
            scrollFocus = true;
            if (FindField(canon) == null)
            {
                var dest = StepForField(canon);
                if (dest.HasValue && dest.Value != progress.Current && report != null
                    && report[dest.Value].Gate != StepGate.Skip
                    && StepperModel.CanJump(progress, report, dest.Value)
                    && StepperModel.TryJump(progress, report, dest.Value))
                {
                    confirmStep = false;
                    Persist();
                }
            }
            Sync();
        }

        void ApplyScrollOffset(float y)
        {
            savedScroll = y;
            if (scroll == null) return;
            var bar = scroll.verticalScroller;
            if (bar != null && bar.highValue < y) bar.highValue = y;
            holdScroll = true;
            scroll.scrollOffset = new Vector2(0f, y);
            holdScroll = false;
        }

        void WatchScroll()
        {
            var bar = scroll != null ? scroll.verticalScroller : null;
            if (bar == null) return;
            bar.valueChanged += y =>
            {
                if (!holdScroll) savedScroll = y;
            };
        }

        void ConsumeNote(StepCheck check)
        {
            if (!keepInstruction)
            {
                if (check == null || check.Gate == StepGate.Pass || (shownCode != null && check.Code != shownCode))
                    fixNote = null;
            }
            shownCode = check != null ? check.Code : null;
            keepInstruction = false;
        }

        static bool WantsFix(StepCheck check)
        {
            if (check == null || string.IsNullOrEmpty(check.Code)) return false;
            return check.Gate == StepGate.Red || check.Gate == StepGate.Amber;
        }

        string Headline(StepCheck check)
        {
            if (!string.IsNullOrEmpty(fixNote)) return StepperPlain.Clean(fixNote);
            if (!bridge.IsEngineer)
            {
                string kid = FileKid(check);
                if (!string.IsNullOrEmpty(kid)) return kid;
                string numbers = StepperPlain.Numbers(check);
                if (!string.IsNullOrEmpty(numbers)) return numbers;
                return check.Gate == StepGate.Pass ? "Ready." : "Check this step.";
            }
            string line = StepperPlain.Numbers(check);
            if (!string.IsNullOrEmpty(line)) return line;
            string eng = StepperPlain.Clean(check.Engineer);
            if (!string.IsNullOrEmpty(eng)) return eng;
            return FileKid(check);
        }

        string Detail(StepCheck check, string headline)
        {
            if (!string.IsNullOrEmpty(fixNote)) return "";
            if (!bridge.IsEngineer)
            {
                string numbers = StepperPlain.Numbers(check);
                if (!string.IsNullOrEmpty(numbers) && numbers != headline) return numbers;
                return "";
            }
            string kidLine = FileKid(check);
            if (!string.IsNullOrEmpty(kidLine) && kidLine != headline) return kidLine;
            return "";
        }

        static string FileKid(StepCheck check)
        {
            if (check != null && !string.IsNullOrEmpty(check.Code))
            {
                string over = StepperCopy.Current.FileText(StepperCopy.CheckKidId(check.Code));
                if (!string.IsNullOrEmpty(over))
                {
                    string clean = StepperPlain.Clean(over);
                    if (!string.IsNullOrEmpty(clean)) return clean;
                }
            }
            return StepperPlain.Kid(check);
        }

        static DesignerStep? StepForField(string field)
        {
            switch (StepperControls.Canonical(field))
            {
                case "mission":
                case "payload":
                case "orbit":
                    return DesignerStep.Mission;
                case "propellant":
                case "engineCount":
                case "mixtureOffsetPct":
                case "pc":
                case "expansionRatio":
                case "gimbal":
                    return DesignerStep.Engines;
                case "propMass":
                case "stages":
                case "safetyFactor":
                case "pressure":
                case "insulation":
                case "material":
                case "avionics":
                    return DesignerStep.Tanks;
                case "nose":
                case "fins":
                case "fairing":
                case "maxQThrottle":
                case "gLimit":
                    return DesignerStep.Aero;
                case "recoveryMode":
                case "legs":
                case "landingReserve":
                    return DesignerStep.Recovery;
                default:
                    return null;
            }
        }

        VisualElement FindField(string field)
        {
            if (string.IsNullOrEmpty(field)) return null;
            var direct = root.Q("field-" + field);
            if (direct != null) return direct;
            string canon = StepperControls.Canonical(field);
            return root.Q("field-" + canon);
        }

        void PaintStats(List<StepStat> cells)
        {
            int n = Math.Min(cells.Count, statValues.Count);
            for (int i = 0; i < n; i++)
            {
                if (statCache.Count > i && statCache[i].Key != cells[i].Key) continue;
                statValues[i].text = cells[i].Value;
                statValues[i].style.color = StateColor(cells[i].State);
            }
        }

        void EnsureProgress()
        {
            var raw = bridge.Design?.Obj(StepperProgress.Field);
            string snap = raw == null ? "" : JsonWriter.Stringify(raw);
            if (progress != null && snap == progressSnap) return;
            progress = StepperProgress.Read(bridge.Design);
            progressSnap = snap;
        }

        void Persist()
        {
            if (progress == null || bridge.Design == null) return;
            progress.Write(bridge.Design);
            var raw = bridge.Design.Obj(StepperProgress.Field);
            progressSnap = raw == null ? "" : JsonWriter.Stringify(raw);
        }

        StepperReport Evaluate()
        {
            var ctx = new StepperContext
            {
                Depth = bridge.IsEngineer ? DesignerDepth.Engineer : DesignerDepth.Simple,
                LaunchConfirmArmed = bridge.LaunchArmed != null && bridge.LaunchArmed(),
                Campaign = StepperControls.SharedCampaign(),
            };
            return StepperModel.Evaluate(bridge.Catalog, bridge.Design, ctx);
        }

        DesignerStep EditedStep(StepperReport before)
        {
            if (progress.View == StepperView.Guided) return progress.Current;
            if (bridge.Tab == null) return progress.Current;
            switch (bridge.Tab())
            {
                case DesignerV3Tab.Payload: return DesignerStep.Mission;
                case DesignerV3Tab.Propellant:
                case DesignerV3Tab.Engines: return DesignerStep.Engines;
                case DesignerV3Tab.Tanks:
                case DesignerV3Tab.Stages:
                case DesignerV3Tab.Structure: return DesignerStep.Tanks;
                case DesignerV3Tab.Aero:
                case DesignerV3Tab.Guidance: return DesignerStep.Aero;
                case DesignerV3Tab.Recovery: return DesignerStep.Recovery;
                case DesignerV3Tab.Look: return DesignerStep.Look;
                default: return before != null ? progress.Current : DesignerStep.Mission;
            }
        }

        void HideLegacy()
        {
            var list = bridge.HideWhenGuided;
            if (list == null) return;
            for (int i = 0; i < list.Length; i++)
                if (list[i] != null) list[i].style.display = DisplayStyle.None;
        }

        void ClearHiddenChrome()
        {
            var list = bridge.HideWhenGuided;
            if (list == null) return;
            for (int i = 0; i < list.Length; i++)
                if (list[i] != null) list[i].style.display = StyleKeyword.Null;
        }

        void EnsureGuidedButton()
        {
            var list = bridge.HideWhenGuided;
            VisualElement row = null;
            if (list != null)
            {
                for (int i = 0; i < list.Length; i++)
                    if (list[i] != null && list[i].name == "designer-launch") { row = list[i]; break; }
                if (row == null && list.Length > 0) row = list[list.Length - 1];
            }
            if (row == null || row.Q("stepper-guided") != null) return;
            var button = bridge.Ui.Btn("Guided steps", EnterGuided, "Back to the seven steps. Same rocket.", minWidthPt: 120f);
            button.name = "stepper-guided";
            row.Insert(0, button);
        }

        void ApplyScrollMax()
        {
            if (scroll == null) return;
            scroll.style.maxHeight = scrollMax > 0f ? scrollMax : StyleKeyword.Null;
        }

        static void StyleNav(HangarUi ui, Button button, bool primary)
        {
            button.style.height = ui.U(StepperLayout.NavButtonPt);
            button.style.minHeight = ui.U(StepperLayout.NavButtonPt);
            button.style.fontSize = ui.U(16f);
            button.style.whiteSpace = WhiteSpace.NoWrap;
            if (primary) button.style.flexGrow = 1;
        }

        static string ForwardTip(StepperAction action, StepCheck check, string note)
        {
            if (action.FixIt && !string.IsNullOrEmpty(note)) return note;
            if (action.FixIt) return "Apply one change that clears this warning. " + (check.Kid ?? "");
            if (action.NeedsConfirm && action.Launches) return StepperLaunch.ConfirmTitle;
            if (action.NeedsConfirm) return "This step has a warning. Confirm to go on.";
            if (action.Launches) return "Launch this design";
            return action.Label;
        }

        static string Caption(StepDef def)
        {
            switch (def.Highlight)
            {
                case "engines": return "Stage 1 engines";
                case "tanks": return "Tanks and stages";
                case "fairing": return "Aero and fairing";
                case "legs": return "Recovery";
                case "payload": return "Mission";
                case "body": return "Look";
                case "stack": return "Pre-flight";
                default: return def.Title;
            }
        }

        static string Glyph(StepMark mark, int number)
        {
            switch (mark)
            {
                case StepMark.Done: return "✓";
                case StepMark.Warn: return "!";
                case StepMark.Failed: return "✕";
                case StepMark.Skipped: return "–";
                default: return number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        static string SegClass(StepMark mark)
        {
            switch (mark)
            {
                case StepMark.Current: return "stepper-seg--current";
                case StepMark.Done: return "stepper-seg--done";
                case StepMark.Warn: return "stepper-seg--warn";
                case StepMark.Failed: return "stepper-seg--failed";
                case StepMark.Skipped: return "stepper-seg--skipped";
                case StepMark.Open: return "stepper-seg--open";
                default: return "stepper-seg--locked";
            }
        }

        static void PaintSegment(Button button, StepMark mark)
        {
            switch (mark)
            {
                case StepMark.Current:
                    button.style.backgroundColor = HangarUi.ChipOn;
                    button.style.color = HangarUi.Ink;
                    button.style.borderLeftColor = HangarUi.ChipOn;
                    button.style.borderRightColor = HangarUi.ChipOn;
                    button.style.borderTopColor = HangarUi.ChipOn;
                    button.style.borderBottomColor = HangarUi.ChipOn;
                    break;
                case StepMark.Done:
                    button.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
                    button.style.color = HangarUi.ChipOn;
                    button.style.borderLeftColor = HangarUi.ChipOn;
                    button.style.borderRightColor = HangarUi.ChipOn;
                    button.style.borderTopColor = HangarUi.ChipOn;
                    button.style.borderBottomColor = HangarUi.ChipOn;
                    break;
                case StepMark.Warn:
                    button.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
                    button.style.color = HangarUi.Amber;
                    button.style.borderLeftColor = HangarUi.Amber;
                    button.style.borderRightColor = HangarUi.Amber;
                    button.style.borderTopColor = HangarUi.Amber;
                    button.style.borderBottomColor = HangarUi.Amber;
                    break;
                case StepMark.Failed:
                    button.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
                    button.style.color = HangarUi.Red;
                    button.style.borderLeftColor = HangarUi.Red;
                    button.style.borderRightColor = HangarUi.Red;
                    button.style.borderTopColor = HangarUi.Red;
                    button.style.borderBottomColor = HangarUi.Red;
                    break;
                default:
                    button.style.backgroundColor = HangarUi.ChipOff;
                    button.style.color = HangarUi.Dim;
                    button.style.borderLeftColor = new Color(1f, 1f, 1f, 0.08f);
                    button.style.borderRightColor = new Color(1f, 1f, 1f, 0.08f);
                    button.style.borderTopColor = new Color(1f, 1f, 1f, 0.08f);
                    button.style.borderBottomColor = new Color(1f, 1f, 1f, 0.08f);
                    break;
            }
        }

        static string SegmentTip(StepDef def, StepMark mark)
        {
            string state = mark.ToString();
            if (mark == StepMark.Locked) return "Step " + def.Number + ", " + def.Title + ". Locked until you open it.";
            if (mark == StepMark.Current) return "Step " + def.Number + ", " + def.Title + ". Current step.";
            return "Step " + def.Number + ", " + def.Title + ". " + state + ". Tap to open.";
        }

        static DesignerStep? StepForRisk(string tab)
        {
            switch ((tab ?? "").ToLowerInvariant())
            {
                case "payload": return DesignerStep.Mission;
                case "propellant":
                case "fuel":
                case "engines":
                case "engine": return DesignerStep.Engines;
                case "tanks":
                case "tank":
                case "stages":
                case "stage":
                case "structure":
                case "material": return DesignerStep.Tanks;
                case "aero":
                case "fins":
                case "fairing":
                case "guidance": return DesignerStep.Aero;
                case "avionics": return DesignerStep.Tanks;
                case "recovery":
                case "landing":
                case "legs": return DesignerStep.Recovery;
                case "look": return DesignerStep.Look;
                default: return null;
            }
        }

        static CellState GateColor(StepGate gate)
        {
            switch (gate)
            {
                case StepGate.Red: return CellState.Bad;
                case StepGate.Amber: return CellState.Warn;
                case StepGate.Pass: return CellState.Good;
                default: return CellState.Neutral;
            }
        }

        static Color StateColor(CellState state)
        {
            switch (state)
            {
                case CellState.Bad: return HangarUi.Red;
                case CellState.Warn: return HangarUi.Amber;
                case CellState.Good: return HangarUi.Green;
                default: return HangarUi.TextColor;
            }
        }
    }
}
