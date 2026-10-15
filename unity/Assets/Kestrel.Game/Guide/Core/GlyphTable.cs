// Our 5x7 bitmap font. The bits live in Resources/Guide/glyphs.json, baked by
// Guide/Art~/bake_guide_art.py. A missing character means the line uses the panel font
// (Space Grotesk is the approved OFL face when the project bundles it).
using System;
using System.Collections.Generic;
using Kestrel.Core.Json;

namespace Kestrel.Game.Guide
{
    public sealed class GlyphTable
    {
        public int CellW = 6, CellH = 8, GlyphW = 5, GlyphH = 7, Cols = 16, AtlasW, AtlasH;
        readonly Dictionary<char, int> index = new Dictionary<char, int>();
        readonly byte[] masks;

        GlyphTable(int count)
        {
            masks = new byte[count * 7];
        }

        public int Count => index.Count;

        public static GlyphTable Parse(string json)
        {
            var root = MiniJson.ParseObject(json);
            var glyphs = root.Arr("glyphs");
            var table = new GlyphTable(glyphs != null ? glyphs.Count : 0);
            table.CellW = (int)root.Num("cellW", 6);
            table.CellH = (int)root.Num("cellH", 8);
            table.GlyphW = (int)root.Num("glyphW", 5);
            table.GlyphH = (int)root.Num("glyphH", 7);
            table.Cols = (int)root.Num("cols", 16);
            table.AtlasW = (int)root.Num("atlasW");
            table.AtlasH = (int)root.Num("atlasH");
            if (glyphs == null) return table;
            for (int i = 0; i < glyphs.Count; i++)
            {
                var o = glyphs[i] as JsonObject;
                if (o == null) continue;
                string ch = o.Str("ch");
                if (string.IsNullOrEmpty(ch)) continue;
                table.index[ch[0]] = i;
                var rows = o.Arr("rows");
                for (int r = 0; r < 7 && rows != null && r < rows.Count; r++)
                {
                    string bits = rows[r] as string ?? "";
                    byte mask = 0;
                    for (int c = 0; c < bits.Length && c < 5; c++)
                        if (bits[c] == '1') mask |= (byte)(1 << (4 - c));
                    table.masks[i * 7 + r] = mask;
                }
            }
            return table;
        }

        public bool Contains(char c) => index.ContainsKey(c);

        public bool TryIndex(char c, out int glyphIndex) => index.TryGetValue(c, out glyphIndex);

        public bool Covers(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n' || c == '\r') continue;
                if (!index.ContainsKey(c)) return false;
            }
            return true;
        }

        public bool TryMask(char c, out int glyphIndex, out byte row0, out byte row1, out byte row2, out byte row3, out byte row4, out byte row5, out byte row6)
        {
            row0 = row1 = row2 = row3 = row4 = row5 = row6 = 0;
            if (!index.TryGetValue(c, out glyphIndex)) return false;
            int o = glyphIndex * 7;
            row0 = masks[o]; row1 = masks[o + 1]; row2 = masks[o + 2]; row3 = masks[o + 3];
            row4 = masks[o + 4]; row5 = masks[o + 5]; row6 = masks[o + 6];
            return true;
        }

        public byte Row(int glyphIndex, int row) => masks[glyphIndex * 7 + row];
    }

    public static class GlyphLayout
    {
        public static float Advance(float fontPt) => 6f * (fontPt / 7f);
        public static float LineHeight(float fontPt) => 8f * (fontPt / 7f);

        public readonly struct Quad
        {
            public readonly float X, Y, W, H;
            public readonly int Glyph;
            public Quad(float x, float y, float w, float h, int glyph) { X = x; Y = y; W = w; H = h; Glyph = glyph; }
        }

        public static void Layout(string text, float widthPt, float fontPt, GlyphTable table, bool caret, List<Quad> into)
        {
            into.Clear();
            if (fontPt <= 0f) return;
            float scale = fontPt / 7f;
            float adv = 6f * scale;
            float gw = 5f * scale;
            float gh = 7f * scale;
            float lineH = 8f * scale;
            float x = 0f, y = 0f;
            string s = text ?? "";
            int i = 0;
            while (i < s.Length)
            {
                if (s[i] == '\n') { x = 0f; y += lineH; i++; continue; }
                if (s[i] == '\r') { i++; continue; }
                int end = i;
                while (end < s.Length && s[end] != ' ' && s[end] != '\n' && s[end] != '\r') end++;
                float word = (end - i) * adv;
                if (x > 0f && widthPt > 0f && x + word > widthPt + 0.01f) { x = 0f; y += lineH; }
                while (i < end)
                {
                    if (x > 0f && widthPt > 0f && x + adv > widthPt + 0.01f && (end - i) > 0 && word > widthPt)
                    { x = 0f; y += lineH; }
                    int glyph = -2;
                    if (table != null && table.TryIndex(s[i], out int found)) glyph = found;
                    into.Add(new Quad(x, y, gw, gh, glyph));
                    x += adv;
                    i++;
                }
                if (i < s.Length && s[i] == ' ')
                {
                    if (x > 0f && widthPt > 0f && x + adv > widthPt + 0.01f) { x = 0f; y += lineH; }
                    else x += adv;
                    i++;
                }
            }
            if (caret) into.Add(new Quad(x, y, gw, gh, -1));
        }

        public static int CountLines(string text, float widthPt, float fontPt)
        {
            if (string.IsNullOrEmpty(text)) return 1;
            float adv = Advance(fontPt);
            if (adv <= 0f) return 1;
            int lines = 1;
            float x = 0f;
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '\n') { lines++; x = 0f; i++; continue; }
                if (text[i] == '\r') { i++; continue; }
                int end = i;
                while (end < text.Length && text[end] != ' ' && text[end] != '\n' && text[end] != '\r') end++;
                int len = end - i;
                float word = len * adv;
                bool wrapped = false;
                if (x > 0f && widthPt > 0f && x + word > widthPt + 0.01f)
                {
                    lines++;
                    x = 0f;
                    wrapped = true;
                }
                if (widthPt > 0f && word > widthPt && len > 1)
                {
                    int fit = GuideMath.Max(1, (int)(widthPt / adv));
                    int left = len;
                    if (x > 0f && !wrapped) { lines++; x = 0f; }
                    while (left > fit) { lines++; left -= fit; }
                    x = left * adv;
                }
                else x += word;
                i = end;
                if (i < text.Length && text[i] == ' ')
                {
                    if (x > 0f && widthPt > 0f && x + adv > widthPt + 0.01f) { lines++; x = 0f; }
                    else x += adv;
                    i++;
                }
            }
            return lines;
        }
    }
}
