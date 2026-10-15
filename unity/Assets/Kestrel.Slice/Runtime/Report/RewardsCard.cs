// Rewards card on the post-flight sheet. Cause, the flight's own numbers, stars, medals, Kc, then the exits.
// Portrait column, scrolling, so an iPhone SE still reaches every button. No clock and no purchase.
using System;
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
            root.style.backgroundColor = new Color(0.07f, 0.09f, 0.14f, 0.96f);
            root.style.flexDirection = FlexDirection.Column;
            root.style.overflow = Overflow.Hidden;

            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "report-scroll" };
            scroll.style.flexGrow = 1;
            scroll.style.height = Length.Percent(100);
            root.Add(scroll);

            var debriefSafe = debrief ?? new FlightDebrief();
            scroll.Add(Line("failure-cause", debriefSafe.CauseTitle, 20, FontStyle.Bold, Color.white));
            scroll.Add(Line("failure-numbers", debriefSafe.Numbers, 16, FontStyle.Bold, new Color(1f, 0.86f, 0.55f)));

            string kid = failure != null && !string.IsNullOrEmpty(failure.kid) ? failure.kid : debriefSafe.Kid;
            string engineer = failure != null && !string.IsNullOrEmpty(failure.engineer) ? failure.engineer : debriefSafe.Engineer;
            scroll.Add(Line("failure-kid", kid, 16, FontStyle.Normal, Color.white));
            scroll.Add(Line("failure-engineer", engineer, 13, FontStyle.Normal, new Color(0.75f, 0.82f, 0.9f)));
            scroll.Add(Line("narrator-kid", debriefSafe.NarratorKid, 16, FontStyle.Normal, new Color(0.96f, 0.93f, 0.84f)));
            if (!string.IsNullOrEmpty(debriefSafe.NarratorEng))
                scroll.Add(Line("narrator-eng", debriefSafe.NarratorEng, 13, FontStyle.Italic, new Color(0.75f, 0.82f, 0.9f)));

            scroll.Add(Line("report-stars", debriefSafe.StarsLine, 17, FontStyle.Bold, Color.white));
            scroll.Add(Line("report-medals", debriefSafe.MedalsLine, 16, FontStyle.Normal, new Color(0.93f, 0.84f, 0.45f)));
            scroll.Add(Line("report-kc", debriefSafe.KcLine, 15, FontStyle.Normal, new Color(0.8f, 0.9f, 0.84f)));
            if (!string.IsNullOrEmpty(debriefSafe.UnlockLine))
                scroll.Add(Line("report-unlocks", debriefSafe.UnlockLine, 15, FontStyle.Normal, Color.white));
            if (!string.IsNullOrEmpty(debriefSafe.CosmeticLine))
                scroll.Add(Line("report-cosmetics", debriefSafe.CosmeticLine, 15, FontStyle.Normal, Color.white));

            if (log != null) scroll.Add(ReportView.Plots(log, frame.Plots, true));

            float tap = (float)Math.Max(ReportLayout.MinTargetPt, frame.Fix.H);
            var fix = new Button(() =>
            {
                if (actions != null && actions.Fix != null) actions.Fix();
                else onFix?.Invoke();
            })
            { name = "failure-fix", text = "Fix it" };
            StyleButton(fix, tap, new Color(0.18f, 0.45f, 0.86f));
            var link = FixField.For(failure != null ? failure.kind : debriefSafe.Kind);
            fix.tooltip = link.VoiceOver;
            fix.userData = link.Uri;
            scroll.Add(fix);

            NextMission.AddRoutes(scroll, actions, tap);
            root.tooltip = debriefSafe.CauseTitle + ". " + debriefSafe.Numbers;
            return root;
        }

        public static Label Line(string name, string text, int size, FontStyle style, Color color)
        {
            var label = new Label(text ?? "") { name = name };
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = size;
            label.style.unityFontStyleAndWeight = style;
            label.style.color = color;
            label.style.marginBottom = 6;
            label.style.marginLeft = 12;
            label.style.marginRight = 12;
            label.tooltip = label.text;
            return label;
        }

        public static void StyleButton(Button button, float tap, Color fill)
        {
            button.style.minHeight = tap;
            button.style.marginLeft = 12;
            button.style.marginRight = 12;
            button.style.marginBottom = 8;
            button.style.flexShrink = 0;
            button.style.backgroundColor = fill;
            button.style.color = Color.white;
            button.style.fontSize = 17;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
        }
    }
}
