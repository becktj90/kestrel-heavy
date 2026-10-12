using System;

namespace Kestrel.Site
{
    public sealed class SiteMesh
    {
        public float[] Positions = Array.Empty<float>();
        public float[] Normals = Array.Empty<float>();
        public float[] Colors = Array.Empty<float>();
        public int[] Indices = Array.Empty<int>();
        public int VertexCount => Positions.Length / 3;
        public int TriangleCount => Indices.Length / 3;

        public bool ContainsXZ(float x, float z) => TrySurfaceY(x, z, out _);

        /// <summary>Topmost triangle covering this XZ. Vertical cards and the underside are ignored.</summary>
        public bool TrySurfaceY(float x, float z, out float y)
        {
            y = float.NegativeInfinity;
            bool hit = false;
            var p = Positions; var ix = Indices;
            for (int i = 0; i + 2 < ix.Length; i += 3)
            {
                int a = ix[i] * 3, b = ix[i + 1] * 3, c = ix[i + 2] * 3;
                if (!Bary(x, z, p[a], p[a + 2], p[b], p[b + 2], p[c], p[c + 2], out float u, out float v, out float w)) continue;
                float hy = u * p[a + 1] + v * p[b + 1] + w * p[c + 1];
                if (hy > y) { y = hy; hit = true; }
            }
            return hit;
        }

        /// <summary>Drop triangles that overlap any hole. Holes are packed x,z pairs in the same space as the mesh.</summary>
        public static int[] WithoutOpenings(float[] positions, int[] indices, System.Collections.Generic.IList<float[]> holes)
        {
            if (indices == null || indices.Length == 0) return indices ?? Array.Empty<int>();
            if (holes == null || holes.Count == 0) return indices;
            var keep = new System.Collections.Generic.List<int>(indices.Length);
            for (int i = 0; i + 2 < indices.Length; i += 3)
            {
                int ia = indices[i], ib = indices[i + 1], ic = indices[i + 2];
                float ax = positions[ia * 3], az = positions[ia * 3 + 2];
                float bx = positions[ib * 3], bz = positions[ib * 3 + 2];
                float cx = positions[ic * 3], cz = positions[ic * 3 + 2];
                bool hit = false;
                for (int h = 0; h < holes.Count && !hit; h++)
                    hit = TriHitsHole(ax, az, bx, bz, cx, cz, holes[h]);
                if (!hit) { keep.Add(ia); keep.Add(ib); keep.Add(ic); }
            }
            return keep.ToArray();
        }

        static bool TriHitsHole(float ax, float az, float bx, float bz, float cx, float cz, float[] hole)
        {
            if (hole == null || hole.Length < 6) return false;
            if (InsidePoly(hole, ax, az) || InsidePoly(hole, bx, bz) || InsidePoly(hole, cx, cz)) return true;
            int n = hole.Length / 2;
            for (int i = 0; i < n; i++)
            {
                float x = hole[i * 2], z = hole[i * 2 + 1];
                float x2 = hole[((i + 1) % n) * 2], z2 = hole[((i + 1) % n) * 2 + 1];
                if (InTri(x, z, ax, az, bx, bz, cx, cz)) return true;
                if (SegHit(ax, az, bx, bz, x, z, x2, z2) || SegHit(bx, bz, cx, cz, x, z, x2, z2) || SegHit(cx, cz, ax, az, x, z, x2, z2))
                    return true;
            }
            return false;
        }

        static bool InsidePoly(float[] p, float x, float y)
        {
            bool inside = false; int n = p.Length / 2;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float yi = p[i * 2 + 1], yj = p[j * 2 + 1];
                if ((yi > y) == (yj > y)) continue;
                float xCross = p[j * 2] + (p[i * 2] - p[j * 2]) * (y - yj) / (yi - yj);
                if (x <= xCross) inside = !inside;
            }
            return inside;
        }

        static bool SegHit(float ax, float ay, float bx, float by, float cx, float cy, float dx, float dy)
        {
            float d1 = Cross(cx, cy, dx, dy, ax, ay);
            float d2 = Cross(cx, cy, dx, dy, bx, by);
            float d3 = Cross(ax, ay, bx, by, cx, cy);
            float d4 = Cross(ax, ay, bx, by, dx, dy);
            const float e = 1e-4f;
            if (((d1 > e && d2 < -e) || (d1 < -e && d2 > e)) && ((d3 > e && d4 < -e) || (d3 < -e && d4 > e))) return true;
            if (Math.Abs(d1) <= e && OnSeg(ax, ay, cx, cy, dx, dy)) return true;
            if (Math.Abs(d2) <= e && OnSeg(bx, by, cx, cy, dx, dy)) return true;
            if (Math.Abs(d3) <= e && OnSeg(cx, cy, ax, ay, bx, by)) return true;
            if (Math.Abs(d4) <= e && OnSeg(dx, dy, ax, ay, bx, by)) return true;
            return false;
        }

        static float Cross(float ax, float ay, float bx, float by, float cx, float cy) =>
            (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);

        static bool OnSeg(float px, float py, float ax, float ay, float bx, float by)
        {
            const float e = 1e-3f;
            return px >= Math.Min(ax, bx) - e && px <= Math.Max(ax, bx) + e
                && py >= Math.Min(ay, by) - e && py <= Math.Max(ay, by) + e;
        }

        public float AverageNormalY()
        {
            if (Normals.Length < 3) return 0f;
            double s = 0; int n = 0;
            for (int i = 1; i < Normals.Length; i += 3) { s += Normals[i]; n++; }
            return n == 0 ? 0f : (float)(s / n);
        }

        static bool InTri(float x, float z, float ax, float az, float bx, float bz, float cx, float cz) =>
            Bary(x, z, ax, az, bx, bz, cx, cz, out _, out _, out _);

        static bool Bary(float x, float z, float ax, float az, float bx, float bz, float cx, float cz,
            out float u, out float v, out float w)
        {
            u = v = w = 0f;
            float c1 = (x - ax) * (bz - az) - (z - az) * (bx - ax);
            float c2 = (x - bx) * (cz - bz) - (z - bz) * (cx - bx);
            float c3 = (x - cx) * (az - cz) - (z - cz) * (ax - cx);
            const float e = 2f;
            bool neg = c1 <= e && c2 <= e && c3 <= e;
            bool pos = c1 >= -e && c2 >= -e && c3 >= -e;
            if (!neg && !pos) return false;
            float area = (bx - ax) * (cz - az) - (bz - az) * (cx - ax);
            if (Math.Abs(area) < 1f) return false;
            v = ((x - ax) * (cz - az) - (z - az) * (cx - ax)) / area;
            w = ((bx - ax) * (z - az) - (bz - az) * (x - ax)) / area;
            u = 1f - v - w;
            return true;
        }
    }
}
