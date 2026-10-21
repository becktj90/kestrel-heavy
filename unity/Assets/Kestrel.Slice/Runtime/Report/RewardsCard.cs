// Rewards card on the post-flight sheet. Cause, the flight's own numbers, stars, medals, Kc, then the exits.
// Body over an exits grid, sized by ReportCardChrome.Dress / ReportFit to fit with no scrolling. No clock and no purchase.
using System;
using Kestrel.Core;
using Kestrel.Core.Flight;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class ReportActions
    {
        public Action Next, Again, Missions, Home, Fix;
    }

    public static class RewardsCard
    {
        public static VisualElement Build(FailureEvent failure, TelemetryLog log, ReportFrame frame, bool reduceMotion, Action onFix, FlightDebrief debrief, ReportActions actions)
        {
            var root = new VisualElement { name = ReportView.RootName, pickingMode = PickingMode.Position };
            root.AddToClassList("post-flight");
            root.AddToClassList("portrait");
            if (reduceMotion) root.AddToClassList("reduce-motion");
            ReportView.Place(root, frame.Card);
            root.style.flexDirection = FlexDirection.Column;
            root.style.overflow = Overflow.Hidden;
            ReportView.Seal(root);

            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "report-scroll" };
            scroll.style.flexGrow = 1;
            scroll.style.flexShrink = 1;
            scroll.style.minHeight = 0;
            scroll.style.backgroundColor = ReportView.PanelInk;
            root.Add(scroll);

            var debriefSafe = debrief ?? new FlightDebrief();
            scroll.Add(Line("failure-cause", debriefSafe.CauseTitle, 22, FontStyle.Bold, ReportCardChrome.Cream));
            scroll.Add(Line("failure-numbers", debriefSafe.Numbers, 16, FontStyle.Bold, ReportCardChrome.Sun));

            string kid = failure != null && !string.IsNullOrEmpty(failure.kid) ? failure.kid : debriefSafe.Kid;
            string engineer = failure != null && !string.IsNullOrEmpty(failure.engineer) ? failure.engineer : debriefSafe.Engineer;
            scroll.Add(Line("failure-kid", kid, 16, FontStyle.Normal, ReportCardChrome.Cream));
            scroll.Add(Line("failure-engineer", engineer, 16, FontStyle.Normal, ReportCardChrome.Cream));
            scroll.Add(Line("narrator-kid", debriefSafe.NarratorKid, 16, FontStyle.Normal, ReportCardChrome.Cream));
            if (!string.IsNullOrEmpty(debriefSafe.NarratorEng))
                scroll.Add(Line("narrator-eng", debriefSafe.NarratorEng, 16, FontStyle.Italic, ReportCardChrome.Cream));

            scroll.Add(Line("report-stars", debriefSafe.StarsLine, 18, FontStyle.Bold, ReportCardChrome.Cream));
            scroll.Add(Line("report-medals", debriefSafe.MedalsLine, 16, FontStyle.Normal, ReportCardChrome.Sun));
            scroll.Add(Line("report-kc", debriefSafe.KcLine, 16, FontStyle.Normal, ReportCardChrome.Teal));
            if (!string.IsNullOrEmpty(debriefSafe.UnlockLine))
                scroll.Add(Line("report-unlocks", debriefSafe.UnlockLine, 15, FontStyle.Normal, Color.white));
            if (!string.IsNullOrEmpty(debriefSafe.CosmeticLine))
                scroll.Add(Line("report-cosmetics", debriefSafe.CosmeticLine, 15, FontStyle.Normal, Color.white));

            if (log != null) scroll.Add(ReportView.Plots(log, frame.Plots, true));

            float tap = (float)Math.Max(ReportCardChrome.HitPt, frame.Fix.H);
            var foot = new VisualElement { name = "report-actions", pickingMode = PickingMode.Position };
            foot.style.flexShrink = 0;
            foot.style.backgroundColor = ReportView.PanelInk;
            var fix = new Button(() =>
            {
                if (actions != null && actions.Fix != null) actions.Fix();
                else onFix?.Invoke();
            })
            { name = "failure-fix", text = "Fix it" };
            ReportCardChrome.PaintButton(fix, tap, "fix");
            var link = FixField.For(failure != null ? failure.kind : debriefSafe.Kind);
            fix.tooltip = link.VoiceOver;
            fix.userData = link.Uri;
            foot.Add(fix);

            NextMission.AddRoutes(foot, actions, tap);
            root.Add(foot);
            ReportCardChrome.Escape = actions != null ? actions.Home : null;
            root.tooltip = debriefSafe.CauseTitle + ". " + debriefSafe.Numbers;
            Kestrel.Game.Shell.TextFit.Apply(root);
            root.RegisterCallback<GeometryChangedEvent>(_ => Kestrel.Game.Shell.TextFit.Apply(root));
            return root;
        }

        public static Label Line(string name, string text, int size, FontStyle style, Color color)
        {
            var label = new Label(PlayerText.ForPlayer(text ?? "")) { name = name };
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = size;
            label.style.minHeight = size + 8;
            label.style.flexShrink = 0;
            label.style.unityFontStyleAndWeight = style;
            label.style.color = color;
            label.style.marginTop = 2;
            label.style.marginBottom = 8;
            label.style.marginLeft = 12;
            label.style.marginRight = 12;
            label.tooltip = label.text;
            return label;
        }

        public static void StyleButton(Button button, float tap, Color fill)
        {
            ReportCardChrome.PaintButton(button, tap, "home");
            if (fill.a > 0f) button.style.backgroundColor = fill;
        }
    }
}
