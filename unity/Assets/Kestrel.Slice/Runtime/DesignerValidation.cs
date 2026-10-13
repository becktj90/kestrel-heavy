// U1 (docs/DESIGNER_SPEC.md §6, §11.3): designer validation glue between Kestrel.Core
// (Validate.Engineering + Experience.Present) and the Slice design panel. Pure C#, no UnityEngine,
// so EditMode tests cover the wording map, the summary chip and the Launch gate.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;

namespace Kestrel.Slice
{
    public sealed class DesignerState
    {
        public string layer;
        public Presented presented;
        public bool launchEnabled;
        public string chip;        // "" when nothing to fix
        public string reason;      // "Fix first: …" or ""
        public List<Issue> blocks = new List<Issue>(), warnings = new List<Issue>();
    }

    public static class DesignerValidation
    {
        /// <summary>Kid wording per code: the Hangar table (Kestrel.Core.Hangar.KidWords), so the U1 chip and the Hangar warnings never drift. Engineer shows Issue.msg. Spec §6/§11.3.</summary>
        public static readonly IReadOnlyDictionary<string, string> KidWording = Kestrel.Core.Hangar.KidWords.Table;

        /// <summary>dv-margin second message (Validate.cs: margin 0-5%, msg starts "Only").</summary>
        public const string DvMarginThin = Kestrel.Core.Hangar.KidWords.DvMarginThin;

        public const string KidFallback = Kestrel.Core.Hangar.KidWords.Fallback;

        /// <summary>Text shown for an issue on a layer: engineer = exact msg; otherwise kid wording.</summary>
        public static string Wording(Issue issue, string layer)
        {
            if (issue == null) return "";
            if (layer == "engineer") return issue.msg;
            if (issue.code == "dv-margin" && issue.msg != null && issue.msg.StartsWith("Only")) return DvMarginThin;
            return issue.code != null && KidWording.TryGetValue(issue.code, out var w) ? w : KidFallback;
        }

        /// <summary>Summary chip: kid "N things to fix" (blocks only; simple hides warnings), engineer "N block · N warning".</summary>
        public static string Chip(int blocks, int warnings, string layer)
        {
            if (layer == "engineer")
                return blocks + warnings == 0 ? "" : $"{blocks} {(blocks == 1 ? "block" : "blocks")} · {warnings} {(warnings == 1 ? "warning" : "warnings")}";
            int n = blocks + warnings;
            return n == 0 ? "" : n == 1 ? "1 thing to fix" : $"{n} things to fix";
        }

        /// <summary>Launch is enabled only when there are no blocks; warnings never disable it.</summary>
        public static bool LaunchEnabled(EngineeringReport report) => report != null && report.ok && report.blocks.Count == 0;

        public static DesignerState Evaluate(Catalog c, Design d, string layer)
        {
            var report = Validate.Engineering(c, ToJson(d));
            return Evaluate(report, layer);
        }

        public static DesignerState Evaluate(EngineeringReport report, string layer)
        {
            var p = Experience.Present(report, layer);
            bool enabled = LaunchEnabled(report);
            var st = new DesignerState { layer = layer, presented = p, launchEnabled = enabled, blocks = p.blocks, warnings = p.warnings };
            st.chip = Chip(p.blocks.Count, p.warnings.Count, layer);
            st.reason = enabled ? "" : "Fix first: " + (p.blocks.Count > 0 ? Wording(p.blocks[0], layer) : KidFallback);
            return st;
        }

        /// <summary>Design -> the raw JSON tree Validate expects (inverse of Design.FromJson).</summary>
        public static JsonObject ToJson(Design d)
        {
            var o = new JsonObject();
            o.Add("version", (double)d.version);
            if (d.id != null) o.Add("id", d.id);
            o.Add("name", d.name ?? "");
            if (d.tier != null) o.Add("tier", d.tier);
            var stages = new List<object>();
            foreach (var s in d.stages) stages.Add(Stage(s, false));
            o.Add("stages", stages);
            o.Add("strapOns", d.strapOns != null ? Stage(d.strapOns, true) : Obj(("count", 0.0), ("crossfeed", false)));
            o.Add("payload", Obj(("type", d.payloadType), ("mass", d.payloadMass), ("orbit", d.payloadOrbit)));
            o.Add("fairing", Obj(("diameter", d.fairingDiameter), ("length", d.fairingLength), ("material", d.fairingMaterial), ("jettisonAltitudeKm", d.fairingJettisonAltitudeKm)));
            o.Add("aero", Obj(("nose", d.nose), ("fins", d.fins)));
            o.Add("landing", Obj(("legs", d.legs), ("gridFins", d.gridFins), ("rcs", d.rcs)));
            o.Add("avionics", Obj(("tier", d.avionicsTier)));
            o.Add("safetyFactor", d.safetyFactor);
            o.Add("livery", Obj(("primary", d.liveryPrimary), ("accent", d.liveryAccent), ("name", d.liveryName ?? "")));
            return o;
        }

        static JsonObject Stage(StagePart s, bool strap)
        {
            var o = new JsonObject();
            if (s.name != null) o.Add("name", s.name);
            if (strap) { o.Add("count", (double)s.count); if (s.kind != null) o.Add("kind", s.kind); }
            o.Add("propellant", s.propellant); o.Add("engine", s.engine);
            o.Add("engineCount", (double)s.engineCount); o.Add("propMass", s.propMass); o.Add("diameter", s.diameter);
            o.Add("material", s.material); o.Add("interstageMass", s.interstageMass); o.Add("separationMass", s.separationMass);
            o.Add("landingPropMass", s.landingPropMass); o.Add("crossfeed", s.crossfeed);
            if (s.tune != null) o.Add("tune", Obj(("thrustPct", s.tune.thrustPct), ("ispPct", s.tune.ispPct)));
            return o;
        }

        static JsonObject Obj(params (string k, object v)[] kv)
        {
            var o = new JsonObject();
            foreach (var (k, v) in kv) if (v != null) o.Add(k, v is int i ? (double)i : v);
            return o;
        }

        /// <summary>Which existing Slice control an issue points at (U1 has only S1 engines/propellant + preset).</summary>
        public static string ControlFor(Issue issue)
        {
            if (issue == null) return null;
            bool stage1 = issue.index == null || issue.index == 0;
            switch (issue.tab)
            {
                case "engines": return stage1 ? "engines" : "preset";
                case "propellants": return stage1 ? "prop" : "preset";
                default: return "preset";
            }
        }
    }
}
