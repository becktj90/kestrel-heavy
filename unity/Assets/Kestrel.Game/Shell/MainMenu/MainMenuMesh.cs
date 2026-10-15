// Meshes for the Home hangar. The KH-1 stack is a lathe in real metres (base at y = 0, +Y up) so a
// 1.75 m crew figure stays in scale with the 71.4 m vehicle. Panels are unit cubes scaled by the layout.
using System.Collections.Generic;
using UnityEngine;

namespace Kestrel.Game.Shell
{
    public static class MainMenuMesh
    {
        public static Mesh UnitBox() => Box(1f, 1f, 1f);

        public static Mesh Box(float w, float h, float d)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            AddBox(v, n, t, Vector3.zero, new Vector3(w, h, d), Quaternion.identity);
            return Bake("KH_Box", v, n, t);
        }

        /// <summary>Annulus of tick marks in the XY plane, facing +Z, for the KH-1 selector.</summary>
        public static Mesh Ticks(float radius, int count, float length, float thick, float depth)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            count = Mathf.Max(4, count);
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                var rot = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
                Vector3 c = rot * new Vector3(0f, radius, 0f);
                AddBox(v, n, t, c, new Vector3(thick, length, depth), rot);
            }
            return Bake("KH_Ticks", v, n, t);
        }

        public static Mesh Disc(float radius, int segments)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            int seg = Mathf.Max(8, segments);
            int c = 0;
            v.Add(Vector3.zero); n.Add(Vector3.forward);
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                v.Add(new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
                n.Add(Vector3.forward);
            }
            for (int i = 1; i <= seg; i++) { t.Add(c); t.Add(i); t.Add(i + 1); }
            return Bake("KH_Disc", v, n, t);
        }

        public static Mesh Icon(string kind)
        {
            switch (kind)
            {
                case "wrench": return Wrench();
                case "planet": return Planet();
                case "book": return Book();
                case "gear": return Gear();
                default: return Disc(0.35f, 16);
            }
        }

        public static Mesh CoreBody() => LatheMesh("KH_Core", CoreProfile(), Vector3.zero, Quaternion.identity, MainMenuLayout.RocketSegments);
        public static Mesh BoosterBody() => LatheMesh("KH_Booster", BoosterProfile(), Vector3.zero, Quaternion.identity, MainMenuLayout.RocketSegments);
        public static Mesh Bell() => LatheMesh("KH_Bell", BellProfile(), Vector3.zero, Quaternion.identity, 20);

        public static Mesh Crew()
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            // 1.75 m figure, feet at the origin. Blocky on purpose so it still reads at a few pixels.
            AddBox(v, n, t, new Vector3(0f, 0.95f, 0f), new Vector3(0.36f, 0.72f, 0.22f), Quaternion.identity);
            AddBox(v, n, t, new Vector3(-0.12f, 0.32f, 0f), new Vector3(0.14f, 0.64f, 0.16f), Quaternion.identity);
            AddBox(v, n, t, new Vector3(0.12f, 0.32f, 0f), new Vector3(0.14f, 0.64f, 0.16f), Quaternion.identity);
            AddBox(v, n, t, new Vector3(0f, 1.38f, 0f), new Vector3(0.26f, 0.22f, 0.22f), Quaternion.identity);
            AddSphere(v, n, t, new Vector3(0f, 1.5f, 0f), 0.13f, 8, 6);
            AddSphere(v, n, t, new Vector3(0f, 1.6f, 0f), 0.15f, 8, 5); // hard hat, crown near 1.75 m
            AddBox(v, n, t, new Vector3(0.26f, 1.05f, 0.04f), new Vector3(0.1f, 0.4f, 0.1f), Quaternion.Euler(0f, 0f, -12f));
            return Bake("KH_Crew", v, n, t);
        }

        public static int CountVertices(Mesh m) => m == null ? 0 : m.vertexCount;

        static Mesh Wrench()
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            var rot = Quaternion.Euler(0f, 0f, 42f);
            AddBox(v, n, t, rot * new Vector3(0f, -0.05f, 0f), new Vector3(0.12f, 0.72f, 0.08f), rot);
            AddBox(v, n, t, rot * new Vector3(0f, 0.36f, 0f), new Vector3(0.42f, 0.12f, 0.08f), rot);
            AddBox(v, n, t, rot * new Vector3(-0.16f, 0.5f, 0f), new Vector3(0.1f, 0.22f, 0.08f), rot);
            AddBox(v, n, t, rot * new Vector3(0.16f, 0.5f, 0f), new Vector3(0.1f, 0.22f, 0.08f), rot);
            return Bake("KH_Wrench", v, n, t);
        }

        static Mesh Planet()
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            AddSphere(v, n, t, Vector3.zero, 0.32f, 16, 10);
            // Ring sits in front of the sphere, tilted so the camera (looking along +Z of the panel) sees it.
            var tilt = Quaternion.Euler(68f, 0f, 16f);
            Vector3 normal = tilt * Vector3.forward;
            int seg = 28;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                Vector3 p0 = tilt * new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f);
                Vector3 p1 = tilt * new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f);
                AddQuad(v, n, t, p0 * 0.48f, p1 * 0.48f, p0 * 0.64f, normal);
            }
            return Bake("KH_Planet", v, n, t);
        }

        static Mesh Book()
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            AddBox(v, n, t, new Vector3(-0.16f, 0f, 0f), new Vector3(0.28f, 0.4f, 0.06f), Quaternion.Euler(0f, 12f, 8f));
            AddBox(v, n, t, new Vector3(0.16f, 0f, 0f), new Vector3(0.28f, 0.4f, 0.06f), Quaternion.Euler(0f, -12f, -8f));
            return Bake("KH_Book", v, n, t);
        }

        static Mesh Gear()
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            AddBox(v, n, t, Vector3.zero, new Vector3(0.36f, 0.36f, 0.08f), Quaternion.identity);
            for (int i = 0; i < 8; i++)
            {
                var rot = Quaternion.Euler(0f, 0f, i * 45f);
                AddBox(v, n, t, rot * new Vector3(0f, 0.28f, 0f), new Vector3(0.12f, 0.16f, 0.08f), rot);
            }
            return Bake("KH_Gear", v, n, t);
        }

        static List<Vector2> CoreProfile()
        {
            var p = new List<Vector2>();
            float r = MainMenuLayout.VehicleDiameterM * 0.5f;
            p.Add(new Vector2(r * 0.72f, 0f));
            p.Add(new Vector2(r, 1.15f));
            for (float y = 1.15f; y < 36f; y += 4.4f)
            {
                p.Add(new Vector2(r, y));
                p.Add(new Vector2(r + 0.05f, y + 0.08f));
                p.Add(new Vector2(r + 0.05f, y + 0.22f));
                p.Add(new Vector2(r, y + 0.3f));
            }
            p.Add(new Vector2(r, 39f));
            p.Add(new Vector2(r * 0.9f, 41.6f));
            p.Add(new Vector2(r, 43.2f));
            p.Add(new Vector2(r, 57.2f));
            float fr = MainMenuLayout.FairingDiameterM * 0.5f;
            p.Add(new Vector2(fr * 0.82f, 58f));
            p.Add(new Vector2(fr, 59.2f));
            p.Add(new Vector2(fr, 64.6f));
            const int n = 16;
            for (int i = 1; i <= n; i++)
            {
                float t = i / (float)n;
                float ang = t * Mathf.PI * 0.5f;
                p.Add(new Vector2(Mathf.Max(0.04f, fr * Mathf.Cos(ang)), 64.6f + (MainMenuLayout.VehicleHeightM - 64.6f) * Mathf.Sin(ang)));
            }
            return p;
        }

        static List<Vector2> BoosterProfile()
        {
            var p = new List<Vector2>();
            float r = MainMenuLayout.VehicleDiameterM * 0.5f;
            p.Add(new Vector2(r * 0.7f, 0f));
            p.Add(new Vector2(r, 1.1f));
            for (float y = 1.1f; y < 32f; y += 5f)
            {
                p.Add(new Vector2(r, y));
                p.Add(new Vector2(r + 0.04f, y + 0.1f));
                p.Add(new Vector2(r, y + 0.28f));
            }
            p.Add(new Vector2(r, 36.5f));
            const int n = 10;
            for (int i = 1; i <= n; i++)
            {
                float t = i / (float)n;
                float ang = t * Mathf.PI * 0.5f;
                p.Add(new Vector2(Mathf.Max(0.04f, r * Mathf.Cos(ang)), 36.5f + 6.2f * Mathf.Sin(ang)));
            }
            return p;
        }

        static List<Vector2> BellProfile()
        {
            var p = new List<Vector2>();
            p.Add(new Vector2(0.55f, -1.55f));
            p.Add(new Vector2(0.48f, -1.15f));
            p.Add(new Vector2(0.28f, -0.55f));
            p.Add(new Vector2(0.16f, -0.15f));
            p.Add(new Vector2(0.2f, 0.05f));
            p.Add(new Vector2(0.12f, 0.2f));
            return p;
        }

        static Mesh LatheMesh(string name, List<Vector2> profile, Vector3 offset, Quaternion rot, int segments)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            Lathe(profile, offset, rot, segments, v, n, t);
            return Bake(name, v, n, t);
        }

        public static void Lathe(List<Vector2> profile, Vector3 offset, Quaternion rot, int segments,
            List<Vector3> verts, List<Vector3> normals, List<int> tris)
        {
            if (profile == null || profile.Count < 2) return;
            int seg = Mathf.Max(8, segments);
            int ring = seg + 1;
            int start = verts.Count;
            for (int i = 0; i < profile.Count; i++)
            {
                Vector2 a = profile[Mathf.Max(0, i - 1)], b = profile[Mathf.Min(profile.Count - 1, i + 1)];
                Vector2 tan = b - a;
                if (tan.sqrMagnitude < 1e-8f) tan = Vector2.up;
                tan.Normalize();
                Vector2 n2 = new Vector2(tan.y, -tan.x);
                for (int s = 0; s <= seg; s++)
                {
                    float ang = s * Mathf.PI * 2f / seg;
                    float c = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                    var local = new Vector3(profile[i].x * c, profile[i].y, profile[i].x * sn);
                    var ln = new Vector3(n2.x * c, n2.y, n2.x * sn);
                    verts.Add(offset + rot * local);
                    normals.Add((rot * ln).normalized);
                }
            }
            for (int i = 0; i < profile.Count - 1; i++)
                for (int s = 0; s < seg; s++)
                {
                    int a0 = start + i * ring + s, a1 = a0 + 1, b0 = a0 + ring, b1 = b0 + 1;
                    tris.Add(a0); tris.Add(b0); tris.Add(a1);
                    tris.Add(a1); tris.Add(b0); tris.Add(b1);
                }
        }

        public static void AddBox(List<Vector3> v, List<Vector3> n, List<int> t, Vector3 center, Vector3 size, Quaternion rot)
        {
            Vector3 hx = rot * new Vector3(size.x * 0.5f, 0f, 0f);
            Vector3 hy = rot * new Vector3(0f, size.y * 0.5f, 0f);
            Vector3 hz = rot * new Vector3(0f, 0f, size.z * 0.5f);
            AddQuad(v, n, t, center + hz - hx - hy, center + hz + hx - hy, center + hz - hx + hy, rot * Vector3.forward);
            AddQuad(v, n, t, center - hz - hx - hy, center - hz - hx + hy, center - hz + hx - hy, rot * Vector3.back);
            AddQuad(v, n, t, center + hy - hx - hz, center + hy + hx - hz, center + hy - hx + hz, rot * Vector3.up);
            AddQuad(v, n, t, center - hy - hx - hz, center - hy - hx + hz, center - hy + hx - hz, rot * Vector3.down);
            AddQuad(v, n, t, center + hx - hy - hz, center + hx - hy + hz, center + hx + hy - hz, rot * Vector3.right);
            AddQuad(v, n, t, center - hx - hy - hz, center - hx + hy - hz, center - hx - hy + hz, rot * Vector3.left);
        }

        /// <summary>Parallelogram a, b, c (d opposite). Winding is flipped so the geometric normal matches outward.</summary>
        public static void AddQuad(List<Vector3> v, List<Vector3> nrm, List<int> tri, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f) { var tmp = b; b = c; c = tmp; }
            Vector3 d = b + c - a;
            Vector3 normal = outward.sqrMagnitude > 1e-8f ? outward.normalized : Vector3.forward;
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            nrm.Add(normal); nrm.Add(normal); nrm.Add(normal); nrm.Add(normal);
            tri.Add(i); tri.Add(i + 1); tri.Add(i + 2);
            tri.Add(i + 1); tri.Add(i + 3); tri.Add(i + 2);
        }

        public static void AddSphere(List<Vector3> v, List<Vector3> n, List<int> t, Vector3 center, float radius, int seg, int rings)
        {
            seg = Mathf.Max(6, seg); rings = Mathf.Max(3, rings);
            int start = v.Count;
            for (int i = 0; i <= rings; i++)
            {
                float vt = i / (float)rings;
                float phi = vt * Mathf.PI;
                float y = Mathf.Cos(phi);
                float rr = Mathf.Sin(phi);
                for (int s = 0; s <= seg; s++)
                {
                    float u = s / (float)seg * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Cos(u) * rr, y, Mathf.Sin(u) * rr);
                    v.Add(center + dir * radius);
                    n.Add(dir.sqrMagnitude > 1e-8f ? dir.normalized : Vector3.up);
                }
            }
            int row = seg + 1;
            for (int i = 0; i < rings; i++)
                for (int s = 0; s < seg; s++)
                {
                    int a0 = start + i * row + s, a1 = a0 + 1, b0 = a0 + row, b1 = b0 + 1;
                    // a0->a1 is +u, a0->b0 is +phi. Cross(u, phi) points out; the other order faces inward.
                    t.Add(a0); t.Add(a1); t.Add(b0);
                    t.Add(a1); t.Add(b1); t.Add(b0);
                }
        }

        public static Mesh Bake(string name, List<Vector3> v, List<Vector3> n, List<int> t)
        {
            var m = new Mesh { name = name };
            if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
