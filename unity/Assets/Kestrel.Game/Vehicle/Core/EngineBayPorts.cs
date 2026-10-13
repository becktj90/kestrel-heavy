// Tail-service plates on the aft skirt (docs/VEHICLE_DETAIL_V1_SPEC.md §5).
// Ids come from UmbilicalIds. Stage 0 is the first stage. +Z of each pose is the outward skin normal.
using System;
using System.Collections.Generic;
using Kestrel.Kit.Contracts;

namespace Kestrel.Vehicle
{
    public struct BayHardware
    {
        public bool Liquid;
        public bool PumpFed;
        public bool Gimbal;
        public ReleaseStyle Release;
    }

    public static class EngineBayPorts
    {
        public static List<AttachPoint> Build(int stage, float skinRadius, float bayHeight, BayHardware hw)
        {
            if (stage < 0) throw new ArgumentException("stage");
            if (!(skinRadius > 0.05f)) throw new ArgumentException("skinRadius");
            if (!(bayHeight > 0.05f)) throw new ArgumentException("bayHeight");
            var list = new List<AttachPoint>();
            if (!hw.Liquid)
            {
                Add(list, stage, UmbilicalType.PowerData, 0.85f, -20f, 0.15f, skinRadius, bayHeight, ReleaseStyle.LanyardPull);
                if (hw.Gimbal) Add(list, stage, UmbilicalType.Hydraulic, 0.40f, 20f, 0.12f, skinRadius, bayHeight, ReleaseStyle.LanyardPull);
                return list;
            }
            float scale = Clamp(skinRadius * 2f / 3.7f, 0.6f, 1.6f);
            Add(list, stage, UmbilicalType.Fuel, 0.60f, 0f, 0.25f * scale, skinRadius, bayHeight, hw.Release);
            Add(list, stage, UmbilicalType.Oxidizer, 0.60f, 20f, 0.25f * scale, skinRadius, bayHeight, hw.Release);
            Add(list, stage, UmbilicalType.PowerData, 0.85f, -20f, 0.15f, skinRadius, bayHeight, hw.Release);
            if (hw.PumpFed) Add(list, stage, UmbilicalType.Purge, 0.40f, 0f, 0.12f, skinRadius, bayHeight, hw.Release);
            if (hw.Gimbal) Add(list, stage, UmbilicalType.Hydraulic, 0.40f, 20f, 0.12f, skinRadius, bayHeight, hw.Release);
            return list;
        }

        static void Add(List<AttachPoint> list, int stage, UmbilicalType type, float yFrac, float azDeg, float plate, float radius, float bay, ReleaseStyle release)
        {
            float az = azDeg * (float)(Math.PI / 180.0);
            float x = radius * (float)Math.Sin(az);
            float z = radius * (float)Math.Cos(az);
            float y = yFrac * bay;
            var pose = new LocalPose(new Vec3(x, y, z), PoseMath.LookOutward(x, z));
            list.Add(new AttachPoint
            {
                Id = UmbilicalIds.Make(stage, type, 0),
                Type = type,
                Stage = stage,
                Pose = pose,
                PlateRadius = plate,
                Release = release
            });
        }

        static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
    }

    /// <summary>Unity's LookRotation for a horizontal outward normal and world up, without UnityEngine.</summary>
    public static class PoseMath
    {
        public static Quat LookOutward(float x, float z)
        {
            float fl = (float)Math.Sqrt(x * x + z * z);
            if (fl < 1e-8f) return Quat.Identity;
            float fx = x / fl, fy = 0f, fz = z / fl;
            // right = cross(up, forward), up = (0,1,0) → (fz, 0, -fx)
            float rx = fz, ry = 0f, rz = -fx;
            // up' = cross(forward, right)
            float ux = fy * rz - fz * ry, uy = fz * rx - fx * rz, uz = fx * ry - fy * rx;
            return FromAxes(rx, ry, rz, ux, uy, uz, fx, fy, fz);
        }

        public static void RotateZ(Quat q, out float x, out float y, out float z)
        {
            float qx = q.X, qy = q.Y, qz = q.Z, qw = q.W;
            float tx = 2f * qy, ty = -2f * qx, tz = 0f;
            float cx = qy * tz - qz * ty, cy = qz * tx - qx * tz, cz = qx * ty - qy * tx;
            x = qw * tx + cx;
            y = qw * ty + cy;
            z = 1f + qw * tz + cz;
        }

        static Quat FromAxes(float r0, float r1, float r2, float u0, float u1, float u2, float f0, float f1, float f2)
        {
            float m00 = r0, m01 = u0, m02 = f0;
            float m10 = r1, m11 = u1, m12 = f1;
            float m20 = r2, m21 = u2, m22 = f2;
            float tr = m00 + m11 + m22;
            float x, y, z, w;
            if (tr > 0f)
            {
                float s = (float)Math.Sqrt(tr + 1f) * 2f;
                w = 0.25f * s; x = (m21 - m12) / s; y = (m02 - m20) / s; z = (m10 - m01) / s;
            }
            else if (m00 > m11 && m00 > m22)
            {
                float s = (float)Math.Sqrt(1f + m00 - m11 - m22) * 2f;
                w = (m21 - m12) / s; x = 0.25f * s; y = (m01 + m10) / s; z = (m02 + m20) / s;
            }
            else if (m11 > m22)
            {
                float s = (float)Math.Sqrt(1f + m11 - m00 - m22) * 2f;
                w = (m02 - m20) / s; x = (m01 + m10) / s; y = 0.25f * s; z = (m12 + m21) / s;
            }
            else
            {
                float s = (float)Math.Sqrt(1f + m22 - m00 - m11) * 2f;
                w = (m10 - m01) / s; x = (m02 + m20) / s; y = (m12 + m21) / s; z = 0.25f * s;
            }
            return new Quat(x, y, z, w);
        }
    }
}
