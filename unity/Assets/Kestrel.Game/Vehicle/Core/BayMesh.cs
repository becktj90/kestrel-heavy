// Canonical engine-bay meshes. One mesh per part kind (shared by every engine), scaled on the instance.
// Lines are a 6-side open tube. Pumps, preburners and valves are short lathes. LOD drops valves, then
// everything except the turbopump and one feed line, so the bay stays inside the engine-bay triangle cap.
using System;
using System.Collections.Generic;

namespace Kestrel.Vehicle
{
    public static class BayMesh
    {
        /// <summary>LOD0 keeps every kind. LOD1 drops valves. LOD2 keeps the turbopump and the feed line.</summary>
        public static bool Draws(string kind, int lod)
        {
            if (string.IsNullOrEmpty(kind)) return false;
            if (lod >= 2) return kind == EngineBayLayout.Turbopump || kind == EngineBayLayout.FeedLine;
            if (lod == 1) return kind != EngineBayLayout.Valve;
            return true;
        }

        public static int Sides(string kind, int lod)
        {
            bool tube = IsTube(kind);
            if (tube) return lod <= 0 ? 6 : 4;
            return lod <= 0 ? 10 : 6;
        }

        public static bool IsTube(string kind) =>
            kind == EngineBayLayout.FeedLine || kind == EngineBayLayout.ExhaustDuct || kind == EngineBayLayout.Actuator;

        public static string Key(string kind, int lod) => "bay/" + kind + "/lod" + Math.Max(0, lod);

        public static MeshData Build(string kind, int lod)
        {
            if (!Draws(kind, lod)) return new MeshData();
            int sides = Sides(kind, lod);
            switch (kind)
            {
                case EngineBayLayout.Turbopump: return LatheMesh.Build(Pump(), sides);
                case EngineBayLayout.Preburner: return LatheMesh.Build(Capsule(), sides);
                case EngineBayLayout.Valve: return LatheMesh.Build(Sphere(), sides);
                default: return OpenCylinder(sides);
            }
        }

        /// <summary>Unit open cylinder, radius 1, height 1, centred on the origin, +Y along the axis. No caps.</summary>
        public static MeshData OpenCylinder(int sides)
        {
            var prof = new List<ProfilePoint>
            {
                new ProfilePoint(1f, -0.5f, 1f, 0f, 0f),
                new ProfilePoint(1f, 0.5f, 1f, 0f, 1f)
            };
            return LatheMesh.Build(prof, sides);
        }

        public static int TriangleCount(string kind, int lod) => Build(kind, lod).TriangleCount;

        /// <summary>Triangles drawn for these parts at <paramref name="lod"/> (mesh tris × instances).</summary>
        public static int TriangleCount(IReadOnlyList<BayPart> parts, int lod)
        {
            int n = 0;
            if (parts == null) return 0;
            for (int i = 0; i < parts.Count; i++) n += TriangleCount(parts[i].Kind, lod);
            return n;
        }

        /// <summary>One batch per visible kind. Engines share the mesh, so this does not grow with engine count.</summary>
        public static int DrawCalls(IReadOnlyList<BayPart> parts, int lod)
        {
            if (parts == null || parts.Count == 0) return 0;
            var seen = new HashSet<string>();
            for (int i = 0; i < parts.Count; i++)
                if (Draws(parts[i].Kind, lod)) seen.Add(parts[i].Kind);
            return seen.Count;
        }

        static List<ProfilePoint> Pump() => Smooth(new[]
        {
            0f, 0.42f, 0.62f, 1f, 1f, 0.50f, 0.42f, 0f
        }, new[]
        {
            -0.5f, -0.38f, -0.18f, -0.05f, 0.14f, 0.28f, 0.42f, 0.5f
        });

        static List<ProfilePoint> Capsule() => Smooth(new[]
        {
            0f, 0.55f, 1f, 1f, 0.70f, 0f
        }, new[]
        {
            -0.5f, -0.32f, -0.12f, 0.18f, 0.36f, 0.5f
        });

        static List<ProfilePoint> Sphere() => Smooth(new[]
        {
            0f, 0.50f, 0.87f, 1f, 0.87f, 0.50f, 0f
        }, new[]
        {
            -0.5f, -0.35f, -0.20f, 0f, 0.20f, 0.35f, 0.5f
        });

        /// <summary>Outward meridian normal from the neighbouring knots. Profiles are unit: max R = 1, Y in [-0.5, 0.5].</summary>
        public static List<ProfilePoint> Smooth(float[] r, float[] y)
        {
            int n = r.Length;
            var list = new List<ProfilePoint>(n);
            float v = 0f;
            for (int i = 0; i < n; i++)
            {
                int a = Math.Max(0, i - 1), b = Math.Min(n - 1, i + 1);
                float dr = r[b] - r[a], dy = y[b] - y[a];
                float nr = dy, ny = -dr;
                float m = (float)Math.Sqrt(nr * nr + ny * ny);
                if (m < 1e-6f) { nr = 1f; ny = 0f; m = 1f; }
                nr /= m; ny /= m;
                if (i > 0)
                {
                    float dR = r[i] - r[i - 1], dY = y[i] - y[i - 1];
                    v += (float)Math.Sqrt(dR * dR + dY * dY);
                }
                list.Add(new ProfilePoint(r[i], y[i], nr, ny, v));
            }
            return list;
        }
    }
}
