// Optional v3 field edits for the portrait designer. Uses HangarSession.Apply; does not extend HangarEditor.
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;

namespace Kestrel.Slice.DesignerV3
{
    public static class DesignerV3Edits
    {
        public static Edit SetMixtureOffset(JsonObject d, int stage, double pct)
            => DesignerKnobs.SetMixtureOffset(d, stage, pct);

        public static Edit SetEnginePc(JsonObject d, int stage, double pc)
            => DesignerKnobs.SetEnginePc(null, d, stage, pc);

        public static Edit SetEnginePc(Catalog c, JsonObject d, int stage, double pc)
            => DesignerKnobs.SetEnginePc(c, d, stage, pc);

        public static Edit SetExpansionRatio(JsonObject d, int stage, double eps)
            => DesignerKnobs.SetExpansionRatio(null, d, stage, eps);

        public static Edit SetExpansionRatio(Catalog c, JsonObject d, int stage, double eps)
            => DesignerKnobs.SetExpansionRatio(c, d, stage, eps);

        public static Edit SetGimbal(JsonObject d, int stage, double deg)
            => DesignerKnobs.SetGimbal(null, d, stage, deg);

        public static Edit SetGimbal(Catalog c, JsonObject d, int stage, double deg)
            => DesignerKnobs.SetGimbal(c, d, stage, deg);

        public static Edit SetThrottleMin(JsonObject d, int stage, double frac)
            => DesignerKnobs.SetThrottleMin(null, d, stage, frac);

        public static Edit SetTankPressure(JsonObject d, int stage, double bar)
            => DesignerKnobs.SetTankPressure(d, stage, bar);

        public static Edit SetInsulation(JsonObject d, int stage, string kind)
        {
            var tank = Tank(d, stage); if (tank == null) return Edit.None;
            string from = tank.Str("insulation") ?? "none";
            JsonMut.Set(tank, "insulation", kind ?? "none");
            return new Edit("Insulation", from, kind ?? "none");
        }

        public static Edit SetCommonBulkhead(JsonObject d, int stage, bool on)
        {
            var tank = Tank(d, stage); if (tank == null) return Edit.None;
            bool was = tank.Bool("commonBulkhead");
            JsonMut.Set(tank, "commonBulkhead", on);
            return new Edit("Common bulkhead", was ? "on" : "off", on ? "on" : "off");
        }

        public static Edit SetSepMode(JsonObject d, int stage, string mode)
        {
            var s = Stage(d, stage); if (s == null) return Edit.None;
            string from = s.Str("sepMode") ?? "cold";
            JsonMut.Set(s, "sepMode", mode ?? "cold");
            return new Edit("Separation", from, mode ?? "cold");
        }

        public static Edit SetGuidance(JsonObject d, string key, double value)
            => DesignerKnobs.SetGuidance(d, key, value);

        public static Edit SetRecoveryMode(JsonObject d, string mode)
        {
            if (d == null) return Edit.None;
            string next = string.IsNullOrEmpty(mode) ? "expendable" : mode;
            string from = d.Str("recoveryMode");
            if (string.IsNullOrEmpty(from)) from = d.Obj("recovery")?.Str("mode") ?? "expendable";
            var rec = d.Obj("recovery");
            if (from == next && d.Str("recoveryMode") == next && rec != null && rec.Str("mode") == next) return Edit.None;
            JsonMut.Set(d, "recoveryMode", next);
            if (rec == null) { rec = new JsonObject(); JsonMut.Set(d, "recovery", rec); }
            JsonMut.Set(rec, "mode", next);
            return new Edit("Recovery", from, next);
        }

        public static Edit SetLegsKind(JsonObject d, string kind)
        {
            var landing = d.Obj("landing");
            if (landing == null) { landing = new JsonObject(); JsonMut.Set(d, "landing", landing); }
            string from = landing.Str("legsKind") ?? "light";
            JsonMut.Set(landing, "legs", true);
            JsonMut.Set(landing, "legsKind", kind ?? "light");
            return new Edit("Legs", from, kind ?? "light");
        }

        /// <summary>Chamber pressure the Engines tab shows: an edited engineParams.pc, otherwise the catalog engine.</summary>
        public static double ShownPc(Catalog c, JsonObject design, int stage)
            => DesignerKnobs.ShownPc(c, design, stage);

        /// <summary>Expansion ratio: an edited override, otherwise the selected nozzle, otherwise the catalog bell.</summary>
        public static double ShownExpansion(Catalog c, JsonObject design, int stage)
            => DesignerKnobs.ShownExpansion(c, design, stage);

        /// <summary>Gimbal degrees: an edited override, otherwise the catalog engine.</summary>
        public static double ShownGimbal(Catalog c, JsonObject design, int stage)
            => DesignerKnobs.ShownGimbal(c, design, stage);

        static JsonObject Stage(JsonObject d, int i)
        {
            var stages = d?.Arr("stages");
            if (stages == null || i < 0 || i >= stages.Count) return null;
            return stages[i] as JsonObject;
        }

        static JsonObject Tank(JsonObject d, int i)
        {
            var s = Stage(d, i); if (s == null) return null;
            var tank = s.Obj("tank");
            if (tank == null) { tank = new JsonObject(); JsonMut.Set(s, "tank", tank); }
            return tank;
        }
    }
}
