// Part addressing for the Hangar stack list, the 3D selection and issue rings.
// The stack reads top to bottom: payload, fairing, top stage, its interstage, ..., stage 1, boosters.
// An interstage belongs to the upper stage of its joint (index i ≥ 1 sits between stage i-1 and stage i),
// the same rule InterstageAero and DesignMigration use.
using System;
using System.Collections.Generic;
using Kestrel.Core.Json;

namespace Kestrel.Core.Hangar
{
    public enum PartKind { Payload, Fairing, Stage, Interstage, Boosters }

    public readonly struct PartRef : IEquatable<PartRef>
    {
        public readonly PartKind kind;
        public readonly int index;

        public PartRef(PartKind kind, int index = 0) { this.kind = kind; this.index = index; }

        public static PartRef Payload => new PartRef(PartKind.Payload);
        public static PartRef Fairing => new PartRef(PartKind.Fairing);
        public static PartRef Boosters => new PartRef(PartKind.Boosters);
        public static PartRef Stage(int i) => new PartRef(PartKind.Stage, i);
        /// <summary>The joint under stage <paramref name="upperStage"/> (≥ 1).</summary>
        public static PartRef Interstage(int upperStage) => new PartRef(PartKind.Interstage, upperStage);

        /// <summary>A stage or the booster set: the parts that carry engines and tanks.</summary>
        public bool HasEngines => kind == PartKind.Stage || kind == PartKind.Boosters;

        public bool Equals(PartRef o) => kind == o.kind && index == o.index;
        public override bool Equals(object obj) => obj is PartRef p && Equals(p);
        public override int GetHashCode() => ((int)kind * 397) ^ index;
        public static bool operator ==(PartRef a, PartRef b) => a.Equals(b);
        public static bool operator !=(PartRef a, PartRef b) => !a.Equals(b);

        public override string ToString()
        {
            switch (kind)
            {
                case PartKind.Stage: return "stage:" + index;
                case PartKind.Interstage: return "interstage:" + index;
                default: return kind.ToString().ToLowerInvariant();
            }
        }

        public static bool TryParse(string s, out PartRef p)
        {
            p = default;
            if (string.IsNullOrEmpty(s)) return false;
            int colon = s.IndexOf(':');
            string head = colon < 0 ? s : s.Substring(0, colon);
            int idx = 0;
            if (colon >= 0 && !int.TryParse(s.Substring(colon + 1), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out idx)) return false;
            switch (head)
            {
                case "payload": p = Payload; return true;
                case "fairing": p = Fairing; return true;
                case "boosters": p = Boosters; return true;
                case "stage": p = Stage(idx); return idx >= 0;
                case "interstage": p = Interstage(idx); return idx >= 1;
                default: return false;
            }
        }
    }

    public sealed class PartInfo
    {
        public PartRef part;
        /// <summary>Engineer label ("Stage 2 · Upper").</summary>
        public string label;
        /// <summary>Kid label ("Top stage", "Nose cone").</summary>
        public string kidLabel;
        /// <summary>Indent level in the stack tree. Interstages are 1 so they read as joints.</summary>
        public int depth;
    }

    public static class HangarParts
    {
        /// <summary>Stack tree, top to bottom. Boosters appear only when the design has some.</summary>
        public static List<PartInfo> List(JsonObject design)
        {
            var o = new List<PartInfo>();
            if (design == null) return o;
            var stages = design.Arr("stages") ?? new List<object>();
            o.Add(new PartInfo { part = PartRef.Payload, label = "Payload", kidLabel = "Cargo" });
            o.Add(new PartInfo { part = PartRef.Fairing, label = "Fairing", kidLabel = "Nose cone" });
            for (int i = stages.Count - 1; i >= 0; i--)
            {
                o.Add(new PartInfo { part = PartRef.Stage(i), label = StageLabel(design, i), kidLabel = KidStage(i, stages.Count) });
                if (i >= 1)
                    o.Add(new PartInfo { part = PartRef.Interstage(i), label = $"Interstage {i}–{i + 1}", kidLabel = "Joint", depth = 1 });
            }
            if (BoosterCount(design) > 0)
                o.Add(new PartInfo { part = PartRef.Boosters, label = $"Boosters ×{BoosterCount(design)}", kidLabel = "Side boosters" });
            return o;
        }

        public static bool Exists(JsonObject design, PartRef p)
        {
            int n = design?.Arr("stages")?.Count ?? 0;
            switch (p.kind)
            {
                case PartKind.Stage: return p.index >= 0 && p.index < n;
                case PartKind.Interstage: return p.index >= 1 && p.index < n;
                case PartKind.Boosters: return BoosterCount(design) > 0;
                default: return design != null;
            }
        }

        public static int BoosterCount(JsonObject design)
        {
            var so = design?.Obj("strapOns");
            return so == null ? 0 : (int)so.Num("count");
        }

        /// <summary>The JSON object an engine/tank edit writes to: a stage or strapOns. Null otherwise.</summary>
        public static JsonObject Body(JsonObject design, PartRef p)
        {
            if (design == null) return null;
            if (p.kind == PartKind.Boosters) return design.Obj("strapOns");
            if (p.kind != PartKind.Stage && p.kind != PartKind.Interstage) return null;
            var stages = design.Arr("stages");
            return stages != null && p.index >= 0 && p.index < stages.Count ? stages[p.index] as JsonObject : null;
        }

        public static string StageLabel(JsonObject design, int i)
        {
            var s = Body(design, PartRef.Stage(i));
            string nm = s?.Str("name");
            return string.IsNullOrEmpty(nm) || nm == $"Stage {i + 1}" ? $"Stage {i + 1}" : $"Stage {i + 1} · {nm}";
        }

        /// <summary>Human label for history and VoiceOver ("Stage 1", "Boosters").</summary>
        public static string Short(PartRef p)
        {
            switch (p.kind)
            {
                case PartKind.Stage: return $"Stage {p.index + 1}";
                case PartKind.Interstage: return $"Interstage {p.index}–{p.index + 1}";
                default: return p.kind.ToString();
            }
        }

        static string KidStage(int i, int n)
        {
            if (n == 1) return "Rocket";
            if (i == 0) return "Bottom stage";
            if (i == n - 1) return "Top stage";
            return "Middle stage";
        }
    }
}
