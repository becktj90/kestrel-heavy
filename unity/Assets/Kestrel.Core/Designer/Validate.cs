// Engineering validation and "Make it fly". Port of web/js/designer/validate.js.
// Operates on the raw JSON tree: Design.FromJson truncates engineCount and hides schema errors.
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Designer
{
    public sealed class Issue
    {
        public string code, tab, msg, kid, field;
        public int? index;
    }

    public sealed class EngineeringReport
    {
        public bool ok;
        public List<Issue> blocks = new List<Issue>();
        public List<Issue> warnings = new List<Issue>();
        public QuickReadout readout;
    }

    public static class Validate
    {
        public const double MIN_TWR = 1.0, GOOD_TWR = 1.25, MIN_MARGIN = 0.05, PACK = 1.1;
        static DesignerLimits Lim => DesignerLimits.Active;
        /// <summary>Warn above this powered-flight max-Q. Never a block. Unguided solids are exempt.</summary>
        public const double MaxQWarnKPa = 45;

        /// <summary>Cargo volume the fairing must hold (<see cref="PayloadFit.SpecFor(Catalog, JsonObject)"/>); 0 without a payload.</summary>
        public static double PayloadVolume(Catalog c, JsonObject d) => PayloadFit.SpecFor(c, d)?.volume ?? 0;

        public static EngineeringReport Engineering(Catalog c, JsonObject design)
        {
            var blocks = new List<Issue>();
            var warnings = new List<Issue>();
            var shape = Shape(c, design);
            var stages = design.Arr("stages") ?? new List<object>();
            bool zeroEng = false, zeroProp = false;
            foreach (JsonObject s in stages)
            {
                if (!(s.Num("engineCount") >= 1)) zeroEng = true;
                if (!(s.Num("propMass") > 0)) zeroProp = true;
            }
            var so = design.Obj("strapOns");
            if (JsonMut.TruthyNumber(so, "count") && !(so.Num("engineCount") >= 1)) zeroEng = true;
            if (so != null && so.Num("count") > 0 && !(so.Num("propMass") > 0)) zeroProp = true;
            if (zeroEng) blocks.Add(Iss("zero-engines", "engines", "Every stage needs at least one engine. Try adding one."));
            if (zeroProp) blocks.Add(Iss("zero-propellant", "propellants", "A stage has no propellant. Try loading some fuel."));
            if (!shape.ok && !zeroEng && !zeroProp) foreach (var e in shape.errors) blocks.Add(Iss("schema", "stages", e));
            DesignRules.SchemaBlocks(c, design, blocks);
            if (blocks.Count > 0) return new EngineeringReport { ok = false, blocks = blocks, warnings = warnings };
            blocks.AddRange(Geometry(c, design));
            QuickReadout r = null;
            try { r = Readout.Quick(c, design); }
            catch (Exception e) { blocks.Add(Iss("math", "stages", "Couldn't compute this design (" + e.Message + ").")); }
            if (r != null)
            {
                if (!(r.liftoffTWR > Lim.minTwr)) blocks.Add(Iss("twr-low", "engines", $"Liftoff thrust-to-weight is {Fmt(r.liftoffTWR)}; it must be above 1 to leave the pad. Try more first-stage engines or less propellant."));
                else if (r.liftoffTWR < Lim.goodTwr) warnings.Add(Iss("twr-marginal", "engines", $"Liftoff TWR {Fmt(r.liftoffTWR)} is slow off the pad (lots of gravity loss). Aim for {Lim.goodTwr.ToString(System.Globalization.CultureInfo.InvariantCulture)} or more."));
                for (int i = 1; i < r.stages.Count; i++)
                {
                    var s = r.stages[i];
                    // designIndex, not the serial readout index. With strap-ons, serial i=1 is still stage 0.
                    if (s.twrVac < Lim.weakUpperTwrVac && s.designIndex >= 0) warnings.Add(Iss("weak-upper", "engines", $"{s.label} pushes at only {Fmt(s.twrVac)} g. A stronger engine or lighter load would help it reach orbit.", s.designIndex));
                }
                if (r.stages.Count > 1 && r.stages[r.stages.Count - 1].deltaV < Lim.weakUpperDvFraction * r.totalDeltaV)
                    warnings.Add(Iss("weak-upper-dv", "propellants", "The top stage does little of the work. More upper-stage propellant usually pays off."));
                if (r.margin < Lim.minMargin)
                    warnings.Add(Iss("dv-margin", "propellants", r.margin < 0
                        ? $"About {Fmt(-r.margin * r.required, 0)} m/s short of {r.orbitLabel}. Try more propellant, a lighter payload, or a higher-Isp upper stage."
                        : $"Only {Fmt(r.margin * 100, 0)}% \u0394v margin for {r.orbitLabel}. A little more propellant gives breathing room."));
                bool landingFuel = false;
                foreach (JsonObject s in stages) if (s.Num("landingPropMass") > 0) landingFuel = true;
                var landing = design.Obj("landing");
                if (landingFuel && !(landing != null && landing.Bool("legs") && landing.Bool("gridFins")))
                    warnings.Add(Iss("landing-hardware", "recovery", "Landing fuel is reserved but legs or grid fins are missing. Add them, or set the landing reserve to 0."));
                if (r.mass.fairing > Math.Max(Lim.heavyFairingKg, r.mass.payload))
                    warnings.Add(Iss("heavy-fairing", "payload", $"The fairing ({Fmt(r.mass.fairing / 1000, 1)} t) outweighs the payload. A composite or shorter fairing saves mass."));
                // Version 2 stability and max-Q wording lives in DesignRules. Emitting both would double the issues.
                if ((int)design.Num("version") < 2)
                    AeroNotes(c, design, r, blocks, warnings);
            }
            if (r != null) DesignRules.Append(c, design, blocks, warnings, r);
            return new EngineeringReport { ok = blocks.Count == 0, blocks = blocks, warnings = warnings, readout = r };
        }

        /// <summary>
        /// v1 readout notes. Same steering and solid exemption as <see cref="DesignRules"/>:
        /// a gimbal on stage 1 or a strap-on steers, including a gimballed solid, and the max-Q
        /// exemption is a stage-1 solid with gimbal 0. An unguided rocket below 0 cal blocks.
        /// The 1–2 cal band and the &gt;4 cal overstable rule apply only to finned or pad-unguided rockets.
        /// </summary>
        static void AeroNotes(Catalog c, JsonObject design, QuickReadout r, List<Issue> blocks, List<Issue> warnings)
        {
            var parsed = Design.FromJson(design);
            bool finned = parsed.fins;
            bool steered = DesignRules.PadSteered(c, parsed);
            bool aeroBand = finned || !steered;
            if (r.stabilityCal < 0)
            {
                if (steered)
                    warnings.Add(Iss("unstable", "aero", $"Liftoff stability is {Fmt(r.stabilityCal)} cal (CP above the CG). The pad engines are engine-steered, so this does not stop the launch."));
                else
                    blocks.Add(Iss("unstable", "aero", "An unguided rocket below 0 cal cannot launch. Add fins or move mass forward."));
            }
            else if (aeroBand && r.stabilityCal < 1)
                warnings.Add(Iss("unstable", "aero", $"Liftoff stability is {Fmt(r.stabilityCal)} cal. Finned and unguided rockets aim for 1–2 cal."));
            if (aeroBand && r.stabilityCal > 4)
                warnings.Add(Iss("overstable", "aero", $"Liftoff stability is {Fmt(r.stabilityCal)} cal, past 4. It may weathercock in the wind."));
            if (!r.maxQStale && r.maxQ_kPa.HasValue && r.maxQ_kPa.Value > MaxQWarnKPa && !DesignRules.UnguidedSolid(c, parsed))
                warnings.Add(Iss("maxq-high", "aero", $"Max-Q is {Fmt(r.maxQ_kPa.Value, 1)} kPa, above {MaxQWarnKPa.ToString(System.Globalization.CultureInfo.InvariantCulture)} kPa. This warns only; it does not stop the launch."));
        }

        public static (JsonObject design, List<string> changes) MakeItFly(Catalog c, JsonObject input)
        {
            var d = JsonMut.CloneObject(input);
            var changes = new List<string>();
            void Say(string m) { if (!changes.Contains(m)) changes.Add(m); }
            if (d.Arr("stages") == null || d.Arr("stages").Count == 0)
            {
                JsonMut.Set(d, "stages", new List<object> { Obj(
                    ("name", "Stage 1"), ("propellant", "kerolox"), ("engine", "gg-kerolox"), ("engineCount", 5d),
                    ("propMass", 100000d), ("diameter", 3d), ("material", "al-li"), ("interstageMass", 0d),
                    ("separationMass", 50d), ("landingPropMass", 0d)) });
                Say("Added a first stage");
            }
            var stages = d.Arr("stages");
            for (int i = 0; i < stages.Count; i++) FixPart(c, (JsonObject)stages[i], i > 0, Say);
            var strap = d.Obj("strapOns");
            if (JsonMut.TruthyNumber(strap, "count"))
            {
                if (strap.Str("kind") == "solid") { JsonMut.Set(strap, "propellant", "solid-apcp"); JsonMut.Set(strap, "crossfeed", false); }
                FixPart(c, strap, false, Say);
            }
            for (int i = 1; i < stages.Count; i++)
            {
                var up = (JsonObject)stages[i]; var below = (JsonObject)stages[i - 1];
                if (up.Num("diameter") > below.Num("diameter") * Lim.topHeavyRatio) { JsonMut.Set(up, "diameter", below.Num("diameter")); Say("Matched upper stage width to the stage below"); }
            }
            DesignRules.FixInterstage(d, Say);
            var top = (JsonObject)stages[stages.Count - 1];
            var fairing = d.Obj("fairing");
            if (fairing.Num("diameter") < top.Num("diameter")) { JsonMut.Set(fairing, "diameter", top.Num("diameter")); Say("Widened the fairing to match the top stage"); }
            var spec = PayloadFit.SpecFor(c, d);
            if (spec != null)
            {
                double needD = PayloadFit.RequiredDiameter(spec);
                if (fairing.Num("diameter") < needD)
                {
                    JsonMut.Set(fairing, "diameter", Math.Ceiling((needD - PayloadFit.DiameterToleranceM) * 10) / 10);
                    Say("Widened the fairing so the payload fits");
                }
                if (!PayloadFit.Check(fairing.Num("diameter"), fairing.Num("length"), spec).fits)
                {
                    JsonMut.Set(fairing, "length", Math.Ceiling(PayloadFit.RequiredLength(spec, fairing.Num("diameter")) * 10) / 10 + 0.5);
                    Say("Stretched the fairing so the payload fits");
                }
            }
            bool landingFuel = false;
            foreach (JsonObject s in stages) if (s.Num("landingPropMass") > 0) landingFuel = true;
            var landing = d.Obj("landing");
            if (landingFuel && !(landing.Bool("legs") && landing.Bool("gridFins")))
            {
                JsonMut.Set(landing, "legs", true); JsonMut.Set(landing, "gridFins", true);
                Say("Added landing legs and grid fins");
            }
            for (int iter = 0; iter < 80; iter++)
            {
                var v = Engineering(c, d);
                var r = v.readout;
                if (r == null) break;
                var s0 = (JsonObject)stages[0];
                if (r.liftoffTWR < 1.3)
                {
                    var e = c.EngineById[s0.Str("engine")];
                    var ep = EnginePoint.Of(e, s0.Str("nozzle"));
                    double maxFit = Math.Floor(Lim.pack * s0.Num("diameter") * s0.Num("diameter") / (ep.diameter * ep.diameter));
                    if (s0.Num("engineCount") < Math.Min(Lim.engineCountMax, maxFit)) { JsonMut.Set(s0, "engineCount", s0.Num("engineCount") + 1); Say("Added first-stage engines for a stronger liftoff"); }
                    else if (s0.Num("diameter") < Lim.diameterMax && s0.Num("engineCount") < Lim.engineCountMax) { JsonMut.Set(s0, "diameter", Math.Min(Lim.diameterMax, JsFormat.ToFixedNumber(s0.Num("diameter") + 0.3, 1))); Say("Widened the first stage to fit more engines"); }
                    else { JsonMut.Set(s0, "propMass", s0.Num("propMass") * 0.9); Say("Trimmed first-stage propellant to lighten liftoff"); }
                    continue;
                }
                int weak = -1;
                for (int i = 1; i < r.stages.Count; i++) if (r.stages[i].twrVac < 0.5) { weak = i; break; }
                if (weak > 0)
                {
                    int si = r.stages[weak].designIndex;
                    if (si >= 0 && si < stages.Count)
                    {
                        var s = (JsonObject)stages[si];
                        if (s.Num("engineCount") < Lim.engineCountMax) { JsonMut.Set(s, "engineCount", s.Num("engineCount") + 1); Say("Added an engine to " + JsonMut.Shown(s, "name")); continue; }
                    }
                }
                if (r.margin < MIN_MARGIN)
                {
                    var up = (JsonObject)stages[stages.Count - 1];
                    if (stages.Count > 1 && r.stages[r.stages.Count - 1].twrVac > 0.7 && iter % 2 == 0) { JsonMut.Set(up, "propMass", up.Num("propMass") * 1.15); Say("Added upper-stage propellant"); continue; }
                    if (r.liftoffTWR > 1.45) { JsonMut.Set(s0, "propMass", s0.Num("propMass") * 1.1); Say("Added first-stage propellant"); continue; }
                    var payload = d.Obj("payload");
                    if (payload.Num("mass") > Lim.payloadMassMin) { JsonMut.Set(payload, "mass", Math.Max(Lim.payloadMassMin, JsMath.Round(payload.Num("mass") * 0.85))); Say("Lightened the payload"); continue; }
                }
                if (v.ok && r.margin >= MIN_MARGIN) break;
                if (v.ok && iter > 60) break;
            }
            return (d, changes);
        }

        static void FixPart(Catalog c, JsonObject s, bool upper, Action<string> say)
        {
            c.EngineById.TryGetValue(s.Str("engine") ?? "", out var e);
            if (e == null || e.propellant != s.Str("propellant"))
            {
                var ne = EngineFor(c, s.Str("propellant"), upper) ?? c.EngineById["gg-kerolox"];
                JsonMut.Set(s, "engine", ne.id); JsonMut.Set(s, "propellant", ne.propellant);
                say($"Matched {NameOrStage(s)} engine to its propellant");
                e = ne;
            }
            if (!(s.Num("engineCount") >= 1)) { JsonMut.Set(s, "engineCount", 1d); say($"Gave {NameOrStage(s)} an engine"); }
            JsonMut.Set(s, "engineCount", Math.Min(Lim.engineCountMax, JsMath.Round(s.Num("engineCount"))));
            if (!(s.Num("propMass") > 0)) { JsonMut.Set(s, "propMass", upper ? 10000d : 50000d); say($"Loaded propellant into {NameOrStage(s)}"); }
            var en = EnginePoint.Of(c.EngineById[s.Str("engine")], s.Str("nozzle"));
            double need = Math.Sqrt(s.Num("engineCount") * en.diameter * en.diameter / Lim.pack) + 0.05;
            if (s.Num("diameter") < need) { JsonMut.Set(s, "diameter", Math.Min(Lim.diameterMax, Math.Ceiling(need * 10) / 10)); say($"Widened {NameOrStage(s)} so the engines fit"); }
        }

        static Engine EngineFor(Catalog c, string propellant, bool upper)
        {
            Engine first = null, pick = null;
            foreach (var e in c.Engines)
            {
                if (e.propellant != propellant) continue;
                if (first == null) first = e;
                if (pick == null && (upper ? e.thrustSL == 0 : e.thrustSL > 0)) pick = e;
            }
            return pick ?? first;
        }

        static string NameOrStage(JsonObject s)
        {
            var n = s?.Str("name");
            return string.IsNullOrEmpty(n) ? "stage" : n;
        }

        public static (bool ok, List<string> errors) Shape(Catalog c, JsonObject d)
        {
            var errs = new List<string>();
            if (d == null) return (false, new List<string> { "design: not an object" });
            if (!d.ContainsKey("version") || !(d["version"] is double ver) || (ver != 1 && ver != 2 && ver != 3))
                errs.Add($"version: expected 1, 2 or 3, got {JsonMut.Shown(d, "version")} (run migrateDesign)");
            if (!(d.ContainsKey("name") && d["name"] is string)) errs.Add("name: string required");
            var stages = d.Arr("stages");
            if (stages == null || stages.Count < Lim.stagesMin || stages.Count > Lim.stagesMax) errs.Add("stages: 1\u20135 required");
            else for (int i = 0; i < stages.Count; i++)
            {
                var s = stages[i] as JsonObject;
                CheckStage(c, s, $"stages[{i}]", errs, false);
                if (s == null) continue;
                foreach (var k in new[] { "interstageMass", "separationMass", "landingPropMass" })
                    if (s.ContainsKey(k) && s[k] != null && (!(s[k] is double n) || double.IsNaN(n) || double.IsInfinity(n) || n < 0))
                        errs.Add($"stages[{i}].{k}: non-negative number");
            }
            var so = d.Obj("strapOns");
            if (so == null || !StrapCount(so.ContainsKey("count") ? so["count"] : null)) errs.Add("strapOns.count: 0, 2 or 4");
            else if (so.Num("count") > 0)
            {
                var kind = so.Str("kind");
                if (kind != "solid" && kind != "liquid") errs.Add("strapOns.kind: solid or liquid");
                CheckStage(c, so, "strapOns", errs, true);
                if (kind == "solid" && so.Str("propellant") != "solid-apcp") errs.Add("strapOns: solid kind needs solid propellant");
                if (kind == "solid" && so.Bool("crossfeed")) errs.Add("strapOns.crossfeed: not possible with solids");
                if (!(so.ContainsKey("crossfeed") && so["crossfeed"] is bool)) errs.Add("strapOns.crossfeed: boolean");
            }
            var pl = d.Obj("payload");
            if (pl == null || !(pl.ContainsKey("mass") && pl["mass"] is double pm && !double.IsNaN(pm) && !double.IsInfinity(pm)) || pl.Num("mass") < 0)
                errs.Add("payload.mass: non-negative number");
            else
            {
                if (pl.ContainsKey("type") && pl["type"] is string ty && ty.Length > 0 && !c.PayloadById.ContainsKey(ty)) errs.Add($"payload.type: unknown \"{ty}\"");
                else if (pl.ContainsKey("type") && !(pl["type"] is string) && pl["type"] != null) errs.Add($"payload.type: unknown \"{JsonMut.ShownValue(pl["type"])}\"");
                string orbit = pl.ContainsKey("orbit") ? JsonMut.Shown(pl, "orbit") : "undefined";
                if (!pl.ContainsKey("orbit") || !(pl["orbit"] is string oid) || !c.Orbits.ContainsKey(oid)) errs.Add($"payload.orbit: unknown \"{orbit}\"");
            }
            var f = d.Obj("fairing");
            if (f == null || !(Pos(f, "diameter")) || !(Pos(f, "length"))) errs.Add("fairing: diameter and length > 0");
            else if (f.Str("material") == null || !c.MaterialById.ContainsKey(f.Str("material"))) errs.Add($"fairing.material: unknown \"{JsonMut.Shown(f, "material")}\"");
            var aero = d.Obj("aero");
            if (aero == null || !(aero.Str("nose") is string nose) || !Nose.ContainsKey(nose)) errs.Add("aero.nose: one of ogive, conic, blunt, von-karman, biconic, hammerhead");
            var landing = d.Obj("landing");
            if (landing == null || !BoolKey(landing, "legs") || !BoolKey(landing, "gridFins") || !BoolKey(landing, "rcs")) errs.Add("landing: legs/gridFins/rcs booleans");
            var av = d.Obj("avionics");
            if (av == null || !(av.Str("tier") is string tier) || (tier != "basic" && tier != "standard" && tier != "redundant")) errs.Add("avionics.tier: one of basic, standard, redundant");
            if (!(d.ContainsKey("safetyFactor") && d["safetyFactor"] is double sf && !double.IsNaN(sf) && !double.IsInfinity(sf)) || sf < Lim.safetyMin || sf > Lim.safetyMax) errs.Add("safetyFactor: 1.0\u20133.0");
            DesignRules.ShapeV2(c, d, errs);
            var liv = d.Obj("livery");
            if (liv == null || !(liv.ContainsKey("primary") && liv["primary"] is string)) errs.Add("livery.primary: string");
            return (errs.Count == 0, errs);
        }

        static readonly Dictionary<string, double> Nose = new Dictionary<string, double>
        {
            ["ogive"] = 0.30, ["conic"] = 0.36, ["blunt"] = 0.50, ["von-karman"] = 0.28,
            ["biconic"] = 0.55, ["hammerhead"] = 0.40,
        };

        static void CheckStage(Catalog c, JsonObject s, string where, List<string> errs, bool strap)
        {
            if (s == null) { errs.Add($"{where}: missing"); return; }
            if (s.Str("propellant") == null || !c.PropellantById.ContainsKey(s.Str("propellant"))) errs.Add($"{where}.propellant: unknown \"{JsonMut.Shown(s, "propellant")}\"");
            var eid = s.Str("engine");
            if (eid == null || !c.EngineById.TryGetValue(eid, out var e)) errs.Add($"{where}.engine: unknown \"{JsonMut.Shown(s, "engine")}\"");
            else if (s.ContainsKey("propellant") && s["propellant"] is string prop && prop.Length > 0 && e.propellant != prop) errs.Add($"{where}.engine: {e.id} burns {e.propellant}, stage has {prop}");
            if (!s.ContainsKey("engineCount") || !JsonMut.IsInteger(s["engineCount"]) || s.Num("engineCount") < Lim.engineCountMin || s.Num("engineCount") > Lim.engineCountMax) errs.Add($"{where}.engineCount: integer 1\u201340");
            double minProp = strap ? 0 : 1;
            if (!(s.ContainsKey("propMass") && s["propMass"] is double pmass && !double.IsNaN(pmass) && !double.IsInfinity(pmass)) || pmass < minProp) errs.Add($"{where}.propMass: must be a positive number");
            if (!(s.ContainsKey("diameter") && s["diameter"] is double dia && !double.IsNaN(dia) && !double.IsInfinity(dia)) || dia < Lim.diameterMin || dia > Lim.diameterMax) errs.Add($"{where}.diameter: 0.3\u201315 m");
            if (s.Str("material") == null || !c.MaterialById.ContainsKey(s.Str("material"))) errs.Add($"{where}.material: unknown \"{JsonMut.Shown(s, "material")}\"");
            var cluster = s.Obj("cluster");
            if (cluster != null && cluster.ContainsKey("pattern"))
            {
                var pat = cluster.Str("pattern");
                if (string.IsNullOrEmpty(pat) || !Lim.TryPattern(pat, out _))
                    errs.Add($"{where}.cluster.pattern: unknown \"{JsonMut.Shown(cluster, "pattern")}\"");
            }
        }

        static bool StrapCount(object v) => v is double d && (d == 0 || d == 2 || d == 4);
        static bool Pos(JsonObject o, string k) => o.ContainsKey(k) && o[k] is double d && !double.IsNaN(d) && !double.IsInfinity(d) && d > 0;
        static bool BoolKey(JsonObject o, string k) => o.ContainsKey(k) && o[k] is bool;

        static List<Issue> Geometry(Catalog c, JsonObject d)
        {
            var outp = new List<Issue>();
            var stages = d.Arr("stages");
            var parts = new List<(JsonObject s, string where, string tab, int? index)>();
            for (int i = 0; i < stages.Count; i++)
            {
                var s = (JsonObject)stages[i];
                string nm = s.Str("name");
                parts.Add((s, string.IsNullOrEmpty(nm) ? $"Stage {i + 1}" : nm, "stages", i));
            }
            var so = d.Obj("strapOns");
            if (JsonMut.TruthyNumber(so, "count")) parts.Add((so, "Strap-ons", "strapons", null));
            foreach (var (s, where, tab, index) in parts)
            {
                if (s.Str("engine") != null && c.EngineById.TryGetValue(s.Str("engine"), out var e) && s.Num("engineCount") * EnginePoint.Of(e, s.Str("nozzle")).diameter * EnginePoint.Of(e, s.Str("nozzle")).diameter > Lim.pack * s.Num("diameter") * s.Num("diameter"))
                    outp.Add(Iss("engines-dont-fit", tab, $"{where}: {CountText(s.Num("engineCount"))} engines don't fit under a {Fmt(s.Num("diameter"), 1)} m body. Try fewer engines or a wider stage.", index));
            }
            for (int i = 1; i < stages.Count; i++)
            {
                var up = (JsonObject)stages[i]; var below = (JsonObject)stages[i - 1];
                if (up.Num("diameter") > below.Num("diameter") * 1.6)
                {
                    string nm = up.Str("name");
                    string where = string.IsNullOrEmpty(nm) ? $"Stage {i + 1}" : nm;
                    outp.Add(Iss("top-heavy", "stages", $"{where} is much wider than the stage below it. Try a diameter under {Fmt(below.Num("diameter") * Lim.topHeavyRatio, 1)} m.", i));
                }
            }
            var top = (JsonObject)stages[stages.Count - 1];
            var fairing = d.Obj("fairing");
            if (fairing != null && top != null && fairing.Num("diameter") < top.Num("diameter") * Lim.fairingNarrowRatio)
                outp.Add(Iss("fairing-narrow", "payload", "The fairing is narrower than half the top stage. Try a fairing at least as wide as the stage."));
            var spec = PayloadFit.SpecFor(c, d);
            if (spec != null && fairing != null)
            {
                var plj = d.Obj("payload");
                string label = plj?.Str("type") is string pt && c.PayloadById.TryGetValue(pt, out var pl) ? pl.label : "cargo";
                var fit = PayloadFit.Check(fairing.Num("diameter"), fairing.Num("length"), spec);
                if (fit.volumeMargin < 0) outp.Add(Iss("payload-doesnt-fit", "payload", $"The {label} needs about {Fmt(spec.volume, 1)} m\u00b3; this fairing holds {Fmt(fit.usableVolume, 1)} m\u00b3. Try a longer or wider fairing."));
                else if (fit.diameterMargin < -PayloadFit.DiameterToleranceM) outp.Add(Iss("payload-doesnt-fit", "payload", $"The {label} is {Fmt(spec.diameter, 1)} m wide; it needs a fairing at least {Fmt(Math.Ceiling((PayloadFit.RequiredDiameter(spec) - PayloadFit.DiameterToleranceM) * 100) / 100, 2)} m wide. Try a wider fairing."));
            }
            return outp;
        }

        static string CountText(double n) => JsonMut.IsInteger(n) ? ((long)n).ToString(System.Globalization.CultureInfo.InvariantCulture) : n.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

        static Issue Iss(string code, string tab, string msg, int? index = null) => new Issue { code = code, tab = tab, msg = msg, index = index };
        static string Fmt(double n, int d = 2) => JsFormat.ToFixed(n, d);

        static JsonObject Obj(params (string k, object v)[] pairs)
        {
            var o = new JsonObject();
            foreach (var (k, v) in pairs) o.Add(k, v);
            return o;
        }
    }
}
