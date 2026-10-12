// Catalog records loaded from data/*.json (the source of truth shared with the web game).
using System;
using System.Collections.Generic;
using Kestrel.Core.Json;

namespace Kestrel.Core.Data
{
    public sealed class Propellant
    {
        public string id, label, fuel, oxidizer, storability;
        public double fuelDensity, oxDensity, bulkDensity, ispSL, ispVac, ispVacMax, boiloffPerHour, costPerKg;
        public double? mixtureRatio;
    }

    public sealed class Engine
    {
        public string id, label, cycle, propellant;
        public double thrustSL, thrustVac, ispSL, ispVac, mass, pc, gimbal, cost, reliability, expansionRatio, diameter;
        public double[] throttle; public bool restart; public int restarts;
    }

    public sealed class Material
    {
        public string id, label; public double structuralFraction, density, yieldMPa, costPerKg, maxTempK; public bool cryoOK;
    }

    public sealed class OrbitTarget { public string id, label; public double apoKm, periKm, incDeg; }

    public sealed class Payload { public string id, label, orbit; public double mass, volume, maxG, maxHeatFlux; public bool crewed, needsAbort; }

    /// <summary>All catalogs plus economy constants. Build with Catalog.FromJson(...) or Catalog.LoadFromRepo(root).</summary>
    public sealed class Catalog
    {
        public readonly List<Propellant> Propellants = new List<Propellant>();
        public readonly List<Engine> Engines = new List<Engine>();
        public readonly List<Material> Materials = new List<Material>();
        public readonly List<Payload> Payloads = new List<Payload>();
        public readonly Dictionary<string, Propellant> PropellantById = new Dictionary<string, Propellant>();
        public readonly Dictionary<string, Engine> EngineById = new Dictionary<string, Engine>();
        public readonly Dictionary<string, Material> MaterialById = new Dictionary<string, Material>();
        public readonly Dictionary<string, Payload> PayloadById = new Dictionary<string, Payload>();
        public readonly Dictionary<string, OrbitTarget> Orbits = new Dictionary<string, OrbitTarget>();
        public readonly List<Vehicle.Design> Presets = new List<Vehicle.Design>();
        public JsonObject Econ;

        public Vehicle.Design Preset(string id) => Presets.Find(p => p.id == id);

        public static Catalog FromJson(string propellants, string engines, string materials, string payloads, string presets, string economyConstants)
        {
            var c = new Catalog();
            foreach (JsonObject o in MiniJson.ParseObject(propellants).Arr("propellants"))
            {
                var p = new Propellant
                {
                    id = o.Str("id"), label = o.Str("label"), fuel = o.Str("fuel"), oxidizer = o.Str("oxidizer"), storability = o.Str("storability"),
                    fuelDensity = o.Num("fuelDensity"), oxDensity = o.Num("oxDensity"), bulkDensity = o.Num("bulkDensity"),
                    ispSL = o.Num("ispSL"), ispVac = o.Num("ispVac"), ispVacMax = o.Num("ispVacMax"),
                    boiloffPerHour = o.Num("boiloffPerHour"), costPerKg = o.Num("costPerKg"), mixtureRatio = o.NumOrNull("mixtureRatio"),
                };
                c.Propellants.Add(p); c.PropellantById[p.id] = p;
            }
            foreach (JsonObject o in MiniJson.ParseObject(engines).Arr("engines"))
            {
                var th = o.Arr("throttle");
                var e = new Engine
                {
                    id = o.Str("id"), label = o.Str("label"), cycle = o.Str("cycle"), propellant = o.Str("propellant"),
                    thrustSL = o.Num("thrustSL"), thrustVac = o.Num("thrustVac"), ispSL = o.Num("ispSL"), ispVac = o.Num("ispVac"),
                    mass = o.Num("mass"), pc = o.Num("pc"), gimbal = o.Num("gimbal"), cost = o.Num("cost"), reliability = o.Num("reliability"),
                    expansionRatio = o.Num("expansionRatio"), diameter = o.Num("diameter"),
                    throttle = new[] { (double)th[0], (double)th[1] }, restart = o.Bool("restart"), restarts = (int)o.Num("restarts"),
                };
                c.Engines.Add(e); c.EngineById[e.id] = e;
            }
            foreach (JsonObject o in MiniJson.ParseObject(materials).Arr("materials"))
            {
                var m = new Material
                {
                    id = o.Str("id"), label = o.Str("label"), structuralFraction = o.Num("structuralFraction"), density = o.Num("density"),
                    yieldMPa = o.Num("yieldMPa"), costPerKg = o.Num("costPerKg"), maxTempK = o.Num("maxTempK"), cryoOK = o.Bool("cryoOK"),
                };
                c.Materials.Add(m); c.MaterialById[m.id] = m;
            }
            var pl = MiniJson.ParseObject(payloads);
            foreach (var k in pl.Obj("orbits").Order)
            {
                var o = (JsonObject)pl.Obj("orbits")[k];
                c.Orbits[k] = new OrbitTarget { id = o.Str("id"), label = o.Str("label"), apoKm = o.Num("apoKm"), periKm = o.Num("periKm"), incDeg = o.Num("incDeg") };
            }
            foreach (JsonObject o in pl.Arr("payloads"))
            {
                var p = new Payload
                {
                    id = o.Str("id"), label = o.Str("label"), orbit = o.Str("orbit"), mass = o.Num("mass"), volume = o.Num("volume"),
                    maxG = o.Num("maxG"), maxHeatFlux = o.Num("maxHeatFlux"), crewed = o.Bool("crewed"), needsAbort = o.Bool("needsAbort"),
                };
                c.Payloads.Add(p); c.PayloadById[p.id] = p;
            }
            foreach (JsonObject o in MiniJson.ParseObject(presets).Arr("presets")) c.Presets.Add(Vehicle.Design.FromJson(o));
            c.Econ = MiniJson.ParseObject(economyConstants);
            return c;
        }

        /// <summary>Load from a checkout: data/*.json + web/js/economy-data/constants.json.</summary>
        public static Catalog LoadFromRepo(string repoRoot)
        {
            string R(params string[] p) => System.IO.File.ReadAllText(System.IO.Path.Combine(repoRoot, System.IO.Path.Combine(p)));
            return FromJson(R("data", "propellants.json"), R("data", "engines.json"), R("data", "materials.json"),
                R("data", "payloads.json"), R("data", "presets.json"), R("web", "js", "economy-data", "constants.json"));
        }

        /// <summary>Walk up from a directory until a folder containing data/golden is found.</summary>
        public static string FindRepoRoot(string start)
        {
            var d = new System.IO.DirectoryInfo(start);
            while (d != null)
            {
                if (System.IO.Directory.Exists(System.IO.Path.Combine(d.FullName, "data", "golden"))) return d.FullName;
                d = d.Parent;
            }
            throw new System.IO.DirectoryNotFoundException("repo root with data/golden not found above " + start);
        }
    }
}
