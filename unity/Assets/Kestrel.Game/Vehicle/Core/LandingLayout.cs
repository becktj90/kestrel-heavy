// Deployed landing legs and grid fins. Legs are two struts plus a foot, four around the aft skirt.
// A grid fin is one lattice mesh (the cells are not separate draws), instanced four times at the top of the booster.
using System;
using System.Collections.Generic;

namespace Kestrel.Vehicle
{
    public readonly struct FinMount
    {
        public readonly float X, Y, Z;
        public readonly float AxisX, AxisZ;
        public readonly float Span, Height, Thickness;
        public FinMount(float x, float y, float z, float ax, float az, float span, float height, float thickness)
        { X = x; Y = y; Z = z; AxisX = ax; AxisZ = az; Span = span; Height = height; Thickness = thickness; }
    }

    public static class LandingLayout
    {
        public const string Leg = "leg";
        public const string Foot = "foot";
        public const int Count = 4;

        /// <summary>Four legs. <paramref name="aftY"/> is the bottom of the stage body (above the bells). Feet sit on the pad.</summary>
        public static List<StrutPose> Legs(float radius, float aftY)
        {
            var list = new List<StrutPose>();
            if (!(radius > 0.05f)) return list;
            float hipY = Math.Max(aftY, radius * 0.45f);
            float hipR = radius * 0.98f;
            float kneeR = radius * 1.42f;
            float kneeY = hipY * 0.45f;
            float footR = radius * 1.62f;
            float footY = 0.04f;
            float strut = Math.Max(0.035f, radius * 0.028f);
            float footLen = radius * 0.28f;
            float footRad = strut * 1.6f;
            for (int i = 0; i < Count; i++)
            {
                double a = Math.PI / 4.0 + i * Math.PI / 2.0;
                float sa = (float)Math.Sin(a), ca = (float)Math.Cos(a);
                float hx = hipR * sa, hz = hipR * ca;
                float kx = kneeR * sa, kz = kneeR * ca;
                float fx = footR * sa, fz = footR * ca;
                Span(list, Leg, hx, hipY, hz, kx, kneeY, kz, strut);
                Span(list, Leg, kx, kneeY, kz, fx, footY, fz, strut);
                // Foot lies on the pad, along the outward radius.
                list.Add(new StrutPose(Foot, fx, footY, fz, footRad, footLen, sa, 0f, ca));
            }
            return list;
        }

        /// <summary>Four fins just below <paramref name="stationY"/> (top of the booster). Local +X of the mesh points outward.</summary>
        public static List<FinMount> Fins(float radius, float stationY)
        {
            var list = new List<FinMount>();
            if (!(radius > 0.05f)) return list;
            float span = radius * 0.55f;
            float height = radius * 0.40f;
            float thick = span * 0.16f;
            float y = stationY - height * 0.35f;
            for (int i = 0; i < Count; i++)
            {
                double a = i * Math.PI / 2.0;
                float sa = (float)Math.Sin(a), ca = (float)Math.Cos(a);
                list.Add(new FinMount(radius * sa, y, radius * ca, sa, ca, span, height, thick));
            }
            return list;
        }

        public static int LegTriangles(IReadOnlyList<StrutPose> legs, int lod)
        {
            if (legs == null || legs.Count == 0) return 0;
            return legs.Count * BayMesh.OpenCylinder(lod <= 0 ? 6 : 4).TriangleCount;
        }

        public static int LegDrawCalls(IReadOnlyList<StrutPose> legs) => legs != null && legs.Count > 0 ? 1 : 0;

        static void Span(List<StrutPose> list, string kind, float x0, float y0, float z0, float x1, float y1, float z1, float radius)
        {
            float dx = x1 - x0, dy = y1 - y0, dz = z1 - z0;
            float len = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (len < 1e-4f) return;
            list.Add(new StrutPose(kind, (x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f, radius, len, dx / len, dy / len, dz / len));
        }
    }

    /// <summary>One grid-fin lattice in local space: +X radial (0..1), Y height (-0.5..0.5), Z thickness (-0.5..0.5).
    /// The whole fin is one mesh so four fins are one batch, not one batch per cell.</summary>
    public static class FinMesh
    {
        public static int Cells(int lod) => lod <= 0 ? 3 : lod == 1 ? 2 : 1;

        public static MeshData Build(int lod)
        {
            int cells = Cells(lod);
            var md = new MeshData();
            int lines = cells + 1;
            float bar = 0.045f;
            for (int i = 0; i < lines; i++)
            {
                float x = i / (float)cells;
                Box(md, x - bar * 0.5f, x + bar * 0.5f, -0.5f, 0.5f, -0.5f, 0.5f);
            }
            for (int i = 0; i < lines; i++)
            {
                float y = -0.5f + i / (float)cells;
                Box(md, 0f, 1f, y - bar * 0.5f, y + bar * 0.5f, -0.5f, 0.5f);
            }
            return md;
        }

        public static int TriangleCount(int lod) => Build(lod).TriangleCount;

        /// <summary>Four instances of one mesh.</summary>
        public static int DrawCalls(int fins) => fins > 0 ? 1 : 0;

        static void Box(MeshData md, float x0, float x1, float y0, float y1, float z0, float z1)
        {
            Face(md, x1, y0, z1, x1, y1, z1, x0, y1, z1, x0, y0, z1, 0, 0, 1);
            Face(md, x0, y0, z0, x0, y1, z0, x1, y1, z0, x1, y0, z0, 0, 0, -1);
            Face(md, x0, y1, z0, x0, y1, z1, x1, y1, z1, x1, y1, z0, 0, 1, 0);
            Face(md, x0, y0, z1, x0, y0, z0, x1, y0, z0, x1, y0, z1, 0, -1, 0);
            Face(md, x1, y0, z0, x1, y1, z0, x1, y1, z1, x1, y0, z1, 1, 0, 0);
            Face(md, x0, y0, z1, x0, y1, z1, x0, y1, z0, x0, y0, z0, -1, 0, 0);
        }

        static void Face(MeshData md, float x0, float y0, float z0, float x1, float y1, float z1, float x2, float y2, float z2, float x3, float y3, float z3, float nx, float ny, float nz)
        {
            int i = md.VertexCount;
            md.Vertex(x0, y0, z0, nx, ny, nz, 1, 0, 0, 0);
            md.Vertex(x1, y1, z1, nx, ny, nz, 1, 0, 0, 1);
            md.Vertex(x2, y2, z2, nx, ny, nz, 1, 0, 1, 1);
            md.Vertex(x3, y3, z3, nx, ny, nz, 1, 0, 1, 0);
            md.Triangles.Add(i); md.Triangles.Add(i + 1); md.Triangles.Add(i + 2);
            md.Triangles.Add(i); md.Triangles.Add(i + 2); md.Triangles.Add(i + 3);
        }
    }
}
