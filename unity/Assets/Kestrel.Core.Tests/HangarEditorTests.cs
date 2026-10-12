// Hangar editor ops: add/remove/reorder stages, per-part options, boosters, fairing, payload,
// recovery, look, and a seeded fuzz that no sequence of UI edits produces a malformed design.
using System;
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class HangarEditorTests
    {
        static Data.Catalog C => Golden.Catalog;
        static HangarData H => HangarFixtures.Data;

        static JsonObject Stage(JsonObject d, int i) => (JsonObject)d.Arr("stages")[i];

        [Test]
        public void AddStage_OnTop_IsAVacuumUpperWithAStraightInterstage()
        {
            var d = HangarFixtures.Template("workhorse");
            var e = HangarEditor.AddStage(C, d, 2);
            Assert.IsTrue(e.changed);
            Assert.AreEqual(3, d.Arr("stages").Count);
            var s = Stage(d, 2);
            Assert.AreEqual("kerolox", s.Str("propellant"));
            Assert.AreEqual(0, C.EngineById[s.Str("engine")].thrustSL, "vacuum engine up high");
            Assert.AreEqual(1, s.Num("engineCount"));
            Assert.AreEqual(Math.Round(Stage(d, 1).Num("propMass") * 0.25), s.Num("propMass"));
            Assert.AreEqual("straight", s.Obj("interstage").Str("shape"));
            Assert.AreEqual("Stage 3", s.Str("name"));
            Assert.IsTrue(Validate.Shape(C, d).ok, string.Join("; ", Validate.Shape(C, d).errors));
        }

        [Test]
        public void AddStage_AtBottom_RenumbersAndMovesTheInterstage()
        {
            var d = HangarFixtures.Template("hopper");
            Assert.AreEqual("Hopper", Stage(d, 0).Str("name"));
            HangarEditor.AddStage(C, d, 0);
            Assert.AreEqual(2, d.Arr("stages").Count);
            Assert.IsNull(Stage(d, 0).Obj("interstage"), "stage 1 has no interstage of its own");
            Assert.AreEqual("straight", Stage(d, 1).Obj("interstage").Str("shape"));
            Assert.AreEqual("Stage 1", Stage(d, 0).Str("name"));
            Assert.AreEqual("Hopper", Stage(d, 1).Str("name"), "custom names are kept");
        }

        [Test]
        public void AddStage_StopsAtTheLimit_RemoveStopsAtOne()
        {
            var d = HangarFixtures.Template("hopper");
            int max = DesignerLimits.Active.stagesMax;
            for (int i = 0; i < 10; i++) HangarEditor.AddStage(C, d, d.Arr("stages").Count);
            Assert.AreEqual(max, d.Arr("stages").Count);
            Assert.IsFalse(HangarEditor.AddStage(C, d, 0).changed);
            for (int i = 0; i < 10; i++) HangarEditor.RemoveStage(d, 0);
            Assert.AreEqual(1, d.Arr("stages").Count);
            Assert.IsFalse(HangarEditor.RemoveStage(d, 0).changed);
            Assert.IsNull(Stage(d, 0).Obj("interstage"));
        }

        [Test]
        public void MoveStage_ReordersAndKeepsEachStagesSettings()
        {
            var d = HangarFixtures.Template("moonshot");
            string e0 = Stage(d, 0).Str("engine"), e2 = Stage(d, 2).Str("engine");
            var e = HangarEditor.MoveStage(d, 2, 0);
            Assert.IsTrue(e.changed);
            Assert.AreEqual("position 3", e.from);
            Assert.AreEqual("position 1", e.to);
            Assert.AreEqual(e2, Stage(d, 0).Str("engine"));
            Assert.AreEqual(e0, Stage(d, 1).Str("engine"));
            Assert.IsNull(Stage(d, 0).Obj("interstage"));
            Assert.IsNotNull(Stage(d, 1).Obj("interstage"));
            Assert.IsFalse(HangarEditor.MoveStage(d, 1, 1).changed);
        }

        [Test]
        public void SetEngine_FollowsItsPropellant_AndResetsAnUnknownNozzle()
        {
            var d = HangarFixtures.Template("workhorse");
            var p = PartRef.Stage(0);
            HangarEditor.SetNozzle(C, d, p, "extended");
            Assert.AreEqual("extended", Stage(d, 0).Str("nozzle"));
            var e = HangarEditor.SetEngine(C, d, p, "ff-methalox");
            Assert.IsTrue(e.changed);
            Assert.AreEqual("methalox", Stage(d, 0).Str("propellant"));
            Assert.AreEqual("extended", Stage(d, 0).Str("nozzle"), "ff-methalox has an extended bell too");
            HangarEditor.SetEngine(C, d, p, "hybrid-mid");
            Assert.AreEqual("hybrid", Stage(d, 0).Str("propellant"));
            Assert.AreEqual("default", Stage(d, 0).Str("nozzle"));
            Assert.IsFalse(HangarEditor.SetEngine(C, d, p, "no-such-engine").changed);
            Assert.IsFalse(HangarEditor.SetEngine(C, d, PartRef.Fairing, "gg-kerolox").changed, "fairings have no engines");
        }

        [Test]
        public void SetPropellant_PicksAnEngineOfTheSameKind()
        {
            var d = HangarFixtures.Template("workhorse");
            HangarEditor.SetPropellant(C, d, PartRef.Stage(1), "hydrolox");
            var up = C.EngineById[Stage(d, 1).Str("engine")];
            Assert.AreEqual("hydrolox", up.propellant);
            Assert.AreEqual(0, up.thrustSL, "the upper stage keeps a vacuum engine");
            HangarEditor.SetPropellant(C, d, PartRef.Stage(0), "methalox");
            Assert.Greater(C.EngineById[Stage(d, 0).Str("engine")].thrustSL, 0, "stage 1 keeps a sea-level engine");
            Assert.IsFalse(HangarEditor.SetPropellant(C, d, PartRef.Stage(0), "unobtainium").changed);
        }

        [Test]
        public void EngineCount_ClampsAndDropsAFixedPattern()
        {
            var d = HangarFixtures.Template("workhorse");
            var p = PartRef.Stage(0);
            Assert.AreEqual("ring7", Stage(d, 0).Obj("cluster").Str("pattern"));
            var e = HangarEditor.SetEngineCount(d, p, 8);
            Assert.AreEqual("7", e.from); Assert.AreEqual("8", e.to);
            Assert.AreEqual("auto", Stage(d, 0).Obj("cluster").Str("pattern"));
            HangarEditor.SetEngineCount(d, p, 99);
            Assert.AreEqual(DesignerLimits.Active.engineCountMax, Stage(d, 0).Num("engineCount"));
            HangarEditor.SetEngineCount(d, p, -3);
            Assert.AreEqual(1, Stage(d, 0).Num("engineCount"));
            HangarEditor.SetClusterPattern(d, p, "r1p8");
            Assert.AreEqual(9, Stage(d, 0).Num("engineCount"), "a fixed pattern sets the count");
            Assert.IsFalse(HangarEditor.SetClusterPattern(d, p, "hexagon").changed);
        }

        [Test]
        public void Nozzles_AreDataDriven()
        {
            var d = HangarFixtures.Template("workhorse");
            CollectionAssert.AreEqual(new[] { "default", "extended" }, HangarEditor.NozzleChoices(C, d, PartRef.Stage(0)));
            Assert.IsFalse(HangarEditor.SetNozzle(C, d, PartRef.Stage(0), "giant").changed);
            var before = DataSheet.Build(C, H, d);
            HangarEditor.SetNozzle(C, d, PartRef.Stage(1), "extended");
            var after = DataSheet.Build(C, H, d);
            Assert.Greater(after.parts[0].ispVac_s, before.parts[0].ispVac_s, "the extended bell gains vacuum Isp");
            var srm = HangarFixtures.Template("hopper");
            HangarEditor.SetEngine(C, srm, PartRef.Stage(0), "srm-small");
            CollectionAssert.AreEqual(new[] { "default" }, HangarEditor.NozzleChoices(C, srm, PartRef.Stage(0)));
        }

        [Test]
        public void Tank_LengthAndDiameterEdits()
        {
            var d = HangarFixtures.Template("workhorse");
            var p = PartRef.Stage(1);
            double kg = Stage(d, 1).Num("propMass");
            double len = HangarEditor.TankLength(C, Stage(d, 1));
            HangarEditor.SetTankLength(C, d, p, len * 2);
            Assert.Greater(Stage(d, 1).Num("propMass"), kg * 2, "doubling the length more than doubles the load (domes are fixed)");
            Assert.AreEqual(len * 2, HangarEditor.TankLength(C, Stage(d, 1)), 0.01);
            double kg2 = Stage(d, 1).Num("propMass");
            var e = HangarEditor.SetDiameter(d, p, 4.26);
            Assert.AreEqual(4.3, Stage(d, 1).Num("diameter"), "0.1 m steps");
            Assert.AreEqual("3.7 m", e.from); Assert.AreEqual("4.3 m", e.to);
            Assert.AreEqual(kg2, Stage(d, 1).Num("propMass"), "width keeps the propellant");
            HangarEditor.SetDiameter(d, p, 99);
            Assert.AreEqual(DesignerLimits.Active.diameterMax, Stage(d, 1).Num("diameter"));
            HangarEditor.SetPropMass(d, p, -5);
            Assert.AreEqual(DesignerLimits.Active.propMassMin, Stage(d, 1).Num("propMass"));
        }

        [Test]
        public void LandingReserve_IsCappedAndFollowsTheLoad()
        {
            var d = HangarFixtures.Template("lander");
            HangarEditor.SetLandingReserve(d, 0, 0.9);
            Assert.AreEqual(Math.Floor(Stage(d, 0).Num("propMass") * 0.3), Stage(d, 0).Num("landingPropMass"));
            HangarEditor.SetPropMass(d, PartRef.Stage(0), 10000);
            Assert.AreEqual(3000, Stage(d, 0).Num("landingPropMass"), "less fuel shrinks the reserve with it");
        }

        [Test]
        public void Interstage_ShapeAndMassOverride()
        {
            var d = HangarFixtures.Template("workhorse");
            Assert.IsFalse(HangarEditor.SetInterstageShape(d, 0, "flare").changed, "stage 1 has no interstage");
            Assert.IsTrue(HangarEditor.SetInterstageShape(d, 1, "flare").changed);
            Assert.AreEqual("flare", Stage(d, 1).Obj("interstage").Str("shape"));
            Assert.IsFalse(HangarEditor.SetInterstageShape(d, 1, "zigzag").changed);
            double derived = DataSheet.Build(C, H, d).dryMass_kg;
            HangarEditor.SetInterstageMass(d, 1, 0);
            Assert.Less(DataSheet.Build(C, H, d).dryMass_kg, derived, "a 0 kg override drops the derived frustum mass");
            var e = HangarEditor.SetInterstageMass(d, 1, null);
            Assert.AreEqual("auto", e.to);
            Assert.AreEqual(derived, DataSheet.Build(C, H, d).dryMass_kg, 1e-9);
        }

        [Test]
        public void Boosters_CountKindAndCrossfeed()
        {
            var d = HangarFixtures.Template("workhorse");
            Assert.IsFalse(HangarEditor.SetBoosterCount(C, d, 3).changed, "0, 2 or 4 only");
            HangarEditor.SetBoosterCount(C, d, 2);
            var so = d.Obj("strapOns");
            Assert.AreEqual(2, so.Num("count"));
            Assert.AreEqual("solid", so.Str("kind"));
            Assert.Greater(so.Num("propMass"), 0);
            Assert.IsTrue(HangarParts.Exists(d, PartRef.Boosters));
            Assert.IsFalse(HangarEditor.SetCrossfeed(d, true).changed, "no crossfeed with solids");
            HangarEditor.SetBoosterKind(C, d, "liquid");
            Assert.AreEqual("kerolox", so.Str("propellant"));
            Assert.AreEqual("gg-kerolox", so.Str("engine"));
            Assert.IsTrue(HangarEditor.SetCrossfeed(d, true).changed);
            HangarEditor.SetEngine(C, d, PartRef.Boosters, "srm-large");
            Assert.AreEqual("solid", so.Str("kind"), "a solid motor makes the set solid");
            Assert.IsFalse(so.Bool("crossfeed"));
            Assert.IsTrue(Validate.Shape(C, d).ok, string.Join("; ", Validate.Shape(C, d).errors));
            HangarEditor.SetBoosterCount(C, d, 0);
            Assert.IsFalse(HangarParts.Exists(d, PartRef.Boosters));
        }

        [Test]
        public void FairingPayloadAndRecovery()
        {
            var d = HangarFixtures.Template("workhorse");
            HangarEditor.SetFairingLength(d, 99);
            Assert.AreEqual(DesignerLimits.Active.fairingLengthMax, d.Obj("fairing").Num("length"));
            HangarEditor.SetDiameter(d, PartRef.Fairing, 5.04);
            Assert.AreEqual(5, d.Obj("fairing").Num("diameter"));
            Assert.IsTrue(HangarEditor.SetMaterial(C, d, PartRef.Fairing, "steel").changed);
            Assert.IsTrue(HangarEditor.SetNose(d, "von-karman").changed);
            Assert.IsFalse(HangarEditor.SetNose(d, "square").changed);
            HangarEditor.SetJettisonAltitude(d, 123);
            Assert.AreEqual(125, d.Obj("fairing").Num("jettisonAltitudeKm"));
            var e = HangarEditor.SetPayloadType(C, d, "lunar-probe");
            Assert.AreEqual("KH-5", e.to);
            Assert.AreEqual(C.PayloadById["lunar-probe"].mass, d.Obj("payload").Num("mass"));
            Assert.AreEqual("tli", d.Obj("payload").Str("orbit"), "a payload brings its home orbit");
            Assert.IsTrue(HangarEditor.SetOrbit(C, d, "gto").changed);
            Assert.IsFalse(HangarEditor.SetOrbit(C, d, "mars").changed);
            HangarEditor.SetPayloadMass(d, 1);
            Assert.AreEqual(DesignerLimits.Active.payloadMassMin, d.Obj("payload").Num("mass"));
            Assert.IsTrue(HangarEditor.SetLegs(d, true).changed);
            Assert.IsFalse(HangarEditor.SetLegs(d, true).changed);
            Assert.IsTrue(HangarEditor.SetGridFins(d, true).changed);
            Assert.IsTrue(HangarEditor.SetRcs(d, true).changed);
            Assert.IsTrue(HangarEditor.SetFins(d, true).changed);
            HangarEditor.SetFinCount(d, 12);
            Assert.AreEqual(DesignerLimits.Active.finCountMax, d.Obj("aero").Num("finCount"));
            HangarEditor.SetFinSpan(d, 1.234);
            Assert.AreEqual(1.2, d.Obj("aero").Num("finSpanM"), 1e-12);
            Assert.IsTrue(HangarEditor.SetAvionics(d, "redundant").changed);
            Assert.IsFalse(HangarEditor.SetAvionics(d, "quantum").changed);
            HangarEditor.SetSafetyFactor(d, 1.53);
            Assert.AreEqual(1.55, d.Num("safetyFactor"), 1e-12);
            Assert.IsTrue(Validate.Shape(C, d).ok, string.Join("; ", Validate.Shape(C, d).errors));
        }

        [Test]
        public void Look_PaintLiveryFinishesDecals()
        {
            var d = HangarFixtures.Template("workhorse");
            Assert.IsFalse(HangarEditor.SetPaint(d, "red", null).changed, "hex only");
            Assert.IsTrue(HangarEditor.SetPaint(d, "#c8102e", null).changed);
            Assert.AreEqual("#C8102E", d.Obj("livery").Str("primary"));
            Assert.IsFalse(d.Obj("look").ContainsKey("livery"), "hand paint clears the livery id");

            Assert.IsTrue(HangarEditor.ApplyLivery(H, HangarProgress.None, d, "night_shift").changed);
            Assert.AreEqual("#14161A", d.Obj("livery").Str("primary"));
            Assert.IsFalse(HangarEditor.ApplyLivery(H, HangarProgress.None, d, "first_light").changed, "earned after the first orbit");
            var flown = new HangarProgress { orbits = 1 };
            Assert.IsTrue(HangarEditor.ApplyLivery(H, flown, d, "first_light").changed);

            Assert.IsTrue(HangarEditor.SetFinish(H, HangarProgress.None, d, "tank", "foam_orange").changed);
            Assert.IsFalse(HangarEditor.SetFinish(H, HangarProgress.None, d, "fairing", "foam_orange").changed, "not a fairing finish");
            Assert.IsFalse(HangarEditor.SetFinish(H, HangarProgress.None, d, "tank", "soot_weathered").changed, "earned by landings");
            Assert.IsTrue(HangarEditor.SetFinish(H, HangarProgress.None, d, "tank", null).changed);
            Assert.IsNull(d.Obj("look").Obj("finishes").Str("tank"));

            Assert.AreEqual(2, HangarEditor.DecalCount(d));
            Assert.IsFalse(HangarEditor.AddDecal(H, HangarProgress.None, d, "decal_star_ring", "slot_serial_aft").changed, "insignia doesn't go in the serial slot");
            Assert.IsFalse(HangarEditor.AddDecal(H, HangarProgress.None, d, "decal_star_ring", "slot_safety_stencils").changed, "auto slot");
            Assert.IsFalse(HangarEditor.AddDecal(H, HangarProgress.None, d, "decal_comet_patch", "slot_mission_patch").changed, "locked");
            Assert.IsFalse(HangarEditor.AddDecal(H, HangarProgress.None, d, "decal_star_ring", "slot_insignia_upper", 2).changed, "scale steps");
            Assert.IsTrue(HangarEditor.AddDecal(H, HangarProgress.None, d, "decal_star_ring", "slot_insignia_upper", 1.5, 90).changed);
            Assert.IsFalse(HangarEditor.AddDecal(H, HangarProgress.None, d, "decal_kestrel_wing", "slot_insignia_upper").changed, "one decal per slot");
            Assert.IsTrue(HangarEditor.SetDecalScale(H, d, 2, 0.5).changed);
            Assert.IsTrue(HangarEditor.SetDecalRotation(H, d, 2, 0).changed);
            Assert.IsTrue(HangarEditor.RemoveDecal(d, 0).changed);
            Assert.AreEqual(2, HangarEditor.DecalCount(d));

            Assert.IsTrue(HangarEditor.SetVehicleName(d, "  Big   Bird \n the  Rocket of Doom ").changed);
            Assert.AreEqual("Big Bird the Rocket", d.Obj("livery").Str("name"), "collapsed and capped at 20");
            Assert.IsTrue(Validate.Shape(C, d).ok);
        }

        [Test]
        public void DecalLimit_IsSixPerVehicle()
        {
            var d = HangarFixtures.Template("hopper");
            var all = new HangarProgress { orbits = 99, landings = 99 };
            all.missions.Add("KH-5");
            int placed = HangarEditor.DecalCount(d);
            foreach (var slot in H.decalSlots)
                foreach (var decal in H.DecalsFor(slot.id))
                    if (HangarEditor.AddDecal(H, all, d, decal.id, slot.id).changed) { placed++; break; }
            Assert.AreEqual(Math.Min(H.maxDecals, placed), HangarEditor.DecalCount(d));
            Assert.LessOrEqual(HangarEditor.DecalCount(d), H.maxDecals);
        }

        [Test]
        public void PartList_ReadsTopToBottom()
        {
            var d = HangarFixtures.Template("heavy");
            var parts = HangarParts.List(d).ConvertAll(p => p.part.ToString());
            CollectionAssert.AreEqual(new[] { "payload", "fairing", "stage:1", "interstage:1", "stage:0", "boosters" }, parts);
            foreach (var s in parts) { Assert.IsTrue(PartRef.TryParse(s, out var p)); Assert.AreEqual(s, p.ToString()); }
            Assert.IsFalse(PartRef.TryParse("interstage:0", out _));
            Assert.IsFalse(PartRef.TryParse("wing", out _));
        }

        /// <summary>Thousands of random UI edits never produce a schema error, NaN or a broken stack.</summary>
        [Test]
        public void Fuzz_RandomEditsKeepTheDesignWellFormed()
        {
            var rng = new Random(20261012);
            string[] engines = new List<string>(C.EngineById.Keys).ToArray();
            string[] props = new List<string>(C.PropellantById.Keys).ToArray();
            string[] mats = new List<string>(C.MaterialById.Keys).ToArray();
            string[] payloads = new List<string>(C.PayloadById.Keys).ToArray();
            string[] orbits = new List<string>(C.Orbits.Keys).ToArray();
            string[] patterns = { "auto", "single", "row2", "row3", "ring7", "r1p8", "r3p9", "r3p10p20", "custom" };
            foreach (var t in H.templates)
            {
                var d = HangarFixtures.Template(t.id);
                for (int step = 0; step < 400; step++)
                {
                    int n = d.Arr("stages").Count;
                    var parts = HangarParts.List(d);
                    var p = parts[rng.Next(parts.Count)].part;
                    var part = p.kind == PartKind.Interstage ? PartRef.Stage(p.index) : p;
                    switch (rng.Next(22))
                    {
                        case 0: HangarEditor.AddStage(C, d, rng.Next(n + 1)); break;
                        case 1: HangarEditor.RemoveStage(d, rng.Next(n)); break;
                        case 2: HangarEditor.MoveStage(d, rng.Next(n), rng.Next(n)); break;
                        case 3: HangarEditor.SetEngine(C, d, part, engines[rng.Next(engines.Length)]); break;
                        case 4: HangarEditor.SetEngineCount(d, part, rng.Next(-2, 45)); break;
                        case 5: HangarEditor.SetPropellant(C, d, part, props[rng.Next(props.Length)]); break;
                        case 6: HangarEditor.SetMaterial(C, d, part, mats[rng.Next(mats.Length)]); break;
                        case 7: HangarEditor.SetDiameter(d, part, rng.NextDouble() * 20 - 1); break;
                        case 8: HangarEditor.SetPropMass(d, part, rng.NextDouble() * 5e6 - 1000); break;
                        case 9: HangarEditor.SetTankLength(C, d, part, rng.NextDouble() * 80); break;
                        case 10: HangarEditor.SetClusterPattern(d, part, patterns[rng.Next(patterns.Length)]); break;
                        case 11: foreach (var nz in HangarEditor.NozzleChoices(C, d, part)) if (rng.Next(2) == 0) HangarEditor.SetNozzle(C, d, part, nz); break;
                        case 12: HangarEditor.SetBoosterCount(C, d, new[] { 0, 2, 4, 3 }[rng.Next(4)]); break;
                        case 13: HangarEditor.SetBoosterKind(C, d, rng.Next(2) == 0 ? "solid" : "liquid"); break;
                        case 14: HangarEditor.SetLandingReserve(d, rng.Next(n), rng.NextDouble()); break;
                        case 15: HangarEditor.SetInterstageShape(d, rng.Next(n), HangarEditor.InterstageShapes[rng.Next(3)]); break;
                        case 16: HangarEditor.SetFairingLength(d, rng.NextDouble() * 50); break;
                        case 17: HangarEditor.SetPayloadType(C, d, payloads[rng.Next(payloads.Length)]); break;
                        case 18: HangarEditor.SetOrbit(C, d, orbits[rng.Next(orbits.Length)]); break;
                        case 19: HangarEditor.SetTune(d, part, rng.Next(-30, 30), rng.Next(-30, 30)); break;
                        case 20: HangarEditor.SetFins(d, rng.Next(2) == 0); HangarEditor.SetFinSpan(d, rng.NextDouble() * 8); break;
                        case 21: HangarEditor.SetSafetyFactor(d, rng.NextDouble() * 4); break;
                    }
                    var shape = Validate.Shape(C, d);
                    Assert.IsTrue(shape.ok, $"{t.id} step {step}: {string.Join("; ", shape.errors)}");
                    var rep = Validate.Engineering(C, d);
                    Assert.IsNull(rep.blocks.Find(b => b.code == "schema" || b.code == "math" || b.code == "nozzle-unknown"), $"{t.id} step {step}");
                    if (rep.readout != null)
                    {
                        Assert.IsFalse(double.IsNaN(rep.readout.totalDeltaV), $"{t.id} step {step} Δv");
                        Assert.IsFalse(double.IsNaN(rep.readout.stabilityCal), $"{t.id} step {step} stability");
                    }
                }
            }
        }
    }
}
