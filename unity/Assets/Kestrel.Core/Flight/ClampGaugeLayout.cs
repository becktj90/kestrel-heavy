// The hold-down release block inside the count card (DECISIONS #100): one line (status left, T/W right), the gauge bar
// with its green band and needle, and the big Release button. Points, whole numbers, budgeted widths
// (FlightHudLayout.BudgetWidth); the caller multiplies by the panel scale and rounds to pixels.
// The button is the count's primary control: 56 pt tall (Apple HIG 44 pt minimum), never smaller than its text.
// Steady styling only (photosensitivity): the needle moves, nothing blinks or pulses.
using System;

namespace Kestrel.Core.Flight
{
    public struct ClampGaugeFit
    {
        public float LinePt;
        /// <summary>The T/W readout shares the status line (false on a very narrow card: it drops).</summary>
        public bool TwrShown;
        public float ButtonW;
        /// <summary>Block height (pt): line, gap, bar, gap, button.</summary>
        public float BlockH;
    }

    public static class ClampGaugeLayout
    {
        public const float LineFontPt = 14f, LineMinPt = 12f;
        public const float BarPt = 14f, BarRadiusPt = 7f, NeedleWPt = 4f, NeedleOverPt = 4f;
        public const float ButtonPt = 56f, ButtonFontPt = 20f, ButtonMinWPt = 160f, ButtonPadXPt = 24f, ButtonRadiusPt = 14f;
        public const float GapPt = 6f, TopGapPt = 8f, TwrGapPt = 10f;

        public static float ButtonWidth(float room) =>
            (float)Math.Ceiling(Math.Min(room, Math.Max(ButtonMinWPt, FlightHudLayout.BudgetWidth(ClampRelease.ReleaseText, ButtonFontPt) + 2f * ButtonPadXPt)));

        /// <summary>Layout for a card <paramref name="cardWPt"/> points wide (outer), with the widest status and T/W text it will show.</summary>
        public static ClampGaugeFit Fit(float cardWPt, string status, string twr)
        {
            var fit = new ClampGaugeFit();
            float room = Math.Max(1f, cardWPt - 2f * CountdownStack.CardPadXPt);
            string line = string.IsNullOrEmpty(twr) ? (status ?? "") : (status ?? "") + "  " + twr;
            fit.LinePt = CountdownStack.OneLine(line, room, LineFontPt, LineMinPt);
            fit.TwrShown = !string.IsNullOrEmpty(twr)
                && FlightHudLayout.BudgetWidth(status ?? "", fit.LinePt) + TwrGapPt + FlightHudLayout.BudgetWidth(twr, fit.LinePt) <= room - 2f;
            if (!fit.TwrShown) fit.LinePt = CountdownStack.OneLine(status ?? "", room, LineFontPt, LineMinPt);
            fit.ButtonW = ButtonWidth(room);
            fit.BlockH = (float)Math.Ceiling(TopGapPt + CountdownStack.LinePt(fit.LinePt) + GapPt + BarPt + NeedleOverPt + GapPt + ButtonPt);
            return fit;
        }

        /// <summary>The longest status the block can show (sizing pass: the face never changes while the needle moves).</summary>
        public static readonly string[] Statuses =
        {
            "Thrust 100%", "Full thrust: release!", "Too early: T/W under 1", "Too early: thrust not full",
            "Clean release!", "Late: auto release", "Auto release", "Released",
        };

        public static string WidestStatus(float fontPt)
        {
            string widest = "";
            float w = -1f;
            foreach (var s in Statuses)
            {
                float sw = FlightHudLayout.BudgetWidth(s, fontPt);
                if (sw > w) { w = sw; widest = s; }
            }
            return widest;
        }
    }
}
