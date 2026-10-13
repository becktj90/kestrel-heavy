// Bell triangle mesh from a BellContour. Regen tubes are a radial sine so a lathe can carry them
// and the seam still meets (tube count divides the segment count). LOD0 adds the inner wall.
using System;
using System.Collections.Generic;

namespace Kestrel.Vehicle
{
    public static class BellMesh
    {
        public const float WallFraction = 0.012f;   // thickness / exit radius

        public static MeshData Build(BellContour contour, int lod)
        {
            int radial = BellLod.Radial(lod);
            var mesh = new MeshData();
            Lathe(mesh, contour, radial, contour.BandDepth, contour.TubeCount, contour.ThroatY, flip: false, inward: 0f);
            if (lod <= 0)
            {
                float t = WallFraction * contour.ExitRadius;
                Lathe(mesh, contour, radial, 0f, contour.TubeCount, contour.ThroatY, flip: true, inward: t);
                Lip(mesh, contour, radial, t);
            }
            return mesh;
        }

        public static int TriangleCount(BellContour contour, int lod) => Build(contour, lod).TriangleCount;

        /// <summary>One shared mesh per engine type, instanced. Draw calls are one per type (under the 1023 cap).</summary>
        public static int DrawCalls(int engineTypes) => engineTypes <= 0 ? 0 : engineTypes;

        static void Lathe(MeshData md, BellContour contour, int segments, float depth, int tubes, float throatY, bool flip, float inward)
        {
            var profile = contour.Wall;
            int S = Math.Max(3, segments), P = profile.Count;
            int start = md.VertexCount;
            for (int i = 0; i < P; i++)
            {
                var p = profile[i];
                float nr = flip ? -p.NR : p.NR, ny = flip ? -p.NY : p.NY;
                for (int j = 0; j <= S; j++)
                {
                    double a = 2 * Math.PI * j / S;
                    float sa = (float)Math.Sin(a), ca = (float)Math.Cos(a);
                    float band = 0f;
                    if (depth > 0f && p.Y > 1e-4f && p.Y < throatY - 1e-3f)
                        band = depth * (float)Math.Sin(tubes * a);
                    float r = p.R + band - inward * p.NR;
                    float y = p.Y - inward * p.NY;
                    if (r < 0f) r = 0f;
                    md.Vertex(r * sa, y, r * ca, nr * sa, ny, nr * ca, ca, -sa, (float)(a * contour.ExitRadius), p.V);
                }
            }
            for (int i = 0; i < P - 1; i++)
            {
                if (LatheMesh.Coincident(profile[i], profile[i + 1]) && inward <= 0f) continue;
                bool lowPole = profile[i].R <= 1e-6f, highPole = profile[i + 1].R <= 1e-6f;
                for (int j = 0; j < S; j++)
                {
                    int A = start + i * (S + 1) + j, B = A + 1, C = A + S + 1, D = C + 1;
                    if (!flip)
                    {
                        if (!lowPole) { md.Triangles.Add(A); md.Triangles.Add(B); md.Triangles.Add(D); }
                        if (!highPole) { md.Triangles.Add(A); md.Triangles.Add(D); md.Triangles.Add(C); }
                    }
                    else
                    {
                        if (!lowPole) { md.Triangles.Add(A); md.Triangles.Add(D); md.Triangles.Add(B); }
                        if (!highPole) { md.Triangles.Add(A); md.Triangles.Add(C); md.Triangles.Add(D); }
                    }
                }
            }
        }

        /// <summary>Closes the exit lip so the shell reads as a wall, not a paper surface.</summary>
        static void Lip(MeshData md, BellContour contour, int segments, float thickness)
        {
            int S = Math.Max(3, segments);
            int start = md.VertexCount;
            float re = contour.ExitRadius, t = thickness;
            for (int j = 0; j <= S; j++)
            {
                double a = 2 * Math.PI * j / S;
                float sa = (float)Math.Sin(a), ca = (float)Math.Cos(a);
                md.Vertex(re * sa, 0f, re * ca, 0f, -1f, 0f, ca, -sa, (float)(a * re), 0f);
                md.Vertex((re + t) * sa, 0f, (re + t) * ca, 0f, -1f, 0f, ca, -sa, (float)(a * re), t);
            }
            for (int j = 0; j < S; j++)
            {
                int A = start + j * 2, B = A + 1, C = A + 2, D = A + 3;
                md.Triangles.Add(A); md.Triangles.Add(C); md.Triangles.Add(B);
                md.Triangles.Add(B); md.Triangles.Add(C); md.Triangles.Add(D);
            }
        }
    }
}
