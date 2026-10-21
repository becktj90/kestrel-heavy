// The countdown's bottom stack as one card (TestFlight 202, #333). Before: the verdict banner ("On the count"),
// the delta-v spare line floating on the sky in 13 pt cream (unreadable over a pale sky), the pad sequence line
// ("Deluge") dressed as a second banner so it read as a button, and a 22 pt "Skip count" bar: four blocks.
// Now: one Ink card holding the title, the spare line in cream on the card, and one row with the status pill
// (a teal-edged indicator, not a control) beside a 44 pt outline "Skip count" secondary button. Three rows.
// Steady styling only: no blink, no pulse, no colour cycling (photosensitivity). Widths go through
// FlightHudLayout.BudgetWidth (Unity's text runs about 15% wider than the width table); every size is whole
// points times the panel scale, rounded to whole pixels by the caller.
using System;

namespace Kestrel.Core.Flight
{
    public struct CountdownStackFit
    {
        /// <summary>Spare line face (pt) and lines; the line is never cut.</summary>
        public float SparePt; public int SpareLines;
        /// <summary>Status pill width (pt) at StatusPt; 0 when there is no status.</summary>
        public float StatusW;
        /// <summary>Skip button width (pt); its height is SkipPt.</summary>
        public float SkipW;
        /// <summary>True when the status pill and Skip share one row; false stacks them (very narrow cards).</summary>
        public bool OneRow;
        /// <summary>Rows in the card: title, spare (if any), status + skip (one or two rows).</summary>
        public int Rows;
        /// <summary>Card height in points (whole points), title included at TitlePt lines.</summary>
        public float CardH;
    }

    public static class CountdownStack
    {
        /// <summary>Card padding (each side) and the gap between rows, points.</summary>
        public const float CardPadXPt = 10f, CardPadYPt = 8f, RowGapPt = 6f;
        /// <summary>Card corner radius and its sun edge, points.</summary>
        public const float CardRadiusPt = 12f, EdgePt = 3f;
        /// <summary>Title face (Fraunces) on the card: one line at 17 pt, stepping to 13.</summary>
        public const float TitlePt = 17f, TitleMinPt = 13f;
        /// <summary>Spare line (Space Grotesk, cream on ink): 14 pt stepping to 12 on one line, then two lines at 12.</summary>
        public const float SparePt = 14f, SpareMinPt = 12f;
        /// <summary>Status pill: 13 pt text, 28 pt tall, 12 pt side padding, 1 pt teal outline, a 8 pt teal dot.</summary>
        public const float StatusPt = 13f, StatusH = 28f, StatusPadXPt = 12f, StatusDotPt = 8f, StatusDotGapPt = 6f, StatusOutlinePt = 1f;
        /// <summary>Skip count: a secondary outline button, 44 pt hit area (Apple HIG), 15 pt text.</summary>
        public const float SkipPt = FlightHudLayout.HitPt, SkipFontPt = 15f, SkipMinWPt = 112f, SkipPadXPt = 16f, SkipOutlinePt = 1.5f;
        public const string SkipText = "Skip count";

        /// <summary>Largest face from <paramref name="maxPt"/> down to <paramref name="minPt"/> (0.5 pt steps) that keeps
        /// the text on one line in <paramref name="room"/> points, budgeted. Past the floor: the floor.</summary>
        public static float OneLine(string text, float room, float maxPt, float minPt)
        {
            if (string.IsNullOrEmpty(text)) return maxPt;
            for (float f = maxPt; f >= minPt - 0.001f; f -= 0.5f)
                if (FlightHudLayout.BudgetWidth(text, f) <= room - 2f) return f;
            return minPt;
        }

        public static float LinePt(float fontPt) => (float)Math.Ceiling(fontPt * FlightHudLayout.LineHeight);

        public static float StatusWidth(string status) =>
            string.IsNullOrEmpty(status) ? 0f
                : (float)Math.Ceiling(2f * StatusPadXPt + StatusDotPt + StatusDotGapPt + FlightHudLayout.BudgetWidth(status, StatusPt) + 2f * StatusOutlinePt);

        public static float SkipWidth() =>
            (float)Math.Ceiling(Math.Max(SkipMinWPt, FlightHudLayout.BudgetWidth(SkipText, SkipFontPt) + 2f * SkipPadXPt + 2f * SkipOutlinePt));

        /// <summary>The card's layout for a card <paramref name="cardW"/> points wide (outer width).</summary>
        public static CountdownStackFit Fit(float cardW, string title, string spare, string status, bool skip)
        {
            var fit = new CountdownStackFit();
            float room = Math.Max(1f, cardW - 2f * CardPadXPt);
            float titlePt = OneLine(title, room, TitlePt, TitleMinPt);
            int titleLines = Math.Max(1, FlightHudLayout.WrapLines(title ?? "", room - 2f, titlePt) == int.MaxValue ? 2 : FlightHudLayout.WrapLines(title ?? "", room - 2f, titlePt));
            float h = 2f * CardPadYPt + EdgePt + titleLines * LinePt(titlePt);
            fit.Rows = 1;
            if (!string.IsNullOrEmpty(spare))
            {
                fit.SparePt = OneLine(spare, room, SparePt, SpareMinPt);
                int lines = FlightHudLayout.WrapLines(spare, room - 2f, fit.SparePt);
                fit.SpareLines = lines == int.MaxValue ? 2 : Math.Max(1, lines);
                h += RowGapPt + fit.SpareLines * LinePt(fit.SparePt);
                fit.Rows++;
            }
            fit.StatusW = Math.Min(room, StatusWidth(status));
            fit.SkipW = skip ? SkipWidth() : 0f;
            if (fit.StatusW > 0f || skip)
            {
                fit.OneRow = fit.StatusW + (fit.StatusW > 0f && skip ? RowGapPt : 0f) + fit.SkipW <= room;
                if (fit.OneRow || !(fit.StatusW > 0f && skip)) { h += RowGapPt + Math.Max(fit.StatusW > 0f ? StatusH : 0f, skip ? SkipPt : 0f); fit.Rows++; }
                else { h += 2f * RowGapPt + StatusH + SkipPt; fit.Rows += 2; }
            }
            fit.CardH = (float)Math.Ceiling(h);
            return fit;
        }

        // ---- COUNT tag (h-stage during the count) ----------------------------------------------------

        /// <summary>The tag beside the clock while counting: 13 pt bold, 8 pt side padding, 24 pt tall, 8 pt from
        /// the clock. In 202 it kept the USS 30 px face (wider than its auto box once Unity measured it) at the
        /// right end of the header, under the narrator portrait (guide pop, top-right).</summary>
        public const float TagPt = 13f, TagPadXPt = 8f, TagH = 24f, TagGapPt = 8f, TagRadiusPt = 6f;
        public const string TagText = "COUNT";

        /// <summary>Tag width (whole points) for its text, budgeted.</summary>
        public static float TagWidth(string text) =>
            (float)Math.Ceiling(FlightHudLayout.BudgetWidth(string.IsNullOrEmpty(text) ? TagText : text, TagPt) + 2f * TagPadXPt);

        /// <summary>Right end (pt from the header's left) of clock + tag. The header must keep it clear of the guide
        /// pop: x + GuidePopPt + margin from the right edge.</summary>
        public static float TagRight(float clockW, string text) => clockW + TagGapPt + TagWidth(text);

        // ---- Follow chip contrast ------------------------------------------------------------------

        /// <summary>sRGB colour, 0..1, with alpha.</summary>
        public struct Rgba
        {
            public float R, G, B, A;
            public Rgba(float r, float g, float b, float a = 1f) { R = r; G = g; B = b; A = a; }
            public static Rgba Bytes(int r, int g, int b, float a = 1f) => new Rgba(r / 255f, g / 255f, b / 255f, a);
            /// <summary>This colour over an opaque backdrop.</summary>
            public Rgba Over(Rgba under) => new Rgba(R * A + under.R * (1 - A), G * A + under.G * (1 - A), B * A + under.B * (1 - A), 1f);
        }

        /// <summary>Following (tapping changes nothing): the dusk Ink plate, cream text, a teal edge, so it reads
        /// as a state on any sky. Was white at 12% under white text (about 1.1:1 on the pale sky).</summary>
        public static readonly Rgba FollowingPlate = Rgba.Bytes(13, 20, 40, 0.94f);
        public static readonly Rgba FollowingText = Rgba.Bytes(246, 235, 210);
        public static readonly Rgba FollowingEdge = Rgba.Bytes(70, 217, 204);
        /// <summary>Follow rocket (tapping glides back): solid amber, near-black text.</summary>
        public static readonly Rgba FollowPlate = Rgba.Bytes(255, 176, 0, 0.96f);
        public static readonly Rgba FollowText = Rgba.Bytes(20, 20, 26);
        public const float FollowEdgePt = 2f;

        /// <summary>Backdrops the chip sits on: pale day sky, the dusk sky, the ground and the white tank.</summary>
        public static readonly Rgba[] Backdrops =
        {
            Rgba.Bytes(196, 214, 240), Rgba.Bytes(255, 255, 255), Rgba.Bytes(18, 22, 40), Rgba.Bytes(0, 0, 0), Rgba.Bytes(76, 92, 70),
        };

        /// <summary>WCAG 2 contrast ratio of two opaque colours (1..21).</summary>
        public static double Contrast(Rgba a, Rgba b)
        {
            double la = Luminance(a), lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        public static double Luminance(Rgba c) => 0.2126 * Lin(c.R) + 0.7152 * Lin(c.G) + 0.0722 * Lin(c.B);

        static double Lin(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

        /// <summary>Lowest text contrast of a chip over every backdrop.</summary>
        public static double WorstContrast(Rgba plate, Rgba text)
        {
            double worst = double.MaxValue;
            foreach (var b in Backdrops) worst = Math.Min(worst, Contrast(text, plate.Over(b)));
            return worst;
        }
    }
}
