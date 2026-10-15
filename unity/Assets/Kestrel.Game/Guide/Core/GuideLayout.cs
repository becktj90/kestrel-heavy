// Portrait placement for the subtitle stack. Numbers are points. The box docks under the
// safe-area header and stays out of the bottom 230 pt thumb zone. iPad columns cap at 600 pt.
using System;

namespace Kestrel.Game.Guide
{
    public readonly struct GuideScreen
    {
        public readonly float Width, Height, SafeTop, SafeBottom, SafeLeft, SafeRight, Header;
        public readonly bool Compact;

        public GuideScreen(float width, float height, float safeTop, float safeBottom, float safeLeft, float safeRight, float header, bool compact)
        {
            Width = width; Height = height;
            SafeTop = safeTop; SafeBottom = safeBottom; SafeLeft = safeLeft; SafeRight = safeRight;
            Header = header; Compact = compact;
        }

        public float DockTop => SafeTop + Header;
        public float LimitBottom => Height - SafeBottom - GuidePacing.ThumbZonePt;
        public float Available => GuideMath.Max(0f, LimitBottom - DockTop);

        public float ColumnWidth
        {
            get
            {
                float inner = Width - SafeLeft - SafeRight - 2f * GuidePacing.SideMarginPt;
                if (inner < 0f) inner = 0f;
                return GuideMath.Min(GuidePacing.MaxColumnPt, inner);
            }
        }

        public float ColumnX
        {
            get
            {
                float inner = Width - SafeLeft - SafeRight - 2f * GuidePacing.SideMarginPt;
                float spare = inner - ColumnWidth;
                if (spare < 0f) spare = 0f;
                return SafeLeft + GuidePacing.SideMarginPt + spare * 0.5f;
            }
        }

        /// <summary>Device pixels (safe rect is bottom-left, like Screen.safeArea) to points.</summary>
        public static GuideScreen FromPixels(float screenWpx, float screenHpx, float safeX, float safeY, float safeW, float safeH, float dpr, float headerPt, bool compact)
        {
            if (dpr <= 0f) dpr = 1f;
            float topPx = screenHpx - (safeY + safeH);
            if (topPx < 0f) topPx = 0f;
            float rightPx = screenWpx - (safeX + safeW);
            if (rightPx < 0f) rightPx = 0f;
            return new GuideScreen(screenWpx / dpr, screenHpx / dpr, topPx / dpr, safeY / dpr, safeX / dpr, rightPx / dpr, headerPt, compact);
        }
    }

    public readonly struct GuideFrame
    {
        public readonly float X, Y, W, H;
        public readonly float Portrait, Font, Skip, TextW;
        public readonly bool PortraitOn;

        public GuideFrame(float x, float y, float w, float h, float portrait, float font, float skip, float textW, bool portraitOn)
        {
            X = x; Y = y; W = w; H = h; Portrait = portrait; Font = font; Skip = skip; TextW = textW; PortraitOn = portraitOn;
        }

        public float Bottom => Y + H;
    }

    public static class GuideLayout
    {
        public static float TextWidth(float boxW, bool portrait, bool compact)
        {
            float portraitPt = portrait ? (compact ? GuidePacing.PortraitCompactPt : GuidePacing.PortraitPt) : 0f;
            float inner = boxW - GuidePacing.BorderPt * 2f - GuidePacing.PadPt * 2f;
            float gaps = (portrait ? 6f : 0f) + 6f;
            return GuideMath.Max(40f, inner - portraitPt - GuidePacing.SkipMinPt - gaps);
        }

        public static float ContentHeight(string text, float boxW, bool portrait, bool compact, float fontPt)
        {
            int lines = GlyphLayout.CountLines(text, TextWidth(boxW, portrait, compact), fontPt);
            float line = GlyphLayout.LineHeight(fontPt);
            float textH = GuideMath.Max(line, lines * line);
            float column = fontPt + 4f + textH + 4f + fontPt;
            float portraitPt = portrait ? (compact ? GuidePacing.PortraitCompactPt : GuidePacing.PortraitPt) : 0f;
            float body = GuideMath.Max(column, GuideMath.Max(portraitPt, GuidePacing.SkipMinPt));
            return GuidePacing.BorderPt * 2f + GuidePacing.PadPt * 2f + body;
        }

        public static GuideFrame[] Place(GuideScreen screen, bool[] portrait, string[] text, float fontPt)
        {
            int n = portrait != null ? portrait.Length : 0;
            if (text != null && text.Length < n) n = text.Length;
            n = GuideMath.Min(n, GuidePacing.MaxBoxes);
            var frames = new GuideFrame[n];
            if (n == 0) return frames;
            if (fontPt < GuidePacing.MinFontPt) fontPt = GuidePacing.MinFontPt;
            float cap = GuidePacing.HeightCap(screen.Compact, SizeFor(fontPt));
            float gap = GuidePacing.StackGapPt;
            float boxW = screen.ColumnWidth;
            var want = new float[n];
            float sum = gap * (n - 1);
            for (int i = 0; i < n; i++)
            {
                want[i] = ContentHeight(text[i], boxW, portrait[i], screen.Compact, fontPt);
                if (want[i] > cap) want[i] = cap;
                sum += want[i];
            }
            float available = screen.Available;
            if (sum > available && sum > 0f)
            {
                float overflow = sum - available;
                float room = 0f;
                float minH = GuidePacing.BorderPt * 2f + GuidePacing.PadPt * 2f + GuidePacing.SkipMinPt;
                for (int i = 0; i < n; i++) room += GuideMath.Max(0f, want[i] - minH);
                if (room > 0f)
                {
                    float cut = overflow < room ? overflow : room;
                    for (int i = 0; i < n; i++)
                    {
                        float spare = GuideMath.Max(0f, want[i] - minH);
                        want[i] -= cut * (spare / room);
                    }
                }
            }
            float y = screen.DockTop;
            float limit = screen.LimitBottom - GuidePacing.ShadowPt;
            for (int i = 0; i < n; i++)
            {
                float h = want[i];
                if (y + h > limit) h = GuideMath.Max(0f, limit - y);
                float portraitPt = portrait[i] ? (screen.Compact ? GuidePacing.PortraitCompactPt : GuidePacing.PortraitPt) : 0f;
                frames[i] = new GuideFrame(screen.ColumnX, y, boxW, h, portraitPt, fontPt, GuidePacing.SkipMinPt,
                    TextWidth(boxW, portrait[i], screen.Compact), portrait[i]);
                y += h + gap;
            }
            return frames;
        }

        static SubtitleSize SizeFor(float fontPt)
        {
            if (fontPt >= GuidePacing.ExtraLargeFontPt - 0.1f) return SubtitleSize.ExtraLarge;
            if (fontPt >= GuidePacing.LargeFontPt - 0.1f) return SubtitleSize.Large;
            return SubtitleSize.Normal;
        }

        /// <summary>Panel pixels for a point size. panelW / widthPt is the scale of a Scale-With-Screen-Size panel.</summary>
        public static float PanelFont(float fontPt, float panelW, float widthPt)
        {
            if (widthPt <= 0f) return fontPt;
            return fontPt * (panelW / widthPt);
        }
    }
}
