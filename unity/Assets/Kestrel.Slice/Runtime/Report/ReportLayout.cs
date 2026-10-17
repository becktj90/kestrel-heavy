// Portrait post-flight sheet from iPhone SE (375 pt) through iPad (§3, §6).
// Origin is top-left, in points. The failure stays visible above the card.
// Touch targets are at least 44 pt. Reduce Motion reveals with no duration.
using System;

namespace Kestrel.Slice
{
    public struct ReportRect
    {
        public double X, Y, W, H;
        public ReportRect(double x, double y, double w, double h) { X = x; Y = y; W = w; H = h; }
        public double Right => X + W;
        public double Bottom => Y + H;
    }

    public sealed class ReportFrame
    {
        public ReportRect Card, Icon, Kid, Engineer, Fix, Plots;
        public double VisualTop, VisualBottom;
        public double RevealSeconds;
        public bool Portrait;
    }

    public static class ReportLayout
    {
        public const double MinTargetPt = 44;
        public const double SideMarginPt = 8;
        public const double CardMaxWidthPt = 560;
        public const double RevealSeconds = 0.35;
        /// <summary>The card is a solid sheet. Designer stats must not show through it.</summary>
        public const double PanelAlpha = 1;
        /// <summary>Phone designer sheet top, as a fraction of the screen. The card starts at or above this.</summary>
        public const double DesignerSheetTop = 0.35;

        public static ReportFrame Solve(double widthPt, double heightPt, double safeTop, double safeBottom, double safeLeft, double safeRight, bool reduceMotion)
        {
            if (widthPt < 1) widthPt = 1;
            if (heightPt < 1) heightPt = 1;
            if (safeTop < 0) safeTop = 0;
            if (safeBottom < 0) safeBottom = 0;
            if (safeLeft < 0) safeLeft = 0;
            if (safeRight < 0) safeRight = 0;
            double contentW = Math.Max(MinTargetPt, widthPt - safeLeft - safeRight - 2 * SideMarginPt);
            double contentTop = safeTop;
            double contentBottom = heightPt - safeBottom;
            double contentH = Math.Max(MinTargetPt, contentBottom - contentTop);
            double cardW = Math.Min(contentW, CardMaxWidthPt);
            // As tall as the failure-visible band allows, so the card covers the designer sheet
            // (its top sits at 35% of the screen) instead of letting the stats row overlap it.
            double tallTop = contentBottom - Math.Min(contentH * 0.58, 640);
            double coverTop = contentTop + heightPt * 0.28;
            double sheetTop = heightPt * DesignerSheetTop;
            double cardY = coverTop;
            if (cardY > sheetTop) cardY = sheetTop;
            if (tallTop < cardY) cardY = tallTop;
            if (cardY < contentTop) cardY = contentTop;
            if (cardY > contentBottom - MinTargetPt) cardY = contentBottom - MinTargetPt;
            double cardH = contentBottom - cardY;
            double cardX = safeLeft + SideMarginPt + (contentW - cardW) * 0.5;
            const double pad = 12;
            double icon = MinTargetPt;
            double x = cardX + pad;
            double innerW = Math.Max(MinTargetPt, cardW - pad * 2);
            double y = cardY + pad;
            var iconR = new ReportRect(x, y, icon, icon);
            y += icon + 8;
            double kidH = 72;
            double engH = 56;
            var kid = new ReportRect(x, y, innerW, kidH);
            y += kidH + 4;
            var eng = new ReportRect(x, y, innerW, engH);
            y += engH + 8;
            double fixH = MinTargetPt;
            double fixY = cardY + cardH - pad - fixH;
            var fix = new ReportRect(x, fixY, innerW, fixH);
            double plotH = Math.Max(36, fixY - 8 - y);
            var plots = new ReportRect(x, y, innerW, plotH);
            return new ReportFrame
            {
                Card = new ReportRect(cardX, cardY, cardW, cardH),
                Icon = iconR,
                Kid = kid,
                Engineer = eng,
                Fix = fix,
                Plots = plots,
                VisualTop = contentTop,
                VisualBottom = cardY,
                RevealSeconds = reduceMotion ? 0 : RevealSeconds,
                Portrait = heightPt >= widthPt,
            };
        }

        /// <summary>The opaque card reaches the designer sheet, so the stats row cannot sit on top of it.</summary>
        public static bool CoversDesignerStats(ReportFrame frame, double heightPt)
        {
            if (frame == null || !(heightPt > 0)) return false;
            return frame.Card.Y <= heightPt * DesignerSheetTop + 0.6
                && frame.Card.H > 1
                && PanelAlpha >= 1;
        }
    }
}
