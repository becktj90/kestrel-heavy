// Gulls at a real wingspan, flown beside the stack. A 70 m vehicle is dozens of these birds tall,
// which is the scale cue: the rocket reads as huge because the birds stay small.
using System;

namespace Kestrel.Site
{
    public struct BirdPose
    {
        public float X, Y, Z;
        public float Yaw;
        public float Lift;
    }

    public static class BirdFlock
    {
        public const int Count = 12;
        /// <summary>Full wingspan of one gull, metres. The core is a few of these across; the stack is dozens tall.</summary>
        public const float WingSpan = 1.05f;
        public const float WingChord = 0.34f;
        public const int VertsPerBird = 16;
        public const float HoldAlt = 280f;
        public const float LeaveAlt = 1100f;

        public static int VertexCount => Count * VertsPerBird;

        /// <summary>1 while the flock is still beside the vehicle, 0 once the rocket has climbed away from them.</summary>
        public static float Alongside(float altitude)
        {
            if (altitude <= HoldAlt) return 1f;
            if (altitude >= LeaveAlt) return 0f;
            float u = (altitude - HoldAlt) / (LeaveAlt - HoldAlt);
            float s = u * u * (3f - 2f * u);
            return 1f - s;
        }

        public static void Evaluate(float time, float rocketX, float rocketY, float flapScale, BirdPose[] into)
        {
            if (into == null || into.Length < Count) return;
            if (flapScale < 0f) flapScale = 0f;
            if (flapScale > 1f) flapScale = 1f;
            float follow = Alongside(rocketY);
            for (int i = 0; i < Count; i++)
            {
                float lane = Count == 1 ? 0.5f : i / (float)(Count - 1);
                // South of the stack, the side the chase camera watches, clear of the tower on +Z.
                float ang = -2.2f + lane * 1.6f;
                float spin = 0.55f + (i % 3) * 0.12f;
                float orbit = ang + (float)Math.Sin(time * spin) * 0.22f;
                float radius = 8.6f + (i % 4) * 1.7f;
                float along = 8f + lane * 46f;
                float x = rocketX * follow + (float)Math.Cos(orbit) * radius;
                float z = (float)Math.Sin(orbit) * radius;
                float y = along + rocketY * follow;
                float phase = time * (5.5f + (i % 3)) + i * 0.7f;
                into[i] = new BirdPose
                {
                    X = x,
                    Y = y,
                    Z = z,
                    Yaw = orbit + 1.5707963f,
                    Lift = (float)Math.Sin(phase) * 0.22f * flapScale,
                };
            }
        }

        /// <summary>
        /// Sixteen vertices: a left wing, a right wing, then the same quads facing the other way.
        /// Wings stand upright so a camera beside the rocket sees the span, not an edge.
        /// </summary>
        public static void WriteBird(in BirdPose b, int vertex, float[] xyz, float[] normals)
        {
            if (xyz == null || vertex < 0 || vertex + VertsPerBird > xyz.Length / 3) return;
            float yaw = b.Yaw;
            float rx = (float)Math.Cos(yaw), rz = (float)Math.Sin(yaw);
            float half = WingSpan * 0.5f;
            WriteWing(b, vertex, xyz, normals, rx, rz, -half, b.Lift);
            WriteWing(b, vertex + 4, xyz, normals, rx, rz, half, b.Lift);
            int back = (vertex + 8) * 3;
            int front = vertex * 3;
            for (int i = 0; i < 24; i++) xyz[back + i] = xyz[front + i];
            if (normals != null)
                for (int i = 0; i < 24; i++) normals[back + i] = -normals[front + i];
        }

        static void WriteWing(in BirdPose b, int vertex, float[] xyz, float[] normals, float rx, float rz, float reach, float lift)
        {
            float chord = WingChord * 0.5f;
            float x0 = b.X, y0 = b.Y, z0 = b.Z;
            float x1 = b.X + rx * reach, y1 = b.Y + lift, z1 = b.Z + rz * reach;
            Put(xyz, normals, vertex + 0, x0, y0 - chord, z0, 0f, 0f, -1f);
            Put(xyz, normals, vertex + 1, x0, y0 + chord, z0, 0f, 0f, -1f);
            Put(xyz, normals, vertex + 2, x1, y1 + chord, z1, 0f, 0f, -1f);
            Put(xyz, normals, vertex + 3, x1, y1 - chord, z1, 0f, 0f, -1f);
        }

        static void Put(float[] xyz, float[] normals, int vertex, float x, float y, float z, float nx, float ny, float nz)
        {
            int o = vertex * 3;
            xyz[o] = x; xyz[o + 1] = y; xyz[o + 2] = z;
            if (normals == null) return;
            normals[o] = nx; normals[o + 1] = ny; normals[o + 2] = nz;
        }

        public static void Indices(int[] into)
        {
            if (into == null || into.Length < Count * 24) return;
            int t = 0;
            for (int i = 0; i < Count; i++)
            {
                Quad(into, ref t, i * VertsPerBird, false);
                Quad(into, ref t, i * VertsPerBird + 4, false);
                Quad(into, ref t, i * VertsPerBird + 8, true);
                Quad(into, ref t, i * VertsPerBird + 12, true);
            }
        }

        static void Quad(int[] into, ref int t, int v, bool flip)
        {
            if (!flip)
            {
                into[t++] = v; into[t++] = v + 1; into[t++] = v + 2;
                into[t++] = v; into[t++] = v + 2; into[t++] = v + 3;
            }
            else
            {
                into[t++] = v; into[t++] = v + 2; into[t++] = v + 1;
                into[t++] = v; into[t++] = v + 3; into[t++] = v + 2;
            }
        }
    }
}
