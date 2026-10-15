// Portrait placement for the subtitle stack. Numbers are points. Ascent and the designer dock
// under the safe-area header and stay out of the bottom 230 pt thumb zone. Menu screens
// (Home, Mission Select, Briefing) dock one box above the Home / Back key. iPad columns cap at 600 pt.
using System;

namespace Kestrel.Game.Guide
{
    public readonly struct GuideScreen
    {
        public readonly float Width, Height, SafeTop, SafeBottom, SafeLeft, SafeRight, Header;
        public readonly bool Compact;
        /// <summary>Points from the panel bottom up to a measured Home / Back key. 0 falls back to the safe inset plus <see cref="GuidePacing.MenuFooterPt"/>.</summary>
        public readonly float Footer;

        public GuideScreen(float width, float height, float safeTop, float safeBottom, float safeLeft, float safeRight, float header, bool compact, float footer = 0f)
        {
            Width = width; Height = height;
            SafeTop = safeTop; SafeBottom = safeBottom; SafeLeft = safeLeft; SafeRight = safeRight;
            Header = header; Compact = compact;
            Footer = footer < 0f ? 0f : footer;
        }

        public GuideScreen WithFooter(float footer)
            => new GuideScreen(Width, Height, SafeTop, SafeBottom, SafeLeft, SafeRight, Header, Compact, footer);

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
        /// <summary>Glyph height in points (7, 14, or 21). The atlas scale is this divided by 7.</summary>
        public readonly float Glyph;
        /// <summary>Body viewport height in points. Equals the wrapped text unless <see cref="Scroll"/> is set.</summary>
        public readonly float Body;
        /// <summary>True when the wrapped line is taller than the viewport. The viewport scrolls; it does not clip.</summary>
        public readonly bool Scroll;

        public GuideFrame(float x, float y, float w, float h, float portrait, float font, float skip, float textW, bool portraitOn, float glyph, float body, bool scroll)
        {
            X = x; Y = y; W = w; H = h; Portrait = portrait; Font = font; Skip = skip; TextW = textW; PortraitOn = portraitOn;
            Glyph = glyph; Body = body; Scroll = scroll;
        }

        public float Bottom => Y + H;
    }

    public static class GuideLayout
    {
        public static float TextWidth(float boxW, bool portrait, bool compact)
        {
            float portraitPt = portrait ? (compact ? GuidePacing.PortraitCompactPt : GuidePacing.PortraitPt) : 0f;
            float inner = boxW - GuidePacing.BorderPt * 2f - GuidePacing.PadPt * 2f;
            float gaps = (portrait ? GuidePacing.ColumnGapPt : 0f) + GuidePacing.ColumnGapPt;
            return GuideMath.Max(40f, inner - portraitPt - GuidePacing.SkipMinPt - gaps);
        }

        /// <summary>Wrapped body height, plus one point so the last pixel row is inside the box.</summary>
        public static float BodyHeight(string text, float textW, float glyphPt)
        {
            if (glyphPt < 7f) glyphPt = 7f;
            int lines = GlyphLayout.CountLines(text, textW, glyphPt);
            if (lines < 1) lines = 1;
            return lines * GlyphLayout.LineHeight(glyphPt) + 1f;
        }

        public static float Chrome(float fontPt)
            => GuidePacing.LabelLine(fontPt) + 4f + GuidePacing.LabelLine(fontPt) + 4f;

        /// <summary>Room left for the body once the name, the hint, and the chrome are in the box.</summary>
        public static float BodyRoom(float boxH, float fontPt)
        {
            float inner = boxH - GuidePacing.BorderPt * 2f - GuidePacing.PadPt * 2f - Chrome(fontPt);
            return inner > 0f ? inner : 0f;
        }

        public static float ContentHeight(string text, float boxW, bool portrait, bool compact, float fontPt, float glyphPt)
        {
            float textH = BodyHeight(text, TextWidth(boxW, portrait, compact), glyphPt);
            float column = Chrome(fontPt) + textH;
            float portraitPt = portrait ? (compact ? GuidePacing.PortraitCompactPt : GuidePacing.PortraitPt) : 0f;
            float body = GuideMath.Max(column, GuideMath.Max(portraitPt, GuidePacing.SkipMinPt));
            return GuidePacing.BorderPt * 2f + GuidePacing.PadPt * 2f + body;
        }

        /// <summary>The text column, the hint, and SKIP share a row only if this is true.
        /// The column ends one gap before the SKIP control.</summary>
        public static bool ClearsSkip(GuideFrame frame)
        {
            float inner = frame.W - GuidePacing.BorderPt * 2f - GuidePacing.PadPt * 2f;
            float gaps = (frame.PortraitOn ? GuidePacing.ColumnGapPt : 0f) + GuidePacing.ColumnGapPt;
            float used = frame.Portrait + gaps + frame.TextW + frame.Skip;
            return used <= inner + 0.51f;
        }

        /// <summary>False when every wrapped line fits in the body viewport, or the viewport scrolls
        /// and is at least one line tall.</summary>
        public static bool Clipped(GuideFrame frame, string text)
        {
            float glyph = frame.Glyph >= 7f ? frame.Glyph : 7f;
            float needed = BodyHeight(text, frame.TextW, glyph);
            float line = GlyphLayout.LineHeight(glyph);
            if (frame.Scroll) return frame.Body + 0.05f < line;
            return frame.Body + 0.75f < needed;
        }

        public static GuideFrame[] Place(GuideScreen screen, bool[] portrait, string[] text, float fontPt)
            => Place(screen, portrait, text, fontPt, GuidePlace.Designer);

        public static GuideFrame[] Place(GuideScreen screen, bool[] portrait, string[] text, float fontPt, GuidePlace place)
        {
            int n = portrait != null ? portrait.Length : 0;
            if (text != null && text.Length < n) n = text.Length;
            int capBoxes = place == GuidePlace.Menu ? 1 : GuidePacing.MaxBoxes;
            n = GuideMath.Min(n, capBoxes);
            var frames = new GuideFrame[n];
            if (n == 0) return frames;
            if (fontPt < GuidePacing.MinFontPt) fontPt = GuidePacing.MinFontPt;
            float cap = GuidePacing.HeightCap(screen.Compact, SizeFor(fontPt));
            float gap = GuidePacing.StackGapPt;
            float boxW = screen.ColumnWidth;
            bool menu = place == GuidePlace.Menu;
            // A measured footer is the distance from the panel bottom to the Home / Back key, and that
            // distance already includes the safe-area inset. The fallback adds the inset itself.
            float reserve = menu
                ? (screen.Footer > 0f ? screen.Footer : screen.SafeBottom + GuidePacing.MenuFooterPt)
                : screen.SafeBottom + GuidePacing.ThumbZonePt;
            float limit = screen.Height - reserve - GuidePacing.ShadowPt;
            float available = GuideMath.Max(0f, limit - screen.DockTop);
            int preferred = GuidePacing.PixelScale(fontPt);
            var scale = new int[n];
            var scroll = new bool[n];
            var want = new float[n];
            var glyph = new float[n];
            for (int i = 0; i < n; i++)
            {
                scale[i] = preferred;
                FitScale(text[i], boxW, portrait[i], screen.Compact, fontPt, cap, ref scale[i], out want[i], out scroll[i]);
            }
            float sum = StackHeight(want, gap);
            int guard = 0;
            while (sum > available && available > 0f && guard < 8)
            {
                guard++;
                int tallest = -1;
                for (int i = 0; i < n; i++)
                {
                    if (scale[i] <= 1) continue;
                    if (tallest < 0 || want[i] > want[tallest]) tallest = i;
                }
                if (tallest < 0) break;
                scale[tallest]--;
                FitScale(text[tallest], boxW, portrait[tallest], screen.Compact, fontPt, cap, ref scale[tallest], out want[tallest], out scroll[tallest]);
                sum = StackHeight(want, gap);
            }
            if (sum > available && sum > 0f)
            {
                float overflow = sum - available;
                float room = 0f;
                var floor = new float[n];
                for (int i = 0; i < n; i++)
                {
                    floor[i] = MinHeight(portrait[i], screen.Compact, fontPt, scale[i] * 7f);
                    room += GuideMath.Max(0f, want[i] - floor[i]);
                }
                if (room > 0f)
                {
                    float cut = overflow < room ? overflow : room;
                    for (int i = 0; i < n; i++)
                    {
                        float spare = GuideMath.Max(0f, want[i] - floor[i]);
                        float next = want[i] - cut * (spare / room);
                        if (next < floor[i]) next = floor[i];
                        if (next + 0.5f < ContentHeight(text[i], boxW, portrait[i], screen.Compact, fontPt, scale[i] * 7f))
                            scroll[i] = true;
                        want[i] = next;
                    }
                }
            }
            for (int i = 0; i < n; i++) glyph[i] = scale[i] * 7f;
            sum = StackHeight(want, gap);
            float y = menu ? limit - sum : screen.DockTop;
            if (y < screen.DockTop) y = screen.DockTop;
            for (int i = 0; i < n; i++)
            {
                float h = want[i];
                if (y + h > limit) h = GuideMath.Max(0f, limit - y);
                float textW = TextWidth(boxW, portrait[i], screen.Compact);
                float needed = BodyHeight(text[i], textW, glyph[i]);
                float room = BodyRoom(h, fontPt);
                bool scrolls = scroll[i] || room + 0.75f < needed;
                float body = scrolls ? room : needed;
                if (body < 0f) body = 0f;
                if (scrolls && body + 0.05f < GlyphLayout.LineHeight(glyph[i]) && h + 1f < limit - y)
                {
                    // Keep a full line on screen when the cap still has slack.
                    float bump = GlyphLayout.LineHeight(glyph[i]) - body;
                    if (y + h + bump <= limit) { h += bump; body += bump; room = body; }
                }
                float portraitPt = portrait[i] ? (screen.Compact ? GuidePacing.PortraitCompactPt : GuidePacing.PortraitPt) : 0f;
                frames[i] = new GuideFrame(screen.ColumnX, y, boxW, h, portraitPt, fontPt, GuidePacing.SkipMinPt,
                    textW, portrait[i], glyph[i], body, scrolls);
                y += h + gap;
            }
            return frames;
        }

        static void FitScale(string text, float boxW, bool portrait, bool compact, float fontPt, float cap, ref int scale, out float height, out bool scroll)
        {
            if (scale < 1) scale = 1;
            while (scale > 1 && ContentHeight(text, boxW, portrait, compact, fontPt, scale * 7f) > cap + 0.01f)
                scale--;
            height = ContentHeight(text, boxW, portrait, compact, fontPt, scale * 7f);
            scroll = false;
            if (height > cap)
            {
                height = cap;
                scroll = true;
            }
        }

        static float MinHeight(bool portrait, bool compact, float fontPt, float glyphPt)
        {
            float line = GlyphLayout.LineHeight(glyphPt < 7f ? 7f : glyphPt);
            float column = Chrome(fontPt) + line;
            float portraitPt = portrait ? (compact ? GuidePacing.PortraitCompactPt : GuidePacing.PortraitPt) : 0f;
            float body = GuideMath.Max(column, GuideMath.Max(portraitPt, GuidePacing.SkipMinPt));
            return GuidePacing.BorderPt * 2f + GuidePacing.PadPt * 2f + body;
        }

        static float StackHeight(float[] want, float gap)
        {
            float sum = gap * (want.Length - 1);
            for (int i = 0; i < want.Length; i++) sum += want[i];
            return sum;
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
