// Width and word-wrap for the four menu subset fonts, engine-free, so the phone Home layout can be
// checked in Kestrel.Core.Tests for any string (the record line, credits, Thrum's line). The tables
// are generated (HomeTypeMetrics.Generated.cs); hmtx advances only, so widths carry a small margin.
using System;

namespace Kestrel.Core.Flight
{
    public enum HomeFace { FrauncesBlack, FrauncesBold, GroteskBold, GroteskMedium }

    public static partial class HomeTypeMetrics
    {
        /// <summary>Kerning and glyph overhang are not in the advance table. Measured widths are grown by this
        /// share, so a string that fits here also fits the TextCore layout.</summary>
        public const float Margin = 1.03f;

        const string Extra = "\u00b7\u2013\u2014\u2019";

        public static int Index(char c)
        {
            if (c >= ' ' && c <= '~') return c - ' ';
            int i = Extra.IndexOf(c);
            return i >= 0 ? 95 + i : -1;
        }

        static float[] Table(HomeFace face)
        {
            switch (face)
            {
                case HomeFace.FrauncesBlack: return FrauncesBlack;
                case HomeFace.FrauncesBold: return FrauncesBold;
                case HomeFace.GroteskBold: return GroteskBold;
                default: return GroteskMedium;
            }
        }

        /// <summary>Advance of <paramref name="text"/> at <paramref name="size"/> px, plus tracking (em per glyph), no margin.</summary>
        public static float Width(HomeFace face, string text, float size, float trackEm = 0f)
        {
            if (string.IsNullOrEmpty(text) || !(size > 0f)) return 0f;
            var t = Table(face);
            float fallback = t[Index('n')];
            float sum = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                int k = Index(text[i]);
                sum += k >= 0 && t[k] > 0f ? t[k] : fallback;
            }
            return sum * size / 100f + trackEm * size * text.Length;
        }

        /// <summary>Width with <see cref="Margin"/>: what a box must hold for the string on one line.</summary>
        public static float Need(HomeFace face, string text, float size, float trackEm = 0f)
            => Width(face, text, size, trackEm) * Margin;

        public static bool Fits(HomeFace face, string text, float size, float width, float trackEm = 0f)
            => Need(face, text, size, trackEm) <= width + 0.01f;

        /// <summary>Greedy word wrap at spaces, the way a UI Toolkit label with white-space: normal breaks.
        /// A word wider than the box takes as many lines as its width needs.</summary>
        public static int Lines(HomeFace face, string text, float size, float width, float trackEm = 0f)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            if (!(width > 1f)) return 99;
            var words = text.Split(' ');
            float space = Need(face, " ", size, trackEm);
            int lines = 1;
            float run = 0f;
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length == 0) continue;
                float w = Need(face, words[i], size, trackEm);
                if (run <= 0f)
                {
                    if (w > width + 0.01f) { int n = (int)Math.Ceiling(w / width); lines += n - 1; run = w - (n - 1) * width; }
                    else run = w;
                    continue;
                }
                if (run + space + w <= width + 0.01f) { run += space + w; continue; }
                lines++;
                if (w > width + 0.01f) { int n = (int)Math.Ceiling(w / width); lines += n - 1; run = w - (n - 1) * width; }
                else run = w;
            }
            return lines;
        }

        /// <summary>Largest size from <paramref name="max"/> down to <paramref name="min"/> (half-px steps) that keeps
        /// the string on one line. Returns min when nothing fits.</summary>
        public static float FitOneLine(HomeFace face, string text, float max, float min, float width, float trackEm = 0f)
        {
            for (float s = max; s >= min - 0.001f; s -= 0.5f)
                if (Fits(face, text, s, width, trackEm)) return s;
            return min;
        }

        /// <summary>The words that fit in <paramref name="lines"/> lines, ending in "..." when the text was cut.</summary>
        public static string Clip(HomeFace face, string text, float size, float width, int lines)
        {
            if (string.IsNullOrEmpty(text) || Lines(face, text, size, width) <= lines) return text ?? "";
            var words = text.Split(' ');
            for (int n = words.Length - 1; n > 0; n--)
            {
                string head = string.Join(" ", words, 0, n).TrimEnd(',', '.', ';', ':', '!', '?') + "...";
                if (Lines(face, head, size, width) <= lines) return head;
            }
            return "...";
        }
    }
}
