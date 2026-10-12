// CSG-free polygon triangulation for deck lids and the ground opening.
// One outer ring minus simple holes, bridged then ear-clipped. No third-party clipper.
using System;
using System.Collections.Generic;

namespace Kestrel.Pad
{
    public static class PolyCut
    {
        public const float Eps = 1e-6f;

        public static float SignedArea(IList<Vec2> p)
        {
            double a = 0; int n = p.Count;
            for (int i = 0; i < n; i++)
            {
                var c = p[i]; var d = p[(i + 1) % n];
                a += (double)c.X * d.Y - (double)d.X * c.Y;
            }
            return (float)(a * 0.5);
        }

        public static void EnsureWinding(List<Vec2> p, bool ccw)
        {
            if (p.Count < 3) return;
            if ((SignedArea(p) > 0f) != ccw) p.Reverse();
        }

        public static float Cross(Vec2 a, Vec2 b, Vec2 c) =>
            (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

        /// <summary>Even-odd point in polygon. Boundary counts as inside.</summary>
        public static bool Contains(IList<Vec2> p, float x, float y)
        {
            bool inP = false; int n = p.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float yi = p[i].Y, yj = p[j].Y;
                if ((yi > y) == (yj > y)) continue;
                float xCross = p[j].X + (p[i].X - p[j].X) * (y - yj) / (yi - yj);
                if (x < xCross) inP = !inP;
            }
            return inP;
        }

        /// <summary>Outer CCW minus holes. Triangle indices refer to <paramref name="verts"/> (the bridged ring).</summary>
        public static void Triangulate(IList<Vec2> outerIn, IList<IList<Vec2>> holes, List<Vec2> verts, List<int> tris)
        {
            var poly = new List<Vec2>(outerIn.Count + 16);
            for (int i = 0; i < outerIn.Count; i++) poly.Add(outerIn[i]);
            Dedup(poly);
            EnsureWinding(poly, true);
            if (holes != null)
            {
                var ordered = new List<List<Vec2>>();
                foreach (var h in holes)
                {
                    if (h == null || h.Count < 3) continue;
                    var hole = new List<Vec2>(h);
                    Dedup(hole);
                    if (hole.Count >= 3) ordered.Add(hole);
                }
                ordered.Sort((a, b) => MinX(a).CompareTo(MinX(b)));
                foreach (var hole in ordered) Bridge(poly, hole);
            }
            RemoveCollinear(poly);
            EarClip(poly, tris);
            verts.AddRange(poly);
        }

        /// <summary>Square [-half, half]² on XZ at height y, with holes. A tight inner rect is triangulated
        /// so a small trench in a huge ground plane does not feed the clipper a 10⁵:1 aspect ratio.</summary>
        public static PadMeshData Frame(float half, IList<IList<Vec2>> holes, float y)
        {
            var mesh = new PadMeshData();
            if (holes == null || holes.Count == 0)
            {
                Quad(mesh, -half, -half, half, half, y);
                return mesh;
            }
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity, maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            foreach (var h in holes) foreach (var p in h)
            {
                if (p.X < minX) minX = p.X; if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y; if (p.Y > maxY) maxY = p.Y;
            }
            float span = Math.Max(maxX - minX, maxY - minY);
            float pad = Math.Max(span * 0.05f, 1e-4f);
            minX -= pad; maxX += pad; minY -= pad; maxY += pad;
            float lim = half - Math.Max(1e-4f, half * 1e-4f);
            minX = Math.Max(minX, -lim); maxX = Math.Min(maxX, lim);
            minY = Math.Max(minY, -lim); maxY = Math.Min(maxY, lim);
            if (maxX - minX < 1e-5f || maxY - minY < 1e-5f) { Quad(mesh, -half, -half, half, half, y); return mesh; }

            if (minY > -half) Quad(mesh, -half, -half, half, minY, y);
            if (maxY < half) Quad(mesh, -half, maxY, half, half, y);
            if (minX > -half) Quad(mesh, -half, minY, minX, maxY, y);
            if (maxX < half) Quad(mesh, maxX, minY, half, maxY, y);

            var outer = new List<Vec2> { new Vec2(minX, minY), new Vec2(maxX, minY), new Vec2(maxX, maxY), new Vec2(minX, maxY) };
            var verts = new List<Vec2>(); var tris = new List<int>();
            Triangulate(outer, holes, verts, tris);
            Emit(mesh, verts, tris, y);
            return mesh;
        }

        static void Quad(PadMeshData mesh, float x0, float z0, float x1, float z1, float y)
        {
            // Clockwise from above so Unity's cross points +Y: (x0,z0) -> (x0,z1) -> (x1,z1).
            mesh.AddTri(x0, y, z0, x0, y, z1, x1, y, z1);
            mesh.AddTri(x0, y, z0, x1, y, z1, x1, y, z0);
        }

        static void Emit(PadMeshData mesh, List<Vec2> verts, List<int> tris, float y)
        {
            for (int i = 0; i + 2 < tris.Count; i += 3)
            {
                var a = verts[tris[i]]; var b = verts[tris[i + 1]]; var c = verts[tris[i + 2]];
                // ny of winding a-b-c. Positive already faces +Y; CCW in XZ does not, so swap it.
                float ny = (b.Y - a.Y) * (c.X - a.X) - (b.X - a.X) * (c.Y - a.Y);
                if (ny >= 0f) mesh.AddTri(a.X, y, a.Y, b.X, y, b.Y, c.X, y, c.Y);
                else mesh.AddTri(a.X, y, a.Y, c.X, y, c.Y, b.X, y, b.Y);
            }
        }

        static float MinX(List<Vec2> p) { float m = p[0].X; for (int i = 1; i < p.Count; i++) if (p[i].X < m) m = p[i].X; return m; }

        static void Dedup(List<Vec2> p)
        {
            for (int i = p.Count - 1; i >= 0 && p.Count > 3; i--)
            {
                var a = p[i]; var b = p[(i + 1) % p.Count];
                if (Math.Abs(a.X - b.X) < Eps && Math.Abs(a.Y - b.Y) < Eps) p.RemoveAt(i);
            }
        }

        static void RemoveCollinear(List<Vec2> p)
        {
            for (int pass = 0; pass < 2; pass++)
                for (int i = p.Count - 1; i >= 0 && p.Count > 3; i--)
                {
                    int n = p.Count;
                    var a = p[(i + n - 1) % n]; var b = p[i]; var c = p[(i + 1) % n];
                    float scale = Math.Abs(a.X - c.X) + Math.Abs(a.Y - c.Y) + 1f;
                    if (Math.Abs(Cross(a, b, c)) <= Eps * scale) p.RemoveAt(i);
                }
        }

        /// <summary>Insert a hole by bridging its rightmost vertex to the outer ring (earcut's ray cast).</summary>
        static void Bridge(List<Vec2> outer, List<Vec2> hole)
        {
            EnsureWinding(hole, false); // CW hole
            int hm = 0;
            for (int i = 1; i < hole.Count; i++)
                if (hole[i].X > hole[hm].X + Eps || (Math.Abs(hole[i].X - hole[hm].X) <= Eps && hole[i].Y < hole[hm].Y)) hm = i;
            var p = hole[hm];
            int n = outer.Count, edge = -1;
            float bestX = float.PositiveInfinity;
            for (int i = 0; i < n; i++)
            {
                var a = outer[i]; var b = outer[(i + 1) % n];
                if ((a.Y > p.Y) == (b.Y > p.Y)) continue;
                if (Math.Abs(b.Y - a.Y) < 1e-12f) continue;
                float t = (p.Y - a.Y) / (b.Y - a.Y);
                float x = a.X + t * (b.X - a.X);
                if (x >= p.X - 1e-4f && x < bestX) { bestX = x; edge = i; }
            }
            if (edge < 0) throw new InvalidOperationException("trench hole is outside the outer ring");
            int i0 = edge, i1 = (edge + 1) % n;
            int conn = Refine(outer, p, i0, i1, bestX);
            var merged = new List<Vec2>(outer.Count + hole.Count + 2);
            for (int i = 0; i <= conn; i++) merged.Add(outer[i]);
            for (int k = 0; k <= hole.Count; k++) merged.Add(hole[(hm + k) % hole.Count]);
            for (int i = conn; i < outer.Count; i++) merged.Add(outer[i]);
            outer.Clear(); outer.AddRange(merged);
        }

        static int Refine(List<Vec2> outer, Vec2 p, int i0, int i1, float hitX)
        {
            var a = outer[i0]; var b = outer[i1];
            int conn = a.X > b.X ? i0 : i1;
            float tanBest = float.PositiveInfinity;
            var hit = new Vec2(hitX, p.Y);
            for (int i = 0; i < outer.Count; i++)
            {
                if (i == i0 || i == i1) continue;
                var q = outer[i];
                if (q.X < p.X - Eps || q.X > hitX + Eps) continue;
                if (!StrictInTri(q, p, a, b) && !StrictInTri(q, p, hit, a) && !StrictInTri(q, p, hit, b)) continue;
                float dx = p.X - q.X; if (Math.Abs(dx) < 1e-8f) continue;
                float t = Math.Abs(p.Y - q.Y) / dx;
                if (t < tanBest) { tanBest = t; conn = i; }
            }
            return conn;
        }

        static bool StrictInTri(Vec2 p, Vec2 a, Vec2 b, Vec2 c)
        {
            float c1 = Cross(a, b, p), c2 = Cross(b, c, p), c3 = Cross(c, a, p);
            float s = Math.Abs(c1) + Math.Abs(c2) + Math.Abs(c3) + 1f;
            float e = Eps * s;
            bool pos = c1 > e && c2 > e && c3 > e;
            bool neg = c1 < -e && c2 < -e && c3 < -e;
            return pos || neg;
        }

        static void EarClip(List<Vec2> poly, List<int> tris)
        {
            int n0 = poly.Count;
            if (n0 < 3) return;
            if (n0 == 3) { tris.Add(0); tris.Add(1); tris.Add(2); return; }
            var idx = new List<int>(n0);
            for (int i = 0; i < n0; i++) idx.Add(i);
            int guard = n0 * n0;
            while (idx.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < idx.Count; i++)
                {
                    if (!IsEar(poly, idx, i)) continue;
                    int n = idx.Count;
                    int i0 = idx[(i + n - 1) % n], i1 = idx[i], i2 = idx[(i + 1) % n];
                    tris.Add(i0); tris.Add(i1); tris.Add(i2);
                    idx.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped) throw new InvalidOperationException($"ear clip stalled at {idx.Count} verts");
            }
            if (idx.Count == 3) { tris.Add(idx[0]); tris.Add(idx[1]); tris.Add(idx[2]); }
            else if (idx.Count > 3) throw new InvalidOperationException("ear clip did not finish");
        }

        static bool IsEar(List<Vec2> poly, List<int> idx, int i)
        {
            int n = idx.Count;
            int i0 = idx[(i + n - 1) % n], i1 = idx[i], i2 = idx[(i + 1) % n];
            var a = poly[i0]; var b = poly[i1]; var c = poly[i2];
            float cr = Cross(a, b, c);
            float scale = Math.Abs(a.X - c.X) + Math.Abs(a.Y - c.Y) + 1f;
            if (cr <= Eps * scale) return false; // reflex or flat: outer ring is CCW, ears are left turns
            for (int j = 0; j < n; j++)
            {
                int k = idx[j];
                if (k == i0 || k == i1 || k == i2) continue;
                if (StrictInTri(poly[k], a, b, c)) return false;
            }
            return true;
        }
    }
}
