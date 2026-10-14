// Optional v3 field edits for the portrait designer. Uses HangarSession.Apply; does not extend HangarEditor.
using System.Globalization;
using Kestrel.Core.Designer;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;

namespace Kestrel.Slice.DesignerV3
{
    public static class DesignerV3Edits
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static Edit SetMixtureOffset(JsonObject d, int stage, double pct)
        {
            var s = Stage(d, stage); if (s == null) return Edit.None;
            string from = Shown(s, "mixtureOffsetPct");
            JsonMut.Set(s, "mixtureOffsetPct", pct);
            return new Edit("Mixture offset", from, pct.ToString("0.0", Inv) + "%");
        }

        public static Edit SetEnginePc(JsonObject d, int stage, double pc)
        {
            var eng = EngineParams(d, stage); if (eng == null) return Edit.None;
            string from = Shown(eng, "pc");
            JsonMut.Set(eng, "pc", pc);
            return new Edit("Chamber pressure", from, pc.ToString("0", Inv) + " bar");
        }

        public static Edit SetExpansionRatio(JsonObject d, int stage, double eps)
        {
            var eng = EngineParams(d, stage); if (eng == null) return Edit.None;
            string from = Shown(eng, "expansionRatio");
            JsonMut.Set(eng, "expansionRatio", eps);
            return new Edit("Expansion ratio", from, eps.ToString("0", Inv));
        }

        public static Edit SetGimbal(JsonObject d, int stage, double deg)
        {
            var eng = EngineParams(d, stage); if (eng == null) return Edit.None;
            string from = Shown(eng, "gimbalDeg");
            JsonMut.Set(eng, "gimbalDeg", deg);
            return new Edit("Gimbal", from, deg.ToString("0.0", Inv) + "°");
        }

        public static Edit SetThrottleMin(JsonObject d, int stage, double frac)
        {
            var eng = EngineParams(d, stage); if (eng == null) return Edit.None;
            string from = Shown(eng, "throttleMin");
            JsonMut.Set(eng, "throttleMin", frac);
            return new Edit("Min throttle", from, (frac * 100).ToString("0", Inv) + "%");
        }

        public static Edit SetTankPressure(JsonObject d, int stage, double bar)
        {
            var tank = Tank(d, stage); if (tank == null) return Edit.None;
            string from = Shown(tank, "pressureBar");
            JsonMut.Set(tank, "pressureBar", bar);
            return new Edit("Tank pressure", from, bar.ToString("0.0", Inv) + " bar");
        }

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
        {
            var g = d.Obj("guidance");
            if (g == null) { g = new JsonObject(); JsonMut.Set(d, "guidance", g); }
            string from = Shown(g, key);
            JsonMut.Set(g, key, value);
            return new Edit("Guidance " + key, from, value.ToString("R", Inv));
        }

        public static Edit SetRecoveryMode(JsonObject d, string mode)
        {
            string from = d.Str("recoveryMode") ?? "expendable";
            JsonMut.Set(d, "recoveryMode", mode ?? "expendable");
            return new Edit("Recovery", from, mode ?? "expendable");
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

        static JsonObject Stage(JsonObject d, int i)
        {
            var stages = d?.Arr("stages");
            if (stages == null || i < 0 || i >= stages.Count) return null;
            return stages[i] as JsonObject;
        }

        /// <summary>v3 knobs live on engineParams so the catalog engine id string stays intact.</summary>
        static JsonObject EngineParams(JsonObject d, int i)
        {
            var s = Stage(d, i); if (s == null) return null;
            var eng = s.Obj("engineParams");
            if (eng == null) { eng = new JsonObject(); JsonMut.Set(s, "engineParams", eng); }
            return eng;
        }

        static JsonObject Tank(JsonObject d, int i)
        {
            var s = Stage(d, i); if (s == null) return null;
            var tank = s.Obj("tank");
            if (tank == null) { tank = new JsonObject(); JsonMut.Set(s, "tank", tank); }
            return tank;
        }

        static string Shown(JsonObject o, string k) => JsonMut.Shown(o, k);
    }
}
