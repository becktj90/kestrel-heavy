// Gulls at a real wingspan, worked along the shore south of the stack. A 70 m vehicle is dozens of
// these birds tall: the rocket reads as huge because the birds stay small.
//
// They are a skein with a gull planform (a few narrower, more swept birds in the same flock), a
// two-pose wingbeat, and a glide. Lighting stays a sky tint so a bird cannot collapse into a black
// speck. Ignition — the stack leaving the pad — flushes them. Above low altitude none remain.
using System;

namespace Kestrel.Site
{
    public struct BirdPose
    {
        public float X, Y, Z;
        public float Yaw;
        public float Pitch;
        public float Bank;
        /// <summary>Signed flap contribution, metres. 0 while gliding or when Reduce Motion holds the wings.</summary>
        public float Lift;
        /// <summary>1 on the glide pose, 0 in a full flap.</summary>
        public float Glide;
        /// <summary>Wrist angle, radians. Positive lifts the right wing; the left wing mirrors.</summary>
        public float WingMid;
        /// <summary>Wingtip angle, radians. Below the wrist this is the gull's bowed M.</summary>
        public float WingTip;
        /// <summary>Individual size. 1 is <see cref="BirdFlock.WingSpan"/>.</summary>
        public float Scale;
        /// <summary>1 is a broad gull wing. Above 0.5 the hand is narrower and more swept.</summary>
        public float Slender;
        /// <summary>0 hides the bird (feature flag, or the rocket has climbed out of the pad air).</summary>
        public float Presence;
    }

    public static class BirdFlock
    {
        public const int Count = 12;
        /// <summary>Full wingspan of one gull, metres. The core is a few of these across; the stack is dozens tall.</summary>
        public const float WingSpan = 1.05f;
        public const int VertsPerBird = 30;
        /// <summary>Stack altitude where the flush begins. The flock has no thrust signal; liftoff is the ignition it can see.</summary>
        public const float IgniteAlt = 1.2f;
        /// <summary>Fully visible through the flush, then faded out by <see cref="LeaveAlt"/>.</summary>
        public const float HoldAlt = 45f;
        /// <summary>Gone once the stack is above the pad air. Low: the birds never ride the rocket into the sky.</summary>
        public const float LeaveAlt = 140f;
        public const int LeftTip = 12;
        public const int RightTip = 21;
        public const int Beak = 0;
        public const int TailTip = 4;

        /// <summary>Master switch. On: the pad flock draws. Off: every pose is hidden and the view disables the mesh.</summary>
        public static bool Enabled = true;

        public static int VertexCount => Count * VertsPerBird;
        public static int IndicesPerBird => LocalTris.Length;
        public static int IndexCount => Count * LocalTris.Length;

        static readonly int[] LocalTris =
        {
            // Side profile, beak to tail.
            0, 1, 7,  1, 2, 7,  2, 7, 6,  2, 6, 3,  3, 6, 5,  3, 5, 4,
            // Left wing, shoulder to the pointed hand.
            8, 9, 16,  9, 15, 16,  9, 10, 15,  10, 14, 15,  10, 11, 14,  11, 13, 14,  11, 12, 13,
            // Right wing.
            17, 25, 18,  18, 25, 24,  18, 24, 19,  19, 24, 23,  19, 23, 20,  20, 23, 22,  20, 22, 21,
            // Tail wedge and a small chest so the front view has a body between the wings.
            3, 26, 4,  3, 4, 27,  26, 27, 4,
            1, 28, 29,  28, 7, 29,  0, 28, 1,  0, 7, 29,
        };

        /// <summary>1 on the pad, 0 once the rocket has left the low air the flock lives in.</summary>
        public static float Alongside(float altitude)
        {
            if (!Enabled) return 0f;
            if (altitude <= HoldAlt) return 1f;
            if (altitude >= LeaveAlt) return 0f;
            float u = (altitude - HoldAlt) / (LeaveAlt - HoldAlt);
            float s = u * u * (3f - 2f * u);
            return 1f - s;
        }

        /// <summary>0 on the pad, 1 once the stack has climbed far enough to flush the skein.</summary>
        public static float Flush(float altitude)
        {
            if (altitude <= IgniteAlt) return 0f;
            float u = (altitude - IgniteAlt) / 14f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return u * u * (3f - 2f * u);
        }

        /// <summary>
        /// Hold a gull's angular size once the chase camera is too far for a 1 m bird to read,
        /// without ever scaling one past twice life size (a 70 m stack stays dozens of gulls tall).
        /// </summary>
        public static float AppearScale(float distance)
        {
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance < 1f) distance = 1f;
            float s = distance * 0.0072f / WingSpan;
            if (s < 1f) return 1f;
            if (s > 2f) return 2f;
            return s;
        }

        /// <summary>1 while this bird holds the bowed glide, 0 in a flap. Staggered so the skein never beats together.</summary>
        public static float GlideAmount(int bird, float time, float flapScale, float flush)
        {
            if (flapScale <= 0f) return 1f;
            if (flush > 0.12f) return 0f;
            float u = time * 0.17f + bird * 0.137f;
            u -= (float)Math.Floor(u);
            const float open = 0.62f, shut = 0.90f, edge = 0.06f;
            if (u < open || u >= shut) return 0f;
            if (u < open + edge) return Smooth((u - open) / edge);
            if (u > shut - edge) return Smooth((shut - u) / edge);
            return 1f;
        }

        public static void Evaluate(float time, float rocketX, float rocketY, float flapScale, BirdPose[] into)
        {
            if (into == null || into.Length < Count) return;
            if (flapScale < 0f) flapScale = 0f;
            if (flapScale > 1f) flapScale = 1f;
            if (float.IsNaN(rocketY) || rocketY < 0f) rocketY = 0f;
            float presence = Alongside(rocketY);
            float flush = Flush(rocketY);
            var xs = new float[Count];
            var ys = new float[Count];
            var zs = new float[Count];
            var vxs = new float[Count];
            var vys = new float[Count];
            var vzs = new float[Count];
            for (int i = 0; i < Count; i++)
                Cruise(i, time, out xs[i], out ys[i], out zs[i], out vxs[i], out vys[i], out vzs[i]);
            Separate(xs, ys, zs);
            for (int i = 0; i < Count; i++)
            {
                float x = xs[i], y = ys[i], z = zs[i], vx = vxs[i], vy = vys[i], vz = vzs[i];
                float yaw = (float)Math.Atan2(vx, vz);
                float flee = -1.5707963f + (i - (Count - 1) * 0.5f) * 0.28f;
                if (flush > 0f)
                {
                    float dist = (18f + Hash(i) * 10f) * flush;
                    x += (float)Math.Cos(flee) * dist;
                    z += (float)Math.Sin(flee) * dist;
                    y += flush * (7f + Hash(i + 4) * 9f);
                    float fleeYaw = (float)Math.Atan2((float)Math.Cos(flee), (float)Math.Sin(flee));
                    yaw = LerpAngle(yaw, fleeYaw, flush);
                    vx = (float)Math.Sin(yaw);
                    vz = (float)Math.Cos(yaw);
                    vy += flush * 4f;
                }
                Cruise(i, time + 0.12f, out _, out _, out _, out float vx1, out _, out float vz1);
                float yaw1 = (float)Math.Atan2(vx1, vz1);
                float rate = DeltaAngle(yaw, yaw1) / 0.12f;
                float bank = -rate * 0.22f;
                if (bank < -0.55f) bank = -0.55f;
                if (bank > 0.55f) bank = 0.55f;
                float horiz = (float)Math.Sqrt(vx * vx + vz * vz);
                float pitch = (float)Math.Atan2(vy, horiz < 0.5f ? 0.5f : horiz);
                if (pitch < -0.35f) pitch = -0.35f;
                if (pitch > 0.5f) pitch = 0.5f;
                pitch = pitch + (0.42f - pitch) * flush * 0.85f;

                // A few degrees of individual heading so the skein is not a stamped V.
                if (flush < 1f) yaw += (Hash(i + 11) - 0.5f) * 0.26f * (1f - flush);
                // The chase camera sits due south. Pull the long axis across the frame so the wing
                // card is not edge-on; a full billboard would look pasted on, a mild crab does not.
                float face = Math.Abs(DeltaAngle(yaw, 0f)) <= Math.Abs(DeltaAngle(yaw, 3.14159265f)) ? 0f : 3.14159265f;
                yaw = LerpAngle(yaw, face, 0.7f);
                float glide = GlideAmount(i, time, flapScale, flush);
                float freq = 2.35f + Hash(i + 2) * 0.95f;
                if (flush > 0f) freq *= 1f + flush * 0.45f;
                float phase = (time * freq + Hash(i + 7)) * 6.2831853f;
                float wave = (float)Math.Sin(phase);
                float amp = (1f - glide) * flapScale;
                into[i] = new BirdPose
                {
                    X = x,
                    Y = y,
                    Z = z,
                    Yaw = yaw,
                    Pitch = pitch,
                    Bank = bank * (1f - flush),
                    Lift = wave * amp * 0.34f,
                    Glide = glide,
                    // Wrist high, hand drooped: the bowed M. The stroke lifts the hand much more than the arm.
                    WingMid = 0.50f + wave * 0.26f * amp,
                    WingTip = -0.10f + wave * 0.98f * amp,
                    Scale = 0.92f + Hash(i + 5) * 0.16f,
                    Slender = Hash(i + 9) > 0.72f ? 1f : 0f,
                    Presence = presence,
                };
            }
        }

        /// <summary>Sky-family vertex tint. Every channel stays well above black; the shader mixes this with the horizon.</summary>
        public static void VertexColor(int local, out float r, out float g, out float b)
        {
            if (local == LeftTip || local == RightTip) { r = 0.50f; g = 0.56f; b = 0.64f; return; }
            if (local == 11 || local == 13 || local == 20 || local == 22) { r = 0.66f; g = 0.72f; b = 0.78f; return; }
            if (local >= 8 && local <= 25) { r = 0.82f; g = 0.86f; b = 0.90f; return; }
            if (local == TailTip || local == 26 || local == 27) { r = 0.60f; g = 0.64f; b = 0.70f; return; }
            r = 0.94f; g = 0.95f; b = 0.93f;
        }

        public static void WriteBird(in BirdPose b, int vertex, float[] xyz, float[] normals)
        {
            if (xyz == null || vertex < 0 || vertex + VertsPerBird > xyz.Length / 3) return;
            if (b.Presence < 0.02f)
            {
                for (int i = 0; i < VertsPerBird; i++) Put(xyz, normals, vertex + i, b.X, b.Y, b.Z, 0f, 1f, 0f);
                return;
            }
            float scale = b.Scale > 0.2f ? b.Scale : 1f;
            var local = new float[VertsPerBird * 3];
            Shape(b, scale, local);
            float yaw = b.Yaw, pitch = b.Pitch, bank = b.Bank;
            float cb = (float)Math.Cos(bank), sb = (float)Math.Sin(bank);
            float cp = (float)Math.Cos(pitch), sp = (float)Math.Sin(pitch);
            float cy = (float)Math.Cos(yaw), sy = (float)Math.Sin(yaw);
            for (int i = 0; i < VertsPerBird; i++)
            {
                int o = i * 3;
                float x = local[o], y = local[o + 1], z = local[o + 2];
                float x1 = x * cb - y * sb;
                float y1 = x * sb + y * cb;
                float z1 = z;
                float y2 = y1 * cp - z1 * sp;
                float z2 = y1 * sp + z1 * cp;
                float x2 = x1;
                float wx = b.X + x2 * cy + z2 * sy;
                float wy = b.Y + y2;
                float wz = b.Z - x2 * sy + z2 * cy;
                xyz[(vertex + i) * 3] = wx;
                xyz[(vertex + i) * 3 + 1] = wy;
                xyz[(vertex + i) * 3 + 2] = wz;
            }
            if (normals == null) return;
            for (int i = 0; i < VertsPerBird * 3; i++) normals[(vertex * 3) + i] = 0f;
            for (int t = 0; t < LocalTris.Length; t += 3)
            {
                int ia = (vertex + LocalTris[t]) * 3;
                int ib = (vertex + LocalTris[t + 1]) * 3;
                int ic = (vertex + LocalTris[t + 2]) * 3;
                float ax = xyz[ib] - xyz[ia], ay = xyz[ib + 1] - xyz[ia + 1], az = xyz[ib + 2] - xyz[ia + 2];
                float bx = xyz[ic] - xyz[ia], by = xyz[ic + 1] - xyz[ia + 1], bz = xyz[ic + 2] - xyz[ia + 2];
                float nx = ay * bz - az * by, ny = az * bx - ax * bz, nz = ax * by - ay * bx;
                for (int k = 0; k < 3; k++)
                {
                    int iv = (vertex + LocalTris[t + k]) * 3;
                    normals[iv] += nx; normals[iv + 1] += ny; normals[iv + 2] += nz;
                }
            }
            for (int i = 0; i < VertsPerBird; i++)
            {
                int o = (vertex + i) * 3;
                float nx = normals[o], ny = normals[o + 1], nz = normals[o + 2];
                float m = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
                if (m < 1e-6f) { normals[o] = 0f; normals[o + 1] = 1f; normals[o + 2] = 0f; }
                else { normals[o] = nx / m; normals[o + 1] = ny / m; normals[o + 2] = nz / m; }
            }
        }

        public static void Indices(int[] into)
        {
            if (into == null || into.Length < IndexCount) return;
            int t = 0;
            for (int i = 0; i < Count; i++)
            {
                int b = i * VertsPerBird;
                for (int k = 0; k < LocalTris.Length; k++) into[t++] = b + LocalTris[k];
            }
        }

        static void Shape(in BirdPose b, float scale, float[] local)
        {
            float sl = b.Slender > 0.5f ? 1f : 0f;
            float chord = 1f - sl * 0.28f;
            float sweep = sl * 0.10f;
            // Side profile: beak, deep breast, short tail. x = 0 so a camera abeam sees the bird.
            V(local, 0, 0f, 0.030f, 0.220f);
            V(local, 1, 0f, 0.064f, 0.145f);
            V(local, 2, 0f, 0.070f, 0.015f);
            V(local, 3, 0f, 0.046f, -0.105f);
            V(local, 4, 0f, 0.022f, -0.205f);
            V(local, 5, 0f, 0.010f, -0.140f);
            V(local, 6, 0f, -0.018f, 0.010f);
            V(local, 7, 0f, 0.000f, 0.095f);
            Wing(local, 8, -1f, chord, sweep, b.WingMid, b.WingTip);
            Wing(local, 17, 1f, chord, sweep, b.WingMid, b.WingTip);
            V(local, 26, -0.055f * chord, 0.028f, -0.155f);
            V(local, 27, 0.055f * chord, 0.028f, -0.155f);
            V(local, 28, -0.040f, 0.028f, 0.090f);
            V(local, 29, 0.040f, 0.028f, 0.090f);
            for (int i = 0; i < VertsPerBird * 3; i++) local[i] *= scale;
        }

        /// <summary>
        /// Nine outline verts: leading edge shoulder→tip, then the trailing edge back.
        /// Chord is vertical, in the plane the south-facing chase camera looks at. A chord
        /// stored along the bird's nose is edge-on from that camera and collapses to a speck.
        /// </summary>
        static void Wing(float[] local, int v, float side, float chord, float sweep, float mid, float tip)
        {
            float s = side;
            // span, sweep z, vertical chord offset, blend (0 shoulder, 1 tip)
            // High aspect ratio: a gull's hand is a narrow ribbon, not a plank.
            Station(local, v + 0, s, 0.045f, 0.02f * chord, 0.042f * chord, 0.10f, mid, tip);
            Station(local, v + 1, s, 0.170f, 0.01f * chord, 0.040f * chord, 0.38f, mid, tip);
            Station(local, v + 2, s, 0.310f, -0.02f * chord, 0.028f * chord, 0.72f, mid, tip);
            Station(local, v + 3, s, 0.450f, -0.04f - sweep * 0.35f, 0.012f * chord, 0.90f, mid, tip);
            Station(local, v + 4, s, 0.525f, -0.06f - sweep, 0f, 1f, mid, tip);
            Station(local, v + 5, s, 0.440f, -0.055f - sweep * 0.35f, -0.010f * chord, 0.88f, mid, tip);
            Station(local, v + 6, s, 0.300f, -0.03f * chord, -0.032f * chord, 0.68f, mid, tip);
            Station(local, v + 7, s, 0.160f, 0.00f, -0.038f * chord, 0.34f, mid, tip);
            Station(local, v + 8, s, 0.045f, 0.01f * chord, -0.036f * chord, 0.10f, mid, tip);
        }

        static float WingAngle(float blend, float mid, float tip)
        {
            const float wrist = 0.72f;
            if (blend <= wrist) return mid * (blend / wrist);
            return mid + (tip - mid) * ((blend - wrist) / (1f - wrist));
        }

        static void Station(float[] local, int v, float side, float span, float z, float yBias, float blend, float mid, float tip)
        {
            float ang = WingAngle(blend, mid, tip) * side;
            // Positive angle lifts +X. The left wing passes side = -1 so its angle flips.
            float x = side * span;
            const float shoulder = 0.045f;
            float dx = x - side * shoulder;
            float dy = yBias;
            float c = (float)Math.Cos(ang), sn = (float)Math.Sin(ang);
            // Rotate around the bird's nose so the hand rises and the chord stays facing south.
            float xr = side * shoulder + dx * c - dy * sn;
            float yr = dy * c + dx * sn;
            V(local, v, xr, yr, z);
        }

        static void Cruise(int bird, float time, out float x, out float y, out float z, out float vx, out float vy, out float vz)
        {
            // One heading for the skein (the leader now). Positions trail it, so the V banks together.
            Leader(time, out _, out _, out _, out vx, out vy, out vz);
            float lag = bird * 0.11f;
            Leader(time - lag, out float lx, out float ly, out float lz, out _, out _, out _);
            float yaw = (float)Math.Atan2(vx, vz);
            float side = Skein(bird) + 0.28f * (float)Math.Sin(time * 0.74f + bird * 1.31f);
            float bob = 0.50f * (float)Math.Sin(time * 0.57f + bird * 0.97f);
            float rx = (float)Math.Cos(yaw);
            float rz = -(float)Math.Sin(yaw);
            x = lx + rx * side;
            z = lz + rz * side;
            y = ly + bob + (bird % 3) * 0.22f;
            // Slot offsets are perpendicular to travel, so the leader velocity is still the heading.
        }

        static void Leader(float time, out float x, out float y, out float z, out float vx, out float vy, out float vz)
        {
            // Mostly toward and away from the chase camera, which sits due south. That presents the
            // wingspan instead of an edge-on dash. A slower east-west drift keeps the line from being a rail.
            const float w = 0.74f;
            float s = (float)Math.Sin(w * time);
            float c = (float)Math.Cos(w * time);
            float s2 = (float)Math.Sin(0.27f * time + 0.6f);
            float c2 = (float)Math.Cos(0.27f * time + 0.6f);
            x = 6.5f * s2;
            z = -22f + 5.2f * s;
            y = 13.4f + 2.0f * (float)Math.Sin(0.19f * time + 0.4f) + 0.55f * s;
            vx = 6.5f * 0.27f * c2;
            vz = 5.2f * w * c;
            vy = 2.0f * 0.19f * (float)Math.Cos(0.19f * time + 0.4f) + 0.55f * w * c;
        }

        static void Separate(float[] x, float[] y, float[] z)
        {
            const float min = 1.75f;
            float min2 = min * min;
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 0; i < Count; i++)
                for (int j = i + 1; j < Count; j++)
                {
                    float dx = x[j] - x[i], dy = y[j] - y[i], dz = z[j] - z[i];
                    float d2 = dx * dx + dy * dy + dz * dz;
                    if (d2 >= min2 || d2 < 1e-8f) continue;
                    float d = (float)Math.Sqrt(d2);
                    float push = 0.5f * (min - d) / d;
                    x[i] -= dx * push; y[i] -= dy * push; z[i] -= dz * push;
                    x[j] += dx * push; y[j] += dy * push; z[j] += dz * push;
                }
            }
        }

        static float Skein(int bird)
        {
            if (bird == 0) return 0f;
            int row = (bird + 1) / 2;
            float sign = (bird % 2 == 0) ? 1f : -1f;
            return sign * (2.15f + row * 1.55f);
        }

        static void V(float[] a, int i, float x, float y, float z)
        {
            int o = i * 3;
            a[o] = x; a[o + 1] = y; a[o + 2] = z;
        }

        static void Put(float[] xyz, float[] normals, int vertex, float x, float y, float z, float nx, float ny, float nz)
        {
            int o = vertex * 3;
            xyz[o] = x; xyz[o + 1] = y; xyz[o + 2] = z;
            if (normals == null) return;
            normals[o] = nx; normals[o + 1] = ny; normals[o + 2] = nz;
        }

        static float Hash(int i)
        {
            uint n = (uint)(i * 374761393 + 668265263);
            n = (n ^ (n >> 13)) * 1274126177u;
            return (n & 0xFFFFFF) / 16777216f;
        }

        static float Smooth(float u)
        {
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return u * u * (3f - 2f * u);
        }

        static float DeltaAngle(float a, float b)
        {
            float d = b - a;
            while (d > 3.14159265f) d -= 6.2831853f;
            while (d < -3.14159265f) d += 6.2831853f;
            return d;
        }

        static float LerpAngle(float a, float b, float u)
        {
            return a + DeltaAngle(a, b) * u;
        }
    }
}
