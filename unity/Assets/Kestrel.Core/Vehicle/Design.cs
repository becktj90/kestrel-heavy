// Vehicle design (schema v1 or v2, same JSON as presets and saved designs) + tank sizing.
// Port of web/js/vehicle/design.js (data + sizeTank; shape validation stays in JS for now).
using System;
using System.Collections.Generic;
using Kestrel.Core.Json;
using Kestrel.Core.Data;

namespace Kestrel.Core.Vehicle
{
    public sealed class Tune { public double thrustPct, ispPct; }

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
        public StagePart Clone() { var s = (StagePart)MemberwiseClone(); return s; }

        internal static StagePart FromJson(JsonObject o)
        {
            if (o == null) return null;
            var s = new StagePart
            {
                name = o.Str("name"), propellant = o.Str("propellant"), engine = o.Str("engine"), material = o.Str("material"), kind = o.Str("kind"),
                engineCount = (int)o.Num("engineCount", 1), count = (int)o.Num("count"), propMass = o.Num("propMass"), diameter = o.Num("diameter"),
                interstageMass = o.Num("interstageMass"), separationMass = o.Num("separationMass"), landingPropMass = o.Num("landingPropMass"),
                interstageSpecified = o.ContainsKey("interstageMass") && o["interstageMass"] != null,
                crossfeed = o.Bool("crossfeed"),
                nozzle = o.Str("nozzle"),
                tune = o.Obj("tune") is JsonObject t ? new Tune { thrustPct = t.Num("thrustPct"), ispPct = t.Num("ispPct") } : null,
            };
            var cluster = o.Obj("cluster");
            if (cluster != null) s.clusterPattern = cluster.Str("pattern");
            var inter = o.Obj("interstage");
            if (inter != null) s.interstageShape = inter.Str("shape");
            return s;
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
        public bool legs, gridFins, rcs;
        public string avionicsTier = "standard";
        public double safetyFactor = 1.4;
        public string liveryPrimary = "#f2f2f2", liveryAccent = "#1a3c6e", liveryName = "";

        public int StrapOnCount => strapOns?.count ?? 0;

        public Design Clone()
        {
            var d = (Design)MemberwiseClone();
            d.stages = stages.ConvertAll(s => s.Clone());
            d.strapOns = strapOns?.Clone();
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
                if (a.ContainsKey("finCount") && a["finCount"] is double fc) d.finCount = (int)fc;
                if (a.ContainsKey("finSpanM") && a["finSpanM"] is double fs) d.finSpanM = fs;
            }
            var l = o.Obj("landing"); if (l != null) { d.legs = l.Bool("legs"); d.gridFins = l.Bool("gridFins"); d.rcs = l.Bool("rcs"); }
            d.avionicsTier = o.Obj("avionics")?.Str("tier", "standard") ?? "standard";
            d.safetyFactor = o.Num("safetyFactor", 1.4);
            var lv = o.Obj("livery"); if (lv != null) { d.liveryPrimary = lv.Str("primary"); d.liveryAccent = lv.Str("accent"); d.liveryName = lv.Str("name", ""); }
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
}
