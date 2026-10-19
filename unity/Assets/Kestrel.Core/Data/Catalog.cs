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
        public double pcMin, pcRated, epsMin, epsMax, cycleMassFactor;
        public double[] throttle; public bool restart; public int restarts;
        public System.Collections.Generic.List<NozzleVariant> nozzles;
    }

    public sealed class NozzleVariant
    {
        public string id;
        public double expansionRatio, ispSL, ispVac, thrustSL, thrustVac, mass, exitDiameter;
    }

    public sealed class Material
    {
        public string id, label; public double structuralFraction, density, yieldMPa, costPerKg, maxTempK, elasticGPa; public bool cryoOK;
    }

    public sealed class OrbitTarget { public string id, label; public double apoKm, periKm, incDeg; }

    public sealed class Payload
    {
        public string id, displayName, label, orbit;
        public string whatItIs, whatItDoes, why, form;
        public double mass, volume, diameter, maxG, maxHeatFlux;
        public bool crewed, needsAbort;
    }

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
        /// <summary>Mission cargo (campaign.json volumeM3/diameterM) by mission id. A design with that missionId is fitted against it.</summary>
        public readonly Dictionary<string, Vehicle.PayloadSpec> MissionPayloads = new Dictionary<string, Vehicle.PayloadSpec>();
        public readonly List<Vehicle.Design> Presets = new List<Vehicle.Design>();
        public JsonObject Econ;
        /// <summary>propellants.engines.materials.payloads version numbers. Part of the max-Q cache key.</summary>
        public string VersionKey = "0";
        /// <summary>FNV-1a 64 of the four raw catalog documents. A data edit with no version bump still misses the cache.</summary>
        public string ContentHash = "0";

        public Vehicle.Design Preset(string id) => Presets.Find(p => p.id == id);

        public static Catalog FromJson(string propellants, string engines, string materials, string payloads, string presets, string economyConstants)
        {
            var c = new Catalog();
            var propDoc = MiniJson.ParseObject(propellants);
            var engDoc = MiniJson.ParseObject(engines);
            var matDoc = MiniJson.ParseObject(materials);
            var payDoc = MiniJson.ParseObject(payloads);
            c.VersionKey = Ver(propDoc) + "." + Ver(engDoc) + "." + Ver(matDoc) + "." + Ver(payDoc);
            c.ContentHash = Fingerprint(propellants, engines, materials, payloads);
            foreach (JsonObject o in propDoc.Arr("propellants"))
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
            foreach (JsonObject o in engDoc.Arr("engines"))
            {
                var th = o.Arr("throttle");
                var e = new Engine
                {
                    id = o.Str("id"), label = o.Str("label"), cycle = o.Str("cycle"), propellant = o.Str("propellant"),
                    thrustSL = o.Num("thrustSL"), thrustVac = o.Num("thrustVac"), ispSL = o.Num("ispSL"), ispVac = o.Num("ispVac"),
                    mass = o.Num("mass"), pc = o.Num("pc"), gimbal = o.Num("gimbal"), cost = o.Num("cost"), reliability = o.Num("reliability"),
                    expansionRatio = o.Num("expansionRatio"), diameter = o.Num("diameter"),
                    pcMin = o.Num("pcMin"), pcRated = o.Num("pcRated"), epsMin = o.Num("epsMin"), epsMax = o.Num("epsMax"),
                    cycleMassFactor = o.Num("cycleMassFactor"),
                    throttle = new[] { (double)th[0], (double)th[1] }, restart = o.Bool("restart"), restarts = (int)o.Num("restarts"),
                };
                var nozzles = o.Arr("nozzles");
                if (nozzles != null)
                {
                    e.nozzles = new System.Collections.Generic.List<NozzleVariant>();
                    foreach (JsonObject n in nozzles)
                        e.nozzles.Add(new NozzleVariant
                        {
                            id = n.Str("id"), expansionRatio = n.Num("expansionRatio"), ispSL = n.Num("ispSL"), ispVac = n.Num("ispVac"),
                            thrustSL = n.Num("thrustSL"), thrustVac = n.Num("thrustVac"), mass = n.Num("mass"), exitDiameter = n.Num("exitDiameter"),
                        });
                }
                c.Engines.Add(e); c.EngineById[e.id] = e;
            }
            foreach (JsonObject o in matDoc.Arr("materials"))
            {
                var m = new Material
                {
                    id = o.Str("id"), label = o.Str("label"), structuralFraction = o.Num("structuralFraction"), density = o.Num("density"),
                    yieldMPa = o.Num("yieldMPa"), costPerKg = o.Num("costPerKg"), maxTempK = o.Num("maxTempK"), elasticGPa = o.Num("elasticGPa"),
                    cryoOK = o.Bool("cryoOK"),
                };
                c.Materials.Add(m); c.MaterialById[m.id] = m;
            }
            var pl = payDoc;
            foreach (var k in pl.Obj("orbits").Order)
            {
                var o = (JsonObject)pl.Obj("orbits")[k];
                c.Orbits[k] = new OrbitTarget { id = o.Str("id"), label = o.Str("label"), apoKm = o.Num("apoKm"), periKm = o.Num("periKm"), incDeg = o.Num("incDeg") };
            }
            foreach (JsonObject o in pl.Arr("payloads"))
            {
                var p = new Payload
                {
                    id = o.Str("id"), displayName = o.Str("displayName", o.Str("label")), label = o.Str("label"), orbit = o.Str("orbit"), mass = o.Num("mass"), volume = o.Num("volume"), diameter = o.Num("diameter"),
                    maxG = o.Num("maxG"), maxHeatFlux = o.Num("maxHeatFlux"), crewed = o.Bool("crewed"), needsAbort = o.Bool("needsAbort"),
                    whatItIs = o.Str("whatItIs"), whatItDoes = o.Str("whatItDoes"), why = o.Str("why"), form = o.Str("form"),
                };
                c.Payloads.Add(p); c.PayloadById[p.id] = p;
            }
            foreach (JsonObject o in MiniJson.ParseObject(presets).Arr("presets")) c.Presets.Add(Vehicle.Design.FromJson(o));
            c.Econ = MiniJson.ParseObject(economyConstants);
            return c;
        }

        static string Ver(JsonObject doc) => doc.Num("version", 0).ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>FNV-1a 64 of the four raw JSON documents, separated by 0x1F. 16 lowercase hex digits.</summary>
        public static string Fingerprint(string propellants, string engines, string materials, string payloads)
        {
            ulong h = 14695981039346656037UL;
            void Mix(string s)
            {
                if (s == null) s = "";
                for (int i = 0; i < s.Length; i++)
                {
                    h ^= s[i];
                    h *= 1099511628211UL;
                }
                h ^= 0x1F;
                h *= 1099511628211UL;
            }
            Mix(propellants);
            Mix(engines);
            Mix(materials);
            Mix(payloads);
            return h.ToString("x16");
        }

        /// <summary>Load from a checkout: data/*.json + web/js/economy-data/constants.json.</summary>
        public static Catalog LoadFromRepo(string repoRoot)
        {
            string R(params string[] p) => System.IO.File.ReadAllText(System.IO.Path.Combine(repoRoot, System.IO.Path.Combine(p)));
            Designer.DesignerLimits.Load(repoRoot);
            var cat = FromJson(R("data", "propellants.json"), R("data", "engines.json"), R("data", "materials.json"),
                R("data", "payloads.json"), R("data", "presets.json"), R("web", "js", "economy-data", "constants.json"));
            // Mission targets (kh-m1 and the rest) have to be on every catalog the designer opens,
            // not only the one HomeAutoBuild.Choose happened to mutate.
            Kestrel.Core.Flight.HomeAutoBuild.InstallMissionOrbits(cat, Kestrel.Core.Flight.Campaign.Load(repoRoot));
            return cat;
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

        /// <summary>
        /// Checkout that holds data/golden, or null on a player build. A set KESTREL_REPO_ROOT is accepted
        /// only when that directory itself is a checkout: a device (or a test simulating one) must not walk
        /// up from a dummy path and find the CI checkout. An unset variable still walks from the cwd.
        /// </summary>
        public static string RepoRootOrNull()
        {
            var env = System.Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            if (!string.IsNullOrEmpty(env))
            {
                if (System.IO.Directory.Exists(System.IO.Path.Combine(env, "data", "golden"))) return env;
                return null;
            }
            try { return FindRepoRoot(System.IO.Directory.GetCurrentDirectory()); }
            catch (System.Exception) { return null; }
        }
    }
}
