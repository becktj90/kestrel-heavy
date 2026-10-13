// Interstage stringers, joint rings, and tank raceways. All of them are the same unit tube
// (BayMesh.OpenCylinder), scaled and aimed per instance, so a vehicle is one draw.
using System;
using System.Collections.Generic;

namespace Kestrel.Vehicle
{
    /// <summary>One instanced tube. Unit mesh is radius 1, height 1, centred, +Y along the axis.</summary>
    public readonly struct StrutPose
    {
        public readonly string Kind;
        public readonly float X, Y, Z, Radius, Length;
        public readonly float AxisX, AxisY, AxisZ;
        public StrutPose(string kind, float x, float y, float z, float radius, float length, float ax, float ay, float az)
        { Kind = kind; X = x; Y = y; Z = z; Radius = radius; Length = length; AxisX = ax; AxisY = ay; AxisZ = az; }
    }

    public static class AirframeLayout
    {
        public const string Stringer = "stringer";
        public const string JointRing = "joint-ring";
        public const string Raceway = "raceway";
        public const int StringerCount = 8;
        public const float Proud = 0.04f;
        public const float StringerRadius = 0.02f;
        public const float RacewayRadius = 0.03f;
        public const float RingHeight = 0.07f;

        /// <summary>LOD2 drops the stringers. Rings and raceways stay, they are the silhouette.</summary>
        public static bool Draws(string kind, int lod) => !(lod >= 2 && kind == Stringer);

        public static int Sides(int lod) => lod <= 0 ? 6 : 4;

        public static List<StrutPose> Interstage(float y0, float y1, float r0, float r1)
        {
            var list = new List<StrutPose>();
            if (!(y1 > y0 + 0.05f) || !(r0 > 0.05f) || !(r1 > 0.05f)) return list;
            list.Add(Ring(y0 + RingHeight * 0.5f, r0));
            list.Add(Ring(y1 - RingHeight * 0.5f, r1));
            for (int i = 0; i < StringerCount; i++)
            {
                double a = 2 * Math.PI * i / StringerCount;
                float sa = (float)Math.Sin(a), ca = (float)Math.Cos(a);
                float rb = r0 + Proud + StringerRadius, rt = r1 + Proud + StringerRadius;
                float x0 = rb * sa, z0 = rb * ca, x1 = rt * sa, z1 = rt * ca;
                float dx = x1 - x0, dy = y1 - y0, dz = z1 - z0;
                float len = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (len < 1e-3f) continue;
                list.Add(new StrutPose(Stringer, (x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f,
                    StringerRadius, len, dx / len, dy / len, dz / len));
            }
            return list;
        }

        public static StrutPose RacewayRun(float ox, float oy, float oz, float tankRadius, float barrelBottom, float barrelLength)
        {
            float x = ox;
            float z = oz + tankRadius + Proud + RacewayRadius;
            float y = oy + barrelBottom + barrelLength * 0.5f;
            return new StrutPose(Raceway, x, y, z, RacewayRadius, barrelLength, 0f, 1f, 0f);
        }

        /// <summary>Gap between the tank skin and the near side of the raceway tube.</summary>
        public static float RacewayClearance(float tankRadius, StrutPose run, float ox, float oz)
        {
            float dx = run.X - ox, dz = run.Z - oz;
            float dist = (float)Math.Sqrt(dx * dx + dz * dz);
            return dist - run.Radius - tankRadius;
        }

        public static int TriangleCount(IReadOnlyList<StrutPose> poses, int lod)
        {
            if (poses == null || poses.Count == 0) return 0;
            int tris = BayMesh.OpenCylinder(Sides(lod)).TriangleCount;
            int n = 0;
            for (int i = 0; i < poses.Count; i++) if (Draws(poses[i].Kind, lod)) n++;
            return n * tris;
        }

        /// <summary>Every strut shares one tube mesh.</summary>
        public static int DrawCalls(IReadOnlyList<StrutPose> poses, int lod)
        {
            if (poses == null) return 0;
            for (int i = 0; i < poses.Count; i++) if (Draws(poses[i].Kind, lod)) return 1;
            return 0;
        }

        static StrutPose Ring(float y, float radius)
        {
            return new StrutPose(JointRing, 0f, y, 0f, radius + Proud, RingHeight, 0f, 1f, 0f);
        }
    }
}
