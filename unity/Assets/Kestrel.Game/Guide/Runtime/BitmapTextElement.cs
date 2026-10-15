using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Guide
{
    /// <summary>Draws a line with the baked 5x7 atlas, nearest-neighbour. A character the atlas
    /// does not have switches the whole line to a Label so another language still reads.
    /// Space Grotesk (OFL) is the approved face for that Label when the project bundles it.</summary>
    public sealed class BitmapTextElement : VisualElement
    {
        static readonly Color Cream = new Color(0xFA / 255f, 0xF0 / 255f, 0xD7 / 255f, 1f);
        readonly List<GlyphLayout.Quad> quads = new List<GlyphLayout.Quad>();
        Label fallback;

        public GlyphTable Glyphs;
        public Texture2D Atlas;
        public string Text { get; private set; } = "";
        public float FontPx = 15f;
        public float LayoutWidth = 200f;
        public bool Caret;
        public bool UsingFallback { get; private set; }

        public BitmapTextElement()
        {
            generateVisualContent += OnGenerate;
            pickingMode = PickingMode.Ignore;
            style.overflow = Overflow.Hidden;
        }

        public void SetLine(string text, bool caret)
        {
            Text = text ?? "";
            Caret = caret;
            UsingFallback = Glyphs == null || Atlas == null || !Glyphs.Covers(Text);
            if (UsingFallback)
            {
                if (fallback == null)
                {
                    fallback = new Label { name = "guide-fallback", pickingMode = PickingMode.Ignore };
                    fallback.style.color = Cream;
                    fallback.style.whiteSpace = WhiteSpace.Normal;
                    Add(fallback);
                }
                fallback.text = Text;
                fallback.style.fontSize = Snap(FontPx);
                fallback.style.display = DisplayStyle.Flex;
            }
            else if (fallback != null) fallback.style.display = DisplayStyle.None;
            float width = LayoutWidth > 0f ? LayoutWidth : 200f;
            float font = Snap(FontPx);
            int lines = GlyphLayout.CountLines(Text, width, font);
            style.height = GlyphLayout.LineHeight(font) * lines;
            style.flexShrink = 0;
            if (Atlas != null) Atlas.filterMode = FilterMode.Point;
            MarkDirtyRepaint();
        }

        /// <summary>Whole texels. A fractional scale turns nearest-neighbour into uneven pixels.</summary>
        public static float Snap(float fontPx)
        {
            if (fontPx < 7f) fontPx = 7f;
            int texel = (int)Math.Round(fontPx / 7f);
            if (texel < 1) texel = 1;
            return texel * 7f;
        }

        void OnGenerate(MeshGenerationContext ctx)
        {
            if (ctx == null || UsingFallback || Atlas == null || Glyphs == null) return;
            float font = Snap(FontPx);
            float width = LayoutWidth > 0f ? LayoutWidth : contentRect.width;
            GlyphLayout.Layout(Text, width, font, Glyphs, Caret, quads);
            int glyphs = 0, carets = 0;
            for (int i = 0; i < quads.Count; i++)
            {
                if (quads[i].Glyph >= 0) glyphs++;
                else if (quads[i].Glyph == -1) carets++;
            }
            if (glyphs > 0) Write(ctx, Atlas, glyphs, false);
            if (carets > 0) Write(ctx, Texture2D.whiteTexture, carets, true);
        }

        void Write(MeshGenerationContext ctx, Texture tex, int count, bool caretOnly)
        {
            if (tex == null || count <= 0) return;
            var mesh = ctx.Allocate(count * 4, count * 6, tex);
            if (mesh == null) return;
            var tint = new Color32(0xFA, 0xF0, 0xD7, 255);
            int n = 0;
            for (int i = 0; i < quads.Count; i++)
            {
                var q = quads[i];
                bool caret = q.Glyph == -1;
                if (caret != caretOnly || q.Glyph < -1) continue;
                float u0 = 0f, u1 = 1f, v0 = 0f, v1 = 1f;
                if (!caret)
                {
                    int col = q.Glyph % Glyphs.Cols;
                    int row = q.Glyph / Glyphs.Cols;
                    float aw = Glyphs.AtlasW > 0 ? Glyphs.AtlasW : 1f;
                    float ah = Glyphs.AtlasH > 0 ? Glyphs.AtlasH : 1f;
                    u0 = (col * Glyphs.CellW) / aw;
                    u1 = (col * Glyphs.CellW + Glyphs.GlyphW) / aw;
                    v1 = 1f - (row * Glyphs.CellH) / ah;
                    v0 = 1f - (row * Glyphs.CellH + Glyphs.GlyphH) / ah;
                }
                int vi = n * 4;
                mesh.SetNextVertex(Vert(q.X, q.Y, u0, v1, tint));
                mesh.SetNextVertex(Vert(q.X + q.W, q.Y, u1, v1, tint));
                mesh.SetNextVertex(Vert(q.X + q.W, q.Y + q.H, u1, v0, tint));
                mesh.SetNextVertex(Vert(q.X, q.Y + q.H, u0, v0, tint));
                ushort b = (ushort)vi;
                mesh.SetNextIndex(b);
                mesh.SetNextIndex((ushort)(b + 1));
                mesh.SetNextIndex((ushort)(b + 2));
                mesh.SetNextIndex((ushort)(b + 2));
                mesh.SetNextIndex((ushort)(b + 3));
                mesh.SetNextIndex(b);
                n++;
            }
        }

        static Vertex Vert(float x, float y, float u, float v, Color32 tint)
        {
            return new Vertex { position = new Vector3(x, y, Vertex.nearZ), uv = new Vector2(u, v), tint = tint };
        }
    }
}
