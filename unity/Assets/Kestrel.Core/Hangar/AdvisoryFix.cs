// One suggested change for a Hangar "To check" row. Show still points the camera.
// Fix it applies the change the kid line already names.
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;

namespace Kestrel.Core.Hangar
{
    public static class AdvisoryFix
    {
        public static bool CanFix(Catalog catalog, JsonObject design, Advisory advisory)
        {
            if (catalog == null || design == null || advisory == null) return false;
            return Apply(catalog, JsonMut.CloneObject(design), advisory).changed;
        }

        public static Edit Apply(Catalog catalog, JsonObject design, Advisory advisory)
        {
            if (catalog == null || design == null || advisory == null || string.IsNullOrEmpty(advisory.code))
                return Edit.None;
            switch (advisory.code)
            {
                case "payload-g": return Gentler(catalog, design, Top(design));
                case "weak-upper":
                case "engine-out":
                case "twr-low":
                case "twr-marginal": return MoreEngines(design, EnginesOf(advisory, design));
                case "zero-engines": return HangarEditor.SetEngineCount(design, EnginesOf(advisory, design), 1);
                case "engines-dont-fit":
                case "cluster-overhang":
                case "over-budget": return FewerOrWider(design, EnginesOf(advisory, design));
                case "zero-propellant": return Fuel(design, EnginesOf(advisory, design), empty: true);
                case "weak-upper-dv":
                case "dv-margin": return Fuel(design, EnginesOf(advisory, design), empty: false);
                case "top-heavy": return MatchWidthBelow(design, advisory);
                case "fairing-narrow": return MatchFairing(design);
                case "payload-doesnt-fit": return StretchFairing(design);
                case "heavy-fairing": return LighterNose(catalog, design);
                case "landing-hardware": return Gear(design);
                case "unstable": return Steadier(design);
                case "overstable": return Looser(design);
                case "maxq-high": return PointierOrQuieter(design);
                case "nozzle-unknown": return HangarEditor.SetNozzle(catalog, design, EnginesOf(advisory, design), "default");
                case "cluster-count": return HangarEditor.SetClusterPattern(design, EnginesOf(advisory, design), "auto");
                case "interstage-shape": return StraightJoint(design, advisory);
                case "vac-engine-pad": return SeaLevel(catalog, design, EnginesOf(advisory, design));
                case "cryo-composite": return MetalTank(catalog, design, advisory);
                case "rcs-for-landing": return HangarEditor.SetRcs(design, true);
                default: return Edit.None;
            }
        }

        static Edit Gentler(Catalog catalog, JsonObject design, PartRef part)
        {
            var body = HangarParts.Body(design, part);
            if (body == null || !catalog.EngineById.TryGetValue(body.Str("engine") ?? "", out var now)) return Edit.None;
            double nowT = Thrust(now);
            Engine next = null;
            double nextT = -1;
            foreach (var engine in catalog.Engines)
            {
                if (engine.propellant != now.propellant || engine.id == now.id) continue;
                double thrust = Thrust(engine);
                if (!(thrust < nowT)) continue;
                if (next == null || thrust > nextT) { next = engine; nextT = thrust; }
            }
            if (next != null) return HangarEditor.SetEngine(catalog, design, part, next.id);
            int count = (int)body.Num("engineCount");
            if (count > 1) return HangarEditor.SetEngineCount(design, part, count - 1);
            return Edit.None;
        }

        static Edit SeaLevel(Catalog catalog, JsonObject design, PartRef part)
        {
            var body = HangarParts.Body(design, part);
            if (body == null) return Edit.None;
            string prop = body.Str("propellant");
            string current = body.Str("engine");
            Engine pick = null;
            foreach (var engine in catalog.Engines)
            {
                if (engine.propellant != prop || !(engine.thrustSL > 0)) continue;
                if (pick == null || engine.thrustSL > pick.thrustSL) pick = engine;
            }
            if (pick == null || pick.id == current) return Edit.None;
            return HangarEditor.SetEngine(catalog, design, part, pick.id);
        }

        static Edit MoreEngines(JsonObject design, PartRef part)
        {
            var body = HangarParts.Body(design, part);
            if (body == null) return Edit.None;
            int count = (int)body.Num("engineCount");
            if (count < 1) count = 0;
            return HangarEditor.SetEngineCount(design, part, count + 1);
        }

        static Edit FewerOrWider(JsonObject design, PartRef part)
        {
            var body = HangarParts.Body(design, part);
            if (body == null) return Edit.None;
            int count = (int)body.Num("engineCount");
            if (count > 1) return HangarEditor.SetEngineCount(design, part, count - 1);
            return HangarEditor.SetDiameter(design, part, body.Num("diameter") + 0.3);
        }

        static Edit Fuel(JsonObject design, PartRef part, bool empty)
        {
            var body = HangarParts.Body(design, part);
            if (body == null) return Edit.None;
            double mass = body.Num("propMass");
            double next = !empty && mass > 0 ? mass * 1.15 : (part.index > 0 ? 10000d : 50000d);
            return HangarEditor.SetPropMass(design, part, next);
        }

        static Edit MatchWidthBelow(JsonObject design, Advisory advisory)
        {
            var stages = design.Arr("stages");
            int i = advisory.part.kind == PartKind.Stage ? advisory.part.index : 1;
            if (stages == null || i < 1 || i >= stages.Count) return Edit.None;
            double below = ((JsonObject)stages[i - 1]).Num("diameter");
            return HangarEditor.SetDiameter(design, PartRef.Stage(i), below);
        }

        static Edit MatchFairing(JsonObject design)
        {
            int top = TopIndex(design);
            var stages = design.Arr("stages");
            if (top < 0 || stages == null) return Edit.None;
            return HangarEditor.SetDiameter(design, PartRef.Fairing, ((JsonObject)stages[top]).Num("diameter"));
        }

        static Edit StretchFairing(JsonObject design)
        {
            double len = design.Obj("fairing")?.Num("length") ?? 1;
            return HangarEditor.SetFairingLength(design, len + 1);
        }

        static Edit LighterNose(Catalog catalog, JsonObject design)
        {
            var fairing = design.Obj("fairing");
            if (fairing == null) return Edit.None;
            if (fairing.Str("material") != "composite")
                return HangarEditor.SetMaterial(catalog, design, PartRef.Fairing, "composite");
            return HangarEditor.SetFairingLength(design, fairing.Num("length") * 0.8);
        }

        static Edit Gear(JsonObject design)
        {
            var legs = HangarEditor.SetLegs(design, true);
            var fins = HangarEditor.SetGridFins(design, true);
            if (!legs.changed && !fins.changed) return Edit.None;
            return new Edit("Landing gear", null, "legs and grid fins");
        }

        static Edit Steadier(JsonObject design)
        {
            var aero = design.Obj("aero");
            if (aero == null || !aero.Bool("fins")) return HangarEditor.SetFins(design, true);
            return HangarEditor.SetFinSpan(design, aero.Num("finSpanM", 1) + DesignerLimits.Active.finSpanStep);
        }

        static Edit Looser(JsonObject design)
        {
            var aero = design.Obj("aero");
            double span = aero?.Num("finSpanM", 1) ?? 1;
            if (span > DesignerLimits.Active.finSpanMin + 1e-6)
                return HangarEditor.SetFinSpan(design, span - DesignerLimits.Active.finSpanStep);
            return HangarEditor.SetFins(design, false);
        }

        static Edit PointierOrQuieter(JsonObject design)
        {
            if (design.Obj("aero")?.Str("nose") != "von-karman") return HangarEditor.SetNose(design, "von-karman");
            return FewerOrWider(design, PartRef.Stage(0));
        }

        static Edit StraightJoint(JsonObject design, Advisory advisory)
        {
            int i = advisory.part.kind == PartKind.Interstage ? advisory.part.index : 1;
            if (i < 1) return Edit.None;
            return HangarEditor.SetInterstageShape(design, i, "straight");
        }

        static Edit MetalTank(Catalog catalog, JsonObject design, Advisory advisory)
        {
            var part = advisory.part.HasEngines ? advisory.part : PartRef.Stage(0);
            return HangarEditor.SetMaterial(catalog, design, part, "al-li");
        }

        static PartRef EnginesOf(Advisory advisory, JsonObject design)
        {
            if (advisory.part.HasEngines) return advisory.part;
            return Top(design);
        }

        static PartRef Top(JsonObject design)
        {
            int i = TopIndex(design);
            return i >= 0 ? PartRef.Stage(i) : PartRef.Stage(0);
        }

        static int TopIndex(JsonObject design)
        {
            var stages = design?.Arr("stages");
            return stages == null || stages.Count == 0 ? -1 : stages.Count - 1;
        }

        static double Thrust(Engine engine)
        {
            if (engine == null) return 0;
            return engine.thrustVac > 0 ? engine.thrustVac : engine.thrustSL;
        }
    }
}
