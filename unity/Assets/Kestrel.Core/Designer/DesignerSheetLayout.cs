// Portrait designer sheet. Points, origin top-left.
// The stats row, the delta line, and the warning banner each get their own band so a long
// max-Q reading cannot sit on top of the cost, and the delta line cannot sit on the stats.
// While the post-flight card is up the sheet is not live, so the rocket name is not under it.
using Kestrel.Core.Flight;

namespace Kestrel.Core.Designer
{
    public sealed class DesignerSheetFrame
    {
        public HudBox Title, Stats, Delta, Budget, Banner, Card;
        public HudBox[] Cells = new HudBox[4];
        public string[] CellText = new string[4];
        public float[] CellFont = new float[4];
        public bool ReportOpen;
    }

    public static class DesignerSheetLayout
    {
        public const float SidePt = 8f;
        public const float GapPt = 4f;
        public const float TitlePt = 44f;
        public const float LabelPt = 13f;
        public const float ValueStartPt = 16f;
        public const float ValueMinPt = 11f;
        public const float BannerFontPt = 13f;
        public const float LinePt = 18f;
        public const float BudgetPt = 28f;
        public const float BannerMinPt = 44f;
        public const float CellPadPt = 4f;

        public static DesignerSheetFrame Place(FlightHudLayout.Device d, bool reportOpen, string[] values, string delta, string banner)
        {
            float left = d.SafeLeft + SidePt;
            float width = d.W - d.SafeLeft - d.SafeRight - SidePt * 2f;
            if (width < 32f) width = 32f;
            var f = new DesignerSheetFrame { ReportOpen = reportOpen };
            float cardH = d.H * 0.58f;
            f.Card = new HudBox { X = d.SafeLeft, Y = d.H - d.SafeBottom - cardH, W = d.W - d.SafeLeft - d.SafeRight, H = cardH };
            if (reportOpen) return f;

            float y = d.SafeTop;
            f.Title = Box(left, y, width, TitlePt);
            y = f.Title.Bottom + GapPt;

            float statsH = LabelPt + LinePt + GapPt;
            f.Stats = Box(left, y, width, statsH);
            float cellW = (width - GapPt * 3f) / 4f;
            for (int i = 0; i < 4; i++)
            {
                f.Cells[i] = Box(left + i * (cellW + GapPt), y, cellW, statsH);
                string raw = values != null && i < values.Length ? values[i] ?? "" : "";
                float inner = cellW - CellPadPt * 2f;
                if (inner < 8f) inner = 8f;
                float font = FitFont(raw, inner);
                f.CellFont[i] = font;
                f.CellText[i] = Ellipsize(raw, inner, font);
            }

            y = f.Stats.Bottom + GapPt;
            if (!string.IsNullOrEmpty(delta))
            {
                int lines = LineCount(delta, width - CellPadPt * 2f, BannerFontPt);
                f.Delta = Box(left, y, width, lines * LinePt);
                y = f.Delta.Bottom + GapPt;
            }

            f.Budget = Box(left, y, width, BudgetPt);
            y = f.Budget.Bottom + GapPt;

            if (!string.IsNullOrEmpty(banner))
            {
                int lines = LineCount(banner, width - CellPadPt * 2f, BannerFontPt);
                float h = lines * LinePt;
                if (h < BannerMinPt) h = BannerMinPt;
                f.Banner = Box(left, y, width, h);
            }
            return f;
        }

        public static float FitFont(string text, float width)
        {
            float size = ValueStartPt;
            if (string.IsNullOrEmpty(text)) return size;
            while (size > ValueMinPt && FlightHudLayout.TextWidth(text, size) > width)
                size -= 1f;
            return size;
        }

        public static string Ellipsize(string text, float width, float fontPt)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (FlightHudLayout.TextWidth(text, fontPt) <= width) return text;
            string t = text;
            while (t.Length > 1 && FlightHudLayout.TextWidth(t + "…", fontPt) > width)
                t = t.Substring(0, t.Length - 1);
            return t.TrimEnd() + "…";
        }

        public static int LineCount(string text, float width, float fontPt)
        {
            if (string.IsNullOrEmpty(text)) return 1;
            if (width < 8f) width = 8f;
            var words = text.Split(' ');
            int lines = 1;
            float used = 0f;
            for (int i = 0; i < words.Length; i++)
            {
                float w = FlightHudLayout.TextWidth(words[i], fontPt);
                float gap = used > 0f ? FlightHudLayout.TextWidth(" ", fontPt) : 0f;
                if (used > 0f && used + gap + w > width + 0.1f)
                {
                    lines++;
                    used = w;
                }
                else used += gap + w;
            }
            return lines;
        }

        static HudBox Box(float x, float y, float w, float h) => new HudBox { X = x, Y = y, W = w, H = h };
    }
}
