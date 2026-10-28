// Phase 1 "Starter" designer screen (PR 5). Mission header, a clear window onto the procedural rocket on
// the pad (SliceApp's HangarRocketView draws it behind this panel), the four controls, one hint line, one
// readiness line and an always-visible Launch, with Back top-left and Reset top-right. Every element is
// pinned to a Kestrel.Core.Designer.StarterLayout box, so the Core size tests and the EditMode device test
// check the same rectangles. No animation: state changes are instant (photosensitivity-safe).
using System;
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using Kestrel.Game.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class StarterPanel
    {
        public const string RootName = UiIds.ScreenStarter;

        static readonly Color SheetBg = new Color(8f / 255f, 12f / 255f, 20f / 255f, 0.88f);
        static readonly Color CornerBg = new Color(8f / 255f, 12f / 255f, 20f / 255f, 0.80f);
        static readonly Color Ink = new Color(0.95f, 0.95f, 0.95f);
        static readonly Color Muted = new Color(0.78f, 0.82f, 0.90f);
        static readonly Color Accent = new Color(1f, 176f / 255f, 0f);
        static readonly Color AccentInk = new Color(20f / 255f, 20f / 255f, 30f / 255f);
        static readonly Color OptionBg = new Color(1f, 1f, 1f, 0.10f);
        static readonly Color ReadyGreen = new Color(126f / 255f, 224f / 255f, 138f / 255f);
        static readonly Color TightAmber = new Color(1f, 196f / 255f, 92f / 255f);
        static readonly Color ShortRed = new Color(1f, 128f / 255f, 128f / 255f);
        static readonly Color LaunchRed = new Color(200f / 255f, 16f / 255f, 46f / 255f);
        static readonly Color LaunchOff = new Color(90f / 255f, 90f / 255f, 100f / 255f);

        sealed class Row
        {
            public VisualElement Box;
            public Label Label;
            public Button[] Options;
        }

        public event Action LaunchRequested;
        public event Action BackRequested;

        public StarterRocket Rocket { get; }
        public VisualElement Root { get; }
        public StarterFrame Frame { get; private set; }
        public StarterLayoutInput Input { get; private set; }
        public bool LaunchReady { get; private set; }
        public StarterKnob LastKnob { get; private set; } = StarterKnob.None;
        public bool Visible => Root.resolvedStyle.display != DisplayStyle.None && shown;

        readonly VisualElement rocketBox, header, sheet;
        readonly Label title, subtitle, hint, badge, readiness, fuelLess, fuelMore;
        readonly Button back, reset, launch;
        readonly Row engine, stages, fuel, boosters;
        readonly VisualElement[] fuelDots = new VisualElement[StarterRocket.FuelNotches];
        float upp = 1f, wPt, hPt, safeT, safeB, safeL, safeR;
        bool wide, reserveExit, shown = true, laidOut;

        public StarterPanel(StarterRocket rocket, VisualElement parent, int insertAt = -1)
        {
            Rocket = rocket ?? throw new ArgumentNullException(nameof(rocket));
            Root = new VisualElement { name = RootName, pickingMode = PickingMode.Ignore };
            Root.style.position = Position.Absolute;
            Root.style.left = 0f; Root.style.top = 0f; Root.style.right = 0f; Root.style.bottom = 0f;

            rocketBox = new VisualElement { name = UiIds.StarterRocket, pickingMode = PickingMode.Ignore };
            rocketBox.tooltip = "Your rocket on the pad.";
            Root.Add(rocketBox);

            header = new VisualElement { name = "starter-header", pickingMode = PickingMode.Ignore };
            title = Text(UiIds.StarterTitle, 20f, Ink, true, true);
            subtitle = Text(UiIds.StarterSubtitle, 14f, Muted, false, true);
            header.Add(title);
            header.Add(subtitle);

            sheet = new VisualElement { name = UiIds.StarterSheet };
            sheet.style.backgroundColor = SheetBg;
            Root.Add(sheet);

            engine = MakeRow(UiIds.StarterEngine, "engine", StarterScreen.EngineLabel, StarterRocket.EngineOrder.Count, k => UiIds.StarterEngineOption(StarterRocket.EngineOrder[k]), k => Pick(StarterKnob.Engine, () => Rocket.Engine = StarterRocket.EngineOrder[k]));
            stages = MakeRow(UiIds.StarterStages, "stages", StarterScreen.StagesLabel, StarterRocket.StageValues.Count, k => UiIds.StarterStagesOption(StarterRocket.StageValues[k]), k => Pick(StarterKnob.Stages, () => Rocket.Stages = StarterRocket.StageValues[k]));
            fuel = MakeRow(UiIds.StarterFuel, "fuel", StarterScreen.FuelLabel, StarterRocket.FuelNotches, k => UiIds.StarterFuelNotch(k + 1), k => Pick(StarterKnob.FuelNotch, () => Rocket.FuelNotch = k));
            boosters = MakeRow(UiIds.StarterBoosters, "boosters", StarterScreen.BoostersLabel, StarterRocket.BoosterValues.Count, k => UiIds.StarterBoostersOption(StarterRocket.BoosterValues[k]), k => Pick(StarterKnob.Boosters, () => Rocket.Boosters = StarterRocket.BoosterValues[k]));
            for (int i = 0; i < StarterRocket.EngineOrder.Count; i++) engine.Options[i].text = StarterScreen.OptionText(StarterRocket.EngineOrder[i]);
            for (int i = 0; i < StarterRocket.StageValues.Count; i++) stages.Options[i].text = StarterRocket.StageValues[i].ToString(System.Globalization.CultureInfo.InvariantCulture);
            for (int i = 0; i < StarterRocket.BoosterValues.Count; i++) boosters.Options[i].text = StarterScreen.BoosterText(StarterRocket.BoosterValues[i]);
            engine.Label.tooltip = StarterScreen.EngineHint;
            stages.Label.tooltip = StarterScreen.StagesHint;
            fuel.Label.tooltip = StarterScreen.FuelHint;
            boosters.Label.tooltip = StarterScreen.BoostersHint;
            for (int i = 0; i < StarterRocket.FuelNotches; i++)
            {
                var dot = new VisualElement { name = "starter-fuel-dot-" + (i + 1), pickingMode = PickingMode.Ignore };
                dot.style.position = Position.Absolute;
                fuelDots[i] = dot;
                fuel.Options[i].Add(dot);
            }
            fuelLess = Text("starter-label-less", 13f, Muted, false, false);
            fuelLess.text = StarterScreen.FuelLess;
            fuelMore = Text("starter-label-more", 13f, Muted, false, false);
            fuelMore.text = StarterScreen.FuelMore;
            fuelMore.style.unityTextAlign = TextAnchor.MiddleRight;
            fuel.Box.Add(fuelLess);
            fuel.Box.Add(fuelMore);

            hint = Text(UiIds.StarterHint, 13f, Muted, false, true);
            badge = Text(UiIds.StarterBadge, 16f, ReadyGreen, true, false);
            readiness = Text(UiIds.StarterReadiness, 16f, Ink, false, true);
            sheet.Add(hint);
            sheet.Add(badge);
            sheet.Add(readiness);

            launch = new Button(() => { if (LaunchReady) LaunchRequested?.Invoke(); }) { name = UiIds.StarterLaunch, text = StarterScreen.LaunchText };
            Style(launch, LaunchRed, Color.white, true);
            sheet.Add(launch);

            back = new Button(() => BackRequested?.Invoke()) { name = UiIds.StarterBack, text = StarterScreen.BackText, tooltip = StarterScreen.BackHint };
            Style(back, CornerBg, Color.white, true);
            reset = new Button(() => { LastKnob = StarterKnob.None; Rocket.Reset(); Refresh(); }) { name = UiIds.StarterReset, text = StarterScreen.ResetText, tooltip = StarterScreen.ResetHint };
            Style(reset, CornerBg, Color.white, true);
            Root.Add(back);
            Root.Add(reset);
            Root.Add(header); // re-parented into the sheet on landscape web

            Rocket.Changed += OnRocketChanged;
            if (parent != null)
            {
                if (insertAt >= 0 && insertAt <= parent.childCount) parent.Insert(insertAt, Root);
                else parent.Add(Root);
            }
            Refresh();
        }

        public void Detach()
        {
            Rocket.Changed -= OnRocketChanged;
            Root.RemoveFromHierarchy();
        }

        public void SetVisible(bool on)
        {
            shown = on;
            Root.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Top of the sheet in panel units (portrait), for the rocket framing above it. NaN before the first layout.</summary>
        public float SheetTopPanel => Frame == null ? float.NaN : Frame.Sheet.Y * upp;

        /// <summary>Lay out for a panel of <paramref name="widthPt"/> x <paramref name="heightPt"/> points; <paramref name="unitsPerPoint"/>
        /// converts points to panel units (1 on landscape web, where the 1280x720 panel's units are the type-cap points).</summary>
        public void Layout(float widthPt, float heightPt, float safeTop, float safeBottom, float safeLeft, float safeRight,
            float unitsPerPoint, bool wideWeb, bool reserveWebExit)
        {
            wPt = widthPt; hPt = heightPt; safeT = safeTop; safeB = safeBottom; safeL = safeLeft; safeR = safeRight;
            upp = unitsPerPoint > 0f ? unitsPerPoint : 1f;
            wide = wideWeb; reserveExit = reserveWebExit;
            laidOut = true;
            Relayout();
        }

        /// <summary>Selection, allowed options, hint and readiness from the rocket; re-places when a line count changes.</summary>
        public void Refresh()
        {
            var r = Rocket;
            Select(engine, k => r.Engine == StarterRocket.EngineOrder[k], k => r.IsEngineAllowed(StarterRocket.EngineOrder[k]));
            Select(stages, k => r.Stages == StarterRocket.StageValues[k], k => r.IsStagesAllowed(StarterRocket.StageValues[k]));
            Select(fuel, k => r.FuelNotch == k, k => r.IsFuelNotchAllowed(k));
            for (int k = 0; k < fuel.Options.Length; k++)
            {
                bool filled = k <= r.FuelNotch;
                fuelDots[k].style.backgroundColor = filled ? (k == r.FuelNotch ? AccentInk : Accent) : new Color(1f, 1f, 1f, 0.28f);
                fuel.Options[k].style.backgroundColor = k == r.FuelNotch ? Accent : OptionBg;
                fuel.Options[k].tooltip = StarterScreen.FuelSpoken(k);
            }
            fuel.Box.tooltip = StarterScreen.FuelSpoken(r.FuelNotch);
            Select(boosters, k => r.Boosters == StarterRocket.BoosterValues[k], k => r.IsBoostersAllowed(StarterRocket.BoosterValues[k]));
            boosters.Box.EnableInClassList("starter-locked", r.BoostersLocked);
            boosters.Label.style.color = r.BoostersLocked ? Muted : Ink;
            boosters.Label.tooltip = r.BoostersLocked ? StarterScreen.BoostersLockedHint : StarterScreen.BoostersHint;

            title.text = StarterScreen.Title(r.Mission);
            subtitle.text = StarterScreen.Subtitle(r.Mission);
            hint.text = StarterScreen.Hint(r, LastKnob);
            // The existing readout and the launch predictor (Honest Ready) as plain information. Launch is off only on a
            // hard Validate block, whose one plain reason is the line; a rocket the predictor says breaks up shows its
            // cause and still launches (and may explode). An unchecked setting reads "Test-flying it" until TickPrediction lands.
            LaunchReady = StarterScreen.ReadinessNow(r, out var b, out var rest);
            badge.text = b;
            badge.style.color = b == StarterScreen.ReadyBadge ? ReadyGreen : b == StarterScreen.TightBadge ? TightAmber : ShortRed;
            readiness.text = rest;
            readiness.tooltip = StarterScreen.Spoken(b, rest);
            launch.SetEnabled(LaunchReady);
            launch.style.backgroundColor = LaunchReady ? LaunchRed : LaunchOff;
            launch.tooltip = LaunchReady ? "Launch this rocket." : rest;
            Relayout();
        }

        void OnRocketChanged(StarterRocket _) => Refresh();

        /// <summary>Honest Ready: advance the launch predictor by one frame budget (ReferenceAscent.FrameBudgetMs), the
        /// current setting first, then its one-tap neighbours. SliceApp calls this from Update while Starter shows.</summary>
        public void TickPrediction()
        {
            try { if (Rocket.StepPrediction(ReferenceAscent.FrameBudgetMs)) Refresh(); }
            catch (Exception e) { Debug.LogWarning("[Starter] predictor: " + e.Message); }
        }

        /// <summary>Finish the current setting's prediction now (captures and tests settle the badge with this).</summary>
        public void FinishPrediction()
        {
            try { if (!Rocket.Check().Blocked) Rocket.Verdict(); }
            catch (Exception e) { Debug.LogWarning("[Starter] predictor: " + e.Message); }
            Refresh();
        }

        void Pick(StarterKnob knob, Action set)
        {
            LastKnob = knob;
            set();
            Refresh(); // an unchanged value raises no Changed, but the hint still follows the tap
        }

        void Relayout()
        {
            if (!laidOut) return;
            var r = Rocket;
            Input = new StarterLayoutInput
            {
                Width = wPt, Height = hPt, SafeTop = safeT, SafeBottom = safeB, SafeLeft = safeL, SafeRight = safeR,
                Wide = wide, ReserveExit = reserveExit, BoostersVisible = r.BoostersVisible,
                Title = title.text, Subtitle = subtitle.text, Hint = hint.text, Badge = badge.text, Readiness = readiness.text,
                Engines = Texts(engine), Stages = Texts(stages), Boosters = Texts(boosters),
            };
            var f = StarterLayout.Place(Input);
            Frame = f;
            Apply(f);
        }

        void Apply(StarterFrame f)
        {
            var zero = new HudBox();
            Pin(rocketBox, f.Rocket, zero);
            Pin(sheet, f.Sheet, zero);
            float rad = (f.Wide ? 18f : 18f) * upp;
            sheet.style.borderTopLeftRadius = rad; sheet.style.borderTopRightRadius = rad;
            sheet.style.borderBottomLeftRadius = f.Wide ? rad : 0f; sheet.style.borderBottomRightRadius = f.Wide ? rad : 0f;

            var headerParent = f.HeaderInSheet ? sheet : Root;
            if (header.parent != headerParent) headerParent.Add(header);
            Pin(header, f.Header, f.HeaderInSheet ? f.Sheet : zero);
            Pin(title, f.Title, f.Header); Font(title, f.TitlePt);
            subtitle.style.display = f.Subtitle.Live ? DisplayStyle.Flex : DisplayStyle.None;
            if (f.Subtitle.Live) { Pin(subtitle, f.Subtitle, f.Header); Font(subtitle, f.SubtitlePt); }

            Corner(back, f.Back, f.CornerPt);
            Corner(reset, f.Reset, f.CornerPt);

            PlaceRow(engine, f.Engine, f.Sheet, f.LabelPt, f.OptionPt);
            PlaceRow(stages, f.Stages, f.Sheet, f.LabelPt, f.OptionPt);
            PlaceRow(fuel, f.Fuel, f.Sheet, f.LabelPt, f.OptionPt);
            Pin(fuelLess, f.Fuel.Less, f.Fuel.Row); Font(fuelLess, f.EndPt);
            Pin(fuelMore, f.Fuel.More, f.Fuel.Row); Font(fuelMore, f.EndPt);
            for (int k = 0; k < fuelDots.Length && k < f.Fuel.Options.Length; k++)
            {
                float d = 14f * upp, ow = f.Fuel.Options[k].W * upp, oh = f.Fuel.Options[k].H * upp;
                var dot = fuelDots[k];
                dot.style.width = d; dot.style.height = d;
                dot.style.left = (ow - d) * 0.5f; dot.style.top = (oh - d) * 0.5f;
                dot.style.borderTopLeftRadius = d * 0.5f; dot.style.borderTopRightRadius = d * 0.5f;
                dot.style.borderBottomLeftRadius = d * 0.5f; dot.style.borderBottomRightRadius = d * 0.5f;
            }
            boosters.Box.style.display = f.Boosters != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (f.Boosters != null) PlaceRow(boosters, f.Boosters, f.Sheet, f.LabelPt, f.OptionPt);

            Pin(hint, f.Hint, f.Sheet); Font(hint, f.HintPt);
            badge.style.display = f.Badge.Live ? DisplayStyle.Flex : DisplayStyle.None;
            if (f.Badge.Live) { Pin(badge, f.Badge, f.Sheet); Font(badge, f.ReadinessPt); }
            Pin(readiness, f.Readiness, f.Sheet); Font(readiness, f.ReadinessPt);
            Pin(launch, f.Launch, f.Sheet); Font(launch, f.CtaPt);
            Radius(launch, 14f);
        }

        void PlaceRow(Row row, StarterRowFrame rf, HudBox sheetBox, float labelPt, float optionPt)
        {
            Pin(row.Box, rf.Row, sheetBox);
            Pin(row.Label, rf.Label, rf.Row);
            Font(row.Label, labelPt);
            for (int k = 0; k < row.Options.Length; k++)
            {
                var b = row.Options[k];
                if (k >= rf.Options.Length) { b.style.display = DisplayStyle.None; continue; }
                b.style.display = DisplayStyle.Flex;
                Pin(b, rf.Options[k], rf.Row);
                Font(b, optionPt);
                Radius(b, 10f);
                b.style.paddingLeft = StarterLayout.OptionPadX * upp; b.style.paddingRight = StarterLayout.OptionPadX * upp;
            }
        }

        void Corner(Button b, HudBox box, float pt)
        {
            Pin(b, box, new HudBox());
            Font(b, pt);
            Radius(b, 12f);
            b.style.paddingLeft = StarterLayout.ButtonPadX * upp; b.style.paddingRight = StarterLayout.ButtonPadX * upp;
        }

        Row MakeRow(string rowName, string key, string label, int n, Func<int, string> optionName, Action<int> pick)
        {
            var row = new Row { Box = new VisualElement { name = rowName, pickingMode = PickingMode.Ignore } };
            row.Label = Text("starter-label-" + key, 16f, Ink, true, false);
            row.Label.text = label;
            row.Label.style.unityTextAlign = TextAnchor.MiddleLeft;
            row.Box.Add(row.Label);
            row.Options = new Button[n];
            for (int k = 0; k < n; k++)
            {
                int at = k;
                var b = new Button(() => pick(at)) { name = optionName(k) };
                Style(b, OptionBg, Ink, true);
                row.Options[k] = b;
                row.Box.Add(b);
            }
            sheet.Add(row.Box);
            return row;
        }

        static void Select(Row row, Func<int, bool> selected, Func<int, bool> allowed)
        {
            for (int k = 0; k < row.Options.Length; k++)
            {
                var b = row.Options[k];
                bool on = selected(k), ok = allowed(k);
                // A value the mission does not offer (KH-10/KH-19's landing pair) is greyed and not tappable.
                b.SetEnabled(ok);
                b.EnableInClassList("starter-on", on);
                b.style.backgroundColor = on ? Accent : OptionBg;
                b.style.color = on ? AccentInk : Ink;
                b.style.opacity = ok ? 1f : 0.35f;
            }
        }

        static string[] Texts(Row row)
        {
            var o = new string[row.Options.Length];
            for (int k = 0; k < o.Length; k++) o[k] = row.Options[k].text ?? "";
            return o;
        }

        static Label Text(string name, float pt, Color color, bool bold, bool wraps)
        {
            var l = new Label { name = name, pickingMode = PickingMode.Ignore };
            l.style.color = color;
            l.style.fontSize = pt;
            l.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            l.style.whiteSpace = wraps ? WhiteSpace.Normal : WhiteSpace.NoWrap;
            l.style.marginLeft = 0f; l.style.marginRight = 0f; l.style.marginTop = 0f; l.style.marginBottom = 0f;
            l.style.paddingLeft = 0f; l.style.paddingRight = 0f; l.style.paddingTop = 0f; l.style.paddingBottom = 0f;
            l.style.unityTextAlign = wraps ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            return l;
        }

        static void Style(Button b, Color bg, Color ink, bool bold)
        {
            b.style.backgroundColor = bg;
            b.style.color = ink;
            b.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            b.style.whiteSpace = WhiteSpace.NoWrap;
            b.style.unityTextAlign = TextAnchor.MiddleCenter;
            b.style.borderLeftWidth = 0f; b.style.borderRightWidth = 0f; b.style.borderTopWidth = 0f; b.style.borderBottomWidth = 0f;
            b.style.marginLeft = 0f; b.style.marginRight = 0f; b.style.marginTop = 0f; b.style.marginBottom = 0f;
            b.style.paddingTop = 0f; b.style.paddingBottom = 0f;
        }

        void Radius(VisualElement e, float pt)
        {
            float r = pt * upp;
            e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r; e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
        }

        void Font(VisualElement e, float pt) => e.style.fontSize = pt * upp;

        /// <summary>Absolute box in points, relative to its parent's box, scaled to panel units.</summary>
        void Pin(VisualElement e, HudBox b, HudBox parentBox)
        {
            if (e == null) return;
            e.style.position = Position.Absolute;
            e.style.left = (b.X - parentBox.X) * upp;
            e.style.top = (b.Y - parentBox.Y) * upp;
            e.style.width = Mathf.Max(0f, b.W) * upp;
            e.style.height = Mathf.Max(0f, b.H) * upp;
            e.style.right = StyleKeyword.Auto;
            e.style.bottom = StyleKeyword.Auto;
            e.style.minWidth = StyleKeyword.Null; e.style.minHeight = StyleKeyword.Null;
            e.style.flexShrink = 0f;
        }
    }
}
