// Thrum's corner sprite. One 16×16 portrait, a slide in from the right, and at most one caption line.
// The caption is the card text (tokens filled). It shows only when her voice will not.
using System;
using System.Text;

namespace Kestrel.Game.Guide
{
    public enum GuidePopAction { Finish, Replay, Dismiss }

    public readonly struct GuidePopFrame
    {
        public readonly float X, Y, Size, Block, CaptionW;
        public GuidePopFrame(float x, float y, float size, float block, float captionW)
        {
            X = x; Y = y; Size = size; Block = block; CaptionW = captionW;
        }
    }

    public static class GuidePopLayout
    {
        public const float ArtPx = 16f;
        public const int PixelScale = 4;
        public const float MarginPt = 8f;
        public const float SlideSeconds = 0.28f;
        public const float CaptionMaxPt = 220f;

        public static float DisplayPt => ArtPx * PixelScale;

        /// <summary>Menu, designer, briefing and the post-flight report. Never the ascent.</summary>
        public static bool Shows(GuidePlace place) => place != GuidePlace.Ascent;

        /// <summary>0 hides the block past the right edge. 1 rests it in the top-right corner.</summary>
        public static float Slide(float t, bool reduceMotion)
        {
            if (reduceMotion) return 1f;
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return t * t * (3f - 2f * t);
        }

        public static GuidePopFrame Place(GuideScreen screen, float slideT, bool reduceMotion, bool caption)
        {
            float size = DisplayPt;
            float captionW = CaptionWidth(screen, size);
            float block = caption ? GuideMath.Max(size, captionW) : size;
            float restX = screen.Width - screen.SafeRight - MarginPt - block;
            float travel = block + MarginPt + screen.SafeRight;
            float x = restX + (1f - Slide(slideT, reduceMotion)) * travel;
            float y = screen.SafeTop + MarginPt;
            return new GuidePopFrame(x, y, size, block, captionW);
        }

        public static float CaptionWidth(GuideScreen screen, float size)
        {
            float room = screen.Width - screen.SafeLeft - screen.SafeRight - 2f * MarginPt;
            if (room < 0f) room = 0f;
            float cap = room < CaptionMaxPt ? room : CaptionMaxPt;
            // 335 pt is the narrow portrait and the narrow landscape. The caption
            // stays in the right half so it does not cover the title or the HUD.
            float half = screen.Width * 0.5f;
            float corner = screen.Width - screen.SafeRight - MarginPt - half;
            if (corner < size) corner = size;
            if (cap > corner) cap = corner;
            if (cap < size) cap = size;
            if (room >= size && cap > room) cap = room;
            return cap;
        }

        /// <summary>Finish a caption that is still typing. Replay when a clip can play and is quiet.
        /// Otherwise dismiss.</summary>
        public static GuidePopAction OnTap(bool caption, bool captionDone, bool canPlay, bool sounding)
        {
            if (caption && !captionDone) return GuidePopAction.Finish;
            if (canPlay && !sounding) return GuidePopAction.Replay;
            return GuidePopAction.Dismiss;
        }
    }

    public static class GuideCaption
    {
        public static bool Visible(bool voiceOn, bool muted, bool clipMissing)
            => !voiceOn || muted || clipMissing;

        public static float Glyph(SubtitleSize size) => GuidePacing.GlyphPt(GuidePacing.FontPt(size));

        /// <summary>One line. Newlines become spaces. A line that does not fit ends with "...".</summary>
        public static string Line(string text, float widthPt, float glyphPt)
        {
            string flat = Flatten(text);
            if (flat.Length == 0) return "";
            if (glyphPt < 7f) glyphPt = 7f;
            float adv = GlyphLayout.Advance(glyphPt);
            if (!(adv > 0f) || !(widthPt > 0f)) return flat;
            int slots = (int)Math.Floor(widthPt / adv);
            if (slots < 4) slots = 4;
            if (flat.Length <= slots) return flat;
            if (slots <= 3) return flat.Substring(0, slots);
            return flat.Substring(0, slots - 3) + "...";
        }

        static string Flatten(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder(text.Length);
            bool space = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n' || c == '\r' || c == '\t' || c == ' ')
                {
                    space = sb.Length > 0;
                    continue;
                }
                if (space) { sb.Append(' '); space = false; }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
