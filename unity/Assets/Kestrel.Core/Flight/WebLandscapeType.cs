// Landscape web shell scale and caps (LANDSCAPE_WEB_BRIEF §1). Portrait keeps the phone column and
// its 375x667 match-height panel. Landscape web uses a 1280x720 reference matched halfway between
// width and height, so a 1080p or 1440p monitor no longer inflates type 1.6-2.2x, and every size
// below is a hard cap in panel points after that scale. Content sits in a centred band at most
// 1280 wide; the margins outside it are themed fill.
using System;

namespace Kestrel.Core.Flight
{
    public static class WebLandscapeType
    {
        public const float RefW = 1280f, RefH = 720f, Match = 0.5f;
        public const float PhoneRefW = 375f, PhoneRefH = 667f, PhoneMatch = 1f;

        /// <summary>Widest content band, panel points. Wider panels get themed fill either side.</summary>
        public const float BandMax = 1280f;
        /// <summary>Below this panel width the landscape shell tightens (smaller floors, narrower side columns).</summary>
        public const float Tight = 1024f;

        public const float BodyMin = 12f, BodyCap = 16f;
        public const float TileMin = 13f, TileCap = 18f;
        public const float CtaMin = 16f, CtaCap = 22f;
        public const float TitleMin = 18f, TitleCap = 26f;
        public const float WordmarkMin = 28f, WordmarkCap = 48f;
        public const float HudMin = 12f, HudCap = 18f;

        /// <summary>Designer side columns (parts, readouts). DECISIONS #62 caps a side panel at 400.</summary>
        public const float SideMin = 200f, SideMax = 360f, SideCap = 400f;
        /// <summary>The rocket keeps at least this share of the band.</summary>
        public const float RocketMinFraction = 0.22f;

        /// <summary>Reference resolution and width/height match for the shell and Slice panels.</summary>
        public static void PanelReference(bool wide, out float refW, out float refH, out float match)
        {
            if (wide) { refW = RefW; refH = RefH; match = Match; }
            else { refW = PhoneRefW; refH = PhoneRefH; match = PhoneMatch; }
        }

        /// <summary>Unity's ScaleWithScreenSize, MatchWidthOrHeight: device pixels per panel point.</summary>
        public static float PanelScale(float screenW, float screenH, float refW, float refH, float match)
        {
            if (!(screenW > 0f) || !(screenH > 0f) || !(refW > 0f) || !(refH > 0f)) return 1f;
            double lw = Math.Log(screenW / refW, 2), lh = Math.Log(screenH / refH, 2);
            return (float)Math.Pow(2, lw + (lh - lw) * match);
        }

        /// <summary>The centred content band inside [left, right]: at most <see cref="BandMax"/> wide.</summary>
        public static void Band(float left, float right, out float bandLeft, out float bandRight)
        {
            float w = right - left;
            if (w <= BandMax) { bandLeft = left; bandRight = right; return; }
            float inset = (w - BandMax) * 0.5f;
            bandLeft = left + inset;
            bandRight = right - inset;
        }

        public static float Body(float fitted) => Cap(fitted, BodyMin, BodyCap);
        public static float Tile(float fitted) => Cap(fitted, TileMin, TileCap);
        public static float Cta(float fitted) => Cap(fitted, CtaMin, CtaCap);
        public static float Title(float fitted) => Cap(fitted, TitleMin, TitleCap);
        public static float Hud(float fitted) => Cap(fitted, HudMin, HudCap);

        /// <summary>Wordmark cap: 48 pt, and never more than 12% of the panel height.</summary>
        public static float Wordmark(float fitted, float panelHeight)
        {
            float cap = WordmarkCap;
            if (panelHeight > 0f) cap = Math.Min(cap, Math.Max(WordmarkMin, panelHeight * 0.12f));
            return Cap(fitted, WordmarkMin, cap);
        }

        /// <summary>A fitted size never grows past its cap. The floor applies only when the box allowed more:
        /// a size fitted below the floor stays small rather than overflowing its box.</summary>
        public static float Cap(float fitted, float min, float cap)
        {
            if (float.IsNaN(fitted) || fitted <= 0f) return min;
            return Math.Min(fitted, cap);
        }

        /// <summary>The guided stepper / parts column. The stepper header, its seven step tiles (four per row)
        /// and the Back / Next anyway / Fix it nav need about 360 pt inside the panel padding, so this column
        /// is 31% of the band, 300-400. At 300 the step title, tabs and nav collided (web overlap audit).</summary>
        public const float PartsMin = 300f, PartsTightMin = 260f;

        /// <summary>The readout column only holds the gauges, the thumb row and Launch: 22% of the band, 200-300.</summary>
        public const float ReadoutMax = 300f;

        public static float PartsColumn(float band)
        {
            float lo = band < Tight ? PartsTightMin : PartsMin;
            float w = Math.Max(lo, Math.Min(SideCap, band * 0.31f));
            return Math.Min(w, Math.Max(120f, band - ReadoutColumn(band) - band * RocketMinFraction - 24f));
        }

        public static float ReadoutColumn(float band)
        {
            float lo = band < Tight ? 160f : SideMin;
            return Math.Max(lo, Math.Min(ReadoutMax, band * 0.22f));
        }

        /// <summary>Side column width for a band: 24% of it, 200-360 (narrower when tight), never past 400.</summary>
        public static float SideColumn(float band)
        {
            float lo = band < Tight ? 160f : SideMin;
            float w = Math.Max(lo, Math.Min(SideMax, band * 0.24f));
            float rocketMin = band * RocketMinFraction;
            float most = (band - rocketMin) * 0.5f - 12f;
            if (w > most) w = Math.Max(120f, most);
            return Math.Min(w, SideCap);
        }
    }
}
