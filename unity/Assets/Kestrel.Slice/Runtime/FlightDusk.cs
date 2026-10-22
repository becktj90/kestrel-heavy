// Dusk flight chrome. Fraunces for the clock and callout titles, Space Grotesk for
// the sentences. Ink plate, cream type, a sun or alarm edge. No pulse, no scale pop.
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public static class FlightDusk
    {
        public static readonly Color Ink = new Color(13f / 255f, 20f / 255f, 40f / 255f, 0.94f);
        public static readonly Color Cream = new Color(246f / 255f, 235f / 255f, 210f / 255f, 1f);
        public static readonly Color Sun = new Color(232f / 255f, 140f / 255f, 60f / 255f, 1f);
        public static readonly Color Teal = new Color(70f / 255f, 217f / 255f, 204f / 255f, 1f);
        public static readonly Color Alarm = new Color(242f / 255f, 84f / 255f, 61f / 255f, 1f);
        public static readonly Color AlarmInk = new Color(46f / 255f, 14f / 255f, 18f / 255f, 0.94f);

        public static Font TitleFace() => Resources.Load<Font>("KHFonts/Fraunces-Bold");
        public static Font BodyFace() => Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium");

        public static void Clock(Label label, float unitsPerPoint, float fontPt)
        {
            if (label == null) return;
            float u = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            Frame(label, u, false);
            // The header is 36 pt. Frame's 4 pt pads plus a 3 pt edge would shove a 28 pt clock out of it.
            label.style.paddingTop = 0;
            label.style.paddingBottom = 0;
            label.style.borderBottomWidth = 2f * u;
            label.style.fontSize = Mathf.Max(13f, fontPt) * u;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            Face(label, TitleFace());
        }

        public static void Banner(Label label, float unitsPerPoint, bool alarm)
        {
            if (label == null) return;
            float u = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            Frame(label, u, alarm);
            label.style.fontSize = 15f * u;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            Face(label, TitleFace());
            FitBanner(label, u);
        }

        public const float BannerPt = Kestrel.Core.Flight.FlightHudLayout.BannerFontPt;
        public const float BannerMinPt = Kestrel.Core.Flight.FlightHudLayout.BannerMinFontPt;

        /// <summary>The banner's sentence. It is never cut, so this is the label's own text.</summary>
        public static string BannerText(Label label) => label != null ? label.text : null;

        /// <summary>
        /// L1 ("Climbi…" off the edge; #322 SE: 89 x 74 px of text in a 96 x 53 box; then "Climbing through…"): the
        /// whole sentence, never cut (a tooltip does not help on a phone). FlightHudLayout.BannerFit picks the face
        /// (15 -> 11 pt, two lines, widths budgeted at FlightHudLayout.MeasureFactor) for the parent's content width less
        /// the banner's own padding; past the floor it keeps 11 pt and takes the lines it needs. Where two lines at 11 pt
        /// do not fit the stages card, AscentHud gives the card a full-width row (FlightHudLayout.BannerRowPt) first.
        /// The width comes from the parent, never the label, so the face does not chase its own box. Margins zeroed,
        /// height whole pixels.
        /// </summary>
        public static void FitBanner(Label label, float u)
        {
            if (label == null) return;
            string full = label.text;
            label.style.marginLeft = 0; label.style.marginRight = 0;
            label.style.marginTop = 0; label.style.marginBottom = 0;
            label.style.minHeight = 0;
            label.style.maxWidth = Length.Percent(100);
            label.tooltip = full;
            var parent = label.parent;
            float w = parent != null ? parent.contentRect.width : float.NaN;
            if (float.IsNaN(w) || w < 8f || string.IsNullOrEmpty(full)) return;
            // The model's card width: the parent's content box plus the card padding BannerTextW takes off again.
            float cardPt = w / u + 2f * Kestrel.Core.Flight.FlightHudLayout.BannerCardPadPt;
            // A card with a set height (AscentHud's stages card or banner row, a landscape pin) caps the face too. An
            // auto-height parent does not: its height is the label's own, and the face would chase it down.
            var sh = parent.style.height;
            float cardH = sh.keyword == StyleKeyword.Undefined && sh.value.unit == LengthUnit.Pixel && sh.value.value > 8f
                ? sh.value.value / u : float.PositiveInfinity;
            var fit = Kestrel.Core.Flight.FlightHudLayout.BannerFit(full, cardPt, cardH);
            int lines = Kestrel.Core.Flight.FlightHudLayout.BannerLinesAt(fit, cardPt);
            if (lines == int.MaxValue) lines = Math.Max(1, (int)Math.Ceiling(Kestrel.Core.Flight.FlightHudLayout.BudgetWidth(full, fit.FontPt) / Math.Max(1f, Kestrel.Core.Flight.FlightHudLayout.BannerTextW(cardPt) - 2f)) + 1);
            label.style.fontSize = fit.FontPt * u;
            label.style.height = Mathf.Ceil(Kestrel.Core.Flight.FlightHudLayout.BannerLabelPt(Math.Max(1, lines), fit.FontPt) * u);
        }

        public static void Callout(Label label, float unitsPerPoint, bool alarm)
        {
            if (label == null) return;
            float u = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            Frame(label, u, alarm);
            label.style.fontSize = 13f * u;
            label.style.minHeight = Kestrel.Core.Flight.FlightStaging.CalloutPt * u;
            label.style.height = StyleKeyword.Auto;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.overflow = Overflow.Hidden;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            Face(label, BodyFace());
        }

        public static void Sentence(Label label, float unitsPerPoint)
        {
            if (label == null) return;
            float u = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            label.style.fontSize = 13f * u;
            label.style.color = Cream;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.overflow = Overflow.Hidden;
            Face(label, BodyFace());
        }

        public static void Frame(VisualElement e, float u, bool alarm)
        {
            if (e == null) return;
            e.style.backgroundColor = alarm ? AlarmInk : Ink;
            e.style.color = Cream;
            e.style.paddingLeft = 8f * u;
            e.style.paddingRight = 8f * u;
            e.style.paddingTop = 4f * u;
            e.style.paddingBottom = 4f * u;
            float r = 10f * u;
            e.style.borderTopLeftRadius = r;
            e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = r;
            e.style.borderBottomRightRadius = r;
            e.style.borderTopWidth = 0;
            e.style.borderLeftWidth = 0;
            e.style.borderRightWidth = 0;
            e.style.borderBottomWidth = 3f * u;
            e.style.borderBottomColor = alarm ? Alarm : Sun;
        }

        public static void Face(VisualElement e, Font font)
        {
            if (e == null || font == null) return;
            e.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font));
        }

        public static Color Tone(Kestrel.Core.Flight.VehicleHealthTone tone)
        {
            if (tone == Kestrel.Core.Flight.VehicleHealthTone.Red) return Alarm;
            if (tone == Kestrel.Core.Flight.VehicleHealthTone.Amber) return Sun;
            return Teal;
        }
    }
}
