// Split a vehicle mesh on a horizontal joint so the stack reads as snapped, not faded.
using System.Collections.Generic;
using UnityEngine;

namespace Kestrel.Game.Failure
{
    public static class FailureCut
    {
        public static void Halves(Mesh source, float y, out Mesh lower, out Mesh upper)
        {
            lower = new Mesh { name = "FailureLower" };
            upper = new Mesh { name = "FailureUpper" };
            if (source == null) return;
            var verts = source.vertices;
            var tris = source.triangles;
            var loV = new List<Vector3>();
            var hiV = new List<Vector3>();
            var loT = new List<int>();
            var hiT = new List<int>();
            for (int i = 0; i + 2 < tris.Length; i += 3)
            {
                var a = verts[tris[i]];
                var b = verts[tris[i + 1]];
                var c = verts[tris[i + 2]];
                Add(Clip(new[] { a, b, c }, y, false), loV, loT);
                Add(Clip(new[] { a, b, c }, y, true), hiV, hiT);
            }
            Apply(lower, loV, loT);
            Apply(upper, hiV, hiT);
        }

        public static List<Vector3> Clip(Vector3[] poly, float y, bool keepAbove)
        {
            var output = new List<Vector3>();
            if (poly == null || poly.Length == 0) return output;
            for (int i = 0; i < poly.Length; i++)
            {
                var prev = poly[(i + poly.Length - 1) % poly.Length];
                var cur = poly[i];
                bool curIn = keepAbove ? cur.y >= y - 1e-4f : cur.y <= y + 1e-4f;
                bool prevIn = keepAbove ? prev.y >= y - 1e-4f : prev.y <= y + 1e-4f;
                if (curIn)
                {
                    if (!prevIn) output.Add(Hit(prev, cur, y));
                    output.Add(cur);
                }
                else if (prevIn) output.Add(Hit(prev, cur, y));
            }
            return output;
        }

        public static Vector3 Hit(Vector3 a, Vector3 b, float y)
        {
            float dy = b.y - a.y;
            float u = Mathf.Abs(dy) < 1e-6f ? 0f : (y - a.y) / dy;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return Vector3.Lerp(a, b, u);
        }

        static void Add(List<Vector3> poly, List<Vector3> verts, List<int> tris)
        {
            if (poly == null || poly.Count < 3) return;
            int start = verts.Count;
            for (int i = 0; i < poly.Count; i++) verts.Add(poly[i]);
            for (int i = 1; i + 1 < poly.Count; i++)
            {
                tris.Add(start);
                tris.Add(start + i);
                tris.Add(start + i + 1);
            }
        }

        static void Apply(Mesh mesh, List<Vector3> verts, List<int> tris)
        {
            if (verts.Count == 0 || tris.Count == 0) return;
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
