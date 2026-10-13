// EditMode coverage for save/load, the instant Δv / TWR / mass readout, and catalog shape.
// Numbers below are a hand evaluation of the M1 mass model and the vacuum rocket equation.
// They do not call Mass or RocketEq to build the expected value.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class CatalogAndStatsTests
    {
        static readonly Regex IdRx = new Regex("^[a-z0-9-]+$");

        [Test]
        public void Presets_RoundTripThroughTheSaveStore()
        {
            string dir = TempDir();
            long t = 1000;
            try
            {
                var writing = new NativeStore(dir, () => t++, () => 0.42);
                foreach (JsonObject preset in Presets())
                {
                    string id = preset.Str("id");
                    writing.PutDesign(JsonMut.CloneObject(preset));
                    var reading = new NativeStore(dir, () => t++, () => 0.42);
                    var loaded = reading.GetDesign(id);
                    var expect = DesignMigration.Migrate(JsonMut.CloneObject(preset));
                    JsonSame.Equal(loaded, expect, id);
                    var design = Design.FromJson(loaded);
                    Assert.AreEqual(id, design.id, id);
                    Assert.AreEqual(preset.Str("name"), design.name, id);
                    Assert.Greater(design.stages.Count, 0, id);
                    Assert.AreEqual(preset.Arr("stages").Count, design.stages.Count, id);
                    Assert.AreEqual(((JsonObject)preset.Arr("stages")[0]).Str("engine"), design.stages[0].engine, id);
                    Assert.AreEqual(((JsonObject)preset.Arr("stages")[0]).Num("propMass"), design.stages[0].propMass, 0, id);
                    Assert.AreEqual(preset.Obj("fairing").Num("diameter"), design.fairingDiameter, 0, id);
                    Assert.AreEqual(preset.Obj("livery").Str("name"), design.liveryName, id);
                }
            }
            finally { Directory.Delete(dir, true); }
        }

        [Test]
        public void V2Fields_SurviveSaveAndLoad()
        {
            JsonObject stock = null;
            foreach (JsonObject p in Presets()) if (p.Str("id") == "stock") stock = p;
            Assert.IsNotNull(stock);
            var design = DesignMigration.Migrate(JsonMut.CloneObject(stock));
            var stage0 = (JsonObject)design.Arr("stages")[0];
            var stage1 = (JsonObject)design.Arr("stages")[1];
            JsonMut.Set(stage0.Obj("cluster"), "pattern", "ring7");
            JsonMut.Set(stage0, "nozzle", "default");
            var tune = new JsonObject();
            tune.Add("thrustPct", 5d);
            tune.Add("ispPct", -2d);
            JsonMut.Set(stage0, "tune", tune);
            JsonMut.Set(stage1.Obj("interstage"), "shape", "flare");
            JsonMut.Set(design.Obj("aero"), "fins", true);
            JsonMut.Set(design.Obj("aero"), "finCount", 3d);
            JsonMut.Set(design.Obj("aero"), "finSpanM", 0.75d);
            JsonMut.Set(design.Obj("livery"), "name", "Round Trip");

            string dir = TempDir();
            long t = 50;
            try
            {
                new NativeStore(dir, () => t++, () => 0.5).PutDesign(design, "stock", "Stock Kestrel Heavy");
                var loaded = new NativeStore(dir, () => t++, () => 0.5).GetDesign("stock");
                JsonSame.Equal(loaded, design, "stock");
                var parsed = Design.FromJson(loaded);
                Assert.AreEqual(2, parsed.version);
                Assert.AreEqual("ring7", parsed.stages[0].clusterPattern);
                Assert.AreEqual("default", parsed.stages[0].nozzle);
                Assert.AreEqual(5, parsed.stages[0].tune.thrustPct, 1e-9);
                Assert.AreEqual(-2, parsed.stages[0].tune.ispPct, 1e-9);
                Assert.AreEqual("flare", parsed.stages[1].interstageShape);
                Assert.IsTrue(parsed.fins);
                Assert.AreEqual(3, parsed.finCount);
                Assert.AreEqual(0.75, parsed.finSpanM, 1e-9);
                Assert.AreEqual("Round Trip", parsed.liveryName);
                Assert.IsNull(new NativeStore(dir, () => t, () => 0).GetDesign("missing"));
            }
            finally { Directory.Delete(dir, true); }
        }

        [Test]
        public void TwoStagePencil_MatchesHandDeltaVTwrAndMass()
        {
            // Stage 1: 10 t kerolox, 2 m, one gg-kerolox (845 kN SL / 934 kN vac, Isp 282/311, 470 kg), Al-Li.
            // Stage 2: 2 t kerolox, 2 m, one gg-kerolox-small (90.6/100 kN, Isp 282/311, 95 kg), Al-Li, basic avionics.
            // Fairing 2 m × 5 m composite. Payload 100 kg. No legs, grid fins, RCS, or strap-ons.
            // interstageMass is 0 on both stages, so the frustum is not derived.
            // structure = propellant × 0.045 (kerolox volume scale is 1) at safety factor 1.4.
            // fairing = 2 π r L × 0.85 × 14 kg/m².
            // Δv = Isp × 9.80665 × ln(m0 / (m0 − propellant)). Pad TWR uses sea-level thrust.
            var c = Golden.Catalog;
            var doc = Pencil();
            var design = Design.FromJson(doc);
            var mass = Mass.Of(c, design);
            var read = Readout.Quick(c, doc);

            const double fairing = 373.84952577718536;
            const double dry1 = 920;
            const double dry2 = 245;
            const double m0Upper = 2718.8495257771856;
            const double m0Pad = 13638.849525777186;
            Assert.AreEqual(fairing, mass.fairing, 1e-6);
            Assert.AreEqual(dry1, mass.stages[0].dry, 1e-6);
            Assert.AreEqual(dry2, mass.stages[1].dry, 1e-6);
            Assert.AreEqual(dry1 + dry2, mass.dryMass, 1e-6);
            Assert.AreEqual(m0Pad, mass.liftoffMass, 1e-6);
            Assert.AreEqual(10000, mass.stages[0].usableProp, 0);
            Assert.AreEqual(2000, mass.stages[1].usableProp, 0);

            Assert.AreEqual(2, read.stages.Count);
            Assert.AreEqual(m0Pad, read.liftoffMass, 1e-6);
            Assert.AreEqual(m0Pad, read.stages[0].m0, 1e-6);
            Assert.AreEqual(6.317689760692988, read.liftoffTWR, 1e-9);
            Assert.AreEqual(read.liftoffTWR, read.stages[0].twr, 1e-12);
            Assert.AreEqual(4029.6527370509234, read.stages[0].deltaV, 1e-6);
            Assert.AreEqual(3653.897337132992, read.stages[0].deltaVSL_mps.Value, 1e-6);
            Assert.AreEqual(dry1, read.stages[0].dryMass_kg, 1e-6);
            Assert.AreEqual(10000, read.stages[0].propMass_kg, 0);

            Assert.AreEqual(m0Upper, read.stages[1].m0, 1e-6);
            Assert.AreEqual(3.750543026784248, read.stages[1].twr, 1e-9);
            Assert.AreEqual(read.stages[1].twr, read.stages[1].twrVac, 1e-12);
            Assert.AreEqual(4057.276345297751, read.stages[1].deltaV, 1e-6);
            Assert.IsNull(read.stages[1].deltaVSL_mps);
            Assert.AreEqual(8086.9290823486745, read.totalDeltaV, 1e-6);
            Assert.AreEqual(read.stages[0].deltaV + read.stages[1].deltaV, read.totalDeltaV, 1e-9);

            var tank = DesignMath.SizeTank(c, "kerolox", 10000, 2);
            Assert.AreEqual(10, tank.volume, 1e-12);
            Assert.AreEqual(3.843098861837907, tank.length, 1e-9);
            Assert.AreEqual(2, tank.diameter, 0);
        }

        [Test]
        public void Catalogs_HaveNoMissingFieldsOrDuplicateIds()
        {
            var c = Golden.Catalog;
            var engines = Rows("engines.json", "engines");
            var props = Rows("propellants.json", "propellants");
            var mats = Rows("materials.json", "materials");
            var pays = Rows("payloads.json", "payloads");
            var presets = Rows("presets.json", "presets");

            AssertUnique(engines, "engine");
            AssertUnique(props, "propellant");
            AssertUnique(mats, "material");
            AssertUnique(pays, "payload");
            AssertUnique(presets, "preset");
            Assert.AreEqual(engines.Count, c.Engines.Count, "duplicate engine id would collapse the lookup");
            Assert.AreEqual(props.Count, c.Propellants.Count);
            Assert.AreEqual(mats.Count, c.Materials.Count);
            Assert.AreEqual(pays.Count, c.Payloads.Count);

            string[] engineKeys = { "id", "label", "cycle", "propellant", "thrustSL", "thrustVac", "ispSL", "ispVac", "mass", "pc", "throttle", "restart", "restarts", "gimbal", "cost", "reliability", "expansionRatio", "diameter" };
            foreach (var e in engines)
            {
                Require(e, engineKeys, "engine");
                string id = e.Str("id");
                Assert.IsTrue(IdRx.IsMatch(id), id);
                Assert.IsFalse(string.IsNullOrEmpty(e.Str("label")), id);
                Assert.IsTrue(c.PropellantById.ContainsKey(e.Str("propellant")), id + " propellant");
                Assert.Greater(e.Num("thrustVac"), 0, id);
                Assert.GreaterOrEqual(e.Num("thrustVac"), e.Num("thrustSL"), id);
                Assert.Greater(e.Num("ispVac"), 0, id);
                Assert.IsTrue(e.Num("ispVac") > e.Num("ispSL") || e.Num("ispSL") == 0, id + " isp");
                Assert.LessOrEqual(e.Num("ispVac"), c.PropellantById[e.Str("propellant")].ispVacMax, id);
                var throttle = e.Arr("throttle");
                Assert.AreEqual(2, throttle.Count, id);
                double t0 = (double)throttle[0], t1 = (double)throttle[1];
                Assert.Greater(t0, 0, id);
                Assert.LessOrEqual(t0, t1, id);
                Assert.Greater(e.Num("reliability"), 0, id);
                // Schema maximum is inclusive 1 (engines.schema.json).
                Assert.LessOrEqual(e.Num("reliability"), 1, id);
                Assert.Greater(e.Num("pc"), 0, id);
                Assert.Greater(e.Num("expansionRatio"), 0, id);
                Assert.Greater(e.Num("diameter"), 0, id);
                Assert.GreaterOrEqual(e.Num("cost"), 0, id);
                bool solid = e.Str("cycle") == "solid";
                if (solid)
                {
                    // Solids use stage casing mass; engine mass stays 0.
                    Assert.AreEqual(0, e.Num("mass"), id);
                    Assert.IsFalse(e.Bool("restart"), id);
                    Assert.AreEqual(1, t0, id);
                }
                else
                {
                    // Mass.EnginesMass uses this value directly for liquid/hybrid.
                    Assert.Greater(e.Num("mass"), 0, id);
                    double twr = e.Num("thrustVac") / (e.Num("mass") * 9.80665);
                    Assert.Greater(twr, 10, id);
                    Assert.Less(twr, 220, id);
                }
                var nozzles = e.Arr("nozzles");
                if (nozzles != null)
                {
                    double ispCap = c.PropellantById[e.Str("propellant")].ispVacMax;
                    var seen = new HashSet<string>();
                    foreach (JsonObject n in nozzles)
                    {
                        string nid = id + " nozzle " + n.Str("id");
                        Assert.IsTrue(seen.Add(n.Str("id")), nid);
                        Assert.Greater(n.Num("thrustVac"), 0, nid);
                        Assert.Greater(n.Num("exitDiameter"), 0, nid);
                        // EnginePoint.Of uses the variant Isp; same propellant cap as the default bell.
                        Assert.LessOrEqual(n.Num("ispVac"), ispCap, nid);
                    }
                }
            }

            string[] propKeys = { "id", "label", "fuel", "oxidizer", "fuelDensity", "oxDensity", "mixtureRatio", "bulkDensity", "ispSL", "ispVac", "ispVacMax", "storability", "boiloffPerHour", "costPerKg", "handling" };
            foreach (var p in props)
            {
                Require(p, propKeys, "propellant");
                Assert.Greater(p.Num("bulkDensity"), 50, p.Str("id"));
                Assert.Less(p.Num("bulkDensity"), 2500, p.Str("id"));
                Assert.Greater(p.Num("ispVac"), p.Num("ispSL"), p.Str("id"));
                Assert.GreaterOrEqual(p.Num("ispVacMax"), p.Num("ispVac"), p.Str("id"));
                Assert.IsNotNull(p.Arr("handling"), p.Str("id"));
            }

            string[] matKeys = { "id", "label", "structuralFraction", "density", "yieldMPa", "costPerKg", "maxTempK", "cryoOK" };
            foreach (var m in mats)
            {
                Require(m, matKeys, "material");
                Assert.Greater(m.Num("structuralFraction"), 0, m.Str("id"));
                Assert.LessOrEqual(m.Num("structuralFraction"), 1, m.Str("id"));
                Assert.Greater(m.Num("density"), 0, m.Str("id"));
                Assert.Greater(m.Num("yieldMPa"), 0, m.Str("id"));
                Assert.Greater(m.Num("maxTempK"), 0, m.Str("id"));
                Assert.GreaterOrEqual(m.Num("costPerKg"), 0, m.Str("id"));
            }

            string[] payKeys = { "id", "displayName", "label", "mass", "volume", "maxG", "maxHeatFlux", "orbit" };
            foreach (var p in pays)
            {
                Require(p, payKeys, "payload");
                Assert.IsTrue(c.Orbits.ContainsKey(p.Str("orbit")), p.Str("id"));
                Assert.Greater(p.Num("mass"), 0, p.Str("id"));
                Assert.Greater(p.Num("volume"), 0, p.Str("id"));
                Assert.Greater(p.Num("maxG"), 0, p.Str("id"));
                Assert.Greater(p.Num("maxHeatFlux"), 0, p.Str("id"));
            }

            foreach (var d in presets)
            {
                Assert.IsTrue(c.Orbits.ContainsKey(d.Obj("payload").Str("orbit")), d.Str("id"));
                Assert.IsTrue(c.PayloadById.ContainsKey(d.Obj("payload").Str("type")), d.Str("id") + " payload");
                Assert.IsTrue(c.MaterialById.ContainsKey(d.Obj("fairing").Str("material")), d.Str("id") + " fairing");
                CheckPart(c, d.Obj("strapOns"), d.Str("id") + " strapon");
                foreach (JsonObject s in d.Arr("stages")) CheckPart(c, s, d.Str("id"));
            }
        }

        static void CheckPart(Catalog c, JsonObject part, string where)
        {
            Assert.IsNotNull(part, where);
            Assert.IsTrue(c.EngineById.ContainsKey(part.Str("engine")), where + " engine");
            Assert.IsTrue(c.PropellantById.ContainsKey(part.Str("propellant")), where + " propellant");
            Assert.IsTrue(c.MaterialById.ContainsKey(part.Str("material")), where + " material");
            Assert.Greater(part.Num("diameter"), 0, where);
            Assert.GreaterOrEqual(part.Num("propMass"), 0, where);
        }

        static void Require(JsonObject o, string[] keys, string what)
        {
            foreach (var k in keys) Assert.IsTrue(o.ContainsKey(k), what + " " + o.Str("id") + " missing " + k);
        }

        static void AssertUnique(List<JsonObject> rows, string what)
        {
            var seen = new HashSet<string>();
            Assert.Greater(rows.Count, 0, what);
            foreach (var o in rows)
            {
                string id = o.Str("id");
                Assert.IsFalse(string.IsNullOrEmpty(id), what);
                Assert.IsTrue(seen.Add(id), "duplicate " + what + " " + id);
            }
        }

        static List<JsonObject> Rows(string file, string key)
        {
            var list = new List<JsonObject>();
            foreach (JsonObject o in MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", file))).Arr(key)) list.Add(o);
            return list;
        }

        static List<object> Presets() => MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "presets.json"))).Arr("presets");

        static string TempDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kh-rt-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        static JsonObject Pencil()
        {
            JsonObject Stage(string name, string engine, double prop)
            {
                var s = new JsonObject();
                s.Add("name", name);
                s.Add("propellant", "kerolox");
                s.Add("engine", engine);
                s.Add("engineCount", 1d);
                s.Add("propMass", prop);
                s.Add("diameter", 2d);
                s.Add("material", "al-li");
                s.Add("interstageMass", 0d);
                s.Add("separationMass", 0d);
                s.Add("landingPropMass", 0d);
                return s;
            }
            var d = new JsonObject();
            d.Add("version", 1d);
            d.Add("id", "pencil");
            d.Add("name", "Pencil");
            d.Add("stages", new List<object> { Stage("Stage 1", "gg-kerolox", 10000), Stage("Stage 2", "gg-kerolox-small", 2000) });
            var payload = new JsonObject();
            payload.Add("type", "cubesat");
            payload.Add("mass", 100d);
            payload.Add("orbit", "leo400");
            d.Add("payload", payload);
            var fairing = new JsonObject();
            fairing.Add("diameter", 2d);
            fairing.Add("length", 5d);
            fairing.Add("material", "composite");
            fairing.Add("jettisonAltitudeKm", 110d);
            d.Add("fairing", fairing);
            var aero = new JsonObject();
            aero.Add("nose", "ogive");
            aero.Add("fins", false);
            d.Add("aero", aero);
            var landing = new JsonObject();
            landing.Add("legs", false);
            landing.Add("gridFins", false);
            landing.Add("rcs", false);
            d.Add("landing", landing);
            var av = new JsonObject();
            av.Add("tier", "basic");
            d.Add("avionics", av);
            d.Add("safetyFactor", 1.4d);
            return d;
        }
    }
}
