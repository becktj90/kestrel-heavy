// Test Flight result card (DECISIONS #102, UX spec 2026-11-11). Same sheet as the report card (ReportCardChrome.Dress
// lays it out): "TEST FLIGHT" eyebrow, a small reason icon, then title, kid line, engineer line and the "Try:" hint, in
// that VoiceOver order. No red failure styling; a made orbit gets a gold burst (static, so Reduce Motion is the same).
// Exits: Tweak & Retry full width, then New Design and Home side by side (ReportFit's 1 + 2 grid).
using System;
using Kestrel.Core;
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    /// <summary>The card's three exits. Null actions leave the button in place and do nothing (no dead tree).</summary>
    public sealed class TestCardActions
    {
        public Action Retry, NewDesign, Home;
    }

    public static class TestFlightCardView
    {
        public const float EyebrowPt = 13f, KidPt = 15f, EngineerPt = 13f, HintPt = 15f;
        public static readonly Color Gold = new Color(1f, 0.80f, 0.32f, 1f);

        /// <summary>Body size for a test card line (ReportCardChrome.Dress reads it); 0 when the name is not one.</summary>
        public static float BodyPt(string name)
        {
            switch (name)
            {
                case UiIds.TestCardEyebrow: return EyebrowPt;
                case UiIds.TestCardKid: return KidPt;
                case UiIds.TestCardEngineer: return EngineerPt;
                case UiIds.TestCardHint: return HintPt;
                default: return 0f;
            }
        }

        public static VisualElement Build(TestFlightCard card, ReportFrame frame, bool reduceMotion, TestCardActions actions)
        {
            card = card ?? new TestFlightCard();
            var root = new VisualElement { name = ReportView.RootName, pickingMode = PickingMode.Position };
            root.AddToClassList("post-flight");
            root.AddToClassList("portrait");
            root.AddToClassList(UiIds.TestCard);
            if (reduceMotion) root.AddToClassList("reduce-motion");
            ReportView.Place(root, frame.Card);
            root.style.flexDirection = FlexDirection.Column;
            root.style.overflow = Overflow.Hidden;
            ReportView.Seal(root);
            root.tooltip = TestFlightCopy.Name + ". " + card.Spoken;

            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "report-scroll" };
            scroll.style.flexGrow = 1;
            scroll.style.flexShrink = 1;
            scroll.style.minHeight = 0;
            scroll.style.backgroundColor = ReportView.PanelInk;
            root.Add(scroll);

            // Header row: the reason icon (or the orbit's gold burst) beside the eyebrow. Not read on its own.
            var head = new VisualElement { name = UiIds.TestCard + "-head", pickingMode = PickingMode.Ignore };
            head.style.flexDirection = FlexDirection.Row;
            head.style.alignItems = Align.Center;
            head.style.flexShrink = 0;
            head.style.marginLeft = 12;
            head.Add(card.MadeOrbit ? Burst() : ReasonIcon(card.Kind));
            var eyebrow = RewardsCard.Line(UiIds.TestCardEyebrow, TestFlightCopy.Eyebrow, (int)EyebrowPt, FontStyle.Bold, Gold);
            eyebrow.style.letterSpacing = 1.5f;
            eyebrow.style.marginLeft = 8;
            head.Add(eyebrow);
            scroll.Add(head);

            scroll.Add(Read(RewardsCard.Line(UiIds.TestCardTitle, card.Title, 22, FontStyle.Bold, ReportCardChrome.Cream)));
            scroll.Add(Read(RewardsCard.Line(UiIds.TestCardKid, card.Kid, (int)KidPt, FontStyle.Normal, ReportCardChrome.Cream)));
            scroll.Add(Read(RewardsCard.Line(UiIds.TestCardEngineer, card.Engineer, (int)EngineerPt, FontStyle.Normal, ReportCardChrome.Cream)));
            if (!string.IsNullOrEmpty(card.HintLine))
                scroll.Add(Read(RewardsCard.Line(UiIds.TestCardHint, card.HintLine, (int)HintPt, FontStyle.Bold, ReportCardChrome.Cream)));

            float tap = (float)Math.Max(ReportCardChrome.HitPt, frame.Fix.H);
            var foot = new VisualElement { name = UiIds.ReportFoot, pickingMode = PickingMode.Position };
            foot.style.flexShrink = 0;
            foot.style.backgroundColor = ReportView.PanelInk;
            foot.Add(Exit(UiIds.TestRetry, TestFlightCopy.Retry, TestFlightCopy.RetrySpoken, actions?.Retry, tap, "fix"));
            foot.Add(Exit(UiIds.TestNewDesign, TestFlightCopy.NewDesign, TestFlightCopy.NewDesignSpoken, actions?.NewDesign, tap, "home"));
            foot.Add(Exit(UiIds.TestHome, TestFlightCopy.Home, TestFlightCopy.HomeSpokenButton, actions?.Home, tap, "home"));
            root.Add(foot);
            ReportCardChrome.Escape = actions?.Home;
            Kestrel.Game.Shell.TextFit.Apply(root);
            root.RegisterCallback<GeometryChangedEvent>(_ => Kestrel.Game.Shell.TextFit.Apply(root));
            return root;
        }

        static Label Read(Label l)
        {
            l.tooltip = l.text;
            return l;
        }

        static Button Exit(string name, string text, string spoken, Action click, float tap, string role)
        {
            // First tap must work (#419 ReportLatch lesson): a plain clicked handler, no confirm, no latch.
            var b = new Button(() => click?.Invoke()) { name = name, text = text, tooltip = spoken };
            b.userData = click;
            ReportCardChrome.PaintButton(b, tap, role);
            return b;
        }

        /// <summary>The report's small reason mark, at a third of its size, in the card's own colour (never red).</summary>
        static VisualElement ReasonIcon(string kind)
        {
            var icon = ReportView.Icon(string.IsNullOrEmpty(kind) ? "break" : kind);
            icon.name = UiIds.TestCardIcon;
            icon.pickingMode = PickingMode.Ignore;
            icon.style.position = Position.Relative;
            icon.style.width = 28; icon.style.height = 28;
            icon.style.flexShrink = 0;
            icon.style.overflow = Overflow.Hidden;
            return icon;
        }

        /// <summary>Orbit: a static gold burst (eight rays round a disc). Static, so Reduce Motion shows the same.</summary>
        static VisualElement Burst()
        {
            var burst = new VisualElement { name = UiIds.TestCardBurst, pickingMode = PickingMode.Ignore };
            burst.style.width = 28; burst.style.height = 28; burst.style.flexShrink = 0;
            burst.generateVisualContent += ctx =>
            {
                var p = ctx.painter2D;
                var c = new Vector2(14f, 14f);
                p.strokeColor = Gold;
                p.lineWidth = 2f;
                p.lineCap = LineCap.Round;
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4f;
                    var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    p.BeginPath();
                    p.MoveTo(c + d * 8f);
                    p.LineTo(c + d * 13f);
                    p.Stroke();
                }
                p.fillColor = Gold;
                p.BeginPath();
                p.Arc(c, 5.5f, 0f, 360f);
                p.Fill();
            };
            return burst;
        }
    }
}
