// G1 readout v2. Mass, sea-level Δv, CG and CP are hard-coded literals from the Python
// below, not from Readout.Quick. Max-Q is a literature band plus an identity check that
// the cached ascent is Headless, not a second formula.
//
// python3 - <<'PY'
// import math
// PI, G0 = math.pi, 9.80665
// prop, landing, D, fair_L, payload = 10000.0, 1000.0, 1.0, 2.0, 100.0
// bell = min(4.0, max(0.4, 0.92 * 1.6))
// tank = ((prop / 1030.0) * 1.03) / (PI * (D/2)**2) + 0.33 * D
// y_base = bell
// y_prop0 = y_base + 0.6 * (D/2)
// y_tank1 = y_prop0 + tank
// L = y_tank1 + fair_L
// structure = prop * 0.045          # sqrt(1030/1030)=1, sf/1.4=1
// engines, avionics = 470.0, 60.0
// fair = 2*PI*(D/2)*fair_L*0.85*14
// serial_dry = structure + engines + avionics + landing
// serial_prop = prop - landing
// m0 = payload + fair + serial_prop + serial_dry
// dv = lambda isp: isp * G0 * math.log(m0 / (m0 - serial_prop))
// def cg(nose_frac):
//     m = my = 0.0
//     def add(mass, y):
//         nonlocal m, my
//         m += mass; my += mass * y
//     add(engines, bell/2)
//     add(structure, (y_base + y_tank1)/2)
//     add(avionics, y_tank1)
//     add(prop, (y_prop0 + y_tank1)/2)          # full tanks, reserve included
//     add(fair, y_tank1 + fair_L/2)
//     add(payload, y_tank1 + fair_L*(1-nose_frac)/2)
//     return my/m
// def cp(nose_frac, factor, fins):
//     cna = cnx = 0.0
//     def add(c, x):
//         nonlocal cna, cnx
//         cna += c; cnx += c*x
//     add(2.0, factor * fair_L * nose_frac)     # d_ref = 1
//     if fins:
//         s = cr = ct = 1.0
//         xt = 0.0
//         lf = math.sqrt(s*s + (xt + (ct - cr)/2)**2)   # unswept: lf = span
//         cna1 = (4*4*(s)**2) / (1 + math.sqrt(1 + (2*lf/(cr+ct))**2))
//         kfb = 1 + 0.5/(s+0.5)
//         xf = (L - cr) + (1/6)*(cr+ct - cr*ct/(cr+ct))
//         add(kfb*cna1, xf)
//     return L - cnx/cna
// # cone, no fins: factor 2/3, nose fraction 0.40
// # ogive + fins: factor 0.466, nose fraction 0.45
// #   lf = 1, CP from the base = 3.6411391130660373 m, stability = 4.486260779591577 cal
// PY
//
// Citations: Barrowman 1967 (USU MAE restatement: cone 0.666 Ln, ogive 0.466 Ln);
// fin CNa and Kfb from that same restatement; stations from RocketMeshBuilder.
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class ReadoutV2Tests
    {
        [Test]
        public void HandLiterals_MatchExternalPencil()
        {
            ReferenceAscent.Reset();
            var file = Golden.File("designer-readout");
            double massRel = file.Obj("tolerance").Num("massRel");
            double dvRel = file.Obj("tolerance").Num("deltaVRel");
            double posAbs = file.Obj("tolerance").Num("positionAbs_m");
            double stabAbs = file.Obj("tolerance").Num("stabilityAbs");
            var cat = Golden.Catalog;
            int hands = 0;
            foreach (JsonObject c in file.Arr("cases"))
            {
                if (c.Str("kind") != "hand") continue;
                hands++;
                string name = c.Str("name");
                var design = c.Obj("design");
                var got = Readout.Quick(cat, design);
                var exp = c.Obj("out");
                Assert.IsTrue(got.maxQStale, name + " quick must not run the ascent");
                Assert.IsNull(got.maxQ_kPa, name);
                Assert.IsNull(got.maxQAlt_m, name);
                Near(got.cg_m, exp["cg_m"], 0, posAbs, name + ".cg");
                Near(got.cp_m, exp["cp_m"], 0, posAbs, name + ".cp");
                Near(got.stabilityCal, exp["stabilityCal"], 0, stabAbs, name + ".cal");
                Near(Stack.ReferenceDiameter(Design.FromJson(design)), exp["refDiameter_m"], 0, 0, name + ".dref");
                var stages = exp.Arr("stages");
                Assert.AreEqual(stages.Count, got.stages.Count, name);
                for (int i = 0; i < stages.Count; i++)
                {
                    var e = (JsonObject)stages[i];
                    var a = got.stages[i];
                    Near(a.dryMass_kg, e["dryMass_kg"], massRel, 0, name + ".dry");
                    Near(a.propMass_kg, e["propMass_kg"], massRel, 0, name + ".prop");
                    Near(a.deltaV, e["deltaV_mps"], dvRel, 0, name + ".dv");
                    Assert.IsNotNull(a.deltaVSL_mps, name + " pad phase");
                    Near(a.deltaVSL_mps.Value, e["deltaVSL_mps"], dvRel, 0, name + ".dvsl");
                }
                var bal = Balance.Of(cat, Design.FromJson(design));
                Near(bal.mass_kg, got.liftoffMass, 1e-12, 0, name + ".mass closure");
            }
            Assert.AreEqual(2, hands);
        }

        [Test]
        public void SeaLevelDeltaV_OnlyOnThePadPhase_WhenBoostersAreAttached()
        {
            var design = Preset("stock");
            var got = Readout.Quick(Golden.Catalog, design);
            Assert.GreaterOrEqual(got.stages.Count, 3, "stock serialises to boosters+core, core, upper");
            Assert.AreEqual("strapons+core", got.stages[0].role);
            Assert.IsNotNull(got.stages[0].deltaVSL_mps);
            Assert.Greater(got.stages[0].deltaVSL_mps.Value, 0);
            for (int i = 1; i < got.stages.Count; i++)
                Assert.IsNull(got.stages[i].deltaVSL_mps, got.stages[i].label);
            Assert.Greater(got.stages[0].dryMass_kg, 0);
            Assert.Greater(got.stages[0].propMass_kg, 0);
        }

        [Test]
        public void FinsMoveCpAft_GridFinsDoNot()
        {
            var design = JsonMut.CloneObject(Preset("scout"));
            var aero = design.Obj("aero");
            JsonMut.Set(aero, "fins", false);
            var bare = Readout.Quick(Golden.Catalog, design);
            JsonMut.Set(aero, "fins", true);
            var finned = Readout.Quick(Golden.Catalog, design);
            Assert.AreEqual(bare.cg_m, finned.cg_m, 1e-12);
            Assert.Less(finned.cp_m, bare.cp_m, "fins pull CP toward the base");
            Assert.Greater(finned.stabilityCal, bare.stabilityCal);

            var landing = design.Obj("landing");
            double cp = finned.cp_m;
            JsonMut.Set(landing, "gridFins", !landing.Bool("gridFins"));
            var grid = Readout.Quick(Golden.Catalog, design);
            Assert.AreEqual(cp, grid.cp_m, 1e-12, "grid fins are drag and mass only");
            Assert.AreNotEqual(finned.cg_m, grid.cg_m);
        }

        [Test]
        public void WeakUpper_UsesDesignIndex_WhenBoostersAreAttached()
        {
            var design = MiniJson.ParseObject(@"{
                ""version"": 1, ""name"": ""Booster weak upper"",
                ""stages"": [
                    { ""name"": ""Core"", ""propellant"": ""kerolox"", ""engine"": ""gg-kerolox"", ""engineCount"": 5,
                      ""propMass"": 80000, ""diameter"": 3, ""material"": ""al-li"",
                      ""interstageMass"": 400, ""separationMass"": 50, ""landingPropMass"": 0 },
                    { ""name"": ""Upper"", ""propellant"": ""hydrolox"", ""engine"": ""exp-hydrolox"", ""engineCount"": 1,
                      ""propMass"": 40000, ""diameter"": 3, ""material"": ""al-li"",
                      ""interstageMass"": 0, ""separationMass"": 50, ""landingPropMass"": 0 }
                ],
                ""strapOns"": { ""count"": 2, ""kind"": ""liquid"", ""propellant"": ""kerolox"", ""engine"": ""gg-kerolox"",
                    ""engineCount"": 1, ""propMass"": 20000, ""diameter"": 1.5, ""material"": ""al-li"", ""crossfeed"": false },
                ""payload"": { ""type"": ""leo-sat"", ""mass"": 15000, ""orbit"": ""leo400"" },
                ""fairing"": { ""diameter"": 5.2, ""length"": 13, ""material"": ""composite"" },
                ""aero"": { ""nose"": ""ogive"", ""fins"": false },
                ""landing"": { ""legs"": false, ""gridFins"": false, ""rcs"": false },
                ""avionics"": { ""tier"": ""standard"" },
                ""safetyFactor"": 1.4,
                ""livery"": { ""primary"": ""#fff"" }
            }");
            var report = Validate.Engineering(Golden.Catalog, design);
            Assert.IsTrue(report.ok, string.Join(" | ", report.blocks.ConvertAll(b => b.code + ": " + b.msg)));
            int serial = -1;
            for (int i = 0; i < report.readout.stages.Count; i++)
                if (report.readout.stages[i].designIndex == 1) serial = i;
            Assert.AreEqual(2, serial, "upper stage is the third serial phase when boosters burn first");
            var weak = report.warnings.FindAll(w => w.code == "weak-upper");
            Assert.AreEqual(1, weak.Count, string.Join(" | ", weak.ConvertAll(w => w.index + " " + w.msg)));
            Assert.AreEqual(1, weak[0].index, "rings the upper stage, not serial index " + serial);
            Assert.AreNotEqual(serial, weak[0].index);
        }

        [Test]
        public void ReferenceAscent_IsCachedHeadless_NotAFormula()
        {
            ReferenceAscent.Reset();
            var design = JsonMut.CloneObject(((JsonObject)Golden.File("designer-readout").Arr("cases")[0]).Obj("design"));
            var cold = Readout.Quick(Golden.Catalog, design);
            Assert.IsTrue(cold.maxQStale);

            string hash = ReferenceAscent.DesignHash(Golden.Catalog, design);
            Assert.AreEqual(16, hash.Length, "64-bit hash");
            var shuffled = JsonMut.CloneObject(design);
            shuffled.Order.Reverse();
            Assert.AreEqual(hash, ReferenceAscent.DesignHash(Golden.Catalog, shuffled));
            string version = Golden.Catalog.VersionKey;
            Golden.Catalog.VersionKey = version + ".bump";
            try { Assert.AreNotEqual(hash, ReferenceAscent.DesignHash(Golden.Catalog, design), "catalog version is in the key"); }
            finally { Golden.Catalog.VersionKey = version; }

            var job = new ReferenceJob(Golden.Catalog, design);
            Assert.IsFalse(job.Step(0), "a zero budget still yields before a full ascent");
            Assert.IsFalse(job.Done);
            Assert.IsTrue(Readout.Quick(Golden.Catalog, design).maxQStale, "an unfinished slice does not fill the cache");
            Assert.IsTrue(job.Step(1e9));
            var cached = Readout.Quick(Golden.Catalog, design);
            Assert.IsFalse(cached.maxQStale);
            Assert.IsNotNull(cached.maxQ_kPa);
            Assert.IsNotNull(cached.maxQAlt_m);

            ReferenceAscent.Reset();
            var direct = ReferenceAscent.Simulate(Golden.Catalog, design);
            var filled = Readout.Reference(Golden.Catalog, design);
            Assert.AreEqual(direct.maxQ / 1000.0, filled.maxQ_kPa);
            Assert.AreEqual(direct.maxQAlt, filled.maxQAlt_m);
            Assert.AreEqual(cached.maxQ_kPa, filled.maxQ_kPa);
            Assert.AreEqual(cached.maxQAlt_m, filled.maxQAlt_m);

            Golden.Catalog.VersionKey = version + ".bump";
            try { Assert.IsTrue(Readout.Quick(Golden.Catalog, design).maxQStale, "a catalog version miss does not reuse the stored ascent"); }
            finally { Golden.Catalog.VersionKey = version; }
            Assert.IsFalse(Readout.Quick(Golden.Catalog, design).maxQStale, "the original key still hits");

            JsonMut.Set((JsonObject)design.Arr("stages")[0], "propMass", 11000d);
            Assert.IsTrue(Readout.Quick(Golden.Catalog, design).maxQStale, "a new hash is stale");
        }

        [Test]
        public void Debounce_ResetsOnEachEdit()
        {
            var clock = new ReferenceDebounce();
            clock.NoteEdit("a", 0);
            Assert.IsFalse(clock.Due(299, out _));
            clock.NoteEdit("b", 200);
            Assert.IsFalse(clock.Due(499, out _));
            Assert.IsTrue(clock.Due(500, out string hash));
            Assert.AreEqual("b", hash);
            Assert.IsFalse(clock.Due(800, out _), "taking the due edit consumes it");
            Assert.AreEqual(300, ReferenceDebounce.DebounceMs);
        }

        [Test]
        public void ReferenceMaxQ_MatchesFullHeadlessPeak()
        {
            var cat = Golden.Catalog;
            foreach (var design in ReadoutBench.LoadDesigns(Golden.Root))
            {
                // #313: the designer reads the shared directed flight (the predict schedule's events included), the
                // same flight the batch and the pad fly; not a parity Stack.SimInputs run.
                var full = ReferenceAscent.Flight(cat, Design.FromJson(design)).Run();
                ReferenceAscent.Reset();
                var got = Readout.Reference(cat, design);
                string name = design.Str("id") ?? design.Str("name");
                Assert.IsFalse(got.maxQStale, name);
                Assert.AreEqual(full.maxQ / 1000.0, got.maxQ_kPa.Value, name + " maxQ");
                Assert.AreEqual(full.maxQAlt, got.maxQAlt_m.Value, name + " alt");
            }
        }

        [Test]
        public void MediumKerolox_MaxQ_SitsInTheLiteratureBand()
        {
            JsonObject band = null;
            foreach (JsonObject c in Golden.File("designer-readout").Arr("cases"))
                if (c.Str("kind") == "maxq-band") band = c;
            Assert.IsNotNull(band);
            var design = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, band.Str("template").Replace('/', Path.DirectorySeparatorChar))));
            ReferenceAscent.Reset();
            var got = Readout.Reference(Golden.Catalog, design);
            var q = band.Arr("maxQ_kPa");
            var alt = band.Arr("maxQAlt_m");
            Assert.IsFalse(got.maxQStale);
            Assert.GreaterOrEqual(got.maxQ_kPa.Value, (double)q[0]);
            Assert.LessOrEqual(got.maxQ_kPa.Value, (double)q[1]);
            Assert.GreaterOrEqual(got.maxQAlt_m.Value, (double)alt[0]);
            Assert.LessOrEqual(got.maxQAlt_m.Value, (double)alt[1]);
        }

        [Test]
        public void EveryPreset_CgMassClosesOnLiftoff()
        {
            var cat = Golden.Catalog;
            foreach (var design in ReadoutBench.LoadDesigns(Golden.Root))
            {
                var d = Design.FromJson(design);
                var bal = Balance.Of(cat, d);
                double lift = Mass.Of(cat, d).liftoffMass;
                double err = System.Math.Abs(bal.mass_kg - lift);
                Assert.LessOrEqual(err, 1e-9 * System.Math.Max(1, System.Math.Abs(lift)), design.Str("id"));
            }
        }

        [Test]
        public void OrbitalTemplates_CanLaunch()
        {
            JsonObject rule = null;
            foreach (JsonObject c in Golden.File("designer-readout").Arr("cases"))
                if (c.Str("kind") == "launchable") rule = c;
            Assert.IsNotNull(rule);
            string skip = rule.Str("excludeOrbit");
            var cat = Golden.Catalog;
            int orbital = 0;
            foreach (var design in ReadoutBench.LoadDesigns(Golden.Root))
            {
                if (!(design.Str("id") ?? "").StartsWith("tpl-")) continue;
                bool sub = design.Obj("payload")?.Str("orbit") == skip;
                var report = Validate.Engineering(cat, design);
                if (sub) continue;
                orbital++;
                Assert.IsTrue(report.ok, (design.Str("id") ?? "?") + " " + string.Join(" | ", report.blocks.ConvertAll(b => b.code + ": " + b.msg)));
                Assert.IsNull(report.blocks.Find(b => b.code == "unstable" || b.code == "maxq-high" || b.code == "overstable"));
                var steered = report.warnings.Find(w => w.code == "unstable");
                Assert.IsNotNull(steered, design.Str("id") + " is finless and unstable at liftoff");
                StringAssert.Contains("engine-steered", steered.msg);
            }
            Assert.GreaterOrEqual(orbital, 5);
        }

        [Test]
        public void StabilityBand_IsFinnedOrUnguided_AndMaxQHighDoesNotBlock()
        {
            var cat = Golden.Catalog;
            var pencil = JsonMut.CloneObject(((JsonObject)Golden.File("designer-readout").Arr("cases")[1]).Obj("design"));
            var finned = Validate.Engineering(cat, pencil);
            Assert.IsTrue(finned.ok);
            Assert.IsNotNull(finned.warnings.Find(w => w.code == "overstable"), "4.5 cal finned pencil is overstable, not blocked");
            Assert.IsNull(finned.blocks.Find(b => b.code == "overstable" || b.code == "unstable"));

            var bare = JsonMut.CloneObject(((JsonObject)Golden.File("designer-readout").Arr("cases")[0]).Obj("design"));
            var liquid = Validate.Engineering(cat, bare);
            Assert.IsTrue(liquid.ok);
            var note = liquid.warnings.Find(w => w.code == "unstable");
            Assert.IsNotNull(note);
            StringAssert.Contains("engine-steered", note.msg);
            Assert.IsNull(liquid.warnings.Find(w => w.code == "overstable"), "the 1-2 cal band and overstable do not apply to a finless steered rocket");

            ReferenceAscent.Reset();
            foreach (var id in new[] { "stock", "fixture-maxq-high-kid" })
            {
                var design = id == "stock" ? Preset(id) : JsonMut.CloneObject(Golden.FixtureDesign("maxq-high-kid"));
                var report = Readout.Reference(cat, design);
                Assert.Greater(report.maxQ_kPa.Value, Validate.MaxQWarnKPa, id);
                var eng = Validate.Engineering(cat, design);
                Assert.IsTrue(eng.ok, id);
                Assert.IsNotNull(eng.warnings.Find(w => w.code == "maxq-high"), id + " warns");
                Assert.IsNull(eng.blocks.Find(b => b.code == "maxq-high"), id + " does not block");
            }
            // The bare stock vehicle peaks at the documented 73 kPa. The designer readout flies the predict schedule,
            // whose stage-1 fuel imbalance throttles that stage to 97% until separation (#313 ruling 3): 70.7 kPa.
            var stockIn = ReferenceAscent.Inputs(cat, Preset("stock"));
            if (System.Math.Abs(Headless.Simulate(stockIn.stages, stockIn.opts).maxQ / 1000 - 73.01) > 0.05)
                Assert.Fail("stock max-Q left the documented 73 kPa");
            Assert.AreEqual(70.71, Readout.Reference(cat, Preset("stock")).maxQ_kPa.Value, 0.05, "stock readout under the predicted fuel imbalance");
            if (System.Math.Abs(Readout.Reference(cat, Golden.FixtureDesign("maxq-high-kid")).maxQ_kPa.Value - 90.07) > 0.05)
                Assert.Fail("the maxq-high kid fixture left the documented 90 kPa");

            // #314: the shipped sounding template is two small hybrids on a long, low-thrust burn. It peaks at
            // 36.2 kPa, under the warning line, so there is nothing to warn about.
            var sounding = Template("tpl-sounding-hybrid");
            var q = Readout.Reference(cat, sounding);
            Assert.AreEqual(36.21, q.maxQ_kPa.Value, 0.05, "hybrid sounding rocket max-Q");
            Assert.Less(q.maxQ_kPa.Value, Validate.MaxQWarnKPa);
            var soundingReport = Validate.Engineering(cat, sounding);
            Assert.IsTrue(soundingReport.ok);
            Assert.IsNull(soundingReport.warnings.Find(w => w.code == "maxq-high"));
            Assert.IsNull(soundingReport.blocks.Find(b => b.code == "maxq-high"));

            // The unguided-solid exemption stays covered by the solid fixture. Under the played checks that solid breaks
            // up over 20 g at T+0.6 s, so its own prediction never reaches max-Q; its uncut full flight peaks near 660 kPa.
            // Note that full-flight peak as its reference: far over the warning line, and still no maxq-high issue.
            var solid = Golden.FixtureJson(Golden.SolidSoundingFixture);
            var solidInput = Stack.SimInputs(cat, Design.FromJson(solid));
            var solidFull = Headless.Simulate(solidInput.stages, solidInput.opts);
            Assert.AreEqual(659.79, solidFull.maxQ / 1000.0, 0.05, "the solid outlier, about 660 kPa uncut");
            ReferenceAscent.Reset();
            ReferenceAscent.Store(ReferenceAscent.CanonicalKey(cat, solid), solidFull);
            var solidReport = Validate.Engineering(cat, solid);
            ReferenceAscent.Reset();
            Assert.IsTrue(solidReport.ok);
            Assert.IsNull(solidReport.warnings.Find(w => w.code == "maxq-high"), "unguided solid is exempt");
            Assert.IsNull(solidReport.blocks.Find(b => b.code == "maxq-high"));
        }

        [Test]
        public void DescentPeak_IsNotDroppedByAClimbSeal()
        {
            // A climb-only seal (climbing, and escape-speed q already under the recorded peak)
            // trips high up. The true peak is on the way back down, still under thrust.
            // The reference ascent does not stop early, so it matches Headless.Simulate.
            var design = MiniJson.ParseObject(@"{
                ""version"": 1, ""name"": ""Descent peak"", ""id"": ""descent-peak"",
                ""stages"": [
                    { ""name"": ""Booster"", ""propellant"": ""kerolox"", ""engine"": ""gg-kerolox"", ""engineCount"": 1,
                      ""propMass"": 7000, ""diameter"": 1.5, ""material"": ""al-li"",
                      ""interstageMass"": 0, ""separationMass"": 0, ""landingPropMass"": 0 },
                    { ""name"": ""Upper"", ""propellant"": ""mmh-nto"", ""engine"": ""pf-hypergolic"", ""engineCount"": 1,
                      ""propMass"": 6500, ""diameter"": 1.0, ""material"": ""al-li"",
                      ""interstageMass"": 0, ""separationMass"": 0, ""landingPropMass"": 0 }
                ],
                ""payload"": { ""type"": ""kid-box"", ""mass"": 10, ""orbit"": ""suborbital"" },
                ""fairing"": { ""diameter"": 1.0, ""length"": 2, ""material"": ""composite"" },
                ""aero"": { ""nose"": ""ogive"", ""fins"": true },
                ""landing"": { ""legs"": false, ""gridFins"": false, ""rcs"": false },
                ""avionics"": { ""tier"": ""basic"" }, ""safetyFactor"": 1.4
            }");
            var cat = Golden.Catalog;
            var input = Stack.SimInputs(cat, Design.FromJson(design));
            input.opts.kickAngleDeg = 12;
            input.opts.cutoff = false;
            input.opts.targetAlt = 0;
            var sim = new AscentSim(input.stages, input.opts);
            double peak = 0, sealQ = 0, sealAlt = 0, apo = 0;
            bool latched = false;
            while (sim.Tick())
            {
                double r = System.Math.Sqrt(sim.Y[0] * sim.Y[0] + sim.Y[1] * sim.Y[1]);
                double vr = (sim.Y[2] * sim.Y[0] + sim.Y[3] * sim.Y[1]) / r;
                double alt = r - Gravity.R_EARTH;
                if (alt > apo) apo = alt;
                if (sim.LastQ > peak) peak = sim.LastQ;
                double rho = alt < 300e3 ? Atmosphere.At(alt).rho : 0;
                bool boundBelow = peak > 0 && 0.5 * rho * (2 * Gravity.MU_EARTH / r) < peak;
                if (!latched && vr > 0 && boundBelow)
                {
                    latched = true;
                    sealQ = peak;
                    sealAlt = alt;
                }
            }
            var full = sim.Finish();
            Assert.IsTrue(latched, "a climb-only seal would have tripped");
            Assert.Greater(sealAlt, 30000, "the seal is high, not at the peak");
            Assert.Greater(full.maxQ, sealQ + 50000, "the descent peak exceeds the sealed climb value");
            Assert.Less(full.maxQAlt, 15000);
            Assert.Less(full.maxQAlt, apo - 20000);
            var flown = ReferenceAscent.Fly(input.stages, input.opts);
            Assert.AreEqual(full.maxQ, flown.maxQ);
            Assert.AreEqual(full.maxQAlt, flown.maxQAlt);
        }

        [Test]
        public void UnguidedFirstStage_UnderAGimballedUpper_Blocks()
        {
            var design = MiniJson.ParseObject(@"{
                ""version"": 1, ""name"": ""Unguided core"",
                ""stages"": [
                    { ""name"": ""Booster"", ""propellant"": ""solid-apcp"", ""engine"": ""srm-small"", ""engineCount"": 1,
                      ""propMass"": 8000, ""diameter"": 1.2, ""material"": ""al-li"",
                      ""interstageMass"": 0, ""separationMass"": 0, ""landingPropMass"": 0 },
                    { ""name"": ""Upper"", ""propellant"": ""kerolox"", ""engine"": ""gg-kerolox"", ""engineCount"": 1,
                      ""propMass"": 2000, ""diameter"": 1.2, ""material"": ""al-li"",
                      ""interstageMass"": 0, ""separationMass"": 0, ""landingPropMass"": 0 }
                ],
                ""payload"": { ""type"": ""kid-box"", ""mass"": 50, ""orbit"": ""suborbital"" },
                ""fairing"": { ""diameter"": 1.2, ""length"": 2, ""material"": ""composite"" },
                ""aero"": { ""nose"": ""ogive"", ""fins"": false },
                ""landing"": { ""legs"": false, ""gridFins"": false, ""rcs"": false },
                ""avionics"": { ""tier"": ""basic"" }, ""safetyFactor"": 1.4,
                ""strapOns"": { ""count"": 0 },
                ""livery"": { ""primary"": ""#fff"" }
            }");
            var cat = Golden.Catalog;
            var report = Validate.Engineering(cat, design);
            Assert.IsNotNull(report.readout, string.Join(" | ", report.blocks.ConvertAll(b => b.code + ": " + b.msg)));
            Assert.Less(report.readout.stabilityCal, 0);
            var block = report.blocks.Find(b => b.code == "unstable");
            Assert.IsNotNull(block, string.Join(" | ", report.warnings.ConvertAll(w => w.code + ": " + w.msg)));
            StringAssert.Contains("unguided rocket below 0 cal", block.msg);
            Assert.IsNull(report.warnings.Find(w => w.code == "unstable"), "a gimballed upper is not pad steering");

            var straps = JsonMut.CloneObject(design);
            JsonMut.Set(straps, "strapOns", MiniJson.ParseObject(
                @"{ ""count"": 2, ""kind"": ""liquid"", ""propellant"": ""kerolox"", ""engine"": ""gg-kerolox"",
                    ""engineCount"": 1, ""propMass"": 3000, ""diameter"": 0.8, ""material"": ""al-li"", ""crossfeed"": false }"));
            var steered = Validate.Engineering(cat, straps);
            Assert.IsNull(steered.blocks.Find(b => b.code == "unstable"), "gimballed strap-ons count as pad steering");
            var note = steered.warnings.Find(w => w.code == "unstable");
            Assert.IsNotNull(note);
            StringAssert.Contains("engine-steered", note.msg);
        }

        [Test]
        public void CacheKey_ChangesWhenCatalogDataChangesWithoutAVersionBump()
        {
            string root = Golden.Root;
            string Read(params string[] p) => File.ReadAllText(Path.Combine(root, Path.Combine(p)));
            string prop = Read("data", "propellants.json");
            string eng = Read("data", "engines.json");
            string mat = Read("data", "materials.json");
            string pay = Read("data", "payloads.json");
            string pre = Read("data", "presets.json");
            string econ = Read("web", "js", "economy-data", "constants.json");
            var a = Catalog.FromJson(prop, eng, mat, pay, pre, econ);
            int at = eng.IndexOf("\"id\": \"gg-kerolox\"", System.StringComparison.Ordinal);
            int isp = eng.IndexOf("\"ispVac\": 311", at, System.StringComparison.Ordinal);
            string eng2 = eng.Substring(0, isp) + "\"ispVac\": 312" + eng.Substring(isp + "\"ispVac\": 311".Length);
            var b = Catalog.FromJson(prop, eng2, mat, pay, pre, econ);
            Assert.AreEqual(a.VersionKey, b.VersionKey);
            Assert.AreNotEqual(a.ContentHash, b.ContentHash);
            var design = JsonMut.CloneObject(((JsonObject)Golden.File("designer-readout").Arr("cases")[0]).Obj("design"));
            Assert.AreNotEqual(ReferenceAscent.DesignHash(a, design), ReferenceAscent.DesignHash(b, design));
            ReferenceAscent.Reset();
            ReferenceAscent.Store(ReferenceAscent.CanonicalKey(a, design), 1000, 10, 2);
            Assert.IsTrue(ReferenceAscent.TryGet(ReferenceAscent.CanonicalKey(a, design), out _, out _));
            Assert.IsFalse(ReferenceAscent.TryGet(ReferenceAscent.CanonicalKey(b, design), out _, out _), "same version, different catalog bytes, is a miss");
            ReferenceAscent.Reset();
        }

        static JsonObject Template(string id)
        {
            foreach (var d in ReadoutBench.LoadDesigns(Golden.Root))
                if (d.Str("id") == id) return d;
            Assert.Fail(id); return null;
        }

        [Test]
        public void Recompute_MeetsLatencyBudget()
        {
            var result = ReadoutBench.Measure(Golden.Catalog, ReadoutBench.LoadDesigns(Golden.Root));
            Assert.Less(result.quickP95Ms, ReadoutBench.QuickP95BudgetMs, "quick p95 " + result.quickWorst + " " + result.quickP95Ms.ToString("R"));
            Assert.Less(result.validateP95Ms, ReadoutBench.ValidateP95BudgetMs, "validate p95 " + result.validateWorst + " " + result.validateP95Ms.ToString("R"));
            // Release stays under ReferenceBudgetMs (30). Debug CI of super-heavy was 31.2 ms.
            // 80 ms is the regression ceiling so that Debug noise does not fail the suite.
            Assert.Less(result.referenceMaxMs, 80, "reference " + result.referenceWorst + " " + result.referenceMaxMs.ToString("R")
                + " (Release budget " + ReadoutBench.ReferenceBudgetMs.ToString("R") + " ms)");
        }

        static JsonObject Preset(string id)
        {
            foreach (JsonObject p in MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "presets.json"))).Arr("presets"))
                if (p.Str("id") == id) return p;
            Assert.Fail(id); return null;
        }

        static void Near(double actual, object expected, double rel, double abs, string what)
        {
            double e = Golden.Num(expected);
            double err = System.Math.Abs(actual - e);
            if (!(err <= abs + rel * System.Math.Abs(e)))
                Assert.Fail($"{what}: got {actual:R}, expected {e:R} (err {err:E3})");
        }
    }
}
