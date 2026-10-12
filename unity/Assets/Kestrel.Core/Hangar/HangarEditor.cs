// Every Hangar edit, as a pure operation on the design JSON (schema v2). The JSON tree stays the
// single document: unknown keys survive, saves and undo store it verbatim, and Validate/Readout read it
// directly. Each op clamps to data/designer-limits.json, keeps the design shape-valid where it can
// (engine matches propellant, cluster count matches pattern, landing reserve ≤ its cap), and returns
// what changed for the history label. An op that would change nothing returns Edit.None.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Hangar
{
    public readonly struct Edit
    {
        public readonly bool changed;
        public readonly string what, from, to;

        public Edit(string what, string from = null, string to = null) { changed = true; this.what = what; this.from = from; this.to = to; }

        public static readonly Edit None = default;
    }

    public static class HangarEditor
    {
        static DesignerLimits Lim => DesignerLimits.Active;
        public static readonly string[] Noses = { "ogive", "conic", "blunt", "von-karman" };
        public static readonly string[] InterstageShapes = { "straight", "flare", "shoulder" };
        public static readonly string[] AvionicsTiers = { "basic", "standard", "redundant" };
        static readonly Regex Hex = new Regex("^#[0-9A-Fa-f]{6}$");
        public const int VehicleNameMax = 20;
        const double DomeLengthFactor = 0.33, Ullage = 0.03;

        // ---------------- stages: add, remove, reorder ----------------

        /// <summary>
        /// Insert a stage at <paramref name="at"/> (0 = new bottom, Count = new top). A new upper stage copies the
        /// stage under it with that propellant's vacuum engine, one engine and a quarter of the propellant.
        /// A new bottom stage copies stage 1 with half again the propellant.
        /// </summary>
        public static Edit AddStage(Catalog c, JsonObject d, int at)
        {
            var stages = d.Arr("stages");
            if (stages == null || stages.Count >= Lim.stagesMax) return Edit.None;
            at = Math.Max(0, Math.Min(stages.Count, at));
            JsonObject s;
            if (stages.Count == 0)
                s = NewStage("kerolox", "gg-kerolox", 1, 20000, 2, "al-li");
            else if (at == 0)
            {
                s = JsonMut.CloneObject((JsonObject)stages[0]);
                Set(s, "propMass", Math.Min(Lim.propMassMax, Math.Round(s.Num("propMass") * 1.5)));
                Set(s, "landingPropMass", 0d);
            }
            else
            {
                var below = (JsonObject)stages[at - 1];
                s = JsonMut.CloneObject(below);
                var vac = EngineFor(c, below.Str("propellant"), vacuum: true, keep: null);
                if (vac != null) Set(s, "engine", vac.id);
                Set(s, "engineCount", 1d);
                Set(s, "nozzle", "default");
                Set(s, "cluster", Obj(("pattern", "auto")));
                Set(s, "propMass", Math.Max(Lim.propMassMin, Math.Round(below.Num("propMass") * 0.25)));
                Set(s, "landingPropMass", 0d);
                Remove(s, "tune");
            }
            Set(s, "name", "");
            var before = Positions(stages);
            stages.Insert(at, s);
            Restack(d, before);
            return new Edit($"Added stage {at + 1}");
        }

        public static Edit RemoveStage(JsonObject d, int i)
        {
            var stages = d.Arr("stages");
            if (stages == null || stages.Count <= Lim.stagesMin || i < 0 || i >= stages.Count) return Edit.None;
            string label = HangarParts.StageLabel(d, i);
            var before = Positions(stages);
            stages.RemoveAt(i);
            Restack(d, before);
            return new Edit($"Removed {label}");
        }

        /// <summary>Move a stage to a new position. The stage keeps its engines, tanks and its own interstage shape.</summary>
        public static Edit MoveStage(JsonObject d, int from, int to)
        {
            var stages = d.Arr("stages");
            if (stages == null || from < 0 || from >= stages.Count) return Edit.None;
            to = Math.Max(0, Math.Min(stages.Count - 1, to));
            if (from == to) return Edit.None;
            string label = HangarParts.StageLabel(d, from);
            var before = Positions(stages);
            var s = stages[from];
            stages.RemoveAt(from);
            stages.Insert(to, s);
            Restack(d, before);
            return new Edit($"Moved {label}", $"position {from + 1}", $"position {to + 1}");
        }

        public static Edit SetStageName(JsonObject d, int i, string name)
        {
            var s = HangarParts.Body(d, PartRef.Stage(i));
            if (s == null) return Edit.None;
            name = CleanText(name, 24);
            string old = s.Str("name", "");
            if (old == name) return Edit.None;
            Set(s, "name", name);
            return new Edit($"Stage {i + 1} name", Quote(old), Quote(name));
        }

        // ---------------- engines ----------------

        public static Edit SetEngine(Catalog c, JsonObject d, PartRef p, string engineId)
        {
            var s = Engined(d, p);
            if (s == null || engineId == null || !c.EngineById.TryGetValue(engineId, out var e)) return Edit.None;
            string old = s.Str("engine");
            if (old == engineId) return Edit.None;
            Set(s, "engine", e.id);
            if (s.Str("propellant") != e.propellant) Set(s, "propellant", e.propellant);
            if (!EnginePoint.NozzleKnown(e, s.Str("nozzle"))) Set(s, "nozzle", "default");
            Remove(s, "tune"); // tuning belongs to the old engine (web picker does the same)
            if (p.kind == PartKind.Boosters) FixBoosterKind(c, s);
            return new Edit($"{HangarParts.Short(p)} engine", EngineLabel(c, old), EngineLabel(c, e.id));
        }

        /// <summary>Set the count. A fixed cluster pattern whose size no longer matches falls back to auto.</summary>
        public static Edit SetEngineCount(JsonObject d, PartRef p, int n)
        {
            var s = Engined(d, p);
            if (s == null) return Edit.None;
            n = Math.Max(Lim.engineCountMin, Math.Min(Lim.engineCountMax, n));
            int old = (int)s.Num("engineCount");
            if (old == n) return Edit.None;
            Set(s, "engineCount", (double)n);
            var pat = s.Obj("cluster")?.Str("pattern");
            if (pat != null && Lim.TryPattern(pat, out var pl) && pl.fixedCount && pl.count != n) Set(s.Obj("cluster"), "pattern", "auto");
            return new Edit($"{HangarParts.Short(p)} engines", I(old), I(n));
        }

        /// <summary>Pick a cluster pattern. A fixed pattern sets the engine count to its size.</summary>
        public static Edit SetClusterPattern(JsonObject d, PartRef p, string pattern)
        {
            var s = Engined(d, p);
            if (s == null || !Lim.TryPattern(pattern, out var pl)) return Edit.None;
            var cl = s.Obj("cluster");
            string old = cl?.Str("pattern") ?? "auto";
            bool countChanges = pl.fixedCount && (int)s.Num("engineCount") != pl.count;
            if (old == pattern && !countChanges) return Edit.None;
            if (cl == null) { cl = new JsonObject(); Set(s, "cluster", cl); }
            Set(cl, "pattern", pattern);
            if (countChanges) Set(s, "engineCount", (double)pl.count);
            return new Edit($"{HangarParts.Short(p)} engine layout", old, pattern);
        }

        /// <summary>Nozzle bell: "default" or one of the engine's catalog nozzles (e.g. "extended").</summary>
        public static Edit SetNozzle(Catalog c, JsonObject d, PartRef p, string nozzleId)
        {
            var s = Engined(d, p);
            if (s == null) return Edit.None;
            c.EngineById.TryGetValue(s.Str("engine") ?? "", out var e);
            if (string.IsNullOrEmpty(nozzleId)) nozzleId = "default";
            if (!EnginePoint.NozzleKnown(e, nozzleId)) return Edit.None;
            string old = s.Str("nozzle") ?? "default";
            if (old == nozzleId) return Edit.None;
            Set(s, "nozzle", nozzleId);
            return new Edit($"{HangarParts.Short(p)} nozzle", old, nozzleId);
        }

        /// <summary>Nozzle ids the current engine offers, default first.</summary>
        public static List<string> NozzleChoices(Catalog c, JsonObject d, PartRef p)
        {
            var o = new List<string> { "default" };
            var s = Engined(d, p);
            if (s == null || !c.EngineById.TryGetValue(s.Str("engine") ?? "", out var e) || e.nozzles == null) return o;
            foreach (var n in e.nozzles) if (n.id != "default" && !o.Contains(n.id)) o.Add(n.id);
            return o;
        }

        /// <summary>Engines that burn this part's propellant (the picker list).</summary>
        public static List<Engine> EngineChoices(Catalog c, JsonObject d, PartRef p)
        {
            var s = Engined(d, p);
            var o = new List<Engine>();
            if (s == null) return o;
            foreach (var e in c.Engines) if (e.propellant == s.Str("propellant")) o.Add(e);
            return o;
        }

        public static Edit SetTune(JsonObject d, PartRef p, double thrustPct, double ispPct)
        {
            var s = Engined(d, p);
            if (s == null) return Edit.None;
            double lim = Lim.tuneLimitPct;
            thrustPct = Math.Round(Clamp(thrustPct, -lim, lim));
            ispPct = Math.Round(Clamp(ispPct, -lim, lim));
            var t = s.Obj("tune");
            double ot = t?.Num("thrustPct") ?? 0, oi = t?.Num("ispPct") ?? 0;
            if (ot == thrustPct && oi == ispPct) return Edit.None;
            if (thrustPct == 0 && ispPct == 0) { s.Remove("tune"); s.Order.Remove("tune"); }
            else Set(s, "tune", Obj(("thrustPct", thrustPct), ("ispPct", ispPct)));
            return new Edit($"{HangarParts.Short(p)} tune", $"{Signed(ot)}%/{Signed(oi)}%", $"{Signed(thrustPct)}%/{Signed(ispPct)}%");
        }

        // ---------------- tanks ----------------

        /// <summary>Switch fuel. Keeps the engine when it already burns the new propellant, else picks one of the
        /// same kind (sea-level or vacuum). No engine for that propellant means no change.</summary>
        public static Edit SetPropellant(Catalog c, JsonObject d, PartRef p, string propellantId)
        {
            var s = Engined(d, p);
            if (s == null || propellantId == null || !c.PropellantById.ContainsKey(propellantId)) return Edit.None;
            string old = s.Str("propellant");
            if (old == propellantId) return Edit.None;
            c.EngineById.TryGetValue(s.Str("engine") ?? "", out var cur);
            bool vacuum = cur != null && cur.thrustSL == 0;
            var e = EngineFor(c, propellantId, vacuum, keep: cur);
            if (e == null) return Edit.None;
            Set(s, "propellant", propellantId);
            if (cur == null || cur.id != e.id)
            {
                Set(s, "engine", e.id);
                Remove(s, "tune");
                if (!EnginePoint.NozzleKnown(e, s.Str("nozzle"))) Set(s, "nozzle", "default");
            }
            if (p.kind == PartKind.Boosters) FixBoosterKind(c, s);
            return new Edit($"{HangarParts.Short(p)} fuel", Label(c.PropellantById, old), Label(c.PropellantById, propellantId));
        }

        public static Edit SetMaterial(Catalog c, JsonObject d, PartRef p, string materialId)
        {
            if (materialId == null || !c.MaterialById.ContainsKey(materialId)) return Edit.None;
            var s = p.kind == PartKind.Fairing ? d.Obj("fairing") : Engined(d, p);
            if (s == null) return Edit.None;
            string old = s.Str("material");
            if (old == materialId) return Edit.None;
            Set(s, "material", materialId);
            return new Edit($"{HangarParts.Short(p)} material", Label(c.MaterialById, old), Label(c.MaterialById, materialId));
        }

        /// <summary>Width of a stage, the boosters or the fairing, 0.3–15 m on a 0.1 m step. Propellant stays, so the tank gets shorter.</summary>
        public static Edit SetDiameter(JsonObject d, PartRef p, double m)
        {
            var s = p.kind == PartKind.Fairing ? d.Obj("fairing") : Engined(d, p);
            if (s == null || double.IsNaN(m)) return Edit.None;
            m = Step(Clamp(m, Lim.diameterMin, Lim.diameterMax), 0.1);
            double old = s.Num("diameter");
            if (old == m) return Edit.None;
            Set(s, "diameter", m);
            return new Edit($"{HangarParts.Short(p)} width", M(old), M(m));
        }

        public static Edit SetPropMass(JsonObject d, PartRef p, double kg)
        {
            var s = Engined(d, p);
            if (s == null || double.IsNaN(kg)) return Edit.None;
            kg = Math.Round(Clamp(kg, Lim.propMassMin, Lim.propMassMax));
            double old = s.Num("propMass");
            if (old == kg) return Edit.None;
            Set(s, "propMass", kg);
            CapReserve(s);
            return new Edit($"{HangarParts.Short(p)} fuel load", T(old), T(kg));
        }

        /// <summary>Tank length as the hangar draws it (DesignMath.SizeTank: barrel + domes for liquids).</summary>
        public static double TankLength(Catalog c, JsonObject part)
        {
            if (part == null) return 0;
            return DesignMath.SizeTank(c, part.Str("propellant"), part.Num("propMass"), part.Num("diameter")).length;
        }

        /// <summary>Inverse of SizeTank: the propellant that fills a tank of this length at this diameter.</summary>
        public static double PropMassForLength(Catalog c, string propellantId, double diameter, double lengthM)
        {
            if (!c.PropellantById.TryGetValue(propellantId ?? "", out var pr) || !(diameter > 0)) return 0;
            double barrel = lengthM - (pr.storability == "solid" ? 0 : DomeLengthFactor * diameter);
            if (!(barrel > 0)) return 0;
            double volume = barrel * Math.PI * diameter * diameter / 4;
            return volume / (1 + Ullage) * pr.bulkDensity;
        }

        /// <summary>Stretch or shrink the tank. Sets the propellant that fills that length; width stays.</summary>
        public static Edit SetTankLength(Catalog c, JsonObject d, PartRef p, double lengthM)
        {
            var s = Engined(d, p);
            if (s == null || double.IsNaN(lengthM)) return Edit.None;
            double before = TankLength(c, s);
            double kg = PropMassForLength(c, s.Str("propellant"), s.Num("diameter"), lengthM);
            kg = Math.Round(Clamp(kg, Lim.propMassMin, Lim.propMassMax));
            double old = s.Num("propMass");
            if (old == kg) return Edit.None;
            Set(s, "propMass", kg);
            CapReserve(s);
            return new Edit($"{HangarParts.Short(p)} tank length", M(before), M(TankLength(c, s)));
        }

        /// <summary>Fraction of stage propellant held back for landing, 0–30%.</summary>
        public static Edit SetLandingReserve(JsonObject d, int stage, double fraction)
        {
            var s = HangarParts.Body(d, PartRef.Stage(stage));
            if (s == null || double.IsNaN(fraction)) return Edit.None;
            fraction = Clamp(fraction, Lim.landingReserveMin, Lim.landingReserveMax);
            double kg = Math.Floor(s.Num("propMass") * fraction);
            double old = s.Num("landingPropMass");
            if (old == kg) return Edit.None;
            Set(s, "landingPropMass", kg);
            return new Edit($"Stage {stage + 1} landing fuel", T(old), T(kg));
        }

        // ---------------- interstages ----------------

        public static Edit SetInterstageShape(JsonObject d, int upperStage, string shape)
        {
            var s = HangarParts.Body(d, PartRef.Interstage(upperStage));
            if (s == null || upperStage < 1 || Array.IndexOf(InterstageShapes, shape) < 0) return Edit.None;
            var o = s.Obj("interstage");
            string old = o?.Str("shape") ?? "straight";
            if (old == shape) return Edit.None;
            if (o == null) { o = new JsonObject(); Set(s, "interstage", o); }
            Set(o, "shape", shape);
            return new Edit($"Interstage {upperStage}–{upperStage + 1} shape", old, shape);
        }

        /// <summary>Engineer override of the interstage mass. Null removes the override so the frustum mass is derived.</summary>
        public static Edit SetInterstageMass(JsonObject d, int upperStage, double? kg)
        {
            var s = HangarParts.Body(d, PartRef.Interstage(upperStage));
            if (s == null || upperStage < 1) return Edit.None;
            bool had = s.ContainsKey("interstageMass") && s["interstageMass"] is double;
            double old = s.Num("interstageMass");
            if (kg == null)
            {
                if (!had) return Edit.None;
                s.Remove("interstageMass"); s.Order.Remove("interstageMass");
                return new Edit($"Interstage {upperStage}–{upperStage + 1} mass", Kg(old), "auto");
            }
            if (double.IsNaN(kg.Value) || double.IsInfinity(kg.Value)) return Edit.None;
            double v = Math.Round(Math.Max(0, kg.Value));
            if (had && old == v) return Edit.None;
            Set(s, "interstageMass", v);
            return new Edit($"Interstage {upperStage}–{upperStage + 1} mass", had ? Kg(old) : "auto", Kg(v));
        }

        // ---------------- boosters ----------------

        /// <summary>0, 2 or 4 strap-ons. Turning them on for the first time loads a solid set that can lift itself.</summary>
        public static Edit SetBoosterCount(Catalog c, JsonObject d, int n)
        {
            if (!Lim.strapOnCounts.Contains(n)) return Edit.None;
            var so = d.Obj("strapOns");
            if (so == null) { so = DefaultBoosters(); Set(d, "strapOns", so); }
            int old = (int)so.Num("count");
            if (old == n) return Edit.None;
            Set(so, "count", (double)n);
            if (n > 0)
            {
                if (!(so.Num("propMass") > 0)) Set(so, "propMass", 40000d);
                if (!c.EngineById.ContainsKey(so.Str("engine") ?? "")) { Set(so, "engine", "srm-small"); Set(so, "propellant", "solid-apcp"); }
                if (!(so.Num("engineCount") >= 1)) Set(so, "engineCount", 1d);
                if (!(so.Num("diameter") >= Lim.diameterMin)) Set(so, "diameter", 1.6);
                if (so.Str("material") == null) Set(so, "material", "steel");
                if (!(so.ContainsKey("crossfeed") && so["crossfeed"] is bool)) Set(so, "crossfeed", false);
                if (so.Obj("cluster") == null) Set(so, "cluster", Obj(("pattern", "auto")));
                if (so.Str("nozzle") == null) Set(so, "nozzle", "default");
                FixBoosterKind(c, so);
            }
            return new Edit("Boosters", I(old), I(n));
        }

        /// <summary>Solid boosters burn APCP with no crossfeed; liquid boosters copy stage 1's propellant and engine.</summary>
        public static Edit SetBoosterKind(Catalog c, JsonObject d, string kind)
        {
            var so = d.Obj("strapOns");
            if (so == null || (kind != "solid" && kind != "liquid")) return Edit.None;
            string old = so.Str("kind");
            if (old == kind) return Edit.None;
            Set(so, "kind", kind);
            if (kind == "solid")
            {
                Set(so, "propellant", "solid-apcp"); Set(so, "engine", "srm-small"); Set(so, "engineCount", 1d);
                Set(so, "material", "steel"); Set(so, "crossfeed", false); Set(so, "nozzle", "default");
                Set(so, "cluster", Obj(("pattern", "auto")));
            }
            else
            {
                var s0 = HangarParts.Body(d, PartRef.Stage(0));
                bool liquid0 = s0 != null && c.PropellantById.TryGetValue(s0.Str("propellant") ?? "", out var p0) && p0.storability != "solid";
                Set(so, "propellant", liquid0 ? s0.Str("propellant") : "kerolox");
                var e = EngineFor(c, so.Str("propellant"), vacuum: false, keep: liquid0 && c.EngineById.TryGetValue(s0.Str("engine") ?? "", out var e0) ? e0 : null);
                Set(so, "engine", e.id); Set(so, "material", "al-li"); Set(so, "nozzle", "default");
                if (!(so.Num("engineCount") >= 1)) Set(so, "engineCount", 1d);
            }
            return new Edit("Booster type", old ?? "none", kind);
        }

        public static Edit SetCrossfeed(JsonObject d, bool on)
        {
            var so = d.Obj("strapOns");
            if (so == null || so.Bool("crossfeed") == on || (on && so.Str("kind") == "solid")) return Edit.None;
            Set(so, "crossfeed", on);
            return new Edit("Booster crossfeed", OnOff(!on), OnOff(on));
        }

        // ---------------- fairing and payload ----------------

        public static Edit SetFairingLength(JsonObject d, double m)
        {
            var f = d.Obj("fairing");
            if (f == null || double.IsNaN(m)) return Edit.None;
            m = Step(Clamp(m, Lim.fairingLengthMin, Lim.fairingLengthMax), 0.1);
            double old = f.Num("length");
            if (old == m) return Edit.None;
            Set(f, "length", m);
            return new Edit("Fairing length", M(old), M(m));
        }

        public static Edit SetNose(JsonObject d, string nose)
        {
            if (Array.IndexOf(Noses, nose) < 0) return Edit.None;
            var a = d.Obj("aero");
            if (a == null) { a = Obj(("nose", "ogive"), ("fins", false)); Set(d, "aero", a); }
            string old = a.Str("nose");
            if (old == nose) return Edit.None;
            Set(a, "nose", nose);
            return new Edit("Nose shape", old, nose);
        }

        public static Edit SetJettisonAltitude(JsonObject d, double km)
        {
            var f = d.Obj("fairing");
            if (f == null || double.IsNaN(km)) return Edit.None;
            km = Step(Clamp(km, Lim.jettisonMinKm, Lim.jettisonMaxKm), 5);
            double old = f.Num("jettisonAltitudeKm", 110);
            if (f.ContainsKey("jettisonAltitudeKm") && old == km) return Edit.None;
            Set(f, "jettisonAltitudeKm", km);
            return new Edit("Fairing release height", $"{I(old)} km", $"{I(km)} km");
        }

        /// <summary>Pick a payload. Its catalog mass and home orbit come with it.</summary>
        public static Edit SetPayloadType(Catalog c, JsonObject d, string id)
        {
            if (id == null || !c.PayloadById.TryGetValue(id, out var pl)) return Edit.None;
            var p = d.Obj("payload");
            if (p == null) { p = new JsonObject(); Set(d, "payload", p); }
            string old = p.Str("type");
            if (old == id) return Edit.None;
            Set(p, "type", id);
            Set(p, "mass", Clamp(pl.mass, Lim.payloadMassMin, Lim.payloadMassMax));
            if (pl.orbit != null && c.Orbits.ContainsKey(pl.orbit)) Set(p, "orbit", pl.orbit);
            return new Edit("Payload", PayloadName(c, old), PayloadName(c, id));
        }

        public static Edit SetPayloadMass(JsonObject d, double kg)
        {
            var p = d.Obj("payload");
            if (p == null || double.IsNaN(kg)) return Edit.None;
            kg = Math.Round(Clamp(kg, Lim.payloadMassMin, Lim.payloadMassMax));
            double old = p.Num("mass");
            if (old == kg) return Edit.None;
            Set(p, "mass", kg);
            return new Edit("Payload mass", Kg(old), Kg(kg));
        }

        public static Edit SetOrbit(Catalog c, JsonObject d, string orbitId)
        {
            var p = d.Obj("payload");
            if (p == null || orbitId == null || !c.Orbits.TryGetValue(orbitId, out var o)) return Edit.None;
            string old = p.Str("orbit");
            if (old == orbitId) return Edit.None;
            Set(p, "orbit", orbitId);
            return new Edit("Destination", old != null && c.Orbits.TryGetValue(old, out var oo) ? oo.label : old, o.label);
        }

        // ---------------- aero, recovery, avionics ----------------

        public static Edit SetFins(JsonObject d, bool on)
        {
            var a = Aero(d);
            if (a.Bool("fins") == on) return Edit.None;
            Set(a, "fins", on);
            if (on && !a.ContainsKey("finCount")) Set(a, "finCount", 4d);
            if (on && !a.ContainsKey("finSpanM")) Set(a, "finSpanM", 1d);
            return new Edit("Fins", OnOff(!on), OnOff(on));
        }

        public static Edit SetFinCount(JsonObject d, int n)
        {
            var a = Aero(d);
            n = Math.Max(Lim.finCountMin, Math.Min(Lim.finCountMax, n));
            int old = (int)a.Num("finCount", 4);
            if (a.ContainsKey("finCount") && old == n) return Edit.None;
            Set(a, "finCount", (double)n);
            return new Edit("Fin count", I(old), I(n));
        }

        public static Edit SetFinSpan(JsonObject d, double m)
        {
            var a = Aero(d);
            if (double.IsNaN(m)) return Edit.None;
            m = Step(Clamp(m, Lim.finSpanMin, Lim.finSpanMax), Lim.finSpanStep);
            double old = a.Num("finSpanM", 1);
            if (a.ContainsKey("finSpanM") && old == m) return Edit.None;
            Set(a, "finSpanM", m);
            return new Edit("Fin size", M(old), M(m));
        }

        public static Edit SetLegs(JsonObject d, bool on) => Landing(d, "legs", "Landing legs", on);
        public static Edit SetGridFins(JsonObject d, bool on) => Landing(d, "gridFins", "Grid fins", on);
        public static Edit SetRcs(JsonObject d, bool on) => Landing(d, "rcs", "Steering jets", on);

        public static Edit SetAvionics(JsonObject d, string tier)
        {
            if (Array.IndexOf(AvionicsTiers, tier) < 0) return Edit.None;
            var a = d.Obj("avionics");
            if (a == null) { a = new JsonObject(); Set(d, "avionics", a); }
            string old = a.Str("tier");
            if (old == tier) return Edit.None;
            Set(a, "tier", tier);
            return new Edit("Avionics", old, tier);
        }

        public static Edit SetSafetyFactor(JsonObject d, double v)
        {
            if (double.IsNaN(v)) return Edit.None;
            v = Step(Clamp(v, Lim.safetyMin, Lim.safetyMax), Lim.safetyStep);
            double old = d.Num("safetyFactor", 1.4);
            if (d.ContainsKey("safetyFactor") && old == v) return Edit.None;
            Set(d, "safetyFactor", v);
            return new Edit("Safety factor", F(old, 2), F(v, 2));
        }

        // ---------------- look: paint, livery, finishes, decals ----------------

        /// <summary>Name painted on the rocket (livery.name), 1–20 characters. Empty clears it.</summary>
        public static Edit SetVehicleName(JsonObject d, string name)
        {
            var lv = Livery(d);
            name = CleanText(name, VehicleNameMax);
            string old = lv.Str("name", "");
            if (old == name) return Edit.None;
            Set(lv, "name", name);
            return new Edit("Rocket name", Quote(old), Quote(name));
        }

        /// <summary>Paint colours as #RRGGBB. Null keeps that colour. Picking colours by hand clears the livery id.</summary>
        public static Edit SetPaint(JsonObject d, string primary, string accent)
        {
            var lv = Livery(d);
            if ((primary != null && !Hex.IsMatch(primary)) || (accent != null && !Hex.IsMatch(accent))) return Edit.None;
            string op = lv.Str("primary"), oa = lv.Str("accent");
            string np = primary != null ? primary.ToUpperInvariant() : op, na = accent != null ? accent.ToUpperInvariant() : oa;
            if (string.Equals(op, np, StringComparison.OrdinalIgnoreCase) && string.Equals(oa, na, StringComparison.OrdinalIgnoreCase)) return Edit.None;
            Set(lv, "primary", np); Set(lv, "accent", na);
            var look = d.Obj("look");
            if (look != null && look.ContainsKey("livery")) { look.Remove("livery"); look.Order.Remove("livery"); }
            return new Edit(primary != null && accent == null ? "Main colour" : accent != null && primary == null ? "Stripe colour" : "Paint", $"{op}/{oa}", $"{np}/{na}");
        }

        /// <summary>Wear an earned livery: its pattern id and both colours.</summary>
        public static Edit ApplyLivery(HangarData h, HangarProgress progress, JsonObject d, string liveryId)
        {
            var def = h?.Livery(liveryId);
            if (def == null || !(progress ?? HangarProgress.None).Earned(def.earn)) return Edit.None;
            var look = Look(d);
            var lv = Livery(d);
            string old = look.Str("livery");
            if (old == liveryId && lv.Str("primary") == def.primary && lv.Str("accent") == def.accent) return Edit.None;
            Set(look, "livery", liveryId);
            Set(lv, "primary", def.primary); Set(lv, "accent", def.accent);
            return new Edit("Livery", old ?? "custom", liveryId);
        }

        /// <summary>Surface finish for a part slot (tank, booster, fairing, ...). Null returns the slot to the default look.</summary>
        public static Edit SetFinish(HangarData h, HangarProgress progress, JsonObject d, string slot, string finishId)
        {
            if (h == null || slot == null || !h.finishSlots.Contains(slot)) return Edit.None;
            var fin = d.Obj("look")?.Obj("finishes");
            string old = fin?.Str(slot);
            if (finishId == null)
            {
                if (old == null) return Edit.None; // nothing to reset: never create an empty look
                fin.Remove(slot); fin.Order.Remove(slot);
                return new Edit($"{Cap(slot)} finish", old, "default");
            }
            var def = h.Finish(finishId);
            if (def == null || !def.partTypes.Contains(slot) || !(progress ?? HangarProgress.None).Earned(def.earn)) return Edit.None;
            if (old == finishId) return Edit.None;
            var look = Look(d);
            if (fin == null) { fin = new JsonObject(); Set(look, "finishes", fin); }
            Set(fin, slot, finishId);
            return new Edit($"{Cap(slot)} finish", old ?? "default", finishId);
        }

        public static int DecalCount(JsonObject d) => d.Obj("look")?.Arr("decals")?.Count ?? 0;

        /// <summary>Place an earned decal in a slot. Max 6 per vehicle, one decal per slot, content must suit the slot.</summary>
        public static Edit AddDecal(HangarData h, HangarProgress progress, JsonObject d, string decalId, string slotId, double scale = 1, int rotate = 0)
        {
            if (h == null) return Edit.None;
            var def = h.Decal(decalId);
            var slot = h.DecalSlot(slotId);
            if (def == null || slot == null || slot.auto || !slot.content.Contains(def.content)) return Edit.None;
            if (!(progress ?? HangarProgress.None).Earned(def.earn)) return Edit.None;
            if (!h.decalScales.Contains(scale) || !h.decalRotations.Contains(rotate)) return Edit.None;
            var look = Look(d);
            var list = look.Arr("decals");
            if (list == null) { list = new List<object>(); Set(look, "decals", list); }
            if (list.Count >= h.maxDecals) return Edit.None;
            foreach (JsonObject x in list) if (x.Str("slot") == slotId) return Edit.None;
            list.Add(Obj(("id", decalId), ("slot", slotId), ("scale", scale), ("rotate", (double)rotate)));
            return new Edit($"Added decal {decalId}");
        }

        public static Edit RemoveDecal(JsonObject d, int index)
        {
            var list = d.Obj("look")?.Arr("decals");
            if (list == null || index < 0 || index >= list.Count) return Edit.None;
            string id = (list[index] as JsonObject)?.Str("id");
            list.RemoveAt(index);
            return new Edit($"Removed decal {id}");
        }

        public static Edit SetDecalScale(HangarData h, JsonObject d, int index, double scale)
        {
            var x = DecalAt(d, index);
            if (x == null || h == null || !h.decalScales.Contains(scale)) return Edit.None;
            double old = x.Num("scale", 1);
            if (old == scale) return Edit.None;
            Set(x, "scale", scale);
            return new Edit("Decal size", F(old, 1) + "×", F(scale, 1) + "×");
        }

        public static Edit SetDecalRotation(HangarData h, JsonObject d, int index, int rotate)
        {
            var x = DecalAt(d, index);
            if (x == null || h == null || !h.decalRotations.Contains(rotate)) return Edit.None;
            int old = (int)x.Num("rotate");
            if (old == rotate) return Edit.None;
            Set(x, "rotate", (double)rotate);
            return new Edit("Decal turn", I(old) + "°", I(rotate) + "°");
        }

        // ---------------- whole design ----------------

        /// <summary>Copy a starter template into a new design tagged meta.forkedFrom (same shape as web templates-fork.js).</summary>
        public static JsonObject ForkTemplate(StarterTemplate t)
        {
            var d = Save.DesignMigration.Migrate(t.design);
            if (d.ContainsKey("id")) { d.Remove("id"); d.Order.Remove("id"); }
            var meta = d.Obj("meta");
            if (meta == null) { meta = new JsonObject(); Set(d, "meta", meta); }
            Set(meta, "forkedFrom", Obj(("templateId", t.id), ("templateVersion", (double)t.version)));
            Normalize(d);
            return d;
        }

        /// <summary>
        /// Keep the stack consistent after structural edits: stage 1 has no interstage of its own, every upper stage
        /// has an interstage shape, default names follow position, the landing reserve stays inside its cap.
        /// </summary>
        public static void Normalize(JsonObject d)
        {
            var stages = d.Arr("stages");
            if (stages == null) return;
            for (int i = 0; i < stages.Count; i++)
            {
                if (!(stages[i] is JsonObject s)) continue;
                if (i == 0)
                {
                    if (s.ContainsKey("interstage")) { s.Remove("interstage"); s.Order.Remove("interstage"); }
                }
                else if (s.Obj("interstage") == null) Set(s, "interstage", Obj(("shape", "straight")));
                if (s.Obj("cluster") == null) Set(s, "cluster", Obj(("pattern", "auto")));
                if (s.Str("nozzle") == null) Set(s, "nozzle", "default");
                if (!s.ContainsKey("separationMass")) Set(s, "separationMass", 50d);
                if (!s.ContainsKey("landingPropMass")) Set(s, "landingPropMass", 0d);
                if (s.Str("name", "").Length == 0) Set(s, "name", $"Stage {i + 1}");
                CapReserve(s);
            }
        }

        static Dictionary<JsonObject, int> Positions(List<object> stages)
        {
            var o = new Dictionary<JsonObject, int>();
            for (int i = 0; i < stages.Count; i++) if (stages[i] is JsonObject j) o[j] = i;
            return o;
        }

        /// <summary>
        /// After add/remove/move: a name that was its old position's default ("Stage 3" at index 2) follows the new
        /// position; any other name, including a custom "Stage 42", is kept. A stage that becomes the bottom stage
        /// drops its interstage mass override, since the joint under it no longer exists.
        /// </summary>
        static void Restack(JsonObject d, Dictionary<JsonObject, int> before)
        {
            var stages = d.Arr("stages");
            for (int i = 0; i < stages.Count; i++)
            {
                if (!(stages[i] is JsonObject s) || !before.TryGetValue(s, out int was) || was == i) continue;
                if (s.Str("name", "") == $"Stage {was + 1}") Set(s, "name", $"Stage {i + 1}");
                if (i == 0) Remove(s, "interstageMass");
            }
            Normalize(d);
        }

        static void Remove(JsonObject o, string k)
        {
            if (o != null && o.ContainsKey(k)) { o.Remove(k); o.Order.Remove(k); }
        }

        // ---------------- helpers ----------------

        static JsonObject Engined(JsonObject d, PartRef p) => p.HasEngines ? HangarParts.Body(d, p) : null;

        static void CapReserve(JsonObject s)
        {
            double cap = Math.Floor(s.Num("propMass") * Lim.landingReserveMax);
            if (s.Num("landingPropMass") > cap) Set(s, "landingPropMass", cap);
        }

        static void FixBoosterKind(Catalog c, JsonObject so)
        {
            bool solid = c.PropellantById.TryGetValue(so.Str("propellant") ?? "", out var pr) && pr.storability == "solid";
            Set(so, "kind", solid ? "solid" : "liquid");
            if (solid) Set(so, "crossfeed", false);
        }

        /// <summary>Engine for a propellant: <paramref name="keep"/> when it already fits, else the first of the wanted kind, else any.</summary>
        static Engine EngineFor(Catalog c, string propellant, bool vacuum, Engine keep)
        {
            if (keep != null && keep.propellant == propellant) return keep;
            Engine first = null;
            foreach (var e in c.Engines)
            {
                if (e.propellant != propellant) continue;
                if (first == null) first = e;
                if (vacuum ? e.thrustSL == 0 : e.thrustSL > 0) return e;
            }
            return first;
        }

        static JsonObject NewStage(string prop, string engine, int n, double propMass, double diameter, string material) => Obj(
            ("name", ""), ("propellant", prop), ("engine", engine), ("engineCount", (double)n), ("propMass", propMass),
            ("diameter", diameter), ("material", material), ("separationMass", 50d), ("landingPropMass", 0d),
            ("cluster", Obj(("pattern", "auto"))), ("nozzle", "default"));

        static JsonObject DefaultBoosters() => Obj(
            ("count", 0d), ("kind", "solid"), ("propellant", "solid-apcp"), ("engine", "srm-small"), ("engineCount", 1d),
            ("propMass", 0d), ("diameter", 1.6), ("material", "steel"), ("crossfeed", false),
            ("cluster", Obj(("pattern", "auto"))), ("nozzle", "default"));

        static Edit Landing(JsonObject d, string key, string what, bool on)
        {
            var l = d.Obj("landing");
            if (l == null) { l = Obj(("legs", false), ("gridFins", false), ("rcs", false)); Set(d, "landing", l); }
            if (l.Bool(key) == on && l.ContainsKey(key)) return Edit.None;
            Set(l, key, on);
            return new Edit(what, OnOff(!on), OnOff(on));
        }

        static JsonObject Aero(JsonObject d)
        {
            var a = d.Obj("aero");
            if (a == null) { a = Obj(("nose", "ogive"), ("fins", false)); Set(d, "aero", a); }
            return a;
        }

        static JsonObject Livery(JsonObject d)
        {
            var lv = d.Obj("livery");
            if (lv == null) { lv = Obj(("primary", "#F2F2F2"), ("accent", "#1A3C6E"), ("name", "")); Set(d, "livery", lv); }
            return lv;
        }

        static JsonObject Look(JsonObject d)
        {
            var look = d.Obj("look");
            if (look == null) { look = new JsonObject(); Set(d, "look", look); }
            return look;
        }

        static JsonObject DecalAt(JsonObject d, int index)
        {
            var list = d.Obj("look")?.Arr("decals");
            return list != null && index >= 0 && index < list.Count ? list[index] as JsonObject : null;
        }

        static void Set(JsonObject o, string k, object v) => JsonMut.Set(o, k, v);

        static JsonObject Obj(params (string k, object v)[] kv)
        {
            var o = new JsonObject();
            foreach (var (k, v) in kv) o.Add(k, v);
            return o;
        }

        /// <summary>Trim, collapse inner whitespace, drop control characters, cap the length.</summary>
        public static string CleanText(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder();
            bool space = false;
            foreach (char ch in s.Trim())
            {
                if (char.IsWhiteSpace(ch)) { if (!space) sb.Append(' '); space = true; continue; }
                if (char.IsControl(ch)) continue;
                space = false;
                sb.Append(ch);
            }
            var o = sb.ToString();
            if (o.Length <= max) return o;
            int cut = max;
            if (char.IsHighSurrogate(o[cut - 1])) cut--; // never split an emoji or other surrogate pair
            return o.Substring(0, cut).TrimEnd();
        }

        static double Clamp(double v, double lo, double hi) => v < lo ? lo : v > hi ? hi : v;
        static double Step(double v, double step) => step > 0 ? Math.Round(Math.Round(v / step) * step, 6) : v;
        static string I(double v) => ((long)Math.Round(v)).ToString(CultureInfo.InvariantCulture);
        static string F(double v, int dp) => v.ToString("0." + new string('#', Math.Max(1, dp)), CultureInfo.InvariantCulture);
        static string M(double v) => F(v, 1) + " m";
        static string Kg(double v) => I(v) + " kg";
        static string T(double kg) => kg >= 10000 ? I(kg / 1000) + " t" : F(kg / 1000, 1) + " t";
        static string Signed(double v) => (v > 0 ? "+" : "") + I(v);
        static string OnOff(bool on) => on ? "on" : "off";
        static string Quote(string s) => "\"" + s + "\"";
        static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        static string EngineLabel(Catalog c, string id) => id != null && c.EngineById.TryGetValue(id, out var e) ? e.label : id ?? "none";
        static string PayloadName(Catalog c, string id) => id != null && c.PayloadById.TryGetValue(id, out var p) ? (p.displayName ?? p.label) : id ?? "none";
        static string Label(Dictionary<string, Propellant> map, string id) => id != null && map.TryGetValue(id, out var p) ? p.label : id;
        static string Label(Dictionary<string, Material> map, string id) => id != null && map.TryGetValue(id, out var m) ? m.label : id;
    }
}
