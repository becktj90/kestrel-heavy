// Rewards card on the post-flight sheet. Cause, the flight's own numbers, stars, medals, Kc, then the exits.
// Body over an exits grid, sized by ReportCardChrome.Dress / ReportFit to fit with no scrolling. No clock and no purchase.
using System;
using Kestrel.Core;
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class ReportActions
    {
        public Action Next, Again, Missions, Home, Fix;
        /// <summary>NAV-04 / F4: reopen the designer with the rocket that flew, mission kept.</summary>
        public Action Designer;
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
            // #416 item 3: a pass opens on the win, not on a cause line and a red Fix it.
            bool pass = IsPass(failure, debrief);
            if (pass)
            {
                root.AddToClassList(UiIds.ReportPassClass);
                scroll.Add(Line(UiIds.ReportHero, HeroText, 26, FontStyle.Bold, ReportCardChrome.Win));
                scroll.Add(StarRow(debriefSafe.Stars));
                if (!string.IsNullOrEmpty(debriefSafe.KcHeadline))
                    scroll.Add(Line(UiIds.ReportKcHeadline, debriefSafe.KcHeadline, 20, FontStyle.Bold, ReportCardChrome.Sun));
            }
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
            if (!pass)
            {
                // A fail (or a flight that ended short) keeps Fix it as the full-width lead.
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
            }

            NextMission.AddRoutes(foot, actions, tap, pass);
            if (pass)
                foot.Query<Button>().ForEach(b => ReportCardChrome.PaintButton(b, tap, ReportCardChrome.PassRole(b.name)));
            root.Add(foot);
            ReportCardChrome.Escape = actions != null ? actions.Home : null;
            root.tooltip = debriefSafe.CauseTitle + ". " + debriefSafe.Numbers;
            Kestrel.Game.Shell.TextFit.Apply(root);
            root.RegisterCallback<GeometryChangedEvent>(_ => Kestrel.Game.Shell.TextFit.Apply(root));
            return root;
        }

        public const string HeroText = "MISSION COMPLETE";
        public const int MaxStars = 3;

        /// <summary>The pass card: a debrief that passed, with no failure event.</summary>
        public static bool IsPass(FailureEvent failure, FlightDebrief debrief)
            => failure == null && debrief != null && debrief.Passed;

        /// <summary>Three drawn stars (the fonts have no star glyph), lit up to <paramref name="stars"/>.</summary>
        public static VisualElement StarRow(int stars)
        {
            int lit = Math.Max(0, Math.Min(MaxStars, stars));
            var row = new VisualElement { name = UiIds.ReportStarRow, pickingMode = PickingMode.Ignore };
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexShrink = 0;
            row.style.height = StarPt;
            row.style.marginLeft = 12;
            row.style.marginBottom = 6;
            for (int i = 0; i < MaxStars; i++)
            {
                var pip = new ReportStar { name = "report-star", On = i < lit };
                pip.style.width = StarPt;
                pip.style.height = StarPt;
                pip.style.marginRight = 6;
                row.Add(pip);
            }
            row.tooltip = lit + " of " + MaxStars + " stars";
            return row;
        }

        /// <summary>Star row height in points; ReportCardChrome.Dress takes it out of the body room.</summary>
        public const float StarPt = 30f;

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

    /// <summary>One drawn five-point star for the pass card (no font here has a star glyph). Lit is Sun, unlit is faint cream.</summary>
    public sealed class ReportStar : VisualElement
    {
        public bool On = true;

        public ReportStar()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext ctx)
        {
            var p = ctx == null ? null : ctx.painter2D;
            if (p == null) return;
            var r = contentRect;
            float s = Mathf.Min(r.width, r.height);
            if (s < 1f) return;
            var c = new Vector2(r.width * 0.5f, r.height * 0.5f);
            float radius = s * 0.48f;
            var cream = ReportCardChrome.Cream;
            p.fillColor = On ? ReportCardChrome.Sun : new Color(cream.r, cream.g, cream.b, 0.25f);
            p.BeginPath();
            for (int i = 0; i < 10; i++)
            {
                float ang = -Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                float rad = (i % 2 == 0) ? radius : radius * 0.42f;
                var pt = c + new Vector2(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad);
                if (i == 0) p.MoveTo(pt);
                else p.LineTo(pt);
            }
            p.ClosePath();
            p.Fill();
        }
    }
}
