// Procedural mesh helpers: box beams, I-beams, pipes along a polyline, and catenary hoses.
// Meshes are built once per (shape, LOD) and shared through instancing; nothing is generated per frame.
using System.Collections.Generic;
using UnityEngine;

namespace Kestrel.Kit
{
    public static class KitMesh
    {
        /// <summary>Unit box beam along +Y (length 1, cross-section w x d). Scale Y to length when instancing.</summary>
        public static Mesh Beam(float width, float depth)
        {
            float x = width * 0.5f, z = depth * 0.5f;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 nn)
            { int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); for (int k = 0; k < 4; k++) n.Add(nn); t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 }); }
            Quad(new Vector3(-x, 0, -z), new Vector3(-x, 1, -z), new Vector3(x, 1, -z), new Vector3(x, 0, -z), Vector3.back);
            Quad(new Vector3(x, 0, z), new Vector3(x, 1, z), new Vector3(-x, 1, z), new Vector3(-x, 0, z), Vector3.forward);
            Quad(new Vector3(-x, 0, z), new Vector3(-x, 1, z), new Vector3(-x, 1, -z), new Vector3(-x, 0, -z), Vector3.left);
            Quad(new Vector3(x, 0, -z), new Vector3(x, 1, -z), new Vector3(x, 1, z), new Vector3(x, 0, z), Vector3.right);
            Quad(new Vector3(-x, 1, -z), new Vector3(-x, 1, z), new Vector3(x, 1, z), new Vector3(x, 1, -z), Vector3.up);
            Quad(new Vector3(-x, 0, z), new Vector3(-x, 0, -z), new Vector3(x, 0, -z), new Vector3(x, 0, z), Vector3.down);
            return Build("Kit_Beam", v, n, null, t);
        }

        /// <summary>Unit I-beam along +Y: flange width w, depth d, flange/web thickness th. 3 boxes, 36 tris.</summary>
        public static Mesh IBeam(float w, float d, float th)
        {
            var parts = new[] { (new Vector3(0, 0, -d / 2 + th / 2), new Vector2(w, th)), (Vector3.zero, new Vector2(th, d - 2 * th)), (new Vector3(0, 0, d / 2 - th / 2), new Vector2(w, th)) };
            var combine = new CombineInstance[3];
            for (int i = 0; i < 3; i++) combine[i] = new CombineInstance { mesh = Beam(parts[i].Item2.x, parts[i].Item2.y), transform = Matrix4x4.Translate(parts[i].Item1) };
            var m = new Mesh { name = "Kit_IBeam" }; m.CombineMeshes(combine, true, true); m.RecalculateBounds();
            foreach (var c in combine) Object.DestroyImmediate(c.mesh);
            return m;
        }

        /// <summary>Tube swept along a polyline with parallel-transport frames (no twist). Open ends.
        /// uv.x wraps the circumference, uv.y is metres along the path (so stripes tile by length).</summary>
        public static Mesh Tube(IReadOnlyList<Vector3> path, float radius, int radialSegments, string name = "Kit_Tube")
        {
            int rs = Mathf.Max(3, radialSegments), ns = path.Count;
            if (ns < 2) return new Mesh { name = name };
            var v = new List<Vector3>(ns * (rs + 1)); var n = new List<Vector3>(v.Capacity); var uv = new List<Vector2>(v.Capacity); var t = new List<int>();
            Vector3 tan = (path[1] - path[0]).normalized;
            Vector3 normal = Vector3.Cross(tan, Mathf.Abs(Vector3.Dot(tan, Vector3.up)) > 0.9f ? Vector3.right : Vector3.up).normalized;
            float along = 0;
            for (int i = 0; i < ns; i++)
            {
                Vector3 nt = i < ns - 1 ? (path[i + 1] - path[i]).normalized : tan;
                Vector3 avg = (tan + nt).normalized; if (avg.sqrMagnitude < 1e-6f) avg = nt;
                normal = (normal - Vector3.Dot(normal, avg) * avg).normalized;   // parallel transport
                Vector3 bin = Vector3.Cross(avg, normal);
                if (i > 0) along += Vector3.Distance(path[i], path[i - 1]);
                for (int r = 0; r <= rs; r++)
                {
                    float a = r * Mathf.PI * 2 / rs;
                    Vector3 dir = Mathf.Cos(a) * normal + Mathf.Sin(a) * bin;
                    v.Add(path[i] + dir * radius); n.Add(dir); uv.Add(new Vector2((float)r / rs, along));
                }
                tan = nt;
            }
            for (int i = 0; i < ns - 1; i++) for (int r = 0; r < rs; r++)
            {
                int a = i * (rs + 1) + r, b = a + rs + 1;
                t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
            }
            return Build(name, v, n, uv, t);
        }

        /// <summary>Straight pipe from a to b with optional 90-degree-ish bends already in the path.</summary>
        public static Mesh Pipe(IReadOnlyList<Vector3> path, float radius, int lod) => Tube(path, radius, LodPolicy.RadialSegments(lod), "Kit_Pipe");

        /// <summary>Sagging hose between two world points: catenary in the vertical plane through both ends.
        /// slack = arc length / chord (1.05 to 1.3 reads well).</summary>
        public static Vector3[] HosePath(Vector3 a, Vector3 b, float slack, int samples)
        {
            var flat = new Vector3(b.x - a.x, 0, b.z - a.z); float w = flat.magnitude; var dirH = w > 1e-5f ? flat / w : Vector3.right;
            float chord = Vector3.Distance(a, b);
            var hs = new double[samples + 1]; var vs = new double[samples + 1];
            Catenary.Sample2D(0, a.y, w, b.y, chord * Mathf.Max(1.0001f, slack), samples, hs, vs);
            var pts = new Vector3[samples + 1];
            for (int i = 0; i <= samples; i++) pts[i] = new Vector3(a.x, 0, a.z) + dirH * (float)hs[i] + Vector3.up * (float)vs[i];
            return pts;
        }

        public static Mesh Hose(Vector3 a, Vector3 b, float radius, float slack, int lod)
            => Tube(HosePath(a, b, slack, lod <= 0 ? 24 : lod == 1 ? 12 : 6), radius, LodPolicy.RadialSegments(lod), "Kit_Hose");

        public static int TriangleCount(Mesh m) { long c = 0; for (int i = 0; i < m.subMeshCount; i++) c += m.GetIndexCount(i); return (int)(c / 3); }

        static Mesh Build(string name, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t)
        {
            var m = new Mesh { name = name };
            if (v.Count > 65535) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(v); m.SetNormals(n); if (uv != null) m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds();
            return m;
        }
    }
}
