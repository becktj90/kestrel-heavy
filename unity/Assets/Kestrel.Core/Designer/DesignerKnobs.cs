// Bounds for the designer steppers. Every +/- stops at the limit and refuses NaN or Infinity.
// The portrait panel and the guided steps both call these, so the two views share one design.
using System;
using System.Globalization;
using Kestrel.Core.Data;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Designer
{
    public static class DesignerKnobs
    {
        public const double MixtureMin = -MixtureOffset.LimitPct;
        public const double MixtureMax = MixtureOffset.LimitPct;
        public const double MixtureStep = 1;
        public const double PressureMin = TankModel.PressureMinBar;
        public const double PressureMax = TankModel.PressureMaxBar;
        public const double PressureStep = 0.5;
        public const double PressureDefault = 3;
        public const double ThrottleMin = 0.05;
        public const double ThrottleMax = 1;
        public const double ThrottleStep = 0.05;
        public const double GimbalStep = 0.5;
        public const double PcStep = 5;
        public const double EpsStep = 2;
        /// <summary>data/designer-limits.json v3.guidance.pitchKickAltM. 12000 m stays 12000 m.</summary>
        public static double PitchAltMin => DesignerLimits.Active.pitchKickAltMin;
        public static double PitchAltMax => DesignerLimits.Active.pitchKickAltMax;
        public const double PitchAltStep = 50;
        public const double PitchAltDefault = 100;
        public const double PitchDegMin = 0;
        public const double PitchDegMax = 20;
        public const double PitchDegStep = 0.5;
        public const double PitchDegDefault = 2;
        public const double GLimitMin = 1.5;
        public const double GLimitMax = 8;
        public const double GLimitStep = 0.5;
        public const double GLimitDefault = 4;
        public const double TurnMin = 0;
        public const double TurnMax = 1;
        public const double TurnStep = 0.05;
        public const double TurnDefault = 0.5;

        /// <summary>Hangar noses plus the v3 biconic and hammerhead shapes.</summary>
        public static readonly string[] Noses = { "ogive", "conic", "blunt", "von-karman", "biconic", "hammerhead" };

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        public static double Clamp(double v, double min, double max)
        {
            if (!Finite(v)) return min;
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        public static Edit SetMixtureOffset(JsonObject d, int stage, double pct)
        {
            var s = Stage(d, stage);
            if (s == null || !Finite(pct)) return Edit.None;
            pct = Clamp(pct, MixtureMin, MixtureMax);
            if (s.ContainsKey("mixtureOffsetPct") && Math.Abs(s.Num("mixtureOffsetPct") - pct) < 1e-9) return Edit.None;
            string from = JsonMut.Shown(s, "mixtureOffsetPct");
            JsonMut.Set(s, "mixtureOffsetPct", pct);
            return new Edit("Mixture offset", from, pct.ToString("0.0", Inv) + "%");
        }

        public static Edit NudgeMixture(JsonObject d, int stage, int dir)
        {
            double shown = Stage(d, stage)?.Num("mixtureOffsetPct") ?? 0;
            double next = Clamp(shown + dir * MixtureStep, MixtureMin, MixtureMax);
            if (Math.Abs(next - shown) < 1e-9) return Edit.None;
            return SetMixtureOffset(d, stage, next);
        }

        public static Edit SetEnginePc(Catalog c, JsonObject d, int stage, double pc)
        {
            if (!Finite(pc)) return Edit.None;
            pc = ClampPc(c, d, stage, pc);
            var eng = EngineParams(d, stage);
            if (eng == null) return Edit.None;
            if (eng.ContainsKey("pc") && Math.Abs(eng.Num("pc") - pc) < 1e-6) return Edit.None;
            string from = JsonMut.Shown(eng, "pc");
            JsonMut.Set(eng, "pc", pc);
            return new Edit("Chamber pressure", from, pc.ToString("0.##", Inv) + " bar");
        }

        public static Edit NudgePc(Catalog c, JsonObject d, int stage, int dir)
        {
            double shown = ShownPc(c, d, stage);
            double next = ClampPc(c, d, stage, shown + dir * PcStep);
            if (Math.Abs(next - shown) < 1e-4) return Edit.None;
            return SetEnginePc(c, d, stage, next);
        }

        public static double ShownPc(Catalog c, JsonObject design, int stage)
        {
            var s = Stage(design, stage);
            var over = s?.Obj("engineParams");
            double raw;
            if (over != null && over.ContainsKey("pc") && Finite(over.Num("pc"))) raw = over.Num("pc");
            else
            {
                var catalog = EngineOf(c, s);
                raw = catalog != null && catalog.pc > 0 ? catalog.pc : 0;
            }
            // The Engines tab and the nudge both start here. Physics already clamps, so the
            // number on screen is the pressure the selected engine can actually run.
            return ResolvedPc(c, s, raw);
        }

        static double ResolvedPc(Catalog c, JsonObject stage, double raw)
        {
            var e = EngineOf(c, stage);
            if (e == null) return Finite(raw) ? Clamp(raw, 0, 500) : 0;
            var env = EngineModel.EnvelopeFor(e);
            double lo = env.pcMin > 0 ? env.pcMin : 0;
            double hi = env.pcRated > 0 ? env.pcRated * EngineModel.PcOverRated : 0;
            if (!(hi > lo) || !Finite(raw)) return Finite(raw) ? raw : 0;
            return Clamp(raw, lo, hi);
        }

        public static double PcMin(Catalog c, JsonObject design, int stage)
        {
            var env = EngineModel.EnvelopeFor(EngineOf(c, Stage(design, stage)));
            return env.pcMin > 0 ? env.pcMin : 0;
        }

        public static double PcMax(Catalog c, JsonObject design, int stage)
        {
            var env = EngineModel.EnvelopeFor(EngineOf(c, Stage(design, stage)));
            double hi = env.pcRated > 0 ? env.pcRated * EngineModel.PcOverRated : 0;
            return hi > 0 ? hi : ShownPc(c, design, stage);
        }

        public static Edit SetExpansionRatio(Catalog c, JsonObject d, int stage, double eps)
        {
            if (!Finite(eps)) return Edit.None;
            eps = ClampEps(c, d, stage, eps);
            var eng = EngineParams(d, stage);
            if (eng == null) return Edit.None;
            if (eng.ContainsKey("expansionRatio") && Math.Abs(eng.Num("expansionRatio") - eps) < 1e-6) return Edit.None;
            string from = JsonMut.Shown(eng, "expansionRatio");
            JsonMut.Set(eng, "expansionRatio", eps);
            return new Edit("Expansion ratio", from, eps.ToString("0.##", Inv));
        }

        public static Edit NudgeExpansion(Catalog c, JsonObject d, int stage, int dir)
        {
            double shown = ShownExpansion(c, d, stage);
            double next = ClampEps(c, d, stage, shown + dir * EpsStep);
            if (Math.Abs(next - shown) < 1e-4) return Edit.None;
            return SetExpansionRatio(c, d, stage, next);
        }

        public static double ShownExpansion(Catalog c, JsonObject design, int stage)
        {
            var s = Stage(design, stage);
            var over = s?.Obj("engineParams");
            double raw;
            if (over != null && over.ContainsKey("expansionRatio") && Finite(over.Num("expansionRatio")))
                raw = over.Num("expansionRatio");
            else
            {
                var e = EngineOf(c, s);
                raw = 0;
                if (e != null)
                {
                    raw = e.expansionRatio;
                    string nozzle = s.Str("nozzle");
                    if (!string.IsNullOrEmpty(nozzle) && nozzle != "default" && e.nozzles != null)
                        for (int i = 0; i < e.nozzles.Count; i++)
                            if (e.nozzles[i].id == nozzle && e.nozzles[i].expansionRatio > 0)
                                raw = e.nozzles[i].expansionRatio;
                }
            }
            return ClampEps(c, design, stage, raw);
        }

        public static Edit SetGimbal(Catalog c, JsonObject d, int stage, double deg)
        {
            if (!Finite(deg)) return Edit.None;
            deg = ClampGimbal(c, d, stage, deg);
            var eng = EngineParams(d, stage);
            if (eng == null) return Edit.None;
            if (eng.ContainsKey("gimbalDeg") && Math.Abs(eng.Num("gimbalDeg") - deg) < 1e-6) return Edit.None;
            string from = JsonMut.Shown(eng, "gimbalDeg");
            JsonMut.Set(eng, "gimbalDeg", deg);
            return new Edit("Gimbal", from, deg.ToString("0.0", Inv) + "°");
        }

        public static Edit NudgeGimbal(Catalog c, JsonObject d, int stage, int dir)
        {
            double shown = ShownGimbal(c, d, stage);
            double next = ClampGimbal(c, d, stage, shown + dir * GimbalStep);
            if (Math.Abs(next - shown) < 1e-4) return Edit.None;
            return SetGimbal(c, d, stage, next);
        }

        public static double ShownGimbal(Catalog c, JsonObject design, int stage)
        {
            var s = Stage(design, stage);
            var over = s?.Obj("engineParams");
            double raw;
            if (over != null && over.ContainsKey("gimbalDeg") && Finite(over.Num("gimbalDeg"))) raw = over.Num("gimbalDeg");
            else
            {
                var catalog = EngineOf(c, s);
                raw = catalog != null ? catalog.gimbal : 0;
            }
            return ClampGimbal(c, design, stage, raw);
        }

        public static Edit SetThrottleMin(Catalog c, JsonObject d, int stage, double frac)
        {
            if (!Finite(frac)) return Edit.None;
            frac = Clamp(frac, ThrottleMin, ThrottleMax);
            var eng = EngineParams(d, stage);
            if (eng == null) return Edit.None;
            if (eng.ContainsKey("throttleMin") && Math.Abs(eng.Num("throttleMin") - frac) < 1e-6) return Edit.None;
            string from = JsonMut.Shown(eng, "throttleMin");
            JsonMut.Set(eng, "throttleMin", frac);
            return new Edit("Min throttle", from, (frac * 100).ToString("0", Inv) + "%");
        }

        public static Edit NudgeThrottle(Catalog c, JsonObject d, int stage, int dir)
        {
            double shown = ShownThrottle(c, d, stage);
            double next = Clamp(shown + dir * ThrottleStep, ThrottleMin, ThrottleMax);
            if (Math.Abs(next - shown) < 1e-4) return Edit.None;
            return SetThrottleMin(c, d, stage, next);
        }

        public static double ShownThrottle(Catalog c, JsonObject design, int stage)
        {
            var s = Stage(design, stage);
            var over = s?.Obj("engineParams");
            if (over != null && over.ContainsKey("throttleMin") && Finite(over.Num("throttleMin")))
                return over.Num("throttleMin");
            var e = EngineOf(c, s);
            if (e != null && e.throttle != null && e.throttle.Length > 0 && Finite(e.throttle[0])) return e.throttle[0];
            return 1;
        }

        public static Edit SetTankPressure(JsonObject d, int stage, double bar)
        {
            if (!Finite(bar)) return Edit.None;
            bar = Clamp(bar, PressureMin, PressureMax);
            var tank = Tank(d, stage);
            if (tank == null) return Edit.None;
            if (tank.ContainsKey("pressureBar") && Math.Abs(tank.Num("pressureBar") - bar) < 1e-6) return Edit.None;
            string from = JsonMut.Shown(tank, "pressureBar");
            JsonMut.Set(tank, "pressureBar", bar);
            return new Edit("Tank pressure", from, bar.ToString("0.0", Inv) + " bar");
        }

        public static Edit NudgePressure(JsonObject d, int stage, int dir)
        {
            double shown = ShownPressure(d, stage);
            double next = Clamp(shown + dir * PressureStep, PressureMin, PressureMax);
            if (Math.Abs(next - shown) < 1e-6) return Edit.None;
            return SetTankPressure(d, stage, next);
        }

        public static double ShownPressure(JsonObject design, int stage)
        {
            var tank = Stage(design, stage)?.Obj("tank");
            if (tank != null && tank.ContainsKey("pressureBar") && Finite(tank.Num("pressureBar")))
                return tank.Num("pressureBar");
            return PressureDefault;
        }

        public static Edit SetGuidance(JsonObject d, string key, double value)
        {
            if (d == null || string.IsNullOrEmpty(key) || !Finite(value)) return Edit.None;
            // maxQThrottle keeps the caller's units (0.95 or 95). The stepper uses NudgeGuidance.
            if (key == "maxQThrottle")
            {
                if (value > 100) value = 100;
                else if (value < 0) value = 0;
            }
            else value = ClampGuidance(key, value);
            var g = d.Obj("guidance");
            if (g == null) { g = new JsonObject(); JsonMut.Set(d, "guidance", g); }
            if (g.ContainsKey(key) && Finite(g.Num(key)) && Math.Abs(g.Num(key) - value) < 1e-9) return Edit.None;
            string from = JsonMut.Shown(g, key);
            JsonMut.Set(g, key, value);
            return new Edit("Guidance " + key, from, value.ToString("R", Inv));
        }

        public static Edit NudgeGuidance(JsonObject d, string key, int dir)
        {
            if (key == "maxQThrottle") return MaxQThrottle.Nudge(d, dir);
            double shown = ShownGuidance(d, key);
            double next = ClampGuidance(key, shown + dir * GuidanceStep(key));
            if (Math.Abs(next - shown) < 1e-6) return Edit.None;
            return SetGuidance(d, key, next);
        }

        public static double ShownGuidance(JsonObject design, string key)
        {
            var g = design?.Obj("guidance");
            if (g != null && g.ContainsKey(key) && Finite(g.Num(key))) return g.Num(key);
            switch (key)
            {
                case "pitchKickAlt": return PitchAltDefault;
                case "pitchKickDeg": return PitchDegDefault;
                case "gLimit": return GLimitDefault;
                case "turnShape": return TurnDefault;
                case "maxQThrottle": return MaxQThrottle.Percent(design);
                default: return 0;
            }
        }

        public static Edit SetNose(JsonObject d, string nose)
        {
            if (d == null || Array.IndexOf(Noses, nose) < 0) return Edit.None;
            if (Array.IndexOf(HangarEditor.Noses, nose) >= 0) return HangarEditor.SetNose(d, nose);
            var a = d.Obj("aero");
            if (a == null)
            {
                a = new JsonObject();
                JsonMut.Set(d, "aero", a);
                JsonMut.Set(a, "fins", false);
            }
            string old = a.Str("nose");
            if (old == nose) return Edit.None;
            JsonMut.Set(a, "nose", nose);
            return new Edit("Nose shape", old, nose);
        }

        static double ClampPc(Catalog c, JsonObject d, int stage, double pc)
        {
            if (EngineOf(c, Stage(d, stage)) == null)
                return Finite(pc) ? Clamp(pc, 0, 500) : 0;
            double lo = PcMin(c, d, stage);
            double hi = PcMax(c, d, stage);
            if (!(hi > lo)) return Finite(pc) && pc > 0 ? pc : ShownPc(c, d, stage);
            return Clamp(pc, lo, hi);
        }

        static double ClampEps(Catalog c, JsonObject d, int stage, double eps)
        {
            var e = EngineOf(c, Stage(d, stage));
            if (e == null) return Finite(eps) ? Clamp(eps, 8, 250) : 8;
            var env = EngineModel.EnvelopeFor(e);
            double lo = env.epsMin > 0 ? env.epsMin : 8;
            double hi = env.epsMax > 0 ? env.epsMax : 120;
            if (hi < lo) hi = lo;
            return Clamp(eps, lo, hi);
        }

        static double ClampGimbal(Catalog c, JsonObject d, int stage, double deg)
        {
            var e = EngineOf(c, Stage(d, stage));
            if (e == null) return Finite(deg) ? Clamp(deg, 0, 25) : 0;
            double hi = e.gimbal > 0 ? e.gimbal : 0;
            if (hi <= 0) return 0;
            return Clamp(deg, 0, hi);
        }

        static double ClampGuidance(string key, double value)
        {
            switch (key)
            {
                case "pitchKickAlt": return Clamp(value, PitchAltMin, PitchAltMax);
                case "pitchKickDeg": return Clamp(value, PitchDegMin, PitchDegMax);
                case "gLimit": return Clamp(value, GLimitMin, GLimitMax);
                case "turnShape": return Clamp(value, TurnMin, TurnMax);
                default: return value;
            }
        }

        static double GuidanceStep(string key)
        {
            switch (key)
            {
                case "pitchKickAlt": return PitchAltStep;
                case "pitchKickDeg": return PitchDegStep;
                case "gLimit": return GLimitStep;
                case "turnShape": return TurnStep;
                default: return 1;
            }
        }

        static Engine EngineOf(Catalog c, JsonObject stage)
        {
            if (c == null || stage == null) return null;
            string id = stage.Str("engine");
            if (string.IsNullOrEmpty(id) || !c.EngineById.TryGetValue(id, out var e)) return null;
            return e;
        }

        static JsonObject Stage(JsonObject d, int i)
        {
            var stages = d?.Arr("stages");
            if (stages == null || i < 0 || i >= stages.Count) return null;
            return stages[i] as JsonObject;
        }

        static JsonObject EngineParams(JsonObject d, int i)
        {
            var s = Stage(d, i);
            if (s == null) return null;
            var eng = s.Obj("engineParams");
            if (eng == null) { eng = new JsonObject(); JsonMut.Set(s, "engineParams", eng); }
            return eng;
        }

        static JsonObject Tank(JsonObject d, int i)
        {
            var s = Stage(d, i);
            if (s == null) return null;
            var tank = s.Obj("tank");
            if (tank == null) { tank = new JsonObject(); JsonMut.Set(s, "tank", tank); }
            return tank;
        }
    }
}
