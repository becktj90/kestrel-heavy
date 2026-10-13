// Engine-bay parts in the bell's frame: exit at Y = 0, gimbal at the top of the chamber, +Y up.
// Counts follow the cycle (docs/VEHICLE_DETAIL_V1_SPEC.md §3). Positions are deterministic per
// (engine type, vehicle seed) so a cluster can share one layout. No real-engine plumbing.
using System;
using System.Collections.Generic;

namespace Kestrel.Vehicle
{
    public readonly struct BayPart
    {
        public readonly string Kind;
        public readonly float X, Y, Z;
        public BayPart(string kind, float x, float y, float z) { Kind = kind; X = x; Y = y; Z = z; }
    }

    public static class EngineBayLayout
    {
        public const string Turbopump = "turbopump";
        public const string Preburner = "preburner";
        public const string ExhaustDuct = "exhaust-duct";
        public const string FeedLine = "feed-line";
        public const string Valve = "valve";
        public const string Actuator = "actuator";

        public static int TurbopumpCount(EngineCycleKind cycle)
        {
            switch (cycle)
            {
                case EngineCycleKind.FullFlow: return 2;
                case EngineCycleKind.PressureFed:
                case EngineCycleKind.Solid:
                case EngineCycleKind.Hybrid: return 0;
                default: return 1;   // gas-generator, staged-combustion, expander
            }
        }

        public static int PreburnerCount(EngineCycleKind cycle)
        {
            switch (cycle)
            {
                case EngineCycleKind.GasGenerator:
                case EngineCycleKind.StagedCombustion: return 1;
                case EngineCycleKind.FullFlow: return 2;
                default: return 0;
            }
        }

        public static int ExhaustDuctCount(EngineCycleKind cycle) => cycle == EngineCycleKind.GasGenerator ? 1 : 0;

        public static int FeedLineCount(EngineCycleKind cycle)
        {
            if (cycle == EngineCycleKind.Solid) return 0;
            if (cycle == EngineCycleKind.Hybrid) return 1;
            return 2;
        }

        /// <summary>Parts for one engine. <paramref name="skinRadius"/> is the aft-skirt radius; every part stays inside it.</summary>
        public static List<BayPart> Build(BellSpec spec, bool gimbal, float skinRadius)
        {
            if (!(skinRadius > 0.05f)) throw new ArgumentException("skinRadius");
            var problems = spec.Validate();
            if (problems.Count > 0) throw new ArgumentException(string.Join("; ", problems));

            var contour = BellProfile.Build(spec, 2);
            float yTop = contour.Length;
            float yMid = contour.ThroatY + (yTop - contour.ThroatY) * 0.45f;
            if (yMid < contour.ThroatY) yMid = contour.ThroatY;
            if (yMid > yTop) yMid = yTop;

            ulong h = BellVariation.Mix(spec.VehicleSeed ^ BellVariation.HashId(spec.EngineId) ^ (ulong)spec.Cycle);
            float az = (h % 4) * (float)(Math.PI / 2.0);
            float reach = skinRadius * 0.42f;
            var list = new List<BayPart>();

            int pumps = TurbopumpCount(spec.Cycle);
            for (int i = 0; i < pumps; i++)
            {
                float a = az + (pumps == 2 ? (i == 0 ? -0.9f : 0.9f) : 0f);
                Place(list, Turbopump, a, reach, yMid);
            }
            int pres = PreburnerCount(spec.Cycle);
            for (int i = 0; i < pres; i++)
            {
                float a = az + 0.6f + i * 0.8f;
                Place(list, Preburner, a, reach * 0.85f, yMid + (yTop - yMid) * 0.35f);
            }
            if (ExhaustDuctCount(spec.Cycle) == 1)
                Place(list, ExhaustDuct, az + 1.4f, reach, yMid * 0.72f);
            int feeds = FeedLineCount(spec.Cycle);
            for (int i = 0; i < feeds; i++)
            {
                float a = az + (i == 0 ? 0.35f : -0.35f);
                Place(list, FeedLine, a, reach * 0.7f, (yMid + yTop) * 0.5f);
            }
            int valves = spec.Cycle == EngineCycleKind.Solid ? 0 : 2;
            for (int i = 0; i < valves; i++)
                Place(list, Valve, az + 2.2f + i * 0.4f, reach * 0.55f, yTop * 0.92f);
            if (gimbal)
            {
                Place(list, Actuator, az + 1.1f, reach * 0.9f, yTop * 0.88f);
                Place(list, Actuator, az - 1.1f, reach * 0.9f, yTop * 0.88f);
            }
            return list;
        }

        public static int Count(IReadOnlyList<BayPart> parts, string kind)
        {
            int n = 0;
            for (int i = 0; i < parts.Count; i++) if (parts[i].Kind == kind) n++;
            return n;
        }

        /// <summary>Instance pose. The mesh is unit (radius 1, height 1, centred). Scale (Radius, Length, Radius)
        /// and aim +Y along the axis. The centre stays inside the skirt.</summary>
        public readonly struct BayPose
        {
            public readonly string Kind;
            public readonly float X, Y, Z;
            public readonly float Radius, Length;
            public readonly float AxisX, AxisY, AxisZ;
            public BayPose(string kind, float x, float y, float z, float radius, float length, float ax, float ay, float az)
            { Kind = kind; X = x; Y = y; Z = z; Radius = radius; Length = length; AxisX = ax; AxisY = ay; AxisZ = az; }
        }

        public static List<BayPose> Poses(BellSpec spec, bool gimbal, float skinRadius)
        {
            var parts = Build(spec, gimbal, skinRadius);
            float bay = BellProfile.Build(spec, 2).Length;
            var list = new List<BayPose>(parts.Count);
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                Size(p.Kind, skinRadius, bay, out float radius, out float length);
                Axis(p.Kind, p.X, p.Z, out float ax, out float ay, out float az);
                list.Add(new BayPose(p.Kind, p.X, p.Y, p.Z, radius, length, ax, ay, az));
            }
            return list;
        }

        /// <summary>Metres. Kept small relative to the skirt so the part, including its half-length, stays inside the skin.</summary>
        public static void Size(string kind, float skinRadius, float bayHeight, out float radius, out float length)
        {
            float h = Math.Max(0.2f, bayHeight);
            switch (kind)
            {
                case Turbopump: radius = 0.10f * skinRadius; length = Math.Min(0.28f * h, 0.36f * skinRadius); break;
                case Preburner: radius = 0.065f * skinRadius; length = Math.Min(0.18f * h, 0.24f * skinRadius); break;
                case ExhaustDuct: radius = 0.040f * skinRadius; length = Math.Min(0.32f * h, 0.40f * skinRadius); break;
                case FeedLine: radius = 0.025f * skinRadius; length = Math.Min(0.40f * h, 0.50f * skinRadius); break;
                case Valve: radius = 0.045f * skinRadius; length = Math.Min(0.09f * h, 0.12f * skinRadius); break;
                case Actuator: radius = 0.020f * skinRadius; length = 0.28f * skinRadius; break;
                default: radius = 0.04f * skinRadius; length = 0.10f * h; break;
            }
        }

        /// <summary>Feed lines stand on the bell axis. Ducts and actuators point out through the skirt.</summary>
        public static void Axis(string kind, float x, float z, out float ax, out float ay, out float az)
        {
            if (kind == Actuator || kind == ExhaustDuct)
            {
                float radial = (float)Math.Sqrt(x * x + z * z);
                if (radial < 1e-4f) { ax = 0f; ay = 1f; az = 0f; return; }
                ax = x / radial; az = z / radial; ay = kind == ExhaustDuct ? 0.35f : 0.12f;
                float m = (float)Math.Sqrt(ax * ax + ay * ay + az * az);
                ax /= m; ay /= m; az /= m;
                return;
            }
            ax = 0f; ay = 1f; az = 0f;
        }

        /// <summary>Centre plus the part's own reach (radius and the horizontal half-length) stays inside the skin, and the part sits above the exit plane.</summary>
        public static bool InsideSkin(BayPose p, float skinRadius)
        {
            float radial = (float)Math.Sqrt(p.X * p.X + p.Z * p.Z);
            float horiz = (float)Math.Sqrt(p.AxisX * p.AxisX + p.AxisZ * p.AxisZ) * (p.Length * 0.5f);
            float above = p.Length * 0.5f * Math.Abs(p.AxisY);
            return radial + p.Radius + horiz < skinRadius + 1e-3f && p.Y + 1e-3f > above;
        }

        static void Place(List<BayPart> list, string kind, float azimuth, float radius, float y)
        {
            list.Add(new BayPart(kind, radius * (float)Math.Sin(azimuth), y, radius * (float)Math.Cos(azimuth)));
        }
    }
}
