// The hold-down release block in the count card (DECISIONS #100): a status line with the live T/W, the gauge (track,
// green band, needle) and the big Release button. Kestrel.Core.Flight.ClampRelease is the state; ClampGaugeLayout
// sizes it in points. Built under the count card only from ignition to liftoff, and removed before AscentHud adopts
// the card. Steady styles only (photosensitivity): the needle moves, nothing blinks.
using Kestrel.Core.Flight;
using Kestrel.Core.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class ClampGaugeView
    {
        static readonly Color Track = new Color(0.10f, 0.14f, 0.22f, 1f);
        static readonly Color Green = new Color(0.30f, 0.82f, 0.55f, 1f);
        static readonly Color GreenDim = new Color(0.30f, 0.82f, 0.55f, 0.55f);

        VisualElement root, bar, track, band, needle;
        Label status, twr;
        Button release;
        System.Action onRelease;
        float sizedFor = -1f, sizedUpp = -1f;
        ClampGaugeFit fit;

        public VisualElement Root => root;
        public Button ReleaseButton => release;
        public bool Shown => root != null && root.parent != null && root.style.display != DisplayStyle.None;

        public ClampGaugeView(System.Action onRelease) { this.onRelease = onRelease; }

        static float Px(float pt, float u) => Mathf.Round(pt * u);

        void Build()
        {
            root = new VisualElement { name = UiIds.FlightClamp, pickingMode = PickingMode.Ignore };
            root.style.flexDirection = FlexDirection.Column;
            root.style.alignItems = Align.Stretch;
            root.style.flexShrink = 0;

            var line = new VisualElement { name = UiIds.FlightClamp + "-line", pickingMode = PickingMode.Ignore };
            line.style.flexDirection = FlexDirection.Row;
            line.style.justifyContent = Justify.SpaceBetween;
            line.style.alignItems = Align.Center;
            line.style.flexShrink = 0;
            line.style.overflow = Overflow.Hidden;
            status = new Label { name = UiIds.FlightClampStatus, pickingMode = PickingMode.Ignore };
            twr = new Label { name = UiIds.FlightClampTwr, pickingMode = PickingMode.Ignore };
            foreach (var l in new[] { status, twr })
            {
                l.style.whiteSpace = WhiteSpace.NoWrap;
                l.style.unityFontStyleAndWeight = FontStyle.Bold;
                l.style.color = FlightDusk.Cream;
                l.style.paddingLeft = 0; l.style.paddingRight = 0; l.style.paddingTop = 0; l.style.paddingBottom = 0;
                l.style.marginLeft = 0; l.style.marginRight = 0; l.style.marginTop = 0; l.style.marginBottom = 0;
                l.style.overflow = Overflow.Hidden;
                FlightDusk.Face(l, FlightDusk.BodyFace());
            }
            status.style.flexShrink = 1;
            status.style.unityTextAlign = TextAnchor.MiddleLeft;
            twr.style.flexShrink = 0;
            twr.style.unityTextAlign = TextAnchor.MiddleRight;
            line.Add(status);
            line.Add(twr);

            // The bar is as tall as the needle; the track sits centred in it, so nothing pokes out of its parent.
            bar = new VisualElement { name = UiIds.FlightClampGauge, pickingMode = PickingMode.Ignore };
            bar.style.flexShrink = 0;
            bar.style.overflow = Overflow.Hidden;
            track = new VisualElement { name = UiIds.FlightClampGauge + "-track", pickingMode = PickingMode.Ignore };
            track.style.position = Position.Absolute;
            track.style.left = 0; track.style.right = 0;
            track.style.backgroundColor = Track;
            track.style.overflow = Overflow.Hidden;
            band = new VisualElement { name = UiIds.FlightClampGauge + "-green", pickingMode = PickingMode.Ignore };
            band.style.position = Position.Absolute;
            band.style.top = 0; band.style.bottom = 0; band.style.right = 0;
            band.style.backgroundColor = GreenDim;
            track.Add(band);
            needle = new VisualElement { name = UiIds.FlightClampGauge + "-needle", pickingMode = PickingMode.Ignore };
            needle.style.position = Position.Absolute;
            needle.style.top = 0; needle.style.bottom = 0;
            needle.style.backgroundColor = FlightDusk.Cream;
            bar.Add(track);
            bar.Add(needle);

            release = new Button { name = UiIds.FlightClampRelease, text = ClampRelease.ReleaseText };
            release.tooltip = "Release the hold-down clamps. Tap when the needle is in the green.";
            release.clicked += () => onRelease?.Invoke();
            release.style.alignSelf = Align.Center;
            release.style.flexShrink = 0;
            release.style.unityFontStyleAndWeight = FontStyle.Bold;
            release.style.unityTextAlign = TextAnchor.MiddleCenter;
            release.style.whiteSpace = WhiteSpace.NoWrap;
            release.style.color = new Color(0.08f, 0.08f, 0.1f, 1f);
            release.style.borderTopWidth = 0; release.style.borderBottomWidth = 0; release.style.borderLeftWidth = 0; release.style.borderRightWidth = 0;
            release.style.paddingLeft = 0; release.style.paddingRight = 0; release.style.paddingTop = 0; release.style.paddingBottom = 0;
            release.style.marginLeft = 0; release.style.marginRight = 0; release.style.marginBottom = 0;
            FlightDusk.Face(release, FlightDusk.BodyFace());

            root.Add(line);
            root.Add(bar);
            root.Add(release);
        }

        /// <summary>Show the block for <paramref name="clamp"/> at count time <paramref name="t"/> in the count card.</summary>
        public void Paint(VisualElement card, ClampRelease clamp, double t, float u, float cardWPt)
        {
            if (card == null || clamp == null || clamp.Phase(t) == ClampPhase.Idle) { Detach(); return; }
            if (root == null) Build();
            if (root.parent != card) { root.RemoveFromHierarchy(); card.Add(root); }
            root.style.display = DisplayStyle.Flex;
            u = u > 0.5f ? u : 1f;
            if (!Mathf.Approximately(sizedFor, cardWPt) || !Mathf.Approximately(sizedUpp, u)) Size(cardWPt, u);

            status.text = clamp.Status(t);
            string tw = clamp.TwrText(t);
            twr.text = fit.TwrShown ? tw : "";
            twr.style.display = fit.TwrShown && !string.IsNullOrEmpty(tw) ? DisplayStyle.Flex : DisplayStyle.None;
            bool green = clamp.InGreen(t) && !clamp.Released;
            band.style.backgroundColor = green ? Green : GreenDim;
            float from = (float)clamp.GaugeGreenFrom;
            band.style.left = Length.Percent(from * 100f);
            float n = Mathf.Clamp01((float)clamp.Needle(t));
            float nw = Px(ClampGaugeLayout.NeedleWPt, u);
            needle.style.width = nw;
            // Percent of the bar, pulled back by half the needle so its centre is the reading and it never leaves the bar.
            needle.style.left = Length.Percent(n * 100f);
            needle.style.marginLeft = -Mathf.Round(nw * n);
            bool live = !clamp.Released;
            release.SetEnabled(live);
            release.style.backgroundColor = live ? (green ? Green : FlightDusk.Sun) : new Color(0.35f, 0.38f, 0.44f, 1f);
        }

        void Size(float cardWPt, float u)
        {
            sizedFor = cardWPt; sizedUpp = u;
            fit = ClampGaugeLayout.Fit(cardWPt, ClampGaugeLayout.WidestStatus(ClampGaugeLayout.LineFontPt), "T/W 0.00");
            root.style.marginTop = Px(ClampGaugeLayout.TopGapPt, u);
            float linePx = Px(CountdownStack.LinePt(fit.LinePt), u);
            status.parent.style.height = linePx;
            status.style.fontSize = Px(fit.LinePt, u);
            twr.style.fontSize = Px(fit.LinePt, u);
            twr.style.marginLeft = Px(ClampGaugeLayout.TwrGapPt, u);
            float barH = Px(ClampGaugeLayout.BarPt + ClampGaugeLayout.NeedleOverPt, u);
            bar.style.height = barH;
            bar.style.marginTop = Px(ClampGaugeLayout.GapPt, u);
            float trackH = Px(ClampGaugeLayout.BarPt, u);
            track.style.height = trackH;
            track.style.top = Mathf.Floor((barH - trackH) * 0.5f);
            float r = Px(ClampGaugeLayout.BarRadiusPt, u);
            track.style.borderTopLeftRadius = r; track.style.borderTopRightRadius = r;
            track.style.borderBottomLeftRadius = r; track.style.borderBottomRightRadius = r;
            release.style.marginTop = Px(ClampGaugeLayout.GapPt, u);
            release.style.width = Px(fit.ButtonW, u);
            release.style.height = Px(ClampGaugeLayout.ButtonPt, u);
            release.style.minHeight = Px(ClampGaugeLayout.ButtonPt, u);
            release.style.fontSize = Px(ClampGaugeLayout.ButtonFontPt, u);
            float br = Px(ClampGaugeLayout.ButtonRadiusPt, u);
            release.style.borderTopLeftRadius = br; release.style.borderTopRightRadius = br;
            release.style.borderBottomLeftRadius = br; release.style.borderBottomRightRadius = br;
        }

        public void Detach()
        {
            if (root == null) return;
            root.RemoveFromHierarchy();
            sizedFor = -1f;
        }
    }
}
