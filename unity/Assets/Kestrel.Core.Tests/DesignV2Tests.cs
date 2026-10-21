using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class DesignV2Tests
    {
        static JsonObject Literals => Golden.File("design-v2-literals");
        static JsonObject Spec => Golden.File("design-v2");
        static JsonObject Words => Spec.Obj("wording");

        [Test]
        public void Limits_MatchTheHistoricalThresholds()
        {
            var lim = DesignerLimits.Active;
            Assert.AreEqual(Validate.MIN_TWR, lim.minTwr);
            Assert.AreEqual(Validate.GOOD_TWR, lim.goodTwr);
            Assert.AreEqual(Validate.MIN_MARGIN, lim.minMargin);
            Assert.AreEqual(Validate.PACK, lim.pack);
            Assert.AreEqual(15, lim.tuneLimitPct);
            Assert.AreEqual(45, lim.maxQWarnKPa);
            Assert.AreEqual(0.6, lim.interstageLengthFactor);
            Assert.AreEqual(0.02, lim.interstageDragCd);
            // Ordinal: culture word-sort skips hyphens, so "cubesat=" precedes "cubesat-dispenser".
            var names = Golden.Catalog.Payloads.Select(p => p.id + "=" + p.displayName)
                .OrderBy(s => s, StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "cargo-capsule=Trunk", "crew-capsule=KH-4", "cubesat-dispenser=Cinder Rack", "cubesat=KH-1",
                "geo-relay=Westline", "gto-comsat=KH-3", "kid-box=KH-K", "leo-sat=KH-2",
                "lunar-lander=Lumen Down", "lunar-probe=KH-5", "space-telescope=Meridian", "weather-sat=Plover",
            }, names);
        }

        [Test]
        public void InterstageMass_MatchesExternalLiteral()
        {
            var row = Literals.Obj("interstage");
            double length = InterstageAero.Length(row.Num("dLower"), row.Num("dUpper"));
            double mass = InterstageAero.DerivedMass(Golden.Catalog, row.Str("material"), row.Num("dLower"), row.Num("dUpper"));
            Golden.Near("design-v2-literals", length, row["lengthM"], 0, 0, "length");
            Golden.Near("design-v2-literals", mass, row["massKg"], row.Num("massKg") * 1e-12, 0, "mass");
            var built = VehicleJson("flare");
            var withZero = JsonMut.CloneObject(built);
            foreach (JsonObject s in withZero.Arr("stages")) JsonMut.Set(s, "interstageMass", 0d);
            double derived = Mass.Of(Golden.Catalog, Design.FromJson(built)).liftoffMass;
            double pinned = Mass.Of(Golden.Catalog, Design.FromJson(withZero)).liftoffMass;
            Golden.Near("design-v2-literals", derived - pinned, row["massKg"], row.Num("massKg") * 1e-12, 0, "dry delta");
        }

        [Test]
        public void ConicalTerm_MatchesExternalLiterals()
        {
            foreach (var name in new[] { "flare", "shoulder" })
            {
                var row = Literals.Obj("conical").Obj(name);
                InterstageAero.Conical(row.Num("dAft"), row.Num("dForward"), row.Num("length"), row.Num("dRef"), out double cna, out double x);
                Golden.Near("design-v2-literals", cna, row["cna"], 0, 1e-9, name + " cna");
                Golden.Near("design-v2-literals", x, row["xFromForward"], 0, 1e-9, name + " x");
            }
            Assert.Greater(Literals.Obj("conical").Obj("flare").Num("cna"), 0, "a flare has CNα > 0");
            Assert.Less(Literals.Obj("conical").Obj("shoulder").Num("cna"), 0, "a shoulder has CNα < 0");
            Assert.AreEqual(0.3, Balance.FinCpAftOfRootLe(1.2, 1.2, 0), 1e-12, "square fin is 0.25c aft of the root leading edge");
            Assert.AreEqual(0.9, 1.2 - Balance.FinCpAftOfRootLe(1.2, 1.2, 0), 1e-12, "trailing edge on the base puts the quarter chord at 0.75c");
        }

        [Test]
        public void VehicleCp_MatchesExternalLiterals()
        {
            double abs = Literals.Obj("tolerance").Num("cpAbsM");
            double stab = Literals.Obj("tolerance").Num("stabilityAbs");
            double massRel = Literals.Obj("tolerance").Num("massRel");
            foreach (var name in new[] { "straight", "flare", "shoulder", "fin", "overstable" })
            {
                var row = Literals.Obj("vehicles").Obj(name);
                var bal = BalanceOf(name);
                Golden.Near("design-v2-literals", bal.cg_m, row["cg_m"], 0, abs, name + " cg");
                Golden.Near("design-v2-literals", bal.cp_m, row["cp_m"], 0, abs, name + " cp");
                Golden.Near("design-v2-literals", bal.cpRaw_m, row["cpRaw_m"], 0, abs, name + " cp raw");
                Golden.Near("design-v2-literals", bal.stabilityCal, row["stabilityCal"], 0, stab, name + " cal");
                Golden.Near("design-v2-literals", bal.mass_kg, row["mass_kg"], massRel, 0, name + " mass");
                Golden.Near("design-v2-literals", bal.length_m, row["length_m"], 0, abs, name + " length");
                Assert.AreEqual(row.Bool("cpAheadOfNose"), bal.cpAheadOfNose, name + " ahead");
            }
            var flare = Literals.Obj("vehicles").Obj("flare");
            Assert.Less(flare.Num("cpRaw_m"), flare.Num("cpWithoutInterstage_m"), "a flare moves CP aft");
            var shoulder = Literals.Obj("vehicles").Obj("shoulder");
            Assert.Less(shoulder.Num("stabilityCal"), shoulder.Num("stabilityWithoutInterstage"), "a shoulder lowers stability");
            Assert.IsTrue(shoulder.Bool("cpAheadOfNose"));
            Assert.AreEqual(shoulder.Num("length_m"), shoulder.Num("cp_m"), 1e-9);
            Assert.Greater(shoulder.Num("cpRaw_m"), shoulder.Num("length_m"));
        }

        [Test]
        public void Drag_UsesTheFlatLabel()
        {
            double step = Literals.Obj("drag").Num("flatCd");
            Assert.AreEqual(0.02, step);
            Assert.AreEqual(0, InterstageAero.DragCd(Design.FromJson(VehicleJson("straight"))));
            var shaped = VehicleJson("straight");
            JsonMut.Set(((JsonObject)shaped.Arr("stages")[1]).Obj("interstage"), "shape", "flare");
            Assert.AreEqual(step, InterstageAero.DragCd(Design.FromJson(shaped)));
            JsonMut.Set(((JsonObject)shaped.Arr("stages")[1]).Obj("interstage"), "shape", "shoulder");
            Assert.AreEqual(step, InterstageAero.DragCd(Design.FromJson(shaped)));
            var both = VehicleJson("shoulder");
            JsonMut.Set(((JsonObject)both.Arr("stages")[1]).Obj("interstage"), "shape", "flare");
            var third = JsonMut.CloneObject((JsonObject)both.Arr("stages")[1]);
            JsonMut.Set(third.Obj("interstage"), "shape", "shoulder");
            both.Arr("stages").Add(third);
            Assert.AreEqual(step * 2, InterstageAero.DragCd(Design.FromJson(both)));
        }

        [Test]
        public void Migration_FillsDefaults_AndKeepsMass()
        {
            var row = Spec.Obj("migration");
            var got = DesignMigration.Migrate(row.Obj("input"));
            JsonSame.Equal(got, row["out"], row.Str("name"));
            var missing = Spec.Obj("migrationMissingMass");
            JsonSame.Equal(DesignMigration.Migrate(missing.Obj("input")), missing["out"], missing.Str("name"));
            var src = VehicleJson("flare");
            double derived = Mass.Of(Golden.Catalog, Design.FromJson(src)).liftoffMass;
            var asV1 = JsonMut.CloneObject(src);
            JsonMut.Set(asV1, "version", 1d);
            double bare = Mass.Of(Golden.Catalog, Design.FromJson(asV1)).liftoffMass;
            Golden.Near("design-v2-literals", derived - bare, Literals.Obj("interstage")["massKg"], Literals.Obj("interstage").Num("massKg") * 1e-12, 0, "version 1 drops the derived frustum");
            var filled = DesignMigration.Migrate(JsonMut.CloneObject(asV1));
            foreach (JsonObject s in filled.Arr("stages")) Assert.AreEqual(0, s.Num("interstageMass"));
            double pinned = Mass.Of(Golden.Catalog, Design.FromJson(filled)).liftoffMass;
            Assert.AreEqual(bare, pinned, 1e-9, "migration pins the missing key to 0");

            var cat = Golden.Catalog;
            foreach (JsonObject preset in MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "presets.json"))).Arr("presets"))
            {
                var raw = JsonMut.CloneObject(preset);
                double before = Mass.Of(cat, Design.FromJson(raw)).liftoffMass;
                var migrated = DesignMigration.Migrate(JsonMut.CloneObject(raw));
                double after = Mass.Of(cat, Design.FromJson(migrated)).liftoffMass;
                Assert.AreEqual(before, after, preset.Str("id"));
                Assert.AreEqual(2, (int)migrated.Num("version"), preset.Str("id"));
            }
        }

        [Test]
        public void Stability_FollowsSteeringAndFins()
        {
            var steered = Report(VehicleJson("straight"));
            var note = Find(steered.warnings, "unstable");
            Assert.IsNull(Find(steered.blocks, "unstable"));
            Assert.IsNull(Find(steered.warnings, "overstable"));
            AssertWording(note, "aero", Words.Str("engineSteeredKid"), Words.Str("engineSteeredEngineer"), BalanceOf("straight"));

            var finned = Report(VehicleJson("fin"));
            Assert.IsNull(Find(finned.blocks, "unstable"), "a steered finned rocket is not blocked");
            Assert.IsNull(Find(finned.warnings, "overstable"));
            AssertWording(Find(finned.warnings, "unstable"), "aero", Words.Str("unstableKid"), Words.Str("unstableEngineer"), BalanceOf("fin"));

            var over = Report(VehicleJson("overstable"));
            Assert.IsNull(Find(over.blocks, "unstable"));
            Assert.IsNull(Find(over.warnings, "unstable"));
            AssertWording(Find(over.warnings, "overstable"), "aero", Words.Str("overstableKid"), Words.Str("overstableEngineer"), BalanceOf("overstable"));

            var solid = VehicleJson("straight");
            var s0 = (JsonObject)solid.Arr("stages")[0];
            JsonMut.Set(s0, "propellant", "solid-apcp");
            JsonMut.Set(s0, "engine", "srm-small");
            JsonMut.Set(solid.Obj("aero"), "fins", false);
            var unguided = Report(solid);
            var blocked = Find(unguided.blocks, "unstable");
            Assert.IsNotNull(blocked, "unguided and negative margin blocks");
            Assert.IsNull(Find(unguided.warnings, "unstable"));
            Assert.AreEqual(Words.Str("unstableKid"), blocked.kid);
            StringAssert.Contains("Aim for 1–2 cal.", blocked.msg);

            var v1 = VehicleJson("straight");
            JsonMut.Set(v1, "version", 1d);
            var v1Report = Report(v1);
            Assert.IsNull(Find(v1Report.blocks, "unstable"), "v1 does not take the v2 block");
            var v1Note = Find(v1Report.warnings, "unstable");
            Assert.IsNotNull(v1Note, "v1 still gets the readout engine-steered note");
            StringAssert.Contains("engine-steered", v1Note.msg);
            Assert.IsNull(v1Note.kid, "the v2 kid text stays off until migration");

            var straps = VehicleJson("straight");
            var core = (JsonObject)straps.Arr("stages")[0];
            JsonMut.Set(core, "propellant", "solid-apcp");
            JsonMut.Set(core, "engine", "srm-small");
            JsonMut.Set(straps.Obj("aero"), "fins", false);
            var so = straps.Obj("strapOns");
            JsonMut.Set(so, "count", 2d);
            JsonMut.Set(so, "kind", "liquid");
            JsonMut.Set(so, "propellant", "kerolox");
            JsonMut.Set(so, "engine", "gg-kerolox");
            JsonMut.Set(so, "engineCount", 1d);
            JsonMut.Set(so, "propMass", 5000d);
            JsonMut.Set(so, "crossfeed", false);
            var boosted = Report(straps);
            Assert.IsNotNull(boosted.readout);
            Assert.IsNull(Find(boosted.blocks, "schema"));
            Assert.IsNull(Find(boosted.blocks, "unstable"), "gimballed straps steer a gimbal-less core");

            var dead = JsonMut.CloneObject(straps);
            JsonMut.Set(dead.Obj("strapOns"), "kind", "solid");
            JsonMut.Set(dead.Obj("strapOns"), "propellant", "solid-apcp");
            JsonMut.Set(dead.Obj("strapOns"), "engine", "srm-small");
            JsonMut.Set(dead.Obj("strapOns"), "crossfeed", false);
            Assert.IsNotNull(Find(Report(dead).blocks, "unstable"), "unguided below 0 cal stays a block");
        }

        [Test]
        public void Cluster_Nozzle_Interstage_AndPadEngine()
        {
            var count = VehicleJson("straight");
            var stage = (JsonObject)count.Arr("stages")[0];
            JsonMut.Set(stage, "engineCount", 9d);
            JsonMut.Set(stage, "cluster", O(("pattern", "ring7")));
            var countReport = Report(count);
            AssertIssue(Find(countReport.blocks, "cluster-count"), "engines", Words.Str("clusterCountMsg"), Words.Str("clusterCountKid"), 0);
            Assert.IsNull(Find(countReport.warnings, "cluster-count"));

            var hang = VehicleJson("straight");
            var wide = (JsonObject)hang.Arr("stages")[0];
            JsonMut.Set(wide, "engineCount", 33d);
            JsonMut.Set(wide, "diameter", 6d);
            JsonMut.Set(wide, "cluster", O(("pattern", "r3p10p20")));
            var hangReport = Report(hang);
            Assert.IsNull(Find(hangReport.blocks, "cluster-overhang"));
            AssertIssue(Find(hangReport.warnings, "cluster-overhang"), "engines", Words.Str("clusterOverhangMsg"), Words.Str("clusterOverhangKid"), 0);

            var pad = VehicleJson("straight");
            JsonMut.Set((JsonObject)pad.Arr("stages")[0], "engine", "gg-kerolox-vac");
            var padReport = Report(pad);
            AssertIssue(Find(padReport.blocks, "vac-engine-pad"), "engines", Words.Str("vacEngineMsg"), Words.Str("vacEngineKid"), 0);
            var upper = VehicleJson("straight");
            JsonMut.Set((JsonObject)upper.Arr("stages")[1], "engine", "gg-kerolox-vac");
            Assert.IsNull(Find(Report(upper).blocks, "vac-engine-pad"), "a vacuum bell on an upper stage is allowed");

            var nozzle = VehicleJson("straight");
            JsonMut.Set((JsonObject)nozzle.Arr("stages")[0], "nozzle", "no-such");
            var nozzleReport = Validate.Engineering(Golden.Catalog, nozzle);
            AssertIssue(Find(nozzleReport.blocks, "nozzle-unknown"), "engines", Words.Str("nozzleUnknownMsg"), Words.Str("nozzleUnknownKid"), 0);
            var shown = Experience.Present(nozzleReport, "standard");
            Assert.AreEqual(Words.Str("nozzleUnknownKid"), shown.blocks[0].msg);

            var bad = VehicleJson("straight");
            JsonMut.Set((JsonObject)bad.Arr("stages")[0], "cluster", O(("pattern", "nope")));
            var badReport = Validate.Engineering(Golden.Catalog, bad);
            Assert.AreEqual("schema", badReport.blocks[0].code);
            Assert.AreEqual(Words.Str("unknownPattern"), badReport.blocks[0].msg);
            Assert.IsNull(Find(badReport.blocks, "cluster-count"));

            var joint = VehicleJson("straight");
            JsonMut.Set((JsonObject)joint.Arr("stages")[1], "diameter", 3d);
            var jointReport = Report(joint);
            Assert.IsNull(Find(jointReport.blocks, "interstage-shape"));
            AssertIssue(Find(jointReport.warnings, "interstage-shape"), "stages", Words.Str("interstageMsg"), Words.Str("interstageKid"), 1);
        }

        [Test]
        public void MaxQ_IsWarnOnly_AndSkipsUnguidedSolids()
        {
            var design = VehicleJson("straight");
            NoteReference(design, 46000, 1);
            var report = Report(design);
            Assert.IsNull(Find(report.blocks, "maxq-high"));
            AssertIssue(Find(report.warnings, "maxq-high"), "aero", Words.Str("maxqMsg"), Words.Str("maxqKid"), null);

            var harder = JsonMut.CloneObject(design);
            JsonMut.Set(harder, "name", "Hard q");
            NoteReference(harder, 200000, 1);
            Assert.IsNull(Find(Report(harder).blocks, "maxq-high"), "max-Q never blocks");
            Assert.IsNotNull(Find(Report(harder).warnings, "maxq-high"));

            var solid = VehicleJson("straight");
            JsonMut.Set(solid, "name", "Solid q");
            var s0 = (JsonObject)solid.Arr("stages")[0];
            JsonMut.Set(s0, "propellant", "solid-apcp");
            JsonMut.Set(s0, "engine", "srm-small");
            NoteReference(solid, 80000, 1);
            Assert.IsNull(Find(Report(solid).warnings, "maxq-high"));
            Assert.IsNull(Find(Report(solid).blocks, "maxq-high"));

            var quiet = VehicleJson("straight");
            JsonMut.Set(quiet, "name", "Under q");
            NoteReference(quiet, 44000, 1);
            Assert.IsNull(Find(Report(quiet).warnings, "maxq-high"));
        }

        [Test]
        public void PayloadG_Cryo_Rcs_AndEngineOut()
        {
            var bumpy = VehicleJson("straight");
            JsonMut.Set(bumpy, "name", "Bumpy sat");
            NoteReference(bumpy, 1000, 7.5);
            var sat = Report(bumpy);
            Assert.IsNull(Find(sat.blocks, "payload-g"));
            AssertIssue(Find(sat.warnings, "payload-g"), "payload", Words.Str("payloadWarnMsg"), Words.Str("payloadWarnKid"), null);

            var crew = VehicleJson("straight");
            JsonMut.Set(crew, "name", "Bumpy crew");
            JsonMut.Set(crew.Obj("payload"), "type", "crew-capsule");
            NoteReference(crew, 1000, 5);
            var crewReport = Report(crew);
            Assert.IsNull(Find(crewReport.warnings, "payload-g"));
            AssertIssue(Find(crewReport.blocks, "payload-g"), "payload", Words.Str("payloadBlockMsg"), Words.Str("payloadBlockKid"), null);

            var cryo = VehicleJson("straight");
            foreach (JsonObject s in cryo.Arr("stages"))
            {
                JsonMut.Set(s, "material", "composite");
                JsonMut.Set(s, "propellant", "hydrolox");
            }
            JsonMut.Set((JsonObject)cryo.Arr("stages")[0], "engine", "exp-hydrolox");
            JsonMut.Set((JsonObject)cryo.Arr("stages")[1], "engine", "exp-hydrolox");
            var cryoReport = Report(cryo);
            Assert.IsNull(Find(cryoReport.blocks, "cryo-composite"));
            AssertIssue(Find(cryoReport.warnings, "cryo-composite"), "propellants", Words.Str("cryoMsg"), Words.Str("cryoKid"), null);

            var land = VehicleJson("straight");
            JsonMut.Set((JsonObject)land.Arr("stages")[0], "landingPropMass", 1000d);
            JsonMut.Set(land.Obj("landing"), "rcs", false);
            var landReport = Report(land);
            AssertIssue(Find(landReport.warnings, "rcs-for-landing"), "recovery", Words.Str("rcsMsg"), Words.Str("rcsKid"), null);

            var outage = VehicleJson("straight");
            var booster = (JsonObject)outage.Arr("stages")[0];
            JsonMut.Set(booster, "engineCount", 2d);
            JsonMut.Set(booster, "propMass", 180000d);
            var outReport = Report(outage);
            var issue = Find(outReport.warnings, "engine-out");
            Assert.IsNotNull(issue);
            Assert.IsNull(Find(outReport.blocks, "engine-out"));
            double twr = outReport.readout.liftoffTWR;
            string x = JsFormat.ToFixed(twr * (2 - 1) / 2.0, 2);
            Assert.AreEqual(Words.Str("engineOutEngineer").Replace("{x}", x), issue.msg);
            Assert.AreEqual(Words.Str("engineOutKid"), issue.kid);
            var engineer = Experience.Present(outReport, "engineer");
            Assert.AreEqual(issue.msg, Find(engineer.warnings, "engine-out").msg);
            Assert.IsNull(Find(Experience.Present(outReport, "standard").warnings, "engine-out"));
            Assert.AreEqual(0, Experience.Present(outReport, "simple").warnings.Count);
        }

        [Test]
        public void OrbitalTemplates_StayLaunchable()
        {
            var want = Spec.Arr("launchable").Select(x => (string)x).OrderBy(s => s).ToList();
            var dir = Path.Combine(Golden.Root, "web", "js", "data", "templates");
            var seen = new List<string>();
            foreach (var path in Directory.GetFiles(dir, "*.json").OrderBy(p => p))
            {
                var raw = MiniJson.ParseObject(File.ReadAllText(path));
                string id = raw.Str("id");
                if (raw.Obj("payload").Str("orbit") == "suborbital")
                {
                    CollectionAssert.Contains(Spec.Arr("suborbitalExempt").Select(x => (string)x).ToList(), id);
                    continue;
                }
                seen.Add(id);
                var report = Validate.Engineering(Golden.Catalog, DesignMigration.Migrate(raw));
                var codes = string.Join(", ", report.blocks.Select(b => b.code + ": " + b.msg));
                Assert.AreEqual(0, report.blocks.Count, id + " " + codes);
            }
            CollectionAssert.AreEqual(want, seen.OrderBy(s => s).ToList());
        }

        [Test]
        public void GimballedSolidSounding_AgreesBetweenV1AndV2()
        {
            var v1 = FinlessSounding("srm-large");
            var parsed = Design.FromJson(v1);
            Assert.IsTrue(DesignRules.PadSteered(Golden.Catalog, parsed), "a gimballed solid on the pad steers");
            Assert.IsFalse(DesignRules.UnguidedSolid(Golden.Catalog, parsed), "gimbal 8 is not the solid exemption");
            var bal = Balance.Of(Golden.Catalog, parsed);
            Assert.AreEqual("-1.12", JsFormat.ToFixed(bal.stabilityCal, 2));
            NoteReference(v1, 80000, 1);
            var report = Report(v1);
            CollectionAssert.AreEqual(new[] { "engines-dont-fit" }, report.blocks.Select(b => b.code).ToList(),
                "the 3.7 m motor does not fit the 1.6 m body; stability itself does not block");
            var note = Only(report.warnings, "unstable");
            StringAssert.Contains("engine-steered", note.msg);
            Assert.IsNull(note.kid, "the v2 kid text stays off on a v1 save");
            Assert.IsNotNull(Find(report.warnings, "maxq-high"), "a gimballed solid is not exempt from max-Q");

            var v2 = DesignMigration.Migrate(JsonMut.CloneObject(v1));
            NoteReference(v2, 80000, 1);
            var migrated = Report(v2);
            CollectionAssert.AreEqual(new[] { "engines-dont-fit" }, migrated.blocks.Select(b => b.code).ToList());
            var v2note = Only(migrated.warnings, "unstable");
            AssertWording(v2note, "aero", Words.Str("engineSteeredKid"), Words.Str("engineSteeredEngineer"), Balance.Of(Golden.Catalog, Design.FromJson(v2)));
            Assert.IsNotNull(Find(migrated.warnings, "maxq-high"));

            var mixed = VehicleJson("straight");
            JsonMut.Set(mixed, "version", 1d);
            var booster = (JsonObject)mixed.Arr("stages")[0];
            JsonMut.Set(booster, "propellant", "solid-apcp");
            JsonMut.Set(booster, "engine", "srm-small");
            var mixedDesign = Design.FromJson(mixed);
            Assert.IsTrue(DesignRules.UnguidedSolid(Golden.Catalog, mixedDesign), "a liquid upper does not remove the stage-1 solid exemption");
            Assert.IsFalse(DesignRules.PadSteered(Golden.Catalog, mixedDesign));
            NoteReference(mixed, 80000, 1);
            var mixedReport = Report(mixed);
            Assert.IsNotNull(mixedReport.readout);
            Assert.IsNull(Find(mixedReport.warnings, "maxq-high"));
            Assert.IsNull(Find(mixedReport.blocks, "maxq-high"));
        }

        [Test]
        public void Version1_DoesNotDeriveInterstageMass()
        {
            JsonObject scout = null;
            foreach (JsonObject p in MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "presets.json"))).Arr("presets"))
                if (p.Str("id") == "scout") scout = JsonMut.CloneObject(p);
            Assert.IsNotNull(scout);
            double stored = Mass.Of(Golden.Catalog, Design.FromJson(scout)).liftoffMass;
            foreach (JsonObject s in scout.Arr("stages"))
            {
                s.Remove("interstageMass");
                s.Order.Remove("interstageMass");
            }
            double bare = Mass.Of(Golden.Catalog, Design.FromJson(scout)).liftoffMass;
            Assert.AreEqual(44723.54, bare, 0.01, "absent key on version 1 is 0, matching JS (al-li tanks since #313; was 44129.78 on composite)");
            Assert.AreEqual(200, stored - bare, 0.01, "dropping the keys also drops Scout's stored 200 kg");

            var migrated = DesignMigration.Migrate(JsonMut.CloneObject(scout));
            Assert.AreEqual(2, (int)migrated.Num("version"));
            foreach (JsonObject s in migrated.Arr("stages")) Assert.AreEqual(0, s.Num("interstageMass"));
            double after = Mass.Of(Golden.Catalog, Design.FromJson(migrated)).liftoffMass;
            Assert.AreEqual(bare, after, 1e-6, "migration pins the missing key to 0 and does not derive");

            var native = JsonMut.CloneObject(scout);
            JsonMut.Set(native, "version", 2d);
            double derived = Mass.Of(Golden.Catalog, Design.FromJson(native)).liftoffMass;
            Assert.AreEqual(44845.68, derived, 0.01, "version 2 still derives the frustum when the key is absent (was 44215.28 on composite)");
            Assert.Greater(derived, bare);
        }

        static JsonObject FinlessSounding(string engine)
        {
            var design = Golden.FixtureJson(Golden.SolidSoundingFixture);
            JsonMut.Set(design.Obj("aero"), "fins", false);
            JsonMut.Set((JsonObject)design.Arr("stages")[0], "engine", engine);
            return design;
        }

        static AeroBalance BalanceOf(string name) => Balance.Of(Golden.Catalog, Design.FromJson(VehicleJson(name)));

        static void NoteReference(JsonObject design, double qPa, double maxG)
            => ReferenceAscent.Store(ReferenceAscent.CanonicalKey(Golden.Catalog, design), qPa, 0, maxG);

        static EngineeringReport Report(JsonObject design) => Validate.Engineering(Golden.Catalog, design);

        static void AssertWording(Issue issue, string tab, string kid, string engineer, AeroBalance bal)
        {
            Assert.IsNotNull(issue);
            Assert.AreEqual(tab, issue.tab);
            Assert.AreEqual(kid, issue.kid);
            Assert.AreEqual(engineer.Replace("{cal}", JsFormat.ToFixed(bal.stabilityCal, 2)), issue.msg);
        }

        static void AssertIssue(Issue issue, string tab, string msg, string kid, int? index)
        {
            Assert.IsNotNull(issue, msg);
            Assert.AreEqual(tab, issue.tab, msg);
            Assert.AreEqual(msg, issue.msg);
            Assert.AreEqual(kid, issue.kid);
            Assert.AreEqual(index, issue.index);
        }

        static Issue Find(List<Issue> list, string code)
        {
            if (list == null) return null;
            foreach (var i in list) if (i.code == code) return i;
            return null;
        }

        static Issue Only(List<Issue> list, string code)
        {
            var hits = list.Where(i => i.code == code).ToList();
            Assert.AreEqual(1, hits.Count, code);
            return hits[0];
        }

        static JsonObject VehicleJson(string name)
        {
            var fx = Literals.Obj("fixture");
            var v = Literals.Obj("vehicles").Obj(name);
            double d0 = v.Num("dLower"), d1 = v.Num("dUpper");
            var prop = v.Arr("propMassKg");
            JsonObject Stage(string stageName, double propMass, double diameter, bool upper) 
            {
                var s = O(
                    ("name", stageName), ("propellant", "kerolox"), ("engine", fx.Str("engine")),
                    ("engineCount", fx.Num("engineCount")), ("propMass", propMass), ("diameter", diameter),
                    ("material", fx.Str("material")), ("separationMass", fx.Num("separationMassKg")), ("landingPropMass", 0d),
                    ("cluster", O(("pattern", "auto"))), ("nozzle", "default"));
                if (upper) s.Add("interstage", O(("shape", DesignRules.Agree(d0, d1))));
                return s;
            }
            var aero = O(("nose", fx.Str("nose")), ("fins", v.Bool("fins")));
            if (v.Bool("fins"))
            {
                aero.Add("finCount", (double)fx.Num("finCount"));
                aero.Add("finSpanM", v.Num("finSpanM"));
            }
            return O(
                ("version", 2d), ("name", name),
                ("stages", new List<object>
                {
                    Stage("Booster", (double)prop[0], d0, false),
                    Stage("Upper", (double)prop[1], d1, true),
                }),
                ("strapOns", O(("count", 0d), ("kind", "solid"), ("propellant", "solid-apcp"), ("engine", "srm-small"),
                    ("engineCount", 1d), ("propMass", 0d), ("diameter", 1.6), ("material", "steel"), ("crossfeed", false))),
                ("payload", O(("type", "leo-sat"), ("mass", v.Num("payloadMassKg")), ("orbit", "leo400"))),
                ("fairing", O(("diameter", d1), ("length", v.Num("fairingLengthM")), ("material", fx.Str("fairingMaterial")), ("jettisonAltitudeKm", 110d))),
                ("aero", aero),
                ("landing", O(("legs", false), ("gridFins", false), ("rcs", false))),
                ("avionics", O(("tier", fx.Str("avionics")))),
                ("safetyFactor", fx.Num("safetyFactor")),
                ("livery", O(("primary", "#f2f2f2"), ("accent", "#1a3c6e"), ("name", ""))));
        }

        static JsonObject O(params (string k, object v)[] pairs)
        {
            var o = new JsonObject();
            foreach (var (k, v) in pairs) o.Add(k, v);
            return o;
        }
    }
}
