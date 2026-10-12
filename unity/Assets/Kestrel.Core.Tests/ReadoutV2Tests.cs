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
//         cna1 = (4*4*(s)**2) / 2               # Lf = 0
//         kfb = 1 + 0.5/(s+0.5)
//         xf = (L - cr) + (1/6)*(cr+ct - cr*ct/(cr+ct))
//         add(kfb*cna1, xf)
//     return L - cnx/cna
// # cone, no fins: factor 2/3, nose fraction 0.40
// # ogive + fins: factor 0.466, nose fraction 0.45
// PY
//
// Citations: Barrowman 1967 (USU MAE restatement: cone 0.666 Ln, ogive 0.466 Ln);
// fin CNa and Kfb from that same restatement; stations from RocketMeshBuilder.
using System.Collections.Generic;
using System.IO;
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

            string hash = ReferenceAscent.DesignHash(design);
            var shuffled = JsonMut.CloneObject(design);
            shuffled.Order.Reverse();
            Assert.AreEqual(hash, ReferenceAscent.DesignHash(shuffled));

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
                var input = Stack.SimInputs(cat, Design.FromJson(design));
                var full = Headless.Simulate(input.stages, input.opts);
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
        public void Recompute_MeetsLatencyBudget()
        {
            var result = ReadoutBench.Measure(Golden.Catalog, ReadoutBench.LoadDesigns(Golden.Root));
            Assert.Less(result.quickP95Ms, ReadoutBench.QuickP95BudgetMs, "quick p95 " + result.quickWorst + " " + result.quickP95Ms.ToString("R"));
            Assert.Less(result.validateP95Ms, ReadoutBench.ValidateP95BudgetMs, "validate p95 " + result.validateWorst + " " + result.validateP95Ms.ToString("R"));
            Assert.Less(result.referenceMaxMs, ReadoutBench.ReferenceBudgetMs, "reference " + result.referenceWorst + " " + result.referenceMaxMs.ToString("R"));
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
