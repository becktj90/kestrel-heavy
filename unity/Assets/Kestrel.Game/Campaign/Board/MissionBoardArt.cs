// Dusk sky, constellation trail, and gold star pips for the mission board.
// The sky is a mesh gradient (night at the top, amber at the horizon). Stars are
// quads, not a font glyph. Glass panels stay a baked translucent fill: no blur.
using System.Collections.Generic;
using Kestrel.Core.Flight;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.MissionSelect
{
    public static class MissionBoardPalette
    {
        public static readonly Color Night = Hex(0x05, 0x08, 0x1A);
        public static readonly Color Deep = Hex(0x0F, 0x17, 0x40);
        public static readonly Color Violet = Hex(0x3A, 0x2A, 0x5E);
        public static readonly Color Ember = Hex(0xC7, 0x51, 0x4A);
        public static readonly Color Amber = Hex(0xFF, 0xB0, 0x5A);
        public static readonly Color Horizon = Hex(0xFF, 0xD8, 0x9A);
        public static readonly Color Cream = Hex(0xF6, 0xEB, 0xD2);
        public static readonly Color Ink = Hex(0x0D, 0x14, 0x28);
        public static readonly Color Brass = Hex(0xD2, 0xA2, 0x4F);
        public static readonly Color Accent = Hex(0xFF, 0xA2, 0x3E);
        public static readonly Color Fly = Hex(0xF2, 0x54, 0x3D);
        public static readonly Color FlyDeep = Hex(0xD6, 0x3A, 0x5A);
        public static readonly Color Muted = Hex(0x8E, 0x9A, 0xB8);
        public static readonly Color Teal = Hex(0x46, 0xD9, 0xCC);
        public static readonly Color Glass = new Color(0x0D / 255f, 0x14 / 255f, 0x28 / 255f, 0.92f);
        public static readonly Color GlassLine = new Color(1f, 1f, 1f, 0.22f);
        public static readonly Color Trail = new Color(0x8E / 255f, 0x9A / 255f, 0xB8 / 255f, 0.55f);

        public static Color Hex(byte r, byte g, byte b) => new Color(r / 255f, g / 255f, b / 255f, 1f);
    }

    /// <summary>Full-bleed night-to-dusk sky. Warm light stays at the bottom of the element.</summary>
    public sealed class MissionSkyElement : VisualElement
    {
        static readonly Color[] Stops =
        {
            MissionBoardPalette.Night,
            MissionBoardPalette.Deep,
            MissionBoardPalette.Violet,
            MissionBoardPalette.Ember,
            MissionBoardPalette.Amber,
            MissionBoardPalette.Horizon,
        };

        public MissionSkyElement()
        {
            name = "mission-sky";
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext ctx)
        {
            if (ctx == null) return;
            var r = contentRect;
            if (r.width < 2f || r.height < 2f) return;
            int bands = Stops.Length - 1;
            int stars = 42;
            var mesh = ctx.Allocate((bands + 1) * 2 + stars * 4, bands * 6 + stars * 6);
            if (mesh == null) return;
            float w = r.width;
            float h = r.height;
            for (int i = 0; i < Stops.Length; i++)
            {
                float y = h * i / (Stops.Length - 1);
                mesh.SetNextVertex(Vert(0f, y, Stops[i]));
                mesh.SetNextVertex(Vert(w, y, Stops[i]));
            }
            for (int i = 0; i < bands; i++)
            {
                ushort b = (ushort)(i * 2);
                mesh.SetNextIndex(b);
                mesh.SetNextIndex((ushort)(b + 1));
                mesh.SetNextIndex((ushort)(b + 2));
                mesh.SetNextIndex((ushort)(b + 1));
                mesh.SetNextIndex((ushort)(b + 3));
                mesh.SetNextIndex((ushort)(b + 2));
            }
            uint seed = 0x6B48u;
            ushort starBase = (ushort)(Stops.Length * 2);
            for (int i = 0; i < stars; i++)
            {
                seed = seed * 1664525u + 1013904223u;
                float sx = (seed & 1023) / 1023f * w;
                seed = seed * 1664525u + 1013904223u;
                float sy = (seed & 1023) / 1023f * h * 0.72f;
                float dim = 0.45f + ((seed >> 10) & 7) / 14f;
                var tint = new Color(dim, dim, Mathf.Min(1f, dim + 0.08f), 0.9f);
                float z = 1.4f;
                ushort v = (ushort)(starBase + i * 4);
                mesh.SetNextVertex(Vert(sx, sy, tint));
                mesh.SetNextVertex(Vert(sx + z, sy, tint));
                mesh.SetNextVertex(Vert(sx + z, sy + z, tint));
                mesh.SetNextVertex(Vert(sx, sy + z, tint));
                mesh.SetNextIndex(v);
                mesh.SetNextIndex((ushort)(v + 1));
                mesh.SetNextIndex((ushort)(v + 2));
                mesh.SetNextIndex(v);
                mesh.SetNextIndex((ushort)(v + 2));
                mesh.SetNextIndex((ushort)(v + 3));
            }
        }

        static Vertex Vert(float x, float y, Color c) =>
            new Vertex { position = new Vector3(x, y, Vertex.nearZ), uv = Vector2.zero, tint = (Color32)c };
    }

    /// <summary>Dashed constellation, with a solid brass trail through the cleared flights up to the current one.</summary>
    public sealed class MissionTrailElement : VisualElement
    {
        public BoardNode[] Nodes = System.Array.Empty<BoardNode>();
        public int GoldThrough = -1;

        public MissionTrailElement()
        {
            name = "mission-trail";
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public void Refresh(BoardNode[] nodes, int goldThrough)
        {
            Nodes = nodes ?? System.Array.Empty<BoardNode>();
            GoldThrough = goldThrough;
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext ctx)
        {
            if (ctx == null || Nodes == null || Nodes.Length < 2) return;
            var verts = new List<Vertex>(Nodes.Length * 24);
            var index = new List<ushort>(Nodes.Length * 36);
            for (int i = 0; i < Nodes.Length - 1; i++)
            {
                bool gold = i < GoldThrough;
                var a = new Vector2(Nodes[i].X, Nodes[i].Y);
                var b = new Vector2(Nodes[i + 1].X, Nodes[i + 1].Y);
                Stroke(verts, index, a, b, gold ? 3.2f : 1.6f, gold ? MissionBoardPalette.Brass : MissionBoardPalette.Trail, gold);
            }
            if (verts.Count < 4) return;
            var mesh = ctx.Allocate(verts.Count, index.Count);
            if (mesh == null) return;
            mesh.SetAllVertices(verts.ToArray());
            mesh.SetAllIndices(index.ToArray());
        }

        static void Stroke(List<Vertex> verts, List<ushort> index, Vector2 a, Vector2 b, float width, Color color, bool solid)
        {
            float dx = b.x - a.x;
            float dy = b.y - a.y;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 1f) return;
            float ux = dx / len;
            float uy = dy / len;
            float px = -uy * width * 0.5f;
            float py = ux * width * 0.5f;
            float step = solid ? len : 8f;
            float draw = solid ? len : 3.6f;
            var tint = (Color32)color;
            for (float t = 0f; t < len - 0.2f; t += step)
            {
                float t1 = Mathf.Min(len, t + draw);
                var p0 = new Vector2(a.x + ux * t, a.y + uy * t);
                var p1 = new Vector2(a.x + ux * t1, a.y + uy * t1);
                ushort v = (ushort)verts.Count;
                if (v > 60000) return;
                verts.Add(V(p0.x + px, p0.y + py, tint));
                verts.Add(V(p0.x - px, p0.y - py, tint));
                verts.Add(V(p1.x - px, p1.y - py, tint));
                verts.Add(V(p1.x + px, p1.y + py, tint));
                index.Add(v);
                index.Add((ushort)(v + 1));
                index.Add((ushort)(v + 2));
                index.Add(v);
                index.Add((ushort)(v + 2));
                index.Add((ushort)(v + 3));
            }
        }

        static Vertex V(float x, float y, Color32 tint) =>
            new Vertex { position = new Vector3(x, y, Vertex.nearZ), uv = Vector2.zero, tint = tint };
    }

    /// <summary>One original five-point star. Not a glyph and not an emoji.</summary>
    public sealed class MissionStarPip : VisualElement
    {
        public bool On = true;

        public MissionStarPip()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext ctx)
        {
            var p = ctx == null ? null : ctx.painter2D;
            if (p == null) return;
            var r = contentRect;
            float s = Mathf.Min(r.width, r.height);
            if (s < 1f) return;
            var c = new Vector2(r.width * 0.5f, r.height * 0.5f);
            float radius = s * 0.48f;
            p.fillColor = On ? MissionBoardPalette.Brass : new Color(0x8E / 255f, 0x9A / 255f, 0xB8 / 255f, 0.35f);
            p.BeginPath();
            for (int i = 0; i < 10; i++)
            {
                float ang = -Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                float rad = (i % 2 == 0) ? radius : radius * 0.42f;
                var pt = c + new Vector2(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad);
                if (i == 0) p.MoveTo(pt);
                else p.LineTo(pt);
            }
            p.ClosePath();
            p.Fill();
        }
    }
}
