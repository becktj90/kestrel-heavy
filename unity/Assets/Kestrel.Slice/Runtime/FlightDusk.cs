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

        public const float BannerPt = 15f;
        public const float BannerMinPt = 11f;
        public const int BannerLines = 2;

        sealed class BannerCut { public string Full, Shown; }

        /// <summary>The banner's full sentence (it may be showing a cut copy).</summary>
        public static string BannerText(Label label)
        {
            if (label == null) return null;
            return label.userData is BannerCut c && c.Shown == label.text ? c.Full : label.text;
        }

        /// <summary>
        /// L1 ("Climbi…" off the edge; #322 SE: 89 x 74 px of text in a 96 x 53 box): at most two lines in the parent's
        /// content width less the banner's own padding, the face stepping 15 -> 11 pt with widths budgeted at
        /// FlightHudLayout.MeasureFactor, then cut at a word with the full sentence in the tooltip. The width comes from
        /// the parent, never the label, so the face does not chase its own box. Margins zeroed, height whole pixels.
        /// </summary>
        public static void FitBanner(Label label, float u)
        {
            if (label == null) return;
            string full = BannerText(label);
            label.style.marginLeft = 0; label.style.marginRight = 0;
            label.style.marginTop = 0; label.style.marginBottom = 0;
            label.style.minHeight = 0;
            label.style.maxWidth = Length.Percent(100);
            var parent = label.parent;
            float w = parent != null ? parent.contentRect.width : float.NaN;
            if (float.IsNaN(w) || w < 8f || string.IsNullOrEmpty(full))
            {
                if (label.userData is BannerCut) label.userData = null;
                return;
            }
            w -= 16f * u; // Frame: 8 pt padding each side.
            var fit = Kestrel.Core.Flight.FlightHudLayout.WrapFit(full, w / u, float.PositiveInfinity, BannerLines, BannerPt, BannerMinPt);
            label.style.fontSize = fit.FontPt * u;
            int lines = Math.Max(1, Math.Min(BannerLines, Kestrel.Core.Flight.FlightHudLayout.WrapLines(fit.Text, w / u - 2f, fit.FontPt)));
            // Whole pixels: lines of the line box plus the 4 pt top and bottom padding and the 3 pt bottom border.
            label.style.height = Mathf.Ceil((lines * fit.FontPt * Kestrel.Core.Flight.FlightHudLayout.LineHeight + 11f) * u);
            if (fit.Cut)
            {
                label.text = fit.Text;
                label.tooltip = full;
                label.userData = new BannerCut { Full = full, Shown = fit.Text };
            }
            else
            {
                if (label.text != full) label.text = full;
                label.tooltip = full;
                label.userData = null;
            }
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
