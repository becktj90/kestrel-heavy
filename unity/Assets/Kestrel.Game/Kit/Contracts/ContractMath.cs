// Quaternion helpers for the umbilical frame (engine-free). Same (x, y, z, w) convention as UnityEngine.Quaternion.
using System;

namespace Kestrel.Kit.Contracts
{
    public static class ContractMath
    {
        public static float Len(Vec3 v) => (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);

        public static Vec3 Norm(Vec3 v)
        {
            float n = Len(v); return n < 1e-8f ? new Vec3(0, 0, 0) : new Vec3(v.X / n, v.Y / n, v.Z / n);
        }

        public static Vec3 Cross(Vec3 a, Vec3 b) => new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

        public static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        public static Vec3 Add(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vec3 Scale(Vec3 a, float s) => new Vec3(a.X * s, a.Y * s, a.Z * s);

        public static Vec3 Lerp(Vec3 a, Vec3 b, float t) => new Vec3(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);

        /// <summary>Yaw about +Y, degrees. +90 takes +Z toward +X. Matches <c>Quaternion.Euler(0, deg, 0)</c>.</summary>
        public static Quat Yaw(float deg)
        {
            double h = deg * Math.PI / 360.0;
            return new Quat(0, (float)Math.Sin(h), 0, (float)Math.Cos(h));
        }

        public static Quat Mul(Quat a, Quat b) => new Quat(
            a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
            a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
            a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
            a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);

        public static Vec3 Rotate(Quat q, Vec3 v)
        {
            float x = q.X, y = q.Y, z = q.Z, w = q.W;
            float tx = 2 * (y * v.Z - z * v.Y), ty = 2 * (z * v.X - x * v.Z), tz = 2 * (x * v.Y - y * v.X);
            return new Vec3(
                v.X + w * tx + (y * tz - z * ty),
                v.Y + w * ty + (z * tx - x * tz),
                v.Z + w * tz + (x * ty - y * tx));
        }

        /// <summary>Rotation whose local +Z is <paramref name="forward"/> and whose local +Y is as close as possible to <paramref name="up"/>.</summary>
        public static Quat LookRotation(Vec3 forward, Vec3 up)
        {
            var f = Norm(forward);
            var r = Norm(Cross(up, f));
            if (Len(r) < 1e-6f)
                r = Math.Abs(f.Y) > 0.9f ? new Vec3(1, 0, 0) : Norm(Cross(new Vec3(0, 1, 0), f));
            var u = Cross(f, r);
            return FromAxes(r, u, f);
        }

        /// <summary>Rotate <paramref name="p"/> about <paramref name="axis"/> through <paramref name="pivot"/> by <paramref name="deg"/> degrees (right-handed).</summary>
        public static Vec3 RotateAbout(Vec3 pivot, Vec3 axis, Vec3 p, float deg)
        {
            var n = Norm(axis);
            if (Len(n) < 1e-6f) return p;
            double a = deg * Math.PI / 180.0;
            float c = (float)Math.Cos(a), s = (float)Math.Sin(a);
            var v = new Vec3(p.X - pivot.X, p.Y - pivot.Y, p.Z - pivot.Z);
            var cr = Cross(n, v);
            float d = Dot(n, v);
            return new Vec3(
                pivot.X + v.X * c + cr.X * s + n.X * d * (1 - c),
                pivot.Y + v.Y * c + cr.Y * s + n.Y * d * (1 - c),
                pivot.Z + v.Z * c + cr.Z * s + n.Z * d * (1 - c));
        }

        static Quat FromAxes(Vec3 x, Vec3 y, Vec3 z)
        {
            float m00 = x.X, m01 = y.X, m02 = z.X, m10 = x.Y, m11 = y.Y, m12 = z.Y, m20 = x.Z, m21 = y.Z, m22 = z.Z;
            float tr = m00 + m11 + m22, qx, qy, qz, qw;
            if (tr > 0f)
            {
                float s = (float)Math.Sqrt(tr + 1f) * 2f;
                qw = 0.25f * s; qx = (m21 - m12) / s; qy = (m02 - m20) / s; qz = (m10 - m01) / s;
            }
            else if (m00 > m11 && m00 > m22)
            {
                float s = (float)Math.Sqrt(1f + m00 - m11 - m22) * 2f;
                qw = (m21 - m12) / s; qx = 0.25f * s; qy = (m01 + m10) / s; qz = (m02 + m20) / s;
            }
            else if (m11 > m22)
            {
                float s = (float)Math.Sqrt(1f + m11 - m00 - m22) * 2f;
                qw = (m02 - m20) / s; qx = (m01 + m10) / s; qy = 0.25f * s; qz = (m12 + m21) / s;
            }
            else
            {
                float s = (float)Math.Sqrt(1f + m22 - m00 - m11) * 2f;
                qw = (m10 - m01) / s; qx = (m02 + m20) / s; qy = (m12 + m21) / s; qz = 0.25f * s;
            }
            return new Quat(qx, qy, qz, qw);
        }
    }
}
