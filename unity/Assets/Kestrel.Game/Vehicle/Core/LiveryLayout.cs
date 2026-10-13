// Panel lines, an accent band, and a few decal blocks. No stripe texture: the paint is geometry
// on the tank skin, using the design's accent colour at draw time.
using System;
using System.Collections.Generic;

namespace Kestrel.Vehicle
{
    public readonly struct LiveryMark
    {
        public readonly string Kind;
        public readonly float X, Y, Z;
        public readonly float Sx, Sy, Sz;
        public readonly float AxisX, AxisY, AxisZ;
        public LiveryMark(string kind, float x, float y, float z, float sx, float sy, float sz, float ax, float ay, float az)
        { Kind = kind; X = x; Y = y; Z = z; Sx = sx; Sy = sy; Sz = sz; AxisX = ax; AxisY = ay; AxisZ = az; }
    }

    public static class LiveryLayout
    {
        public const string Ring = "ring";
        public const string Seam = "seam";
        public const string Band = "band";
        public const string Decal = "decal";
        public const float PanelPitch = 2.4f;
        public const float Proud = 0.02f;

        public static bool Accent(string kind) => kind == Band || kind == Decal;

        /// <summary>LOD2 keeps the rings and the accent band. Seams and decals drop first.</summary>
        public static bool Draws(string kind, int lod) => !(lod >= 2 && (kind == Seam || kind == Decal));

        public static List<LiveryMark> Tank(float ox, float oy, float oz, float radius, float barrelBottom, float barrelLength, bool accent)
        {
            var list = new List<LiveryMark>();
            if (!(radius > 0.05f) || !(barrelLength > 0.5f)) return list;
            float skin = radius + Proud;
            for (float along = PanelPitch; along < barrelLength - 0.3f; along += PanelPitch)
            {
                float y = oy + barrelBottom + along;
                list.Add(new LiveryMark(Ring, ox, y, oz, skin, skin, skin, 0f, 1f, 0f));
            }
            if (list.Count == 0)
            {
                float y = oy + barrelBottom + barrelLength * 0.5f;
                list.Add(new LiveryMark(Ring, ox, y, oz, skin, skin, skin, 0f, 1f, 0f));
            }
            float seamR = 0.012f;
            for (int i = 0; i < 2; i++)
            {
                double a = i == 0 ? Math.PI / 2.0 : -Math.PI / 2.0;
                float x = ox + skin * (float)Math.Sin(a);
                float z = oz + skin * (float)Math.Cos(a);
                float y = oy + barrelBottom + barrelLength * 0.5f;
                list.Add(new LiveryMark(Seam, x, y, z, seamR, barrelLength, seamR, 0f, 1f, 0f));
            }
            if (!accent) return list;
            float bandY = oy + barrelBottom + barrelLength * 0.72f;
            float bandR = radius + Proud + 0.012f;
            list.Add(new LiveryMark(Band, ox, bandY, oz, bandR, 0.22f, bandR, 0f, 1f, 0f));
            float face = oz + radius + Proud + 0.01f;
            float y0 = oy + barrelBottom + barrelLength * 0.55f;
            list.Add(new LiveryMark(Decal, ox, y0, face, 0.85f, 0.42f, 0.02f, 0f, 1f, 0f));
            list.Add(new LiveryMark(Decal, ox, y0 - 0.55f, face, 0.36f, 0.18f, 0.02f, 0f, 1f, 0f));
            list.Add(new LiveryMark(Decal, ox + 0.55f, y0 - 0.08f, face, 0.22f, 0.08f, 0.015f, 0f, 1f, 0f));
            return list;
        }

        /// <summary>Distance from the tank axis to the near side of a ring (the ring scale is the major radius).</summary>
        public static float RingClearance(LiveryMark ring, float tankRadius) => ring.Sx - tankRadius;

        public static int TriangleCount(IReadOnlyList<LiveryMark> marks, int lod)
        {
            if (marks == null) return 0;
            int ring = LiveryMesh.TorusTriangles(lod);
            int tube = BayMesh.OpenCylinder(lod <= 0 ? 6 : 4).TriangleCount;
            int box = LiveryMesh.BoxTriangles();
            int n = 0;
            for (int i = 0; i < marks.Count; i++)
            {
                if (!Draws(marks[i].Kind, lod)) continue;
                switch (marks[i].Kind)
                {
                    case Ring: n += ring; break;
                    case Decal: n += box; break;
                    default: n += tube; break;
                }
            }
            return n;
        }

        public static int DrawCalls(IReadOnlyList<LiveryMark> marks, int lod)
        {
            if (marks == null) return 0;
            bool ring = false, seam = false, band = false, decal = false;
            for (int i = 0; i < marks.Count; i++)
            {
                if (!Draws(marks[i].Kind, lod)) continue;
                switch (marks[i].Kind)
                {
                    case Ring: ring = true; break;
                    case Seam: seam = true; break;
                    case Band: band = true; break;
                    case Decal: decal = true; break;
                }
            }
            return (ring ? 1 : 0) + (seam ? 1 : 0) + (band ? 1 : 0) + (decal ? 1 : 0);
        }
    }

    public static class LiveryMesh
    {
        public const float Minor = 0.016f;

        public static int MajorSides(int lod) => lod <= 0 ? 16 : 8;
        public static int MinorSides(int lod) => lod <= 0 ? 4 : 3;

        public static int TorusTriangles(int lod) => MajorSides(lod) * MinorSides(lod) * 2;

        public static MeshData Torus(int lod)
        {
            int major = MajorSides(lod), minorN = MinorSides(lod);
            var md = new MeshData();
            for (int i = 0; i < minorN; i++)
            {
                double b = 2 * Math.PI * i / minorN;
                float cb = (float)Math.Cos(b), sb = (float)Math.Sin(b);
                float r = 1f + Minor * cb;
                float y = Minor * sb;
                for (int j = 0; j <= major; j++)
                {
                    double a = 2 * Math.PI * j / major;
                    float sa = (float)Math.Sin(a), ca = (float)Math.Cos(a);
                    md.Vertex(r * sa, y, r * ca, cb * sa, sb, cb * ca, ca, -sa, (float)a, (float)b);
                }
            }
            for (int i = 0; i < minorN; i++)
            {
                int i2 = (i + 1) % minorN;
                for (int j = 0; j < major; j++)
                {
                    int A = i * (major + 1) + j, B = A + 1, C = i2 * (major + 1) + j, D = C + 1;
                    md.Triangles.Add(A); md.Triangles.Add(B); md.Triangles.Add(D);
                    md.Triangles.Add(A); md.Triangles.Add(D); md.Triangles.Add(C);
                }
            }
            return md;
        }

        public static int BoxTriangles() => 12;

        /// <summary>Unit box, each side 1, centred. Scale it onto the skin.</summary>
        public static MeshData Box()
        {
            var md = new MeshData();
            Face(md, 0.5f, -0.5f, 0.5f, 0.5f, 0.5f, 0.5f, -0.5f, 0.5f, 0.5f, -0.5f, -0.5f, 0.5f, 0, 0, 1);
            Face(md, -0.5f, -0.5f, -0.5f, -0.5f, 0.5f, -0.5f, 0.5f, 0.5f, -0.5f, 0.5f, -0.5f, -0.5f, 0, 0, -1);
            Face(md, -0.5f, 0.5f, -0.5f, -0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, -0.5f, 0, 1, 0);
            Face(md, -0.5f, -0.5f, 0.5f, -0.5f, -0.5f, -0.5f, 0.5f, -0.5f, -0.5f, 0.5f, -0.5f, 0.5f, 0, -1, 0);
            Face(md, 0.5f, -0.5f, -0.5f, 0.5f, 0.5f, -0.5f, 0.5f, 0.5f, 0.5f, 0.5f, -0.5f, 0.5f, 1, 0, 0);
            Face(md, -0.5f, -0.5f, 0.5f, -0.5f, 0.5f, 0.5f, -0.5f, 0.5f, -0.5f, -0.5f, -0.5f, -0.5f, -1, 0, 0);
            return md;
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
