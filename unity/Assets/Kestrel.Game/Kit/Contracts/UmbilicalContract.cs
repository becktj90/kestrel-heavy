// Pad <-> vehicle umbilical contract (docs/UMBILICAL_CONTRACT.md). Engine-free on purpose:
// Vehicle code depends on this assembly only, never on Pad, and the pad never reads vehicle internals.
// Units: metres, seconds relative to T-0 (negative = before liftoff), vehicle-local frame (+Y up the stack,
// origin at the base of the first stage on the centre line, +Z toward the pad tower).
using System;
using System.Collections.Generic;

namespace Kestrel.Kit.Contracts
{
    public enum UmbilicalType { Fuel, Oxidizer, PowerData, Purge, Hydraulic, Vent }

    /// <summary>How a connection leaves the vehicle at release.</summary>
    public enum ReleaseStyle { SwingArmRetract, TailServiceMastDrop, LanyardPull, Hinged }

    public enum UmbilicalEventKind
    {
        Connect,          // arm swung in, plate mated (pad sets up the state at scene build)
        FlowStart,        // propellant / gas / power flowing
        FlowStop,
        Disconnect,       // plate de-mates from the vehicle (visual separation begins)
        ArmRetractStart,  // swing arm / mast begins moving away
        ArmRetractDone,
        Abort             // hold or scrub: return to Connect pose
    }

    public enum UmbilicalState { Detached, Connected, Flowing, Released, Retracted }

    [Serializable]
    public struct Vec3 { public float X, Y, Z; public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; } }

    /// <summary>Quaternion (x, y, z, w), same convention as UnityEngine.Quaternion.</summary>
    [Serializable]
    public struct Quat { public float X, Y, Z, W; public Quat(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }
        public static readonly Quat Identity = new Quat(0, 0, 0, 1); }

    [Serializable]
    public struct LocalPose { public Vec3 Position; public Quat Rotation; public LocalPose(Vec3 p, Quat r) { Position = p; Rotation = r; } }

    /// <summary>One plate on the vehicle skin a pad connector can mate with. The +Z axis of Pose points out of the skin.</summary>
    [Serializable]
    public sealed class AttachPoint
    {
        public string Id;            // stable, e.g. "s1.lox.0"; see UmbilicalIds
        public UmbilicalType Type;
        public int Stage;            // 0 = first stage
        public LocalPose Pose;
        public float PlateRadius;    // m, visual mating plate size
        public ReleaseStyle Release;
    }

    /// <summary>A scheduled umbilical event. Times are seconds from T-0.</summary>
    [Serializable]
    public struct UmbilicalEvent
    {
        public string AttachId; public UmbilicalEventKind Kind; public double T;
        public UmbilicalEvent(string id, UmbilicalEventKind k, double t) { AttachId = id; Kind = k; T = t; }
    }

    /// <summary>Implemented by the vehicle (Kestrel Art). Read-only description plus a visual hook.</summary>
    public interface IUmbilicalVehicle
    {
        IReadOnlyList<AttachPoint> AttachPoints { get; }
        /// <summary>Vehicle-side reaction, e.g. close a door, puff of vent gas. Must not throw.</summary>
        void OnUmbilicalEvent(in UmbilicalEvent e);
    }

    /// <summary>Implemented by each pad connector (swing arm, tail mast, hose). One connector serves one attach point.</summary>
    public interface IUmbilicalConnector
    {
        string AttachId { get; }
        UmbilicalType Type { get; }
        UmbilicalState State { get; }
        /// <summary>Snap to the mated pose at scene build. Returns false if the attach point is out of reach.</summary>
        bool Connect(AttachPoint target, LocalPose vehicleToPad);
        void OnUmbilicalEvent(in UmbilicalEvent e);
    }

    public static class UmbilicalIds
    {
        /// <summary>"s{stage}.{type}.{index}", e.g. s0.fuel.0, s1.pwr.0.</summary>
        public static string Make(int stage, UmbilicalType t, int index) => $"s{stage}.{Short(t)}.{index}";
        public static string Short(UmbilicalType t) => t switch
        {
            UmbilicalType.Fuel => "fuel", UmbilicalType.Oxidizer => "lox", UmbilicalType.PowerData => "pwr",
            UmbilicalType.Purge => "purge", UmbilicalType.Hydraulic => "hyd", _ => "vent"
        };
    }

    /// <summary>State machine shared by both sides so they agree on what each event means.</summary>
    public static class UmbilicalStates
    {
        public static UmbilicalState Apply(UmbilicalState s, UmbilicalEventKind k) => k switch
        {
            UmbilicalEventKind.Connect => UmbilicalState.Connected,
            UmbilicalEventKind.FlowStart when s == UmbilicalState.Connected => UmbilicalState.Flowing,
            UmbilicalEventKind.FlowStop when s == UmbilicalState.Flowing => UmbilicalState.Connected,
            UmbilicalEventKind.Disconnect when s == UmbilicalState.Connected || s == UmbilicalState.Flowing => UmbilicalState.Released,
            UmbilicalEventKind.ArmRetractStart => s == UmbilicalState.Released ? UmbilicalState.Released : s,
            UmbilicalEventKind.ArmRetractDone when s == UmbilicalState.Released => UmbilicalState.Retracted,
            UmbilicalEventKind.Abort when s != UmbilicalState.Retracted && s != UmbilicalState.Detached => UmbilicalState.Connected,
            _ => s
        };
    }

    /// <summary>Default release timeline. Generic, fictional numbers; the MissionEvent timeline (K-track) may
    /// override per pad, but must keep the ordering rules that Validate checks.</summary>
    public static class UmbilicalTimeline
    {
        public const double LiftoffT = 0.0;

        public static double DefaultDisconnectT(UmbilicalType t, ReleaseStyle r) => (t, r) switch
        {
            (_, ReleaseStyle.TailServiceMastDrop) => 0.0,     // at liftoff, mast drops as the vehicle rises
            (_, ReleaseStyle.LanyardPull) => 0.05,            // pulled free by first motion
            (UmbilicalType.Fuel, _) => -150.0,
            (UmbilicalType.Oxidizer, _) => -120.0,
            (UmbilicalType.Vent, _) => -45.0,
            (UmbilicalType.Hydraulic, _) => -60.0,
            (UmbilicalType.Purge, _) => -2.0,
            (UmbilicalType.PowerData, _) => -1.0,
            _ => -1.0
        };

        public static List<UmbilicalEvent> Build(IReadOnlyList<AttachPoint> points, double connectT = -3600, double flowStartT = -2100)
        {
            var list = new List<UmbilicalEvent>();
            foreach (var p in points)
            {
                double d = DefaultDisconnectT(p.Type, p.Release);
                list.Add(new UmbilicalEvent(p.Id, UmbilicalEventKind.Connect, connectT));
                list.Add(new UmbilicalEvent(p.Id, UmbilicalEventKind.FlowStart, flowStartT));
                list.Add(new UmbilicalEvent(p.Id, UmbilicalEventKind.FlowStop, d - 0.5));
                list.Add(new UmbilicalEvent(p.Id, UmbilicalEventKind.Disconnect, d));
                list.Add(new UmbilicalEvent(p.Id, UmbilicalEventKind.ArmRetractStart, d + 0.2));
                list.Add(new UmbilicalEvent(p.Id, UmbilicalEventKind.ArmRetractDone, d + (p.Release == ReleaseStyle.SwingArmRetract ? 8.0 : 1.5)));
            }
            list.Sort((a, b) => a.T != b.T ? a.T.CompareTo(b.T) : a.Kind.CompareTo(b.Kind));
            return list;
        }

        /// <summary>Returns problems (empty = valid): unique ids, every point disconnects, in-order per point,
        /// nothing still mated after liftoff + 0.1 s (except via lanyard/tail mast which must release by +0.1 s).</summary>
        public static List<string> Validate(IReadOnlyList<AttachPoint> points, IReadOnlyList<UmbilicalEvent> events)
        {
            var errs = new List<string>(); var ids = new HashSet<string>();
            foreach (var p in points)
            {
                if (string.IsNullOrEmpty(p.Id)) errs.Add("attach point with empty id");
                else if (!ids.Add(p.Id)) errs.Add($"duplicate attach id {p.Id}");
            }
            foreach (var id in ids)
            {
                var s = UmbilicalState.Detached; double last = double.NegativeInfinity; bool released = false;
                foreach (var e in events)
                {
                    if (e.AttachId != id) continue;
                    if (e.T < last) errs.Add($"{id}: events out of order at {e.Kind}");
                    last = e.T;
                    var n = UmbilicalStates.Apply(s, e.Kind);
                    if (e.Kind == UmbilicalEventKind.Disconnect)
                    {
                        if (n != UmbilicalState.Released) errs.Add($"{id}: Disconnect while {s}");
                        if (e.T > LiftoffT + 0.1) errs.Add($"{id}: still mated after liftoff (T{e.T:+0.##;-0.##})");
                        released = true;
                    }
                    s = n;
                }
                if (!released) errs.Add($"{id}: never disconnects");
            }
            foreach (var e in events) if (!ids.Contains(e.AttachId)) errs.Add($"event for unknown attach id {e.AttachId}");
            return errs;
        }
    }
}
