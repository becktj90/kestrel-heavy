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

        static void Place(List<BayPart> list, string kind, float azimuth, float radius, float y)
        {
            list.Add(new BayPart(kind, radius * (float)Math.Sin(azimuth), y, radius * (float)Math.Cos(azimuth)));
        }
    }
}
