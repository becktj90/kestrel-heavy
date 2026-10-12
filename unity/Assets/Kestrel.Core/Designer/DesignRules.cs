// Design v2 checks: cluster pattern, nozzle id, interstage shape, stability, max-Q, payload g,
// cryo tanks, landing RCS, engine-out. Messages match the Hangar answers (engineer msg + kid text).
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Designer
{
    public static class DesignRules
    {
        public static void SchemaBlocks(Catalog c, JsonObject design, List<Issue> blocks)
        {
            if (design == null) return;
            void Part(JsonObject s, string where, int? index)
            {
                if (s == null) return;
                var id = s.Str("nozzle");
                if (string.IsNullOrEmpty(id) || id == "default") return;
                c.EngineById.TryGetValue(s.Str("engine") ?? "", out var e);
                if (EnginePoint.NozzleKnown(e, id)) return;
                blocks.Add(Issue("nozzle-unknown", "engines",
                    "nozzle " + id + " not in " + (e == null ? s.Str("engine") : e.id) + ".nozzles.",
                    "That nozzle isn't in the shop.", index));
            }
            var stages = design.Arr("stages");
            if (stages != null)
                for (int i = 0; i < stages.Count; i++) Part(stages[i] as JsonObject, "stages", i);
            Part(design.Obj("strapOns"), "strapOns", null);
        }

        public static void Append(Catalog c, JsonObject design, List<Issue> blocks, List<Issue> warnings, QuickReadout readout)
        {
            var d = Design.FromJson(design);
            var lim = DesignerLimits.Active;
            Cluster(c, d, blocks, warnings);
            Interstage(d, warnings);
            // Version 2 only. A v1 save has none of these fields, and the existing goldens
            // must keep today's issue lists. Migration is what turns the checks on.
            if ((int)design.Num("version") >= 2)
            {
                LiftoffEngines(c, d, readout, blocks, warnings);
                Cryo(c, d, warnings);
                Rcs(d, warnings);
                Stability(c, d, blocks, warnings);
                Reference(c, design, d, blocks, warnings);
            }
        }

        public static void ShapeV2(Catalog c, JsonObject d, List<string> errs)
        {
            if (d == null || (int)d.Num("version") < 2) return;
            var lim = DesignerLimits.Active;
            var f = d.Obj("fairing");
            if (f != null && f.ContainsKey("length") && f["length"] is double len && len > 0
                && (len < lim.fairingLengthMin || len > lim.fairingLengthMax))
                errs.Add($"fairing.length: {Num(lim.fairingLengthMin)}–{Num(lim.fairingLengthMax)} m");
            var pl = d.Obj("payload");
            if (pl != null && pl.ContainsKey("mass") && pl["mass"] is double pm && pm >= 0
                && (pm < lim.payloadMassMin || pm > lim.payloadMassMax))
                errs.Add($"payload.mass: {Num(lim.payloadMassMin)}–{Num(lim.payloadMassMax)} kg");
            var stages = d.Arr("stages");
            if (stages == null) return;
            for (int i = 0; i < stages.Count; i++)
            {
                var s = stages[i] as JsonObject;
                if (s == null || !s.ContainsKey("landingPropMass") || !(s["landingPropMass"] is double lp) || lp < 0) continue;
                double prop = s.Num("propMass");
                if (!(prop > 0)) continue;
                double frac = lp / prop;
                if (frac < lim.landingReserveMin || frac > lim.landingReserveMax)
                    errs.Add($"stages[{i}].landingPropMass: {Num(lim.landingReserveMin * 100)}–{Num(lim.landingReserveMax * 100)}% of propellant");
            }
        }

        /// <summary>Make it fly: a stored shape that disagrees with the two diameters is rewritten to the one that fits.</summary>
        public static void FixInterstage(JsonObject design, System.Action<string> say)
        {
            var stages = design?.Arr("stages");
            if (stages == null) return;
            for (int i = 1; i < stages.Count; i++)
            {
                var up = stages[i] as JsonObject;
                var below = stages[i - 1] as JsonObject;
                if (up == null || below == null) continue;
                var inter = up.Obj("interstage");
                if (inter == null || string.IsNullOrEmpty(inter.Str("shape"))) continue;
                string want = Agree(below.Num("diameter"), up.Num("diameter"));
                if (inter.Str("shape") == want) continue;
                JsonMut.Set(inter, "shape", want);
                say("Matched the joint between stages to their sizes");
            }
        }

        static void Cluster(Catalog c, Design d, List<Issue> blocks, List<Issue> warnings)
        {
            void One(StagePart s, int? index)
            {
                if (s == null) return;
                string pattern = string.IsNullOrEmpty(s.clusterPattern) ? "auto" : s.clusterPattern;
                if (pattern == "auto") return;
                var lim = DesignerLimits.Active;
                if (!lim.TryPattern(pattern, out var row)) return;
                if (row.fixedCount && s.engineCount != row.count)
                    blocks.Add(Issue("cluster-count", "engines",
                        "Pattern " + pattern + " takes " + row.count + " engines; stage has " + s.engineCount + ".",
                        "That engine pattern needs a different number of engines.", index));
                if (!row.hasMinDiameter) return;
                if (!c.EngineById.TryGetValue(s.engine ?? "", out var e)) return;
                double de = EnginePoint.Of(e, s.nozzle).diameter;
                double need = row.minDiameterOverExit * de;
                if (s.diameter + 1e-12 < need)
                    warnings.Add(Issue("cluster-overhang", "engines",
                        "Pattern " + pattern + " needs D ≥ " + Fmt(need) + " m; stage is " + Fmt(s.diameter) + " m.",
                        "The engines stick out past the edge. Make the stage a bit wider.", index));
            }
            for (int i = 0; i < d.stages.Count; i++) One(d.stages[i], i);
            if (d.StrapOnCount > 0) One(d.strapOns, null);
        }

        static void Interstage(Design d, List<Issue> warnings)
        {
            for (int i = 1; i < d.stages.Count; i++)
            {
                var up = d.stages[i];
                var below = d.stages[i - 1];
                if (string.IsNullOrEmpty(up.interstageShape)) continue;
                if (!Disagrees(up.interstageShape, below.diameter, up.diameter)) continue;
                string actual = Agree(below.diameter, up.diameter);
                warnings.Add(Issue("interstage-shape", "stages",
                    "Interstage " + up.interstageShape + " with D " + Fmt(below.diameter) + "→" + Fmt(up.diameter) + " m (" + actual + ", " + CnSign(actual) + ").",
                    KidJoint(actual), i));
            }
        }

        public static bool Disagrees(string shape, double dLower, double dUpper)
        {
            if (shape == "straight") return System.Math.Abs(dUpper - dLower) > 1e-9;
            if (shape == "flare") return !(dLower > dUpper);
            if (shape == "shoulder") return !(dUpper > dLower);
            return false;
        }

        public static string Agree(double dLower, double dUpper)
        {
            if (System.Math.Abs(dUpper - dLower) <= 1e-9) return "straight";
            return dLower > dUpper ? "flare" : "shoulder";
        }

        static string CnSign(string actual)
        {
            if (actual == "flare") return "CNα > 0";
            if (actual == "shoulder") return "CNα < 0";
            return "CNα = 0";
        }

        static string KidJoint(string actual)
        {
            if (actual == "flare") return "That joint is a flare: wider at the bottom, CNα positive, so it steadies the rocket.";
            if (actual == "shoulder") return "That joint is a shoulder: wider on top, CNα negative, so it makes the rocket less steady.";
            return "The joint between stages doesn't match their sizes.";
        }

        static void LiftoffEngines(Catalog c, Design d, QuickReadout r, List<Issue> blocks, List<Issue> warnings)
        {
            if (d.stages.Count == 0) return;
            void Vac(StagePart s, int? index)
            {
                if (s == null || !c.EngineById.TryGetValue(s.engine ?? "", out var e)) return;
                if (EnginePoint.Of(e, s.nozzle).thrustSL == 0)
                    blocks.Add(Issue("vac-engine-pad", "engines",
                        e.id + " has zero sea-level thrust on a stage that lights at liftoff.",
                        "This engine only works in space. Use a ground engine on the bottom.", index));
            }
            Vac(d.stages[0], 0);
            if (d.StrapOnCount > 0) Vac(d.strapOns, null);

            var s0 = d.stages[0];
            int n = s0.engineCount;
            if (n >= 2 && r != null && r.liftoffTWR * (n - 1) / n < 1)
            {
                double x = r.liftoffTWR * (n - 1) / n;
                warnings.Add(Issue("engine-out", "engines",
                    "Liftoff TWR with n−1 engines is " + Fmt(x) + " < 1.",
                    "If one engine stops, you'd sink back down. Add a spare.", 0));
            }
        }

        static void Cryo(Catalog c, Design d, List<Issue> warnings)
        {
            void One(StagePart s)
            {
                if (s == null) return;
                if (s.material != "composite" || s.propellant != "hydrolox") return;
                if (!c.MaterialById.TryGetValue(s.material, out var m) || m.cryoOK) return;
                warnings.Add(Issue("cryo-composite", "propellants",
                    "composite + hydrolox: cryoOK false (materials.json).",
                    "Super-cold fuel can crack these tanks. Pick metal tanks.", null));
            }
            for (int i = 0; i < d.stages.Count; i++) One(d.stages[i]);
            if (d.StrapOnCount > 0) One(d.strapOns);
        }

        static void Rcs(Design d, List<Issue> warnings)
        {
            bool fuel = false;
            for (int i = 0; i < d.stages.Count; i++) if (d.stages[i].landingPropMass > 0) fuel = true;
            if (fuel && !d.rcs)
                warnings.Add(Issue("rcs-for-landing", "recovery",
                    "Landing reserve set but `landing.rcs` is false.",
                    "To land, your booster needs little steering jets.", null));
        }

        static void Stability(Catalog c, Design d, List<Issue> blocks, List<Issue> warnings)
        {
            if (d.stages.Count == 0) return;
            var bal = Balance.Of(c, d);
            double cal = bal.stabilityCal;
            var lim = DesignerLimits.Active;
            bool fins = d.fins;
            bool steered = Steered(c, d);
            bool unguided = !steered;
            bool band = fins || unguided;
            if (band && cal > lim.overstableAbove)
                warnings.Add(Issue("overstable", "aero",
                    "Stability margin " + Fmt(cal) + " cal > " + Num(lim.overstableAbove) + "; weathercocks in wind.",
                    "Very steady, maybe too steady to steer well.", null));
            if (!band)
            {
                if (steered && cal < lim.stabilityAimMin)
                    warnings.Add(Issue("unstable", "aero",
                        "Stability margin " + Fmt(cal) + " cal. Engine-steered liquid stage; the 1–2 cal band is for finned or unguided rockets.",
                        "The engines steer this rocket, so it can fly without the fin rule.", null));
                return;
            }
            if (cal < 0 && unguided)
                blocks.Add(Issue("unstable", "aero",
                    "Stability margin " + Fmt(cal) + " cal (CP above CG). Aim for 1–2 cal.",
                    "Your rocket would flip over! Add fins or put heavy parts higher.", null));
            else if (cal < lim.stabilityAimMin)
                warnings.Add(Issue("unstable", "aero",
                    "Stability margin " + Fmt(cal) + " cal (CP above CG). Aim for 1–2 cal.",
                    "Your rocket would flip over! Add fins or put heavy parts higher.", null));
        }

        static void Reference(Catalog c, JsonObject design, Design d, List<Issue> blocks, List<Issue> warnings)
        {
            if (!ReferenceAscent.TryGet(ReferenceAscent.CanonicalKey(c, design), out double qPa, out _, out double g)) return;
            var lim = DesignerLimits.Active;
            double qKpa = qPa / 1000.0;
            if (qKpa > lim.maxQWarnKPa && !UnguidedSolid(c, d))
                warnings.Add(Issue("maxq-high", "aero",
                    "Max-Q " + Fmt(qKpa) + " kPa > " + Num(lim.maxQWarnKPa) + " kPa.",
                    "The air pushes really hard on this rocket. Try a pointy nose or less push low down.", null));
            var plj = design.Obj("payload");
            if (plj == null || plj.Str("type") == null || !c.PayloadById.TryGetValue(plj.Str("type"), out var pl)) return;
            if (!(g > pl.maxG)) return;
            string name = string.IsNullOrEmpty(pl.displayName) ? pl.label : pl.displayName;
            var issue = Issue("payload-g", "payload",
                "Peak " + Fmt(g) + " g > payload maxG " + Fmt(pl.maxG) + ".",
                "Too bumpy for your " + name + "! Use gentler engines on the top stage.", null);
            if (pl.crewed) blocks.Add(issue);
            else warnings.Add(issue);
        }

        static bool Steered(Catalog c, Design d)
        {
            if (d.stages.Count == 0) return false;
            if (Gimballed(c, d.stages[0])) return true;
            return d.StrapOnCount > 0 && Gimballed(c, d.strapOns);
        }

        static bool Gimballed(Catalog c, StagePart s)
        {
            if (s == null || !c.EngineById.TryGetValue(s.engine ?? "", out var e)) return false;
            return e.gimbal > 0;
        }

        static bool UnguidedSolid(Catalog c, Design d)
        {
            if (d.stages.Count == 0) return false;
            var s = d.stages[0];
            if (!c.PropellantById.TryGetValue(s.propellant ?? "", out var p) || p.storability != "solid") return false;
            if (!c.EngineById.TryGetValue(s.engine ?? "", out var e)) return false;
            return !(e.gimbal > 0);
        }

        static Issue Issue(string code, string tab, string msg, string kid, int? index)
            => new Issue { code = code, tab = tab, msg = msg, kid = kid, index = index };

        static string Fmt(double n) => JsFormat.ToFixed(n, 2);
        static string Num(double n) => n.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }
}
