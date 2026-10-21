// Vehicle design (schema v1, v2, or v3) + tank sizing.
// Port of web/js/vehicle/design.js (data + sizeTank; shape validation stays in JS for now).
// v3 fields are optional. Absent means the v2 behaviour: catalog engine point, mixture offset 0,
// no tank override, no separation mode, and no guidance or recovery profile.
// "engine" stays the catalog id string. Overrides live on engineParams so existing readers
// that call Str("engine") keep working.
using System;
using System.Collections.Generic;
using Kestrel.Core.Json;
using Kestrel.Core.Data;

namespace Kestrel.Core.Vehicle
{
    public sealed class Tune { public double thrustPct, ispPct; public Tune Clone() => (Tune)MemberwiseClone(); }

    /// <summary>§2.1 mixture-ratio offset. 0% is the catalog mixture ratio. Pure; flight code does not call it yet.</summary>
    public static class MixtureOffset
    {
        public const double LimitPct = 10;
        public const double HazardPct = 6;
        /// <summary>Isp multiplier loss per 1% off nominal. −0.6% means 0.006.</summary>
        public const double IspPenaltyPerPct = 0.006;

        public static double IspFactor(double offsetPct) => 1 - IspPenaltyPerPct * Math.Abs(offsetPct);

        /// <summary>Beyond ±6% is a combustion-instability hazard. ±6% exactly is still nominal-adjacent.</summary>
        public static bool InstabilityHazard(double offsetPct) => Math.Abs(offsetPct) > HazardPct;
    }

    /// <summary>Optional per-stage engine overrides. Unset fields keep the catalog point.</summary>
    public sealed class EngineParams
    {
        public bool pcSet, expansionRatioSet, throttleMinSet, gimbalDegSet;
        public double pc, expansionRatio, throttleMin, gimbalDeg;
        public bool Any => pcSet || expansionRatioSet || throttleMinSet || gimbalDegSet;
        public EngineParams Clone() => (EngineParams)MemberwiseClone();

        public static EngineParams FromJson(JsonObject o)
        {
            if (o == null) return null;
            var e = new EngineParams();
            if (DesignJson.Take(o, "pc", out e.pc)) e.pcSet = true;
            if (DesignJson.Take(o, "expansionRatio", out e.expansionRatio)) e.expansionRatioSet = true;
            if (DesignJson.Take(o, "throttleMin", out e.throttleMin)) e.throttleMinSet = true;
            if (DesignJson.Take(o, "gimbalDeg", out e.gimbalDeg)) e.gimbalDegSet = true;
            return e.Any ? e : null;
        }

        public JsonObject ToJson()
        {
            var o = new JsonObject();
            if (pcSet) o.Add("pc", pc);
            if (expansionRatioSet) o.Add("expansionRatio", expansionRatio);
            if (throttleMinSet) o.Add("throttleMin", throttleMin);
            if (gimbalDegSet) o.Add("gimbalDeg", gimbalDeg);
            return o;
        }
    }

    /// <summary>Optional tank overrides. Insulation defaults to none. Common bulkhead defaults to off.</summary>
    public sealed class TankParams
    {
        public bool pressureSet, insulationSet, commonBulkheadSet;
        public double pressureBar;
        public string insulation;
        public bool commonBulkhead;
        public bool Any => pressureSet || insulationSet || commonBulkheadSet;
        public string InsulationOrNone => insulationSet && !string.IsNullOrEmpty(insulation) ? insulation : "none";
        public TankParams Clone() => (TankParams)MemberwiseClone();

        public static TankParams FromJson(JsonObject o)
        {
            if (o == null) return null;
            var t = new TankParams();
            if (DesignJson.Take(o, "pressureBar", out t.pressureBar)) t.pressureSet = true;
            if (o.ContainsKey("insulation") && o["insulation"] is string ins) { t.insulationSet = true; t.insulation = ins; }
            if (o.ContainsKey("commonBulkhead") && o["commonBulkhead"] is bool b) { t.commonBulkheadSet = true; t.commonBulkhead = b; }
            return t.Any ? t : null;
        }

        public JsonObject ToJson()
        {
            var o = new JsonObject();
            if (pressureSet) o.Add("pressureBar", pressureBar);
            if (insulationSet) o.Add("insulation", insulation ?? "none");
            if (commonBulkheadSet) o.Add("commonBulkhead", commonBulkhead);
            return o;
        }
    }

    /// <summary>Optional guidance profile. Absent means the sim's built-in turn.</summary>
    public sealed class GuidanceSpec
    {
        public bool pitchKickAltSet, pitchKickDegSet, turnShapeSet, maxQThrottleSet, gLimitSet;
        public double pitchKickAlt, pitchKickDeg, turnShape, maxQThrottle, gLimit;
        public bool Any => pitchKickAltSet || pitchKickDegSet || turnShapeSet || maxQThrottleSet || gLimitSet;
        public GuidanceSpec Clone() => (GuidanceSpec)MemberwiseClone();

        public static GuidanceSpec FromJson(JsonObject o)
        {
            if (o == null) return null;
            var g = new GuidanceSpec();
            if (DesignJson.Take(o, "pitchKickAlt", out g.pitchKickAlt)) g.pitchKickAltSet = true;
            if (DesignJson.Take(o, "pitchKickDeg", out g.pitchKickDeg)) g.pitchKickDegSet = true;
            if (DesignJson.Take(o, "turnShape", out g.turnShape)) g.turnShapeSet = true;
            if (DesignJson.Take(o, "maxQThrottle", out g.maxQThrottle)) g.maxQThrottleSet = true;
            if (DesignJson.Take(o, "gLimit", out g.gLimit)) g.gLimitSet = true;
            return g.Any ? g : null;
        }

        public JsonObject ToJson()
        {
            var o = new JsonObject();
            if (pitchKickAltSet) o.Add("pitchKickAlt", pitchKickAlt);
            if (pitchKickDegSet) o.Add("pitchKickDeg", pitchKickDeg);
            if (turnShapeSet) o.Add("turnShape", turnShape);
            if (maxQThrottleSet) o.Add("maxQThrottle", maxQThrottle);
            if (gLimitSet) o.Add("gLimit", gLimit);
            return o;
        }
    }

    /// <summary>A core stage or the strap-on set (same "stage-like" fields).</summary>
    public sealed class StagePart
    {
        public string name, propellant, engine, material, kind;
        public string clusterPattern, interstageShape, nozzle;
        public int engineCount = 1, count; // count: strap-ons only
        public double propMass, diameter, interstageMass, separationMass, landingPropMass;
        /// <summary>True when interstageMass is present in JSON, including 0. False means 0 on version 1 and a derived frustum on version 2 upper stages.</summary>
        public bool interstageSpecified;
        public bool crossfeed; public Tune tune;
        public EngineParams engineParams;
        public bool mixtureOffsetSet;
        public double mixtureOffsetPct;
        public TankParams tank;
        public string sepMode;
        public bool sepImpulseSet;
        public double sepImpulse;

        public double MixtureOffsetPctOrZero => mixtureOffsetSet ? mixtureOffsetPct : 0;
        public double SepImpulseOrZero => sepImpulseSet ? sepImpulse : 0;

        public StagePart Clone()
        {
            var s = (StagePart)MemberwiseClone();
            s.tune = tune?.Clone();
            s.engineParams = engineParams?.Clone();
            s.tank = tank?.Clone();
            return s;
        }

        internal static StagePart FromJson(JsonObject o)
        {
            if (o == null) return null;
            var s = new StagePart
            {
                name = o.Str("name"), propellant = o.Str("propellant"), material = o.Str("material"), kind = o.Str("kind"),
                engineCount = (int)o.Num("engineCount", 1), count = (int)o.Num("count"), propMass = o.Num("propMass"), diameter = o.Num("diameter"),
                interstageMass = o.Num("interstageMass"), separationMass = o.Num("separationMass"), landingPropMass = o.Num("landingPropMass"),
                interstageSpecified = o.ContainsKey("interstageMass") && o["interstageMass"] != null,
                crossfeed = o.Bool("crossfeed"),
                nozzle = o.Str("nozzle"),
                tune = o.Obj("tune") is JsonObject t ? new Tune { thrustPct = t.Num("thrustPct"), ispPct = t.Num("ispPct") } : null,
            };
            if (o.ContainsKey("engine") && o["engine"] is JsonObject eng)
            {
                s.engine = eng.Str("id");
                s.engineParams = EngineParams.FromJson(eng);
            }
            else s.engine = o.Str("engine");
            var parms = EngineParams.FromJson(o.Obj("engineParams"));
            if (parms != null) s.engineParams = parms;
            if (DesignJson.Take(o, "mixtureOffsetPct", out s.mixtureOffsetPct)) s.mixtureOffsetSet = true;
            s.tank = TankParams.FromJson(o.Obj("tank"));
            if (o.ContainsKey("sepMode") && o["sepMode"] is string mode) s.sepMode = mode;
            if (DesignJson.Take(o, "sepImpulse", out s.sepImpulse)) s.sepImpulseSet = true;
            var cluster = o.Obj("cluster");
            if (cluster != null) s.clusterPattern = cluster.Str("pattern");
            var inter = o.Obj("interstage");
            if (inter != null) s.interstageShape = inter.Str("shape");
            return s;
        }

        public JsonObject ToJson()
        {
            var o = new JsonObject();
            if (name != null) o.Add("name", name);
            if (propellant != null) o.Add("propellant", propellant);
            if (engine != null) o.Add("engine", engine);
            o.Add("engineCount", (double)engineCount);
            if (count != 0 || kind != null) o.Add("count", (double)count);
            o.Add("propMass", propMass);
            o.Add("diameter", diameter);
            if (material != null) o.Add("material", material);
            if (kind != null) o.Add("kind", kind);
            if (interstageSpecified) o.Add("interstageMass", interstageMass);
            o.Add("separationMass", separationMass);
            o.Add("landingPropMass", landingPropMass);
            if (crossfeed) o.Add("crossfeed", true);
            if (nozzle != null) o.Add("nozzle", nozzle);
            if (tune != null) o.Add("tune", DesignJson.Obj(("thrustPct", tune.thrustPct), ("ispPct", tune.ispPct)));
            if (clusterPattern != null) o.Add("cluster", DesignJson.Obj(("pattern", clusterPattern)));
            if (interstageShape != null) o.Add("interstage", DesignJson.Obj(("shape", interstageShape)));
            if (engineParams != null && engineParams.Any) o.Add("engineParams", engineParams.ToJson());
            if (mixtureOffsetSet) o.Add("mixtureOffsetPct", mixtureOffsetPct);
            if (tank != null && tank.Any) o.Add("tank", tank.ToJson());
            if (sepMode != null) o.Add("sepMode", sepMode);
            if (sepImpulseSet) o.Add("sepImpulse", sepImpulse);
            return o;
        }
    }

    public sealed class Design
    {
        public int version = 1;
        public string id, name, tier;
        public List<StagePart> stages = new List<StagePart>();
        public StagePart strapOns;
        public string payloadType, payloadOrbit; public double payloadMass;
        public double fairingDiameter, fairingLength, fairingJettisonAltitudeKm = 110; public string fairingMaterial;
        public string nose = "ogive"; public bool fins;
        // Optional keys. Absent means the defaults. Used when fins is true. Readout v2 uses the same pair.
        public int finCount = 4;
        public double finSpanM = 1;
        public bool finCountSet, finSpanSet;
        public string finPlanform;
        public bool finRootChordSet;
        public double finRootChord;
        public bool legs, gridFins, rcs;
        public string legsKind;
        public bool gridFinDeployAltSet;
        public double gridFinDeployAltM;
        public string recoveryMode;
        public bool landingReserveSet;
        public double landingReserveFraction;
        public bool boosterSepSet;
        public double boosterSepPct;
        public GuidanceSpec guidance;
        public bool heatShieldSet, heatShield;
        public string missionId;
        public string avionicsTier = "standard";
        /// <summary>#341: the optional closed-loop guidance computer (avionics.guidanceComputer, Flight.GuidanceComputer). False is
        /// basic guidance, a pre-set pitch program with no correction; the JSON keeps no key then, so existing designs stay byte-identical.</summary>
        public bool guidanceComputer;
        public double safetyFactor = 1.4;
        public string liveryPrimary = "#f2f2f2", liveryAccent = "#1a3c6e", liveryName = "";

        public int StrapOnCount => strapOns?.count ?? 0;

        public Design Clone()
        {
            var d = (Design)MemberwiseClone();
            d.stages = stages.ConvertAll(s => s.Clone());
            d.strapOns = strapOns?.Clone();
            d.guidance = guidance?.Clone();
            return d;
        }

        public static Design FromJson(JsonObject o)
        {
            var d = new Design { version = (int)o.Num("version", 1), id = o.Str("id"), name = o.Str("name"), tier = o.Str("tier") };
            foreach (JsonObject s in o.Arr("stages")) d.stages.Add(StagePart.FromJson(s));
            d.strapOns = StagePart.FromJson(o.Obj("strapOns"));
            var p = o.Obj("payload"); d.payloadType = p.Str("type"); d.payloadMass = p.Num("mass"); d.payloadOrbit = p.Str("orbit");
            var f = o.Obj("fairing"); d.fairingDiameter = f.Num("diameter"); d.fairingLength = f.Num("length"); d.fairingMaterial = f.Str("material");
            d.fairingJettisonAltitudeKm = f.Num("jettisonAltitudeKm", 110);
            var a = o.Obj("aero"); if (a != null)
            {
                d.nose = a.Str("nose", "ogive"); d.fins = a.Bool("fins");
                if (a.ContainsKey("finCount") && a["finCount"] is double fc) { d.finCount = (int)fc; d.finCountSet = true; }
                if (a.ContainsKey("finSpanM") && a["finSpanM"] is double fs) { d.finSpanM = fs; d.finSpanSet = true; }
            }
            var fin = o.Obj("fins");
            if (fin != null)
            {
                if (fin.ContainsKey("planform") && fin["planform"] is string plan) d.finPlanform = plan;
                if (DesignJson.Take(fin, "rootChord", out d.finRootChord)) d.finRootChordSet = true;
            }
            var l = o.Obj("landing"); if (l != null) { d.legs = l.Bool("legs"); d.gridFins = l.Bool("gridFins"); d.rcs = l.Bool("rcs"); }
            var legs = o.Obj("legs"); if (legs != null && legs.ContainsKey("kind")) d.legsKind = legs.Str("kind");
            if (DesignJson.Take(o, "gridFinDeployAltM", out d.gridFinDeployAltM)) d.gridFinDeployAltSet = true;
            var recovery = o.Obj("recovery"); if (recovery != null && recovery.ContainsKey("mode")) d.recoveryMode = recovery.Str("mode");
            if (DesignJson.Take(o, "landingReserveFraction", out d.landingReserveFraction)) d.landingReserveSet = true;
            if (DesignJson.Take(o, "boosterSepPct", out d.boosterSepPct)) d.boosterSepSet = true;
            d.guidance = GuidanceSpec.FromJson(o.Obj("guidance"));
            if (o.ContainsKey("heatShield") && o["heatShield"] is bool hs) { d.heatShieldSet = true; d.heatShield = hs; }
            if (o.ContainsKey("missionId") && o["missionId"] is string mid) d.missionId = mid;
            d.avionicsTier = o.Obj("avionics")?.Str("tier", "standard") ?? "standard";
            d.guidanceComputer = o.Obj("avionics")?.Bool("guidanceComputer") ?? false;
            d.safetyFactor = o.Num("safetyFactor", 1.4);
            var lv = o.Obj("livery"); if (lv != null) { d.liveryPrimary = lv.Str("primary"); d.liveryAccent = lv.Str("accent"); d.liveryName = lv.Str("name", ""); }
            return d;
        }

        public JsonObject ToJson()
        {
            var d = new JsonObject();
            d.Add("version", (double)version);
            if (id != null) d.Add("id", id);
            if (name != null) d.Add("name", name);
            if (tier != null) d.Add("tier", tier);
            var stages = new List<object>();
            foreach (var s in this.stages) stages.Add(s.ToJson());
            d.Add("stages", stages);
            if (strapOns != null) d.Add("strapOns", strapOns.ToJson());
            d.Add("payload", DesignJson.Obj(("type", payloadType), ("mass", payloadMass), ("orbit", payloadOrbit)));
            d.Add("fairing", DesignJson.Obj(("diameter", fairingDiameter), ("length", fairingLength), ("material", fairingMaterial), ("jettisonAltitudeKm", fairingJettisonAltitudeKm)));
            var aero = new JsonObject();
            aero.Add("nose", nose ?? "ogive");
            aero.Add("fins", fins);
            if (finCountSet) aero.Add("finCount", (double)finCount);
            if (finSpanSet) aero.Add("finSpanM", finSpanM);
            d.Add("aero", aero);
            if (finPlanform != null || finRootChordSet)
            {
                var fins = new JsonObject();
                if (finPlanform != null) fins.Add("planform", finPlanform);
                if (finRootChordSet) fins.Add("rootChord", finRootChord);
                d.Add("fins", fins);
            }
            d.Add("landing", DesignJson.Obj(("legs", legs), ("gridFins", gridFins), ("rcs", rcs)));
            if (legsKind != null) d.Add("legs", DesignJson.Obj(("kind", legsKind)));
            if (gridFinDeployAltSet) d.Add("gridFinDeployAltM", gridFinDeployAltM);
            if (recoveryMode != null) d.Add("recovery", DesignJson.Obj(("mode", recoveryMode)));
            if (landingReserveSet) d.Add("landingReserveFraction", landingReserveFraction);
            if (boosterSepSet) d.Add("boosterSepPct", boosterSepPct);
            if (guidance != null && guidance.Any) d.Add("guidance", guidance.ToJson());
            if (heatShieldSet) d.Add("heatShield", heatShield);
            if (missionId != null) d.Add("missionId", missionId);
            if (guidanceComputer) d.Add("avionics", DesignJson.Obj(("tier", avionicsTier ?? "standard"), ("guidanceComputer", true)));
            else d.Add("avionics", DesignJson.Obj(("tier", avionicsTier ?? "standard")));
            d.Add("safetyFactor", safetyFactor);
            d.Add("livery", DesignJson.Obj(("primary", liveryPrimary), ("accent", liveryAccent), ("name", liveryName ?? "")));
            return d;
        }
    }

    public struct TankSize { public double volume, length, diameter; }

    public static class DesignMath
    {
        public static readonly Dictionary<string, double> NoseShapes = new Dictionary<string, double> { ["ogive"] = 0.30, ["conic"] = 0.36, ["blunt"] = 0.50, ["von-karman"] = 0.28 };
        const double DOME_LENGTH_FACTOR = 0.33, ULLAGE = 0.03;

        public static TankSize SizeTank(Catalog c, string propellantId, double propMass, double diameter)
        {
            if (!c.PropellantById.TryGetValue(propellantId ?? "", out var p) || !(diameter > 0)) return new TankSize { diameter = diameter > 0 ? diameter : 0 };
            double volume = (propMass / p.bulkDensity) * (1 + ULLAGE);
            double area = Math.PI * Math.Pow(diameter / 2, 2);
            double length = volume / area + (p.storability == "solid" ? 0 : DOME_LENGTH_FACTOR * diameter);
            return new TankSize { volume = volume, length = length, diameter = diameter };
        }
    }

    static class DesignJson
    {
        public static bool Take(JsonObject o, string k, out double n)
        {
            n = 0;
            if (o == null || !o.ContainsKey(k) || !(o[k] is double d)) return false;
            n = d;
            return true;
        }

        public static JsonObject Obj(params (string k, object v)[] pairs)
        {
            var o = new JsonObject();
            foreach (var (k, v) in pairs) o.Add(k, v);
            return o;
        }
    }
}
