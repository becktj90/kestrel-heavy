// Designer v3 tanks and structure: GV4, GV5, q_lim, and v2 mass identity (§2.3, §2.6, §8).
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class TankStructureTests
    {
        [Test]
        public void GV4_BoilOff_FoamThreeHours_Leaves99760Point2Kg()
        {
            const double mass = 100000;
            const double rate = 0.002;
            double remain = TankModel.RemainingKg(mass, rate, "foam", 3);
            double expect = mass * Math.Pow(1 - rate * TankModel.FoamBoiloff, 3);
            Assert.AreEqual(expect, remain, 0, "remaining = m·(1−r_eff)^h");
            Assert.AreEqual(mass - remain, TankModel.BoilOffLossKg(mass, rate, "foam", 3), 0);
            Assert.AreEqual(99760.2, remain, 0.05, "GV4 rounds to 99,760.2 kg");

            Assert.AreEqual(1, TankModel.BoiloffFactor("none"), 0);
            Assert.AreEqual(0.4, TankModel.BoiloffFactor("FOAM"), 0);
            Assert.AreEqual(0.15, TankModel.BoiloffFactor("MLI"), 0);
            Assert.AreEqual(mass, TankModel.RemainingKg(mass, rate, "none", 0), 0);

            var part = Stage(propMass: mass, insulation: "foam");
            Assert.AreEqual(remain, TankModel.RemainingPropKg(Golden.Catalog, part, 3), 0, "kerolox boiloffPerHour is 0.002");
        }

        [Test]
        public void GV5_HoopWall_Is1Point554Millimetres_AndGaugeHolds()
        {
            double t = TankModel.WallThicknessM(1.4, 3, 1.85, 500);
            double hand = 1.4 * (3 * TankModel.BarToPa) * 1.85 / (500 * TankModel.MPaToPa);
            Assert.AreEqual(hand, t, 0);
            Assert.AreEqual(0.001554, t, 1e-12);
            Assert.AreEqual(1.554, t * 1000, 1e-9, "GV5");

            double thin = 1.4 * (1.5 * TankModel.BarToPa) * 1.85 / (500 * TankModel.MPaToPa);
            Assert.Less(thin, TankModel.MinGaugeM);
            Assert.AreEqual(TankModel.MinGaugeM, TankModel.WallThicknessM(1.4, 1.5, 1.85, 500), 0);
            Assert.IsTrue(TankModel.PressureInRange(1.5));
            Assert.IsTrue(TankModel.PressureInRange(6));
            Assert.IsFalse(TankModel.PressureInRange(1.49));
            Assert.AreEqual(0, TankModel.WallThicknessM(1.4, 0, 1.85, 500), 0);
        }

        [Test]
        public void TankMass_UsesDomesInsulationAndCommonBulkhead()
        {
            const double prop = 100000, diameter = 3.7, sf = 1.4;
            double r = diameter / 2;
            double barrel = (prop / 1030.0) * (1 + TankModel.Ullage) / (Math.PI * r * r);
            double t = 1.4 * (3 * 1e5) * r / (500 * 1e6);
            double area = 2 * Math.PI * r * barrel + 4 * Math.PI * r * r;
            double shell = 2700 * t * area;
            double fraction = prop * 0.045;

            var bare = Stage(prop, diameter);
            Assert.IsNull(bare.tank);
            Assert.AreEqual(fraction, Mass.StructureMass(Golden.Catalog, bare, sf), 0);
            Assert.AreEqual(fraction, Mass.StructuralFractionMass(Golden.Catalog, bare, sf), 0);

            var press = Stage(prop, diameter, pressure: 3);
            Assert.AreEqual(shell, Mass.StructureMass(Golden.Catalog, press, sf), 1e-6);
            Assert.AreEqual(shell, TankModel.HoopShellKg(Golden.Catalog, press, sf), 1e-6);
            var geo = TankModel.Geometry(Golden.Catalog, press);
            Assert.AreEqual(barrel, geo.barrelLengthM, 1e-9);
            Assert.AreEqual(area, TankModel.SurfaceAreaM2(geo.radiusM, geo.barrelLengthM), 1e-6);

            var foam = Stage(prop, diameter, pressure: 3, insulation: "foam");
            Assert.AreEqual(shell + TankModel.FoamKgPerM2 * area, Mass.StructureMass(Golden.Catalog, foam, sf), 1e-6);
            var mli = Stage(prop, diameter, pressure: 3, insulation: "mli");
            Assert.AreEqual(shell + TankModel.MliKgPerM2 * area, Mass.StructureMass(Golden.Catalog, mli, sf), 1e-6);

            var bulk = Stage(prop, diameter, pressure: 3, bulkhead: true);
            Assert.AreEqual(shell * TankModel.CommonBulkheadFactor, Mass.StructureMass(Golden.Catalog, bulk, sf), 1e-6);
            var both = Stage(prop, diameter, pressure: 3, insulation: "foam", bulkhead: true);
            Assert.AreEqual(shell * 0.92 + 1.2 * area, Mass.StructureMass(Golden.Catalog, both, sf), 1e-6);

            var insulOnly = Stage(prop, diameter, insulation: "foam");
            Assert.AreEqual(fraction + 1.2 * area, Mass.StructureMass(Golden.Catalog, insulOnly, sf), 1e-4);
            var bulkOnly = Stage(prop, diameter, bulkhead: true);
            Assert.AreEqual(fraction * 0.92, Mass.StructureMass(Golden.Catalog, bulkOnly, sf), 1e-6);

            Assert.AreEqual(FinenessBand.Ok, TankModel.Fineness(18));
            Assert.AreEqual(FinenessBand.Warning, TankModel.Fineness(18.0001));
            Assert.AreEqual(FinenessBand.Warning, TankModel.Fineness(22));
            Assert.AreEqual(FinenessBand.Red, TankModel.Fineness(22.0001));
            var skinny = Stage(80000, 1);
            Assert.Greater(TankModel.FinenessRatio(TankModel.Geometry(Golden.Catalog, skinny).lengthM, 1), 22);
            Assert.AreEqual(FinenessBand.Red, TankModel.Fineness(Golden.Catalog, skinny));

            Assert.IsFalse(TankModel.CommonBulkheadHazard(true, 2));
            Assert.IsTrue(TankModel.CommonBulkheadHazard(true, 2.0001));
            Assert.IsFalse(TankModel.CommonBulkheadHazard(false, 4));
        }

        [Test]
        public void QLim_AndMargin_FollowTheSpec()
        {
            Assert.AreEqual(35000, StructureModel.QLimPa(1.4, 500), 1e-6, "reference al-li at SF 1.4");
            double gv10 = StructureModel.QLimPa(1.1, 500);
            Assert.AreEqual(35.0 * (1.1 / 1.4) * (500.0 / 500.0) * 1000.0, gv10, 1e-6);
            Assert.AreEqual(27500, gv10, 1e-4, "GV10 al-li at SF 1.1 is 27.5 kPa");

            var section = new StageSection { radiusM = 1.85, wallM = 0.001554, yieldMPa = 500, safetyFactor = 1.4, pressureBar = 3 };
            double fHand = 500e6 * 2 * Math.PI * 1.85 * 0.001554 / 1.4 + 3e5 * Math.PI * 1.85 * 1.85;
            var allow = StructureModel.Allowables(section);
            Assert.AreEqual(fHand, allow.fAllowN, 1e-4);
            Assert.AreEqual(35000, allow.qLimPa, 1e-6);

            var state = new LoadState
            {
                fAllowN = 320000, massAboveKg = 1000, aAxial = 10, qPa = 10000, areaM2 = 10, cd = 0.5,
                cnAlpha = 2, alphaRad = 0.1, xCpM = 8, xCgM = 3, radiusM = 2,
            };
            Assert.AreEqual(100000, StructureModel.BendingMomentNm(state), 0, "q·S·CN·α·|x|");
            Assert.AreEqual(100000, StructureModel.EquivalentBendingN(state), 0, "2M/r");
            Assert.AreEqual(160000, StructureModel.LoadN(state), 0);
            Assert.AreEqual(1, StructureModel.Margin(state), 0);
            state.fAllowN = 1000;
            Assert.Less(StructureModel.Margin(state), 0);
            Assert.IsTrue(StructureModel.StructuralFailure(StructureModel.Margin(state)));
            state.massAboveKg = 0; state.qPa = 0; state.aAxial = 0;
            Assert.IsTrue(double.IsPositiveInfinity(StructureModel.Margin(state)));

            Assert.IsFalse(StructureModel.PayloadDamaged(6 * StructureModel.G0, 6));
            Assert.IsTrue(StructureModel.PayloadDamaged(6 * StructureModel.G0 + 1e-6, 6));
            Assert.IsFalse(StructureModel.VehicleBreakup(8 * StructureModel.G0));
            Assert.IsTrue(StructureModel.VehicleBreakup(8 * StructureModel.G0 + 1e-6));
        }

        [Test]
        public void GV10_AlLiAtSf1Point1_UnthrottledMaxQExceedsQLim()
        {
            var raw = Preset("stock");
            JsonMut.Set(raw, "safetyFactor", 1.1);
            var guidance = new JsonObject();
            guidance.Add("maxQThrottle", 100d);
            JsonMut.Set(raw, "guidance", guidance);
            var design = Design.FromJson(raw);
            Assert.AreEqual("al-li", design.stages[0].material);
            Assert.AreEqual(1.1, design.safetyFactor, 0);
            Assert.AreEqual(100, design.guidance.maxQThrottle, 0);
            Assert.IsNull(design.stages[0].tank, "GV10 is a safety-factor case, not a hoop-wall case");

            var stage = StructureModel.Allowables(Golden.Catalog, design.stages[0], design.safetyFactor);
            Assert.AreEqual(27500, stage.qLimPa, 1e-4);
            double fairing = StructureModel.FairingQLimPa(Golden.Catalog, design);
            Assert.AreNotEqual(stage.qLimPa, fairing, "the fairing uses its own material");

            // The unthrottled peak of the uncut physics. Since #314 the played flight breaks up first (base heating at
            // T+7.2 s), so the designer's reference stops before max-Q; this case is about the structure model.
            var input = Kestrel.Core.Designer.ReferenceAscent.Inputs(Golden.Catalog, raw);
            if (input.opts.monitor != null) input.opts.monitor.Predict = true;
            var sim = Kestrel.Core.Physics.Headless.Simulate(input.stages, input.opts);
            Assert.IsTrue(StructureModel.ExceedsQLim(sim.maxQ, stage.qLimPa),
                "q " + sim.maxQ.ToString("R") + " Pa vs q_lim " + stage.qLimPa.ToString("R") + " Pa");
            Assert.AreEqual("structural-maxq", StructureModel.MaxQFailureKind);
        }

        [Test]
        public void Materials_HaveElasticModulusAndCryoFlag()
        {
            var doc = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "materials.json")));
            var rows = doc.Arr("materials");
            Assert.GreaterOrEqual(rows.Count, 6);
            foreach (JsonObject m in rows)
            {
                string id = m.Str("id");
                Assert.IsTrue(m.ContainsKey("cryoOK"), id);
                Assert.IsTrue(m.ContainsKey("elasticGPa"), id);
                Assert.AreEqual(TankModel.ElasticGPa(id), m.Num("elasticGPa"), 0, id);
                Assert.Greater(m.Num("elasticGPa"), 0, id);
                Assert.AreEqual(m.Bool("cryoOK"), Golden.Catalog.MaterialById[id].cryoOK, id);
            }
            Assert.IsTrue(Golden.Catalog.MaterialById["al-li"].cryoOK);
            Assert.IsFalse(Golden.Catalog.MaterialById["composite"].cryoOK);
            Assert.IsFalse(Golden.Catalog.MaterialById["glass-phenolic"].cryoOK);

            var limits = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "designer-limits.json")));
            var tank = limits.Obj("v3").Obj("tank");
            Assert.AreEqual(tank.Obj("insulationBoiloff").Num("foam"), TankModel.FoamBoiloff, 0);
            Assert.AreEqual(tank.Obj("insulationBoiloff").Num("mli"), TankModel.MliBoiloff, 0);
            Assert.AreEqual(tank.Obj("insulationKgPerM2").Num("foam"), TankModel.FoamKgPerM2, 0);
            Assert.AreEqual(tank.Obj("insulationKgPerM2").Num("mli"), TankModel.MliKgPerM2, 0);
            Assert.AreEqual(tank.Num("commonBulkheadMassFactor"), TankModel.CommonBulkheadFactor, 0);
            Assert.AreEqual(tank.Num("commonBulkheadHazardDeltaPBar"), TankModel.CommonBulkheadHazardBar, 0);
            Assert.AreEqual(tank.Obj("pressureBar").Num("min"), TankModel.PressureMinBar, 0);
            Assert.AreEqual(tank.Obj("pressureBar").Num("max"), TankModel.PressureMaxBar, 0);
        }

        [Test]
        public void V2Identity_EveryPresetAndTemplate_KeepsTheFractionShell()
        {
            int n = 0;
            foreach (JsonObject preset in Presets())
            {
                CheckIdentity(preset, preset.Str("id"));
                n++;
            }
            Assert.GreaterOrEqual(n, 4, "data/presets.json");

            foreach (JsonObject row in MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "designer-templates.json"))).Arr("templates"))
                CheckIdentity(row.Obj("design"), row.Str("id"));

            foreach (var path in Directory.GetFiles(Path.Combine(Golden.Root, "web", "js", "data", "templates"), "*.json"))
                CheckIdentity(MiniJson.ParseObject(File.ReadAllText(path)), Path.GetFileNameWithoutExtension(path));
        }

        static void CheckIdentity(JsonObject raw, string id)
        {
            var design = Design.FromJson(raw);
            var cat = Golden.Catalog;
            CheckPart(cat, design.stages, design.safetyFactor, id);
            if (design.strapOns != null) CheckPart(cat, new List<StagePart> { design.strapOns }, design.safetyFactor, id + " strap");
            double liftoff = Mass.Of(cat, design).liftoffMass;
            var copy = Design.FromJson(design.ToJson());
            Assert.AreEqual(liftoff, Mass.Of(cat, copy).liftoffMass, 0, id + " round trip");
        }

        static void CheckPart(Catalog cat, List<StagePart> parts, double sf, string id)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                Assert.IsNull(part.tank, id + " stage " + i + " has no v3 tank fields");
                double legacy = Fraction(cat, part, sf);
                Assert.AreEqual(legacy, Mass.StructuralFractionMass(cat, part, sf), 0, id + " fraction " + i);
                Assert.AreEqual(legacy, Mass.StructureMass(cat, part, sf), 0, id + " structure " + i);
            }
        }

        static double Fraction(Catalog c, StagePart part, double safetyFactor)
        {
            if (!c.PropellantById.TryGetValue(part.propellant ?? "", out var prop) || !c.MaterialById.TryGetValue(part.material ?? "", out var mat)) return 0;
            double sfScale = safetyFactor / 1.4;
            if (prop.storability == "solid") return part.propMass * 0.09 * (mat.structuralFraction / 0.045) * sfScale;
            double volScale = Math.Sqrt(1030 / prop.bulkDensity);
            return part.propMass * mat.structuralFraction * volScale * sfScale;
        }

        static StagePart Stage(double propMass, double diameter = 3.7, double pressure = 0, string insulation = null, bool? bulkhead = null)
        {
            var stage = new JsonObject();
            stage.Add("name", "Tank");
            stage.Add("propellant", "kerolox");
            stage.Add("engine", "gg-kerolox");
            stage.Add("engineCount", 1d);
            stage.Add("propMass", propMass);
            stage.Add("diameter", diameter);
            stage.Add("material", "al-li");
            stage.Add("separationMass", 0d);
            stage.Add("landingPropMass", 0d);
            if (pressure > 0 || insulation != null || bulkhead != null)
            {
                var tank = new JsonObject();
                if (pressure > 0) tank.Add("pressureBar", pressure);
                if (insulation != null) tank.Add("insulation", insulation);
                if (bulkhead != null) tank.Add("commonBulkhead", bulkhead.Value);
                stage.Add("tank", tank);
            }
            var raw = new JsonObject();
            raw.Add("version", 3d);
            raw.Add("stages", new List<object> { stage });
            raw.Add("payload", Obj(("type", "leo-sat"), ("mass", 100d), ("orbit", "leo400")));
            raw.Add("fairing", Obj(("diameter", diameter), ("length", 10d), ("material", "composite"), ("jettisonAltitudeKm", 110d)));
            raw.Add("aero", Obj(("nose", "ogive"), ("fins", false)));
            raw.Add("landing", Obj(("legs", false), ("gridFins", false), ("rcs", false)));
            raw.Add("avionics", Obj(("tier", "standard")));
            raw.Add("safetyFactor", 1.4d);
            return Design.FromJson(raw).stages[0];
        }

        static JsonObject Obj(params (string k, object v)[] pairs)
        {
            var o = new JsonObject();
            foreach (var (k, v) in pairs) o.Add(k, v);
            return o;
        }

        static JsonObject Preset(string id)
        {
            foreach (JsonObject p in Presets()) if (p.Str("id") == id) return JsonMut.CloneObject(p);
            Assert.Fail(id);
            return null;
        }

        static IEnumerable<JsonObject> Presets()
        {
            foreach (JsonObject p in MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "presets.json"))).Arr("presets"))
                yield return p;
        }
    }
}
