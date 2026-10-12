using System;
using System.Collections.Generic;

namespace Kestrel.Vehicle
{
    /// <summary>Engine-free mesh data. Runtime copies it into a UnityEngine.Mesh; tests check it without Unity.</summary>
    public sealed class MeshData
    {
        public readonly List<float> Positions = new List<float>();   // xyz
        public readonly List<float> Normals = new List<float>();     // xyz
        public readonly List<float> Tangents = new List<float>();    // xyzw
        public readonly List<float> Uv = new List<float>();          // uv (metres: around, along)
        public readonly List<int> Triangles = new List<int>();
        public int VertexCount => Positions.Count / 3;
        public int TriangleCount => Triangles.Count / 3;
        internal void Vertex(float x, float y, float z, float nx, float ny, float nz, float tx, float tz, float u, float v)
        {
            Positions.Add(x); Positions.Add(y); Positions.Add(z);
            Normals.Add(nx); Normals.Add(ny); Normals.Add(nz);
            Tangents.Add(tx); Tangents.Add(0); Tangents.Add(tz); Tangents.Add(1);
            Uv.Add(u); Uv.Add(v);
        }
    }

    public static class LatheMesh
    {
        /// <summary>Surface of revolution. Vertex at azimuth a: (r sin a, y, r cos a), so a = 0 faces +Z (tower).
        /// UV: u = metres around at the barrel radius (a * maxR), v = profile arc length; tangent = d/da (u direction),
        /// bitangent = +profile, so normal maps tile in metres. Clockwise-from-outside winding (Unity front faces).
        /// Segments whose centre azimuth falls inside the cutaway sector [cutFrom, cutTo) are skipped.</summary>
        public static MeshData Build(IReadOnlyList<ProfilePoint> profile, int segments, float cutFrom = 0, float cutTo = 0)
        {
            int S = Math.Max(3, segments), P = profile.Count;
            var md = new MeshData();
            float maxR = 0; foreach (var p in profile) maxR = Math.Max(maxR, p.R);
            for (int i = 0; i < P; i++)
            {
                var p = profile[i];
                for (int j = 0; j <= S; j++)
                {
                    double a = 2 * Math.PI * j / S; float sa = (float)Math.Sin(a), ca = (float)Math.Cos(a);
                    md.Vertex(p.R * sa, p.Y, p.R * ca, p.NR * sa, p.NY, p.NR * ca, ca, -sa, (float)(a * maxR), p.V);
                }
            }
            bool cut = cutTo > cutFrom;
            for (int i = 0; i < P - 1; i++)
            {
                if (Coincident(profile[i], profile[i + 1])) continue;   // crease: shared position, two normals
                bool lowPole = profile[i].R <= 1e-6f, highPole = profile[i + 1].R <= 1e-6f;
                for (int j = 0; j < S; j++)
                {
                    if (cut) { float mid = (float)(2 * Math.PI * (j + 0.5) / S); if (mid >= cutFrom && mid < cutTo) continue; }
                    int A = i * (S + 1) + j, B = A + 1, C = A + S + 1, D = C + 1;   // A,B lower; C,D upper; B/D at larger azimuth
                    if (!lowPole) { md.Triangles.Add(A); md.Triangles.Add(B); md.Triangles.Add(D); }
                    if (!highPole) { md.Triangles.Add(A); md.Triangles.Add(D); md.Triangles.Add(C); }
                }
            }
            return md;
        }

        public static bool Coincident(ProfilePoint a, ProfilePoint b) => Math.Abs(a.R - b.R) < 1e-6f && Math.Abs(a.Y - b.Y) < 1e-6f;

        /// <summary>Exact triangle count Build() will emit (no cutaway). Used by TankBudget.</summary>
        public static int CountTriangles(IReadOnlyList<ProfilePoint> profile, int segments)
        {
            int S = Math.Max(3, segments), n = 0;
            for (int i = 0; i < profile.Count - 1; i++)
            {
                if (Coincident(profile[i], profile[i + 1])) continue;
                bool lo = profile[i].R <= 1e-6f, hi = profile[i + 1].R <= 1e-6f;
                n += ((lo ? 0 : 1) + (hi ? 0 : 1)) * S;
            }
            return n;
        }
    }
}
