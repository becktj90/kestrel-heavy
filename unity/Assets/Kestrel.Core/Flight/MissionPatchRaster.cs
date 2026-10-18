// Procedural embroidered disc. Circles are coverage-shaded. Symbols are filled
// shapes with a dark thread edge. Rim letters use the 5×7 pixel font, upright
// along the top arc (tops point outward) and upright for the flight number.
using System;

namespace Kestrel.Core.Flight
{
    static class MissionPatchRaster
    {
        struct Rgb
        {
            public byte R, G, B;
            public Rgb(byte r, byte g, byte b) { R = r; G = g; B = b; }
        }

        sealed class Ink
        {
            public readonly byte[] Pix;
            public readonly int N;
            public readonly float Cx, Cy, Clip;
            public Ink(int n, float cx, float cy, float clip)
            {
                N = n;
                Pix = new byte[n * n * 4];
                Cx = cx;
                Cy = cy;
                Clip = clip;
            }
        }

        public static byte[] Draw(MissionPatchSpec spec, int size, PatchChrome chrome)
        {
            var plan = PatchPlan.Build(spec, size);
            var pal = MissionPatchArt.Palette(spec.Act);
            var ink = new Ink(plan.Size, plan.Cx, plan.Cy, plan.Outer);
            var rim = C(pal.RR, pal.RG, pal.RB);
            var field = C(pal.FR, pal.FG, pal.FB);
            var thread = C(pal.TR, pal.TG, pal.TB);
            var accent = C(pal.AR, pal.AG, pal.AB);
            var edge = C(pal.IR, pal.IG, pal.IB);
            var sea = new Rgb(32, 118, 138);

            Disc(ink, plan.Cx, plan.Cy, plan.Outer, rim);
            Stitches(ink, plan, spec.EmblemSeed, thread, edge);
            Ring(ink, plan.Cx, plan.Cy, plan.Outer - 1.6f, 1.35f, thread);
            Ring(ink, plan.Cx, plan.Cy, plan.Field + 0.4f, 1.25f, thread);
            Disc(ink, plan.Cx, plan.Cy, plan.Field - 0.4f, field);
            Emblem(ink, spec, plan, thread, accent, edge, field, sea);
            for (int i = 0; i < plan.StarCount; i++)
                Star(ink, plan.StarX[i], plan.StarY[i], plan.StarR, accent, edge);
            if (plan.NameOnArc)
            {
                ArcBand(ink, plan, rim);
                ArcText(ink, spec.ArcName, plan, thread, edge);
            }
            if (plan.FlightFits)
            {
                FlightBand(ink, spec.Flight, plan, rim);
                Upright(ink, spec.Flight, plan.Cx, plan.FlightY, plan.FlightScale, thread, edge);
            }
            if (chrome == PatchChrome.Medal) Medal(ink, plan, thread, edge);
            if (chrome == PatchChrome.Locked) GreyOutline(ink, plan);
            return ink.Pix;
        }

        static void Stitches(Ink ink, PatchDrawPlan plan, ulong seed, Rgb light, Rgb dark)
        {
            float midR = plan.Field + plan.Rim * 0.5f;
            float width = Math.Max(1.5f, plan.Rim * 0.085f);
            int steps = (int)(2.0 * Math.PI * midR / (width * 2.35f));
            if (steps < 36) steps = 36;
            int phase = (int)(seed % 5UL);
            for (int i = 0; i < steps; i++)
            {
                float ang = (i * 360f / steps) * (float)(Math.PI / 180.0);
                float wob = 0.82f + 0.18f * (((seed >> (i % 12)) & 3UL) / 3f);
                float a0 = plan.Field + 1.2f;
                float a1 = plan.Outer - 1.8f;
                if ((i + phase) % 4 == 0) { a0 += plan.Rim * 0.18f; a1 -= plan.Rim * 0.08f; }
                float x0 = plan.Cx + (float)Math.Sin(ang) * a0;
                float y0 = plan.Cy - (float)Math.Cos(ang) * a0;
                float x1 = plan.Cx + (float)Math.Sin(ang) * (a0 + (a1 - a0) * wob);
                float y1 = plan.Cy - (float)Math.Cos(ang) * (a0 + (a1 - a0) * wob);
                Stroke(ink, x0, y0, x1, y1, width, (i + phase) % 3 == 0 ? dark : light);
            }
        }

        static void Emblem(Ink ink, MissionPatchSpec spec, PatchDrawPlan plan, Rgb thread, Rgb accent, Rgb edge, Rgb field, Rgb sea)
        {
            var m = spec.Marks;
            float cx = plan.EmblemCx, cy = plan.EmblemCy, r = plan.EmblemR;
            if (Has(m, PatchMarks.Orbit)) Orbit(ink, cx, cy, r * 1.28f, accent, edge);
            if (Has(m, PatchMarks.Moon))
            {
                float mx = plan.StarCount > 0 ? cx - r * 0.22f : cx;
                Moon(ink, mx, cy + r * 0.02f, r * 0.62f, thread, field, accent);
            }
            if (Has(m, PatchMarks.Horizon)) Horizon(ink, cx, cy + r * 0.72f, r, thread, edge);
            if (Has(m, PatchMarks.ArcShort)) Trajectory(ink, cx, cy + r * 0.15f, r * 0.9f, 0.35f, false, accent);
            if (Has(m, PatchMarks.ArcTall)) Trajectory(ink, cx, cy, r * 1.15f, 0.95f, false, accent);
            if (Has(m, PatchMarks.ArcReturn)) Trajectory(ink, cx, cy, r * 1.1f, 0.8f, true, accent);
            if (Has(m, PatchMarks.ArcTransfer)) Trajectory(ink, cx, cy + r * 0.05f, r * 1.2f, 0.55f, false, accent);
            if (Has(m, PatchMarks.Coast)) CoastDots(ink, cx, cy - r * 0.15f, r, accent);
            if (Has(m, PatchMarks.Cloud)) Cloud(ink, cx - r * 0.35f, cy - r * 0.25f, r * 0.42f, thread, edge);
            if (Has(m, PatchMarks.Sun)) Sun(ink, cx + r * 0.42f, cy - r * 0.38f, r * 0.28f, accent, edge);
            if (Has(m, PatchMarks.Barge)) Barge(ink, cx, cy + r * 0.78f, r * 1.15f, thread, sea, edge);
            if (Has(m, PatchMarks.Rocket)) Rocket(ink, cx, cy - r * 0.02f, r * 1.15f, thread, accent, edge, Has(m, PatchMarks.Strap));
            if (Has(m, PatchMarks.Box)) Box(ink, cx, cy + r * 0.05f, r * 0.7f, thread, accent, edge);
            if (Has(m, PatchMarks.Lantern)) Lantern(ink, cx, cy, r * 0.85f, thread, accent, edge);
            if (Has(m, PatchMarks.Capsule)) Capsule(ink, cx, cy, r * 0.95f, thread, accent, edge, Has(m, PatchMarks.Window));
            if (Has(m, PatchMarks.Scope)) Scope(ink, cx, cy, r, thread, accent, edge);
            if (Has(m, PatchMarks.Rack)) Rack(ink, cx, cy, r * 0.85f, thread, accent, edge);
            if (Has(m, PatchMarks.Dish)) Dish(ink, cx, cy - r * 0.05f, r * 0.9f, thread, accent, edge);
            if (Has(m, PatchMarks.Legs)) Lander(ink, cx, cy + r * 0.12f, r * 0.9f, thread, accent, edge);
            if (Has(m, PatchMarks.Probe) && !Has(m, PatchMarks.Rocket) && !Has(m, PatchMarks.Legs))
                Probe(ink, cx - r * 0.15f, cy + r * 0.05f, r * 0.55f, thread, accent, edge);
        }

        static void Orbit(Ink ink, float cx, float cy, float r, Rgb color, Rgb edge)
        {
            float rx = r, ry = r * 0.38f;
            float rot = -28f * (float)(Math.PI / 180.0);
            StrokeEllipse(ink, cx, cy, rx, ry, rot, Math.Max(2f, r * 0.06f), edge);
            StrokeEllipse(ink, cx, cy, rx * 0.92f, ry * 0.92f, rot, Math.Max(1.4f, r * 0.045f), color);
            float t = 0.7f;
            float ex = rx * (float)Math.Cos(t), ey = ry * (float)Math.Sin(t);
            float x = cx + ex * (float)Math.Cos(rot) - ey * (float)Math.Sin(rot);
            float y = cy + ex * (float)Math.Sin(rot) + ey * (float)Math.Cos(rot);
            Disc(ink, x, y, Math.Max(2.2f, r * 0.07f), color);
        }

        static void Moon(Ink ink, float cx, float cy, float r, Rgb light, Rgb cut, Rgb crater)
        {
            Disc(ink, cx, cy, r, light);
            Disc(ink, cx + r * 0.42f, cy - r * 0.08f, r * 0.78f, cut);
            Disc(ink, cx - r * 0.28f, cy + r * 0.1f, r * 0.12f, crater);
            Disc(ink, cx - r * 0.05f, cy + r * 0.32f, r * 0.08f, crater);
        }

        static void Horizon(Ink ink, float cx, float y, float r, Rgb color, Rgb edge)
        {
            Stroke(ink, cx - r, y, cx + r, y, Math.Max(2f, r * 0.08f), edge);
            Stroke(ink, cx - r, y, cx + r, y, Math.Max(1.2f, r * 0.045f), color);
            BoxFill(ink, cx - r * 0.08f, y - r * 0.22f, r * 0.16f, r * 0.22f, color, edge);
        }

        static void Trajectory(Ink ink, float cx, float cy, float r, float height, bool hook, Rgb color)
        {
            int n = 16;
            float x0 = cx - r * 0.7f, y0 = cy + r * 0.45f;
            float x1 = cx + r * (hook ? 0.15f : 0.65f);
            float y1 = cy - r * height;
            float x2 = hook ? cx - r * 0.15f : x1;
            float y2 = hook ? cy + r * 0.35f : y1;
            float px = x0, py = y0;
            float w = Math.Max(1.8f, r * 0.055f);
            for (int i = 1; i <= n; i++)
            {
                float t = i / (float)n;
                float u = 1f - t;
                float x = u * u * x0 + 2 * u * t * x1 + t * t * x2;
                float y = u * u * y0 + 2 * u * t * y1 + t * t * y2;
                Stroke(ink, px, py, x, y, w, color);
                px = x; py = y;
            }
            Disc(ink, px, py, w * 1.3f, color);
        }

        static void CoastDots(Ink ink, float cx, float cy, float r, Rgb color)
        {
            for (int i = 0; i < 3; i++)
                Disc(ink, cx - r * 0.35f + i * r * 0.28f, cy - r * 0.55f + i * r * 0.08f, Math.Max(1.6f, r * 0.06f), color);
        }

        static void Cloud(Ink ink, float cx, float cy, float r, Rgb color, Rgb edge)
        {
            Disc(ink, cx, cy, r * 0.55f, edge);
            Disc(ink, cx - r * 0.4f, cy + r * 0.1f, r * 0.42f, edge);
            Disc(ink, cx + r * 0.42f, cy + r * 0.12f, r * 0.4f, edge);
            Disc(ink, cx, cy, r * 0.42f, color);
            Disc(ink, cx - r * 0.38f, cy + r * 0.08f, r * 0.3f, color);
            Disc(ink, cx + r * 0.4f, cy + r * 0.1f, r * 0.28f, color);
        }

        static void Sun(Ink ink, float cx, float cy, float r, Rgb color, Rgb edge)
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i * (float)(Math.PI / 4.0);
                Stroke(ink, cx + (float)Math.Cos(a) * r * 1.15f, cy + (float)Math.Sin(a) * r * 1.15f,
                    cx + (float)Math.Cos(a) * r * 1.7f, cy + (float)Math.Sin(a) * r * 1.7f, Math.Max(1.4f, r * 0.18f), color);
            }
            Disc(ink, cx, cy, r * 1.08f, edge);
            Disc(ink, cx, cy, r * 0.82f, color);
        }

        static void Barge(Ink ink, float cx, float cy, float r, Rgb deck, Rgb sea, Rgb edge)
        {
            var hull = new[]
            {
                P(cx - r * 0.95f, cy), P(cx - r * 0.7f, cy + r * 0.28f), P(cx + r * 0.7f, cy + r * 0.28f), P(cx + r * 0.95f, cy),
            };
            Poly(ink, Scale(hull, 1.08f), edge);
            Poly(ink, hull, deck);
            Stroke(ink, cx - r * 0.8f, cy + r * 0.4f, cx + r * 0.55f, cy + r * 0.48f, Math.Max(1.5f, r * 0.06f), sea);
            Stroke(ink, cx - r * 0.55f, cy + r * 0.55f, cx + r * 0.8f, cy + r * 0.62f, Math.Max(1.5f, r * 0.06f), sea);
            BoxFill(ink, cx - r * 0.08f, cy - r * 0.28f, r * 0.1f, r * 0.28f, deck, edge);
        }

        static void Rocket(Ink ink, float cx, float cy, float h, Rgb body, Rgb band, Rgb edge, bool strap)
        {
            if (strap)
            {
                Rocket(ink, cx - h * 0.38f, cy + h * 0.12f, h * 0.62f, body, band, edge, false);
                Rocket(ink, cx + h * 0.38f, cy + h * 0.12f, h * 0.62f, body, band, edge, false);
            }
            float w = h * 0.22f;
            var pts = new[]
            {
                P(cx, cy - h * 0.62f),
                P(cx + w, cy - h * 0.28f),
                P(cx + w, cy + h * 0.28f),
                P(cx + w * 1.45f, cy + h * 0.42f),
                P(cx + w * 0.35f, cy + h * 0.22f),
                P(cx + w * 0.35f, cy + h * 0.48f),
                P(cx - w * 0.35f, cy + h * 0.48f),
                P(cx - w * 0.35f, cy + h * 0.22f),
                P(cx - w * 1.45f, cy + h * 0.42f),
                P(cx - w, cy + h * 0.28f),
                P(cx - w, cy - h * 0.28f),
            };
            Poly(ink, Scale(pts, 1.06f), edge);
            Poly(ink, pts, body);
            BoxFill(ink, cx - w, cy - h * 0.02f, w * 2f, h * 0.12f, band, band);
            var flame = new[] { P(cx - w * 0.28f, cy + h * 0.48f), P(cx + w * 0.28f, cy + h * 0.48f), P(cx, cy + h * 0.72f) };
            Poly(ink, flame, band);
        }

        static void Box(Ink ink, float cx, float cy, float r, Rgb body, Rgb band, Rgb edge)
        {
            BoxFill(ink, cx - r * 0.55f, cy - r * 0.4f, r * 1.1f, r * 0.9f, body, edge);
            BoxFill(ink, cx - r * 0.55f, cy - r * 0.02f, r * 1.1f, r * 0.16f, band, band);
            Disc(ink, cx, cy - r * 0.62f, r * 0.18f, band);
        }

        static void Lantern(Ink ink, float cx, float cy, float r, Rgb body, Rgb glow, Rgb edge)
        {
            var cap = new[] { P(cx, cy - r * 0.7f), P(cx + r * 0.38f, cy - r * 0.28f), P(cx - r * 0.38f, cy - r * 0.28f) };
            Poly(ink, Scale(cap, 1.08f), edge);
            Poly(ink, cap, body);
            BoxFill(ink, cx - r * 0.32f, cy - r * 0.28f, r * 0.64f, r * 0.7f, body, edge);
            Disc(ink, cx, cy, r * 0.16f, glow);
            BoxFill(ink, cx - r * 0.18f, cy + r * 0.42f, r * 0.36f, r * 0.12f, body, edge);
        }

        static void Capsule(Ink ink, float cx, float cy, float r, Rgb body, Rgb band, Rgb edge, bool window)
        {
            Disc(ink, cx, cy - r * 0.25f, r * 0.42f, edge);
            Disc(ink, cx, cy - r * 0.25f, r * 0.32f, body);
            BoxFill(ink, cx - r * 0.32f, cy - r * 0.2f, r * 0.64f, r * 0.7f, body, edge);
            BoxFill(ink, cx - r * 0.32f, cy + r * 0.15f, r * 0.64f, r * 0.12f, band, band);
            if (window) Disc(ink, cx, cy - r * 0.22f, r * 0.14f, band);
        }

        static void Scope(Ink ink, float cx, float cy, float r, Rgb body, Rgb glass, Rgb edge)
        {
            float ang = -32f * (float)(Math.PI / 180.0);
            float dx = (float)Math.Cos(ang), dy = (float)Math.Sin(ang);
            float x0 = cx - dx * r * 0.55f, y0 = cy - dy * r * 0.55f;
            float x1 = cx + dx * r * 0.35f, y1 = cy + dy * r * 0.35f;
            Stroke(ink, x0, y0, x1, y1, r * 0.28f, edge);
            Stroke(ink, x0, y0, x1, y1, r * 0.18f, body);
            Disc(ink, x1, y1, r * 0.28f, edge);
            Disc(ink, x1, y1, r * 0.18f, glass);
        }

        static void Rack(Ink ink, float cx, float cy, float r, Rgb body, Rgb cell, Rgb edge)
        {
            BoxFill(ink, cx - r * 0.7f, cy - r * 0.45f, r * 1.4f, r * 0.95f, body, edge);
            for (int row = 0; row < 2; row++)
            for (int col = 0; col < 3; col++)
            {
                float x = cx - r * 0.48f + col * r * 0.38f;
                float y = cy - r * 0.28f + row * r * 0.4f;
                BoxFill(ink, x, y, r * 0.26f, r * 0.26f, cell, edge);
            }
        }

        static void Dish(Ink ink, float cx, float cy, float r, Rgb body, Rgb accent, Rgb edge)
        {
            StrokeEllipse(ink, cx, cy + r * 0.05f, r * 0.7f, r * 0.7f, 0f, Math.Max(2f, r * 0.08f), edge);
            int n = 14;
            float prevX = 0, prevY = 0;
            for (int i = 0; i <= n; i++)
            {
                float a = (200f + i * (140f / n)) * (float)(Math.PI / 180.0);
                float x = cx + (float)Math.Cos(a) * r * 0.62f;
                float y = cy + (float)Math.Sin(a) * r * 0.5f;
                if (i > 0) Stroke(ink, prevX, prevY, x, y, Math.Max(1.8f, r * 0.07f), body);
                prevX = x; prevY = y;
            }
            Stroke(ink, cx, cy, cx, cy - r * 0.15f, Math.Max(1.4f, r * 0.05f), accent);
            Disc(ink, cx, cy - r * 0.15f, r * 0.08f, accent);
        }

        static void Lander(Ink ink, float cx, float cy, float r, Rgb body, Rgb accent, Rgb edge)
        {
            var bodyPts = new[] { P(cx, cy - r * 0.55f), P(cx + r * 0.38f, cy + r * 0.05f), P(cx - r * 0.38f, cy + r * 0.05f) };
            Poly(ink, Scale(bodyPts, 1.1f), edge);
            Poly(ink, bodyPts, body);
            Stroke(ink, cx - r * 0.2f, cy + r * 0.02f, cx - r * 0.55f, cy + r * 0.45f, Math.Max(1.8f, r * 0.06f), accent);
            Stroke(ink, cx + r * 0.2f, cy + r * 0.02f, cx + r * 0.55f, cy + r * 0.45f, Math.Max(1.8f, r * 0.06f), accent);
            Disc(ink, cx - r * 0.55f, cy + r * 0.45f, r * 0.08f, accent);
            Disc(ink, cx + r * 0.55f, cy + r * 0.45f, r * 0.08f, accent);
        }

        static void Probe(Ink ink, float cx, float cy, float r, Rgb body, Rgb accent, Rgb edge)
        {
            var pts = new[] { P(cx, cy - r), P(cx + r * 0.7f, cy), P(cx, cy + r), P(cx - r * 0.7f, cy) };
            Poly(ink, Scale(pts, 1.1f), edge);
            Poly(ink, pts, body);
            Disc(ink, cx, cy, r * 0.22f, accent);
        }

        static void Star(Ink ink, float cx, float cy, float r, Rgb fill, Rgb edge)
        {
            var pts = StarPts(cx, cy, r);
            Poly(ink, Scale(pts, 1.18f), edge);
            Poly(ink, pts, fill);
        }

        static Pt[] StarPts(float cx, float cy, float r)
        {
            var pts = new Pt[10];
            for (int i = 0; i < 10; i++)
            {
                float a = (-90f + i * 36f) * (float)(Math.PI / 180.0);
                float rad = (i % 2 == 0) ? r : r * 0.42f;
                pts[i] = P(cx + (float)Math.Cos(a) * rad, cy + (float)Math.Sin(a) * rad);
            }
            return pts;
        }

        static void ArcBand(Ink ink, PatchDrawPlan plan, Rgb rim)
        {
            float pad = 0.10f + 6f / Math.Max(8f, plan.TextR);
            float span = plan.NameSpan * (float)(Math.PI / 180.0) * 0.5f + pad;
            float inner = plan.Field + 0.5f;
            float outer = plan.Outer - 0.8f;
            int x0 = (int)(plan.Cx - outer - 1), y0 = (int)(plan.Cy - outer - 1);
            int x1 = (int)(plan.Cx + outer + 1), y1 = (int)(plan.Cy + outer + 1);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - plan.Cx;
                float dy = plan.Cy - (y + 0.5f);
                float r = (float)Math.Sqrt(dx * dx + dy * dy);
                if (r < inner || r > outer) continue;
                float ang = (float)Math.Atan2(dx, dy);
                if (ang < -span || ang > span) continue;
                Plot(ink, x, y, rim, 1f);
            }
        }

        static void FlightBand(Ink ink, string text, PatchDrawPlan plan, Rgb rim)
        {
            int s = plan.FlightScale;
            float width = (text.Length - 1) * 6f * s + 5f * s;
            float x0 = plan.Cx - width * 0.5f - s;
            float x1 = plan.Cx + width * 0.5f + s;
            float y0 = plan.FlightY - 4f * s;
            float y1 = plan.FlightY + 4f * s;
            for (int y = (int)Math.Floor(y0); y <= (int)Math.Ceiling(y1); y++)
            for (int x = (int)Math.Floor(x0); x <= (int)Math.Ceiling(x1); x++)
                Plot(ink, x, y, rim, 1f);
        }

        static void ArcText(Ink ink, string text, PatchDrawPlan plan, Rgb fill, Rgb edge)
        {
            int n = text.Length;
            if (n == 0) return;
            float span = plan.NameSpan * (float)(Math.PI / 180.0);
            float start = -span * 0.5f;
            for (int i = 0; i < n; i++)
            {
                float a = start + (i + 0.5f) / n * span;
                float x = plan.Cx + (float)Math.Sin(a) * plan.TextR;
                float y = plan.Cy - (float)Math.Cos(a) * plan.TextR;
                Glyph(ink, text[i], x, y, plan.NameScale, a, fill, edge);
            }
        }

        static void Upright(Ink ink, string text, float cx, float y, int scale, Rgb fill, Rgb edge)
        {
            float width = (text.Length - 1) * 6f * scale + 5f * scale;
            float x = cx - width * 0.5f + 2.5f * scale;
            for (int i = 0; i < text.Length; i++)
            {
                Glyph(ink, text[i], x, y, scale, 0f, fill, edge);
                x += 6f * scale;
            }
        }

        static void Glyph(Ink ink, char c, float cx, float cy, int scale, float ang, Rgb fill, Rgb edge)
        {
            float cos = (float)Math.Cos(ang), sin = (float)Math.Sin(ang);
            for (int gy = 0; gy < 7; gy++)
            for (int gx = 0; gx < 5; gx++)
            {
                if (!MissionPatchFont.Row(c, gy, gx)) continue;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (MissionPatchFont.Row(c, gy + dy, gx + dx)) continue;
                    Blot(ink, cx, cy, gx + dx * 0.65f, gy + dy * 0.65f, scale, cos, sin, edge);
                }
                Blot(ink, cx, cy, gx, gy, scale, cos, sin, fill);
            }
        }

        static void Blot(Ink ink, float cx, float cy, float gx, float gy, int scale, float cos, float sin, Rgb color)
        {
            float lx = (gx - 2f) * scale;
            float ly = (gy - 3f) * scale;
            float ox = lx * cos + ly * (-sin);
            float oy = lx * sin + ly * cos;
            Stamp(ink, cx + ox, cy + oy, scale, color);
        }

        static void Medal(Ink ink, PatchDrawPlan plan, Rgb thread, Rgb edge)
        {
            var gold = new Rgb(232, 186, 74);
            var deep = new Rgb(168, 114, 32);
            Ring(ink, plan.Cx, plan.Cy, plan.Outer - 0.3f, Math.Max(2.4f, plan.Rim * 0.28f), gold);
            float ang = 128f * (float)(Math.PI / 180.0);
            float rad = plan.Outer * 0.78f;
            float x = plan.Cx + (float)Math.Sin(ang) * rad;
            float y = plan.Cy - (float)Math.Cos(ang) * rad;
            float mr = Math.Max(5f, plan.Outer * 0.145f);
            Disc(ink, x, y, mr, deep);
            Disc(ink, x, y, mr * 0.78f, gold);
            Star(ink, x, y - mr * 0.02f, mr * 0.42f, thread, edge);
        }

        static void GreyOutline(Ink ink, PatchDrawPlan plan)
        {
            int n = ink.N;
            for (int i = 0; i < ink.Pix.Length; i += 4)
            {
                if (ink.Pix[i + 3] == 0) continue;
                float g = ink.Pix[i] * 0.30f + ink.Pix[i + 1] * 0.56f + ink.Pix[i + 2] * 0.14f;
                byte v = (byte)Math.Min(255, g * 0.62f);
                ink.Pix[i] = v;
                ink.Pix[i + 1] = v;
                ink.Pix[i + 2] = v;
            }
            Ring(ink, plan.Cx, plan.Cy, plan.Outer - 1.1f, 2.1f, new Rgb(214, 218, 224));
        }

        static void Stamp(Ink ink, float cx, float cy, int scale, Rgb color)
        {
            int x0 = (int)Math.Floor(cx - scale * 0.5f);
            int y0 = (int)Math.Floor(cy - scale * 0.5f);
            for (int y = y0; y < y0 + scale; y++)
            for (int x = x0; x < x0 + scale; x++)
                Plot(ink, x, y, color, 1f);
        }

        static void Disc(Ink ink, float cx, float cy, float r, Rgb color)
        {
            int x0 = (int)Math.Floor(cx - r - 1), y0 = (int)Math.Floor(cy - r - 1);
            int x1 = (int)Math.Ceiling(cx + r + 1), y1 = (int)Math.Ceiling(cy + r + 1);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float d = Dist(x + 0.5f, y + 0.5f, cx, cy);
                float a = r - d + 0.5f;
                if (a <= 0f) continue;
                if (a > 1f) a = 1f;
                Plot(ink, x, y, color, a);
            }
        }

        static void Ring(Ink ink, float cx, float cy, float r, float width, Rgb color)
        {
            float outer = r + width * 0.5f, inner = r - width * 0.5f;
            int x0 = (int)Math.Floor(cx - outer - 1), y0 = (int)Math.Floor(cy - outer - 1);
            int x1 = (int)Math.Ceiling(cx + outer + 1), y1 = (int)Math.Ceiling(cy + outer + 1);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float d = Dist(x + 0.5f, y + 0.5f, cx, cy);
                float a = Math.Min(outer - d + 0.5f, d - inner + 0.5f);
                if (a <= 0f) continue;
                if (a > 1f) a = 1f;
                Plot(ink, x, y, color, a);
            }
        }

        static void Stroke(Ink ink, float x0, float y0, float x1, float y1, float width, Rgb color)
        {
            float r = width * 0.5f;
            int minX = (int)Math.Floor(Math.Min(x0, x1) - r - 1);
            int minY = (int)Math.Floor(Math.Min(y0, y1) - r - 1);
            int maxX = (int)Math.Ceiling(Math.Max(x0, x1) + r + 1);
            int maxY = (int)Math.Ceiling(Math.Max(y0, y1) + r + 1);
            float dx = x1 - x0, dy = y1 - y0;
            float len2 = dx * dx + dy * dy;
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float t = len2 < 1e-4f ? 0f : ((px - x0) * dx + (py - y0) * dy) / len2;
                if (t < 0f) t = 0f;
                else if (t > 1f) t = 1f;
                float d = Dist(px, py, x0 + dx * t, y0 + dy * t);
                float a = r - d + 0.5f;
                if (a <= 0f) continue;
                if (a > 1f) a = 1f;
                Plot(ink, x, y, color, a);
            }
        }

        static void StrokeEllipse(Ink ink, float cx, float cy, float rx, float ry, float rot, float width, Rgb color)
        {
            int n = 56;
            float prevX = 0, prevY = 0;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n * (float)(Math.PI * 2.0);
                float ex = rx * (float)Math.Cos(t), ey = ry * (float)Math.Sin(t);
                float x = cx + ex * (float)Math.Cos(rot) - ey * (float)Math.Sin(rot);
                float y = cy + ex * (float)Math.Sin(rot) + ey * (float)Math.Cos(rot);
                if (i > 0) Stroke(ink, prevX, prevY, x, y, width, color);
                prevX = x; prevY = y;
            }
        }

        static void BoxFill(Ink ink, float x, float y, float w, float h, Rgb fill, Rgb edge)
        {
            var pts = new[] { P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h) };
            if (fill.R != edge.R || fill.G != edge.G || fill.B != edge.B)
                Poly(ink, Scale(pts, 1.08f), edge);
            Poly(ink, pts, fill);
        }

        static void Poly(Ink ink, Pt[] pts, Rgb color)
        {
            float minX = pts[0].X, minY = pts[0].Y, maxX = minX, maxY = minY;
            for (int i = 1; i < pts.Length; i++)
            {
                if (pts[i].X < minX) minX = pts[i].X;
                if (pts[i].Y < minY) minY = pts[i].Y;
                if (pts[i].X > maxX) maxX = pts[i].X;
                if (pts[i].Y > maxY) maxY = pts[i].Y;
            }
            int x0 = (int)Math.Floor(minX - 1), y0 = (int)Math.Floor(minY - 1);
            int x1 = (int)Math.Ceiling(maxX + 1), y1 = (int)Math.Ceiling(maxY + 1);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int hit = 0;
                for (int sy = 0; sy < 2; sy++)
                for (int sx = 0; sx < 2; sx++)
                    if (Inside(pts, x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f)) hit++;
                if (hit == 0) continue;
                Plot(ink, x, y, color, hit / 4f);
            }
        }

        static bool Inside(Pt[] pts, float x, float y)
        {
            bool c = false;
            for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
            {
                float yi = pts[i].Y, yj = pts[j].Y;
                if ((yi > y) == (yj > y)) continue;
                float xi = pts[i].X, xj = pts[j].X;
                if (x < (xj - xi) * (y - yi) / (yj - yi + 1e-6f) + xi) c = !c;
            }
            return c;
        }

        static void Plot(Ink ink, int x, int y, Rgb color, float a)
        {
            if (a <= 0f || x < 0 || y < 0 || x >= ink.N || y >= ink.N) return;
            float d = Dist(x + 0.5f, y + 0.5f, ink.Cx, ink.Cy);
            float clip = ink.Clip - d + 0.5f;
            if (clip <= 0f) return;
            if (clip < a) a = clip;
            if (a > 1f) a = 1f;
            int o = (y * ink.N + x) * 4;
            float ia = 1f - a;
            if (ink.Pix[o + 3] == 0)
            {
                ink.Pix[o] = color.R;
                ink.Pix[o + 1] = color.G;
                ink.Pix[o + 2] = color.B;
                ink.Pix[o + 3] = (byte)(a * 255f);
                return;
            }
            ink.Pix[o] = (byte)(color.R * a + ink.Pix[o] * ia);
            ink.Pix[o + 1] = (byte)(color.G * a + ink.Pix[o + 1] * ia);
            ink.Pix[o + 2] = (byte)(color.B * a + ink.Pix[o + 2] * ia);
            ink.Pix[o + 3] = (byte)Math.Min(255, ink.Pix[o + 3] + a * (255 - ink.Pix[o + 3]));
        }

        static float Dist(float x0, float y0, float x1, float y1)
        {
            float dx = x0 - x1, dy = y0 - y1;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        static bool Has(PatchMarks m, PatchMarks bit) => (m & bit) != 0;
        static Rgb C(byte r, byte g, byte b) => new Rgb(r, g, b);
        static Pt P(float x, float y) => new Pt(x, y);

        static Pt[] Scale(Pt[] pts, float k)
        {
            float sx = 0, sy = 0;
            for (int i = 0; i < pts.Length; i++) { sx += pts[i].X; sy += pts[i].Y; }
            sx /= pts.Length; sy /= pts.Length;
            var o = new Pt[pts.Length];
            for (int i = 0; i < pts.Length; i++)
                o[i] = new Pt(sx + (pts[i].X - sx) * k, sy + (pts[i].Y - sy) * k);
            return o;
        }

        struct Pt
        {
            public float X, Y;
            public Pt(float x, float y) { X = x; Y = y; }
        }
    }
}
