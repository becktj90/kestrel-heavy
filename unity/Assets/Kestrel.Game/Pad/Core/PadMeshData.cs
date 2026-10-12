// Engine-free triangle mesh. Runtime uploads it to a Unity Mesh; tests inspect it without Unity.
// Winding matches Unity: Vector3.Cross of (b-a, c-a) points out of the front face.
using System;
using System.Collections.Generic;
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    public struct Vec2
    {
        public float X, Y;
        public Vec2(float x, float y) { X = x; Y = y; }
    }

    public sealed class PadMeshData
    {
        public readonly List<float> Positions = new List<float>();
        public readonly List<float> Normals = new List<float>();
        public readonly List<float> Uv = new List<float>();
        public readonly List<int> Triangles = new List<int>();
        public int VertexCount => Positions.Count / 3;
        public int TriangleCount => Triangles.Count / 3;

        public void Vert(float x, float y, float z, float nx, float ny, float nz, float u, float v)
        {
            Positions.Add(x); Positions.Add(y); Positions.Add(z);
            Normals.Add(nx); Normals.Add(ny); Normals.Add(nz);
            Uv.Add(u); Uv.Add(v);
        }

        /// <summary>One triangle. Degenerate tris are dropped. The stored normal is the unit cross of the winding.</summary>
        public void AddTri(float ax, float ay, float az, float bx, float by, float bz, float cx, float cy, float cz, float uScale = 1f)
        {
            float abx = bx - ax, aby = by - ay, abz = bz - az;
            float acx = cx - ax, acy = cy - ay, acz = cz - az;
            float nx = aby * acz - abz * acy;
            float ny = abz * acx - abx * acz;
            float nz = abx * acy - aby * acx;
            float len = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (len < 1e-6f) return;
            nx /= len; ny /= len; nz /= len;
            int i = VertexCount;
            Vert(ax, ay, az, nx, ny, nz, ax * uScale, az * uScale);
            Vert(bx, by, bz, nx, ny, nz, bx * uScale, bz * uScale);
            Vert(cx, cy, cz, nx, ny, nz, cx * uScale, cz * uScale);
            Triangles.Add(i); Triangles.Add(i + 1); Triangles.Add(i + 2);
        }

        /// <summary>Quad a-b-c-d. Flipped so the normal points the same way as <paramref name="ox,oy,oz"/>.</summary>
        public void AddQuad(Vec3 a, Vec3 b, Vec3 c, Vec3 d, float ox, float oy, float oz)
        {
            float abx = b.X - a.X, aby = b.Y - a.Y, abz = b.Z - a.Z;
            float acx = c.X - a.X, acy = c.Y - a.Y, acz = c.Z - a.Z;
            float nx = aby * acz - abz * acy, ny = abz * acx - abx * acz, nz = abx * acy - aby * acx;
            if (nx * ox + ny * oy + nz * oz >= 0f)
            {
                AddTri(a.X, a.Y, a.Z, b.X, b.Y, b.Z, c.X, c.Y, c.Z);
                AddTri(a.X, a.Y, a.Z, c.X, c.Y, c.Z, d.X, d.Y, d.Z);
            }
            else
            {
                AddTri(a.X, a.Y, a.Z, d.X, d.Y, d.Z, c.X, c.Y, c.Z);
                AddTri(a.X, a.Y, a.Z, c.X, c.Y, c.Z, b.X, b.Y, b.Z);
            }
        }

        /// <summary>Axis-aligned box. <paramref name="bottom"/> includes the down face (hidden against a slab when false).</summary>
        public void AddBox(Vec3 center, Vec3 size, bool bottom = false)
        {
            float x = size.X * 0.5f, y = size.Y * 0.5f, z = size.Z * 0.5f;
            Vec3 C(float dx, float dy, float dz) => new Vec3(center.X + dx, center.Y + dy, center.Z + dz);
            var p000 = C(-x, -y, -z); var p001 = C(-x, -y, z); var p010 = C(-x, y, -z); var p011 = C(-x, y, z);
            var p100 = C(x, -y, -z); var p101 = C(x, -y, z); var p110 = C(x, y, -z); var p111 = C(x, y, z);
            AddQuad(p010, p110, p111, p011, 0, 1, 0);
            if (bottom) AddQuad(p000, p001, p101, p100, 0, -1, 0);
            AddQuad(p001, p011, p111, p101, 0, 0, 1);
            AddQuad(p100, p110, p010, p000, 0, 0, -1);
            AddQuad(p000, p010, p011, p001, -1, 0, 0);
            AddQuad(p101, p111, p110, p100, 1, 0, 0);
        }
    }
}
