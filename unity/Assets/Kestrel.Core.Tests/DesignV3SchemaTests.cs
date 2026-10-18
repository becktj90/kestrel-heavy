// Designer v3 schema, the v2→v3 identity migration, and fairing fit (§2.9).
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class DesignV3SchemaTests
    {
        [Test]
        public void RoundTrip_PreservesV3Fields_AndDefaultsMatchV2()
        {
            var raw = MiniJson.ParseObject(@"{
                ""version"": 3,
                ""id"": ""fit-check"",
                ""name"": ""Fit Check"",
                ""tier"": ""engineer"",
                ""stages"": [{
                    ""name"": ""Core"",
                    ""propellant"": ""kerolox"",
                    ""engine"": ""gg-kerolox"",
                    ""engineCount"": 9,
                    ""propMass"": 300000,
                    ""diameter"": 3.7,
                    ""material"": ""al-li"",
                    ""interstageMass"": 1500,
                    ""separationMass"": 50,
                    ""landingPropMass"": 25000,
                    ""engineParams"": { ""pc"": 250, ""expansionRatio"": 40, ""throttleMin"": 0.55, ""gimbalDeg"": 6 },
                    ""mixtureOffsetPct"": -3,
                    ""tank"": { ""pressureBar"": 3, ""insulation"": ""foam"", ""commonBulkhead"": true },
                    ""sepMode"": ""springs"",
                    ""sepImpulse"": 1200
                }, {
                    ""name"": ""Upper"",
                    ""propellant"": ""kerolox"",
                    ""engine"": ""gg-kerolox-vac"",
                    ""engineCount"": 1,
                    ""propMass"": 70000,
                    ""diameter"": 3.7,
                    ""material"": ""al-li"",
                    ""interstageMass"": 0,
                    ""separationMass"": 50,
                    ""landingPropMass"": 0,
                    ""cluster"": { ""pattern"": ""auto"" },
                    ""interstage"": { ""shape"": ""straight"" },
                    ""nozzle"": ""default"",
                    ""tune"": { ""thrustPct"": 2, ""ispPct"": -1 }
                }],
                ""strapOns"": {
                    ""count"": 2, ""kind"": ""liquid"", ""propellant"": ""kerolox"", ""engine"": ""gg-kerolox"",
                    ""engineCount"": 1, ""propMass"": 40000, ""diameter"": 1.8, ""material"": ""al-li"", ""crossfeed"": true
                },
                ""payload"": { ""type"": ""leo-sat"", ""mass"": 8000, ""orbit"": ""leo400"" },
                ""fairing"": { ""diameter"": 5.2, ""length"": 13, ""material"": ""composite"", ""jettisonAltitudeKm"": 110 },
                ""aero"": { ""nose"": ""ogive"", ""fins"": true, ""finCount"": 4, ""finSpanM"": 1.2 },
                ""fins"": { ""planform"": ""clipped-delta"", ""rootChord"": 0.8 },
                ""landing"": { ""legs"": true, ""gridFins"": true, ""rcs"": false },
                ""legs"": { ""kind"": ""heavy"" },
                ""gridFinDeployAltM"": 4000,
                ""recovery"": { ""mode"": ""rtls"" },
                ""landingReserveFraction"": 0.08,
                ""boosterSepPct"": 85,
                ""guidance"": { ""pitchKickAlt"": 200, ""pitchKickDeg"": 5, ""turnShape"": 0.4, ""maxQThrottle"": 70, ""gLimit"": 4.5 },
                ""heatShield"": true,
                ""missionId"": ""come-back"",
                ""avionics"": { ""tier"": ""redundant"" },
                ""safetyFactor"": 1.4,
                ""livery"": { ""primary"": ""#f2f2f2"", ""accent"": ""#1a3c6e"", ""name"": ""Round"" }
            }");

            var once = Design.FromJson(raw);
            AssertFields(once);
            var again = Design.FromJson(once.ToJson());
            AssertFields(again);
            JsonSame.Equal(again.ToJson(), once.ToJson(), "stable");

            var bare = Design.FromJson(Preset("stock"));
            Assert.AreEqual(0, bare.stages[0].MixtureOffsetPctOrZero);
            Assert.IsNull(bare.stages[0].engineParams);
            Assert.IsNull(bare.stages[0].tank);
            Assert.IsNull(bare.stages[0].sepMode);
            Assert.AreEqual(0, bare.stages[0].SepImpulseOrZero);
            Assert.IsNull(bare.finPlanform);
            Assert.IsFalse(bare.finRootChordSet);
            Assert.IsNull(bare.legsKind);
            Assert.IsNull(bare.recoveryMode);
            Assert.IsFalse(bare.landingReserveSet);
            Assert.IsFalse(bare.boosterSepSet);
            Assert.IsNull(bare.guidance);
            Assert.IsFalse(bare.heatShield);
            Assert.IsNull(bare.missionId);
            var written = bare.ToJson();
            foreach (var key in new[] { "fins", "legs", "recovery", "guidance", "heatShield", "missionId", "boosterSepPct", "gridFinDeployAltM", "landingReserveFraction" })
                Assert.IsFalse(written.ContainsKey(key), key);
            Assert.IsFalse(((JsonObject)written.Arr("stages")[0]).ContainsKey("engineParams"));
            Assert.IsFalse(((JsonObject)written.Arr("stages")[0]).ContainsKey("tank"));
        }

        static void AssertFields(Design d)
        {
            Assert.AreEqual(3, d.version);
            Assert.AreEqual("fit-check", d.id);
            var core = d.stages[0];
            Assert.AreEqual("gg-kerolox", core.engine);
            Assert.AreEqual(250, core.engineParams.pc, 0);
            Assert.AreEqual(40, core.engineParams.expansionRatio, 0);
            Assert.AreEqual(0.55, core.engineParams.throttleMin, 0);
            Assert.AreEqual(6, core.engineParams.gimbalDeg, 0);
            Assert.AreEqual(-3, core.mixtureOffsetPct, 0);
            Assert.AreEqual(3, core.tank.pressureBar, 0);
            Assert.AreEqual("foam", core.tank.InsulationOrNone);
            Assert.IsTrue(core.tank.commonBulkhead);
            Assert.AreEqual("springs", core.sepMode);
            Assert.AreEqual(1200, core.sepImpulse, 0);
            Assert.IsNull(core.tune);
            var upper = d.stages[1];
            Assert.AreEqual("auto", upper.clusterPattern);
            Assert.AreEqual("straight", upper.interstageShape);
            Assert.AreEqual("default", upper.nozzle);
            Assert.AreEqual(2, upper.tune.thrustPct, 0);
            Assert.AreEqual(-1, upper.tune.ispPct, 0);
            Assert.AreEqual(0, upper.MixtureOffsetPctOrZero);
            Assert.AreEqual(2, d.strapOns.count);
            Assert.IsTrue(d.strapOns.crossfeed);
            Assert.AreEqual("clipped-delta", d.finPlanform);
            Assert.AreEqual(0.8, d.finRootChord, 0);
            Assert.AreEqual(4, d.finCount);
            Assert.AreEqual(1.2, d.finSpanM, 0);
            Assert.AreEqual("heavy", d.legsKind);
            Assert.IsTrue(d.legs);
            Assert.AreEqual(4000, d.gridFinDeployAltM, 0);
            Assert.AreEqual("rtls", d.recoveryMode);
            Assert.AreEqual(0.08, d.landingReserveFraction, 0);
            Assert.AreEqual(85, d.boosterSepPct, 0);
            Assert.AreEqual(200, d.guidance.pitchKickAlt, 0);
            Assert.AreEqual(5, d.guidance.pitchKickDeg, 0);
            Assert.AreEqual(0.4, d.guidance.turnShape, 0);
            Assert.AreEqual(70, d.guidance.maxQThrottle, 0);
            Assert.AreEqual(4.5, d.guidance.gLimit, 0);
            Assert.IsTrue(d.heatShield);
            Assert.AreEqual("come-back", d.missionId);
            Assert.AreEqual("redundant", d.avionicsTier);
        }

        [Test]
        public void EngineObject_ReadsIdAndParams_ToJsonKeepsEngineAString()
        {
            var raw = Minimal();
            var stage = (JsonObject)raw.Arr("stages")[0];
            stage.Remove("engine");
            stage.Order.Remove("engine");
            var eng = new JsonObject();
            eng.Add("id", "gg-kerolox");
            eng.Add("pc", 180d);
            eng.Add("gimbalDeg", 2d);
            stage.Add("engine", eng);
            var d = Design.FromJson(raw);
            Assert.AreEqual("gg-kerolox", d.stages[0].engine);
            Assert.AreEqual(180, d.stages[0].engineParams.pc, 0);
            Assert.AreEqual(2, d.stages[0].engineParams.gimbalDeg, 0);
            Assert.IsFalse(d.stages[0].engineParams.expansionRatioSet);
            var back = (JsonObject)d.ToJson().Arr("stages")[0];
            Assert.AreEqual("gg-kerolox", back.Str("engine"));
            Assert.AreEqual(180, back.Obj("engineParams").Num("pc"), 0);
            Assert.IsFalse(back.Obj("engineParams").ContainsKey("expansionRatio"));
        }

        [Test]
        public void Migration_V2ToV3_IsIdentityOnNumbers_AndDefaultStaysV2()
        {
            foreach (JsonObject preset in Presets())
            {
                var v2 = DesignMigration.Migrate(JsonMut.CloneObject(preset));
                Assert.AreEqual(2, (int)v2.Num("version"), preset.Str("id"));
                var again = DesignMigration.Migrate(JsonMut.CloneObject(v2));
                JsonSame.Equal(again, v2, preset.Str("id") + " default");

                var v3 = DesignMigration.Migrate(JsonMut.CloneObject(v2), 3);
                var expect = JsonMut.CloneObject(v2);
                JsonMut.Set(expect, "version", 3d);
                JsonSame.Equal(v3, expect, preset.Str("id") + " identity");

                var cat = Golden.Catalog;
                double mass2 = Mass.Of(cat, Design.FromJson(v2)).liftoffMass;
                double mass3 = Mass.Of(cat, Design.FromJson(v3)).liftoffMass;
                Assert.AreEqual(mass2, mass3, 0, preset.Str("id") + " mass");
                var r2 = Readout.Quick(cat, v2);
                var r3 = Readout.Quick(cat, v3);
                Assert.AreEqual(r2.totalDeltaV, r3.totalDeltaV, 0, preset.Str("id") + " dv");
                Assert.AreEqual(r2.liftoffMass, r3.liftoffMass, 0, preset.Str("id") + " liftoff");
                Assert.AreEqual(r2.liftoffTWR, r3.liftoffTWR, 0, preset.Str("id") + " twr");
            }

            var stamped = DesignMigration.Migrate(JsonMut.CloneObject(Preset("scout")));
            JsonMut.Set((JsonObject)stamped.Arr("stages")[0], "mixtureOffsetPct", 4d);
            JsonMut.Set((JsonObject)stamped.Arr("stages")[0], "sepImpulse", 50d);
            var kept = DesignMigration.Migrate(JsonMut.CloneObject(stamped), 3);
            Assert.AreEqual(3, (int)kept.Num("version"));
            Assert.AreEqual(4, ((JsonObject)kept.Arr("stages")[0]).Num("mixtureOffsetPct"), 0);
            Assert.AreEqual(50, ((JsonObject)kept.Arr("stages")[0]).Num("sepImpulse"), 0);
            Assert.AreEqual(((JsonObject)stamped.Arr("stages")[0]).Num("propMass"), ((JsonObject)kept.Arr("stages")[0]).Num("propMass"), 0);

            var tooNew = new JsonObject();
            tooNew.Add("version", 4d);
            var ex = Assert.Throws<InvalidOperationException>(() => DesignMigration.Migrate(tooNew, 3));
            StringAssert.Contains("newer than supported 3", ex.Message);
        }

        [Test]
        public void PayloadFit_MatchesTheSpecFormula()
        {
            const double diameter = 5;
            const double length = 10;
            double usable = Math.PI * Math.Pow(0.45 * diameter, 2) * 0.8 * length;
            var design = Design.FromJson(Minimal(diameter, length));

            var exact = PayloadFit.Check(design, new PayloadSpec { id = "edge", volume = usable, diameter = diameter - 0.2 });
            Assert.AreEqual(usable, exact.usableVolume, 0);
            Assert.AreEqual(0, exact.volumeMargin, 1e-9);
            Assert.AreEqual(0, exact.diameterMargin, 1e-12);
            Assert.IsTrue(exact.fits, "volume ≤ usable and diameter + 0.2 m ≤ D_f, including equality");

            var spare = PayloadFit.Check(design, new PayloadSpec { volume = 100, diameter = 4 });
            Assert.AreEqual(usable - 100, spare.volumeMargin, 1e-9);
            Assert.AreEqual(5 - 4 - 0.2, spare.diameterMargin, 0);
            Assert.IsTrue(spare.fits);

            var fat = PayloadFit.Check(design, new PayloadSpec { volume = usable + 0.01, diameter = 1 });
            Assert.Less(fat.volumeMargin, 0);
            Assert.Greater(fat.diameterMargin, 0);
            Assert.IsFalse(fat.fits);

            var wide = PayloadFit.Check(design, new PayloadSpec { volume = 1, diameter = 4.81 });
            Assert.AreEqual(5 - 4.81 - 0.2, wide.diameterMargin, 1e-12);
            Assert.Less(wide.diameterMargin, 0);
            Assert.IsFalse(wide.fits);

            var blocked = PayloadFit.Check(design, new PayloadSpec { volume = usable + 1, diameter = 6 });
            Assert.IsFalse(blocked.fits);
            Assert.Less(blocked.volumeMargin, 0);
            Assert.Less(blocked.diameterMargin, 0);
        }

        [Test]
        public void CatalogPayloads_KeepVolume_AndPublishDiameter()
        {
            var expected = new Dictionary<string, (double volume, double diameter)>
            {
                ["cubesat"] = (0.2, 0.5),
                ["leo-sat"] = (12, 2),
                ["gto-comsat"] = (60, 3.4),
                ["crew-capsule"] = (40, 3.6),
                ["lunar-probe"] = (20, 2.2),
                ["kid-box"] = (2, 1.1),
                ["weather-sat"] = (8, 2),
                ["space-telescope"] = (10, 2.4),
                ["cargo-capsule"] = (35, 3.6),
                ["cubesat-dispenser"] = (1.2, 1),
                ["geo-relay"] = (50, 3.2),
                ["lunar-lander"] = (18, 2.6),
            };
            var doc = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "payloads.json")));
            Assert.AreEqual(expected.Count, doc.Arr("payloads").Count);
            foreach (JsonObject row in doc.Arr("payloads"))
            {
                var want = expected[row.Str("id")];
                Assert.AreEqual(want.volume, row.Num("volume"), 0, row.Str("id"));
                Assert.AreEqual(want.diameter, row.Num("diameter"), 0, row.Str("id"));
                Assert.Greater(row.Num("mass"), 0, row.Str("id"));
            }

            var stock = Design.FromJson(Preset("stock"));
            var leo = PayloadSpec.FromJson(FindPayload(doc, "leo-sat"));
            var fit = PayloadFit.Check(stock, leo);
            Assert.IsTrue(fit.fits, "leo-sat in the stock fairing");
            Assert.Greater(fit.volumeMargin, 0);
            Assert.AreEqual(stock.fairingDiameter - leo.diameter - PayloadFit.DiameterClearanceM, fit.diameterMargin, 0);

            var scout = Design.FromJson(Preset("scout"));
            var cube = PayloadSpec.FromJson(FindPayload(doc, "cubesat"));
            Assert.IsTrue(PayloadFit.Check(scout, cube).fits);
            var comsat = PayloadSpec.FromJson(FindPayload(doc, "gto-comsat"));
            Assert.IsFalse(PayloadFit.Check(scout, comsat).fits, "a comsat does not fit the scout fairing");
        }

        [Test]
        public void MixtureOffset_IspPenaltyMatchesSpec()
        {
            Assert.AreEqual(1, MixtureOffset.IspFactor(0), 0);
            Assert.AreEqual(1 - 0.006, MixtureOffset.IspFactor(1), 0);
            Assert.AreEqual(1 - 0.006, MixtureOffset.IspFactor(-1), 0);
            Assert.AreEqual(1 - 0.06, MixtureOffset.IspFactor(10), 1e-12);
            Assert.AreEqual(1 - 0.06, MixtureOffset.IspFactor(-10), 1e-12);
            Assert.IsFalse(MixtureOffset.InstabilityHazard(0));
            Assert.IsFalse(MixtureOffset.InstabilityHazard(6));
            Assert.IsFalse(MixtureOffset.InstabilityHazard(-6));
            Assert.IsTrue(MixtureOffset.InstabilityHazard(6.01));
            Assert.IsTrue(MixtureOffset.InstabilityHazard(-7));
        }

        [Test]
        public void LimitsAndSchema_PublishTheV3Contract_WithoutMovingHistoricalKeys()
        {
            var limits = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "designer-limits.json")));
            var parsed = DesignerLimits.Parse(limits);
            Assert.AreEqual(0.3, parsed.landingReserveMax, 0);
            CollectionAssert.AreEqual(new[] { 0, 2, 4 }, parsed.strapOnCounts);

            var v3 = limits.Obj("v3");
            Assert.AreEqual(-10, v3.Obj("mixtureOffsetPct").Num("min"), 0);
            Assert.AreEqual(10, v3.Obj("mixtureOffsetPct").Num("max"), 0);
            Assert.AreEqual(6, v3.Obj("mixtureOffsetPct").Num("hazardAbs"), 0);
            Assert.AreEqual(MixtureOffset.IspPenaltyPerPct, v3.Obj("mixtureOffsetPct").Num("ispPenaltyPerPct"), 0);
            Assert.AreEqual(8, v3.Obj("engine").Obj("expansionRatio").Num("min"), 0);
            Assert.AreEqual(120, v3.Obj("engine").Obj("expansionRatio").Num("max"), 0);
            Assert.AreEqual(250, v3.Obj("engine").Obj("expansionRatio").Num("vacuumMax"), 0);
            Assert.AreEqual(1.5, v3.Obj("tank").Obj("pressureBar").Num("min"), 0);
            Assert.AreEqual(6, v3.Obj("tank").Obj("pressureBar").Num("max"), 0);
            Assert.AreEqual(0.4, v3.Obj("tank").Obj("insulationBoiloff").Num("foam"), 0);
            Assert.AreEqual(0.15, v3.Obj("tank").Obj("insulationBoiloff").Num("mli"), 0);
            Assert.AreEqual(PayloadFit.RadiusFactor, v3.Obj("payloadFit").Num("radiusFactor"), 0);
            Assert.AreEqual(PayloadFit.LengthFactor, v3.Obj("payloadFit").Num("lengthFactor"), 0);
            Assert.AreEqual(PayloadFit.DiameterClearanceM, v3.Obj("payloadFit").Num("diameterClearanceM"), 0);
            Assert.AreEqual(0.2, v3.Obj("landingReserveFraction").Num("max"), 0);
            Assert.AreEqual(0, v3.Obj("guidance").Obj("pitchKickAltM").Num("min"), 0);
            Assert.AreEqual(50000, v3.Obj("guidance").Obj("pitchKickAltM").Num("max"), 0);
            Assert.AreEqual(0, parsed.pitchKickAltMin, 0);
            Assert.AreEqual(50000, parsed.pitchKickAltMax, 0);
            CollectionAssert.AreEqual(new[] { "cold", "hot", "springs" }, Strings(v3.Arr("sepMode")));
            CollectionAssert.AreEqual(new[] { "expendable", "rtls", "barge" }, Strings(v3.Obj("recovery").Arr("modes")));
            CollectionAssert.AreEqual(new[] { 0d, 2d, 3d, 4d, 6d, 8d }, v3.Arr("strapOnCounts"));

            string schema = File.ReadAllText(Path.Combine(Golden.Root, "data", "schemas", "design.schema.json"));
            foreach (var token in new[] { "engineParams", "mixtureOffsetPct", "pressureBar", "commonBulkhead", "sepMode", "sepImpulse", "boosterSepPct", "planform", "rootChord", "gridFinDeployAltM", "landingReserveFraction", "pitchKickAlt", "maxQThrottle", "heatShield", "missionId" })
                StringAssert.Contains(token, schema);
            string payloads = File.ReadAllText(Path.Combine(Golden.Root, "data", "schemas", "payloads.schema.json"));
            StringAssert.Contains("diameter", payloads);
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

        static JsonObject FindPayload(JsonObject doc, string id)
        {
            foreach (JsonObject row in doc.Arr("payloads")) if (row.Str("id") == id) return row;
            Assert.Fail(id);
            return null;
        }

        static List<string> Strings(List<object> arr)
        {
            var list = new List<string>();
            foreach (var x in arr) list.Add((string)x);
            return list;
        }

        static JsonObject Minimal(double fairingDiameter = 5, double fairingLength = 10)
        {
            return MiniJson.ParseObject($@"{{
                ""version"": 2,
                ""name"": ""Minimal"",
                ""stages"": [{{
                    ""name"": ""Stage 1"", ""propellant"": ""kerolox"", ""engine"": ""gg-kerolox"",
                    ""engineCount"": 1, ""propMass"": 20000, ""diameter"": 2, ""material"": ""al-li"",
                    ""interstageMass"": 0, ""separationMass"": 50, ""landingPropMass"": 0
                }}],
                ""payload"": {{ ""type"": ""leo-sat"", ""mass"": 100, ""orbit"": ""leo400"" }},
                ""fairing"": {{ ""diameter"": {fairingDiameter.ToString(System.Globalization.CultureInfo.InvariantCulture)}, ""length"": {fairingLength.ToString(System.Globalization.CultureInfo.InvariantCulture)}, ""material"": ""composite"", ""jettisonAltitudeKm"": 110 }},
                ""aero"": {{ ""nose"": ""ogive"", ""fins"": false }},
                ""landing"": {{ ""legs"": false, ""gridFins"": false, ""rcs"": false }},
                ""avionics"": {{ ""tier"": ""standard"" }},
                ""safetyFactor"": 1.4,
                ""livery"": {{ ""primary"": ""#f2f2f2"", ""accent"": ""#1a3c6e"", ""name"": """" }}
            }}");
        }
    }
}
