// Hangar data sheet: the stats math checked by hand against the mass and rocket-equation rules,
// agreement with Readout.Quick (the golden-tested readout), stability bands, and the budget line.
using System;
using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class HangarDataSheetTests
    {
        const double G0 = 9.80665;
        static HangarData H => HangarFixtures.Data;

        /// <summary>One kerolox stage, numbers picked so every term is easy to recompute.</summary>
        static JsonObject OneStage()
        {
            var d = DesignMigration.DefaultDesign();
            var s = (JsonObject)d.Arr("stages")[0];
            JsonMut.Set(s, "engine", "gg-kerolox"); JsonMut.Set(s, "engineCount", 3d);
            JsonMut.Set(s, "propMass", 100000d); JsonMut.Set(s, "diameter", 3d);
            JsonMut.Set(d.Obj("payload"), "mass", 1000d);
            JsonMut.Set(d.Obj("payload"), "orbit", "suborbital");
            return DesignMigration.Migrate(d);
        }

        [Test]
        public void SingleStage_MatchesHandCalculation()
        {
            var c = Golden.Catalog;
            var d = OneStage();
            var sheet = DataSheet.Build(c, H, d);
            var eng = c.EngineById["gg-kerolox"];
            var prop = c.PropellantById["kerolox"];
            var mat = c.MaterialById["al-li"];
            double structure = 100000 * mat.structuralFraction * Math.Sqrt(1030 / prop.bulkDensity) * (1.4 / 1.4);
            double dry = structure + 3 * eng.mass + 50 /* separation */ + 150 /* standard avionics */;
            double fairing = 2 * Math.PI * (2.0 / 2) * 5 * 0.85 * 14; // composite 2 m x 5 m
            double m0 = dry + 100000 + 1000 + fairing;
            Assert.AreEqual(m0, sheet.liftoffMass_kg, 1e-6, "liftoff mass");
            Assert.AreEqual(fairing, sheet.fairing_kg, 1e-9, "fairing");
            Assert.AreEqual(3 * eng.thrustSL, sheet.liftoffThrust_N, 1e-6, "pad thrust");
            Assert.AreEqual(3 * eng.thrustSL / (m0 * G0), sheet.liftoffTwr, 1e-9, "TWR");
            double dv = eng.ispVac * G0 * Math.Log(m0 / (m0 - 100000));
            Assert.AreEqual(dv, sheet.totalDeltaV_mps, 1e-6, "Δv (vac Isp)");
            Assert.AreEqual(1, sheet.phases.Count);
            Assert.AreEqual(dv, sheet.phases[0].deltaV_mps, 1e-6);
            double dvSl = eng.ispSL * G0 * Math.Log(m0 / (m0 - 100000));
            Assert.AreEqual(dvSl, sheet.phases[0].deltaVSL_mps.Value, 1e-6, "Δv (SL Isp)");
            double tank = 100000 / prop.bulkDensity * 1.03 / (Math.PI * 1.5 * 1.5) + 0.33 * 3;
            Assert.AreEqual(tank, sheet.parts[0].tankLength_m, 1e-9, "tank length");
            Assert.AreEqual(Math.Sqrt(2 * G0 * 100000) + 1000, sheet.requiredDeltaV_mps, 1e-6, "suborbital need");
            Assert.AreEqual((dv - sheet.requiredDeltaV_mps) / sheet.requiredDeltaV_mps, sheet.margin, 1e-12);
        }

        [Test]
        public void Sheet_AgreesWithReadoutQuick_ForEveryPresetAndTemplate()
        {
            var c = Golden.Catalog;
            foreach (var d in HangarFixtures.AllDesigns())
            {
                var r = Readout.Quick(c, d);
                var s = DataSheet.Build(c, H, d);
                string id = d.Str("id") ?? d.Str("name");
                Assert.AreEqual(r.totalDeltaV, s.totalDeltaV_mps, 0, id);
                Assert.AreEqual(r.liftoffTWR, s.liftoffTwr, 0, id);
                Assert.AreEqual(r.liftoffMass, s.liftoffMass_kg, 0, id);
                Assert.AreEqual(r.stabilityCal, s.stabilityCal, 0, id);
                Assert.AreEqual(r.cg_m, s.cg_m, 0, id);
                Assert.AreEqual(r.cp_m, s.cp_m, 0, id);
                Assert.AreEqual(r.cost, s.costMinor, id);
                Assert.AreEqual(r.stages.Count, s.phases.Count, id);
                double sum = 0;
                foreach (var p in s.phases) sum += p.deltaV_mps;
                Assert.AreEqual(s.totalDeltaV_mps, sum, 1e-6 * s.totalDeltaV_mps, id + " Δv per phase sums to total");
                Assert.AreEqual(s.phases[0].startMass_kg, s.liftoffMass_kg, 1e-6, id + " first phase starts at liftoff mass");
                Assert.AreEqual(s.phases[0].thrust_N / (s.liftoffMass_kg * G0), s.liftoffTwr, 1e-9, id + " TWR = F/(m g)");
                int stages = d.Arr("stages").Count;
                int boosters = HangarParts.BoosterCount(d) > 0 ? 1 : 0;
                Assert.AreEqual(stages + boosters, s.parts.Count, id + " one part row per stage and booster set");
            }
        }

        [Test]
        public void Phases_WithBoosters_StartWithCombinedLiftoff()
        {
            var c = Golden.Catalog;
            var d = DesignMigration.Migrate(HangarFixtures.Template("heavy"));
            var s = DataSheet.Build(c, H, d);
            Assert.AreEqual("strapons+core", s.phases[0].role);
            Assert.AreEqual(-1, s.phases[0].stageIndex);
            Assert.AreEqual(0, s.phases[1].stageIndex, "core continues after booster burnout");
            Assert.Greater(s.liftoffThrust_N, s.parts.Find(p => p.part == PartRef.Stage(0)).thrustSL_N, "boosters add pad thrust");
        }

        [Test]
        public void TankLength_InvertsSizeTank()
        {
            var c = Golden.Catalog;
            foreach (var prop in new[] { "kerolox", "methalox", "hydrolox", "solid-apcp" })
            foreach (var dia in new[] { 1.2, 3.7, 9.0 })
            foreach (var kg in new[] { 5000.0, 120000, 2000000 })
            {
                double len = Vehicle.DesignMath.SizeTank(c, prop, kg, dia).length;
                Assert.AreEqual(kg, HangarEditor.PropMassForLength(c, prop, dia, len), kg * 1e-9, $"{prop} {dia} m {kg} kg");
            }
            Assert.AreEqual(0, HangarEditor.PropMassForLength(c, "kerolox", 3, 0.5), "shorter than the domes holds nothing");
        }

        [Test]
        public void StabilityBands_FollowTheDesignRules()
        {
            var c = Golden.Catalog;
            var hopper = DataSheet.Build(c, H, HangarFixtures.Template("hopper"));
            Assert.AreEqual(StabilityBand.Stable, hopper.stability, $"finned hopper {hopper.stabilityCal:F2} cal");
            Assert.AreEqual("Steady", hopper.StabilityWord);
            var workhorse = DataSheet.Build(c, H, HangarFixtures.Template("workhorse"));
            Assert.Less(workhorse.stabilityCal, 0);
            Assert.AreEqual(StabilityBand.EngineSteered, workhorse.stability, "no fins, gimballed engines");

            var lim = DesignerLimits.Active;
            var finned = Vehicle.Design.FromJson(HangarFixtures.Template("hopper"));
            Assert.AreEqual(StabilityBand.Unstable, DataSheet.Band(c, finned, -0.5, lim));
            Assert.AreEqual(StabilityBand.Twitchy, DataSheet.Band(c, finned, 0.5, lim));
            Assert.AreEqual(StabilityBand.Stable, DataSheet.Band(c, finned, 1.5, lim));
            Assert.AreEqual(StabilityBand.Overstable, DataSheet.Band(c, finned, 4.5, lim));
            var bare = Vehicle.Design.FromJson(HangarFixtures.Template("workhorse"));
            Assert.AreEqual(StabilityBand.Stable, DataSheet.Band(c, bare, 1.2, lim));
        }

        [Test]
        public void Budget_UsesDesignThenOrbitThenDefault()
        {
            var b = H.budgets;
            var d = HangarFixtures.Template("workhorse");
            Assert.AreEqual(b.byOrbit["leo400"], b.For(d));
            JsonMut.Set(d.Obj("payload"), "orbit", "nowhere");
            Assert.AreEqual(b.defaultKc, b.For(d));
            JsonMut.Set(d, "budgetKc", 1234d);
            Assert.AreEqual(1234, b.For(d));
        }

        [Test]
        public void OverBudget_IsAWarningWithKidAndEngineerText()
        {
            var c = Golden.Catalog;
            var d = HangarFixtures.Template("workhorse");
            JsonMut.Set(d, "budgetKc", 1000d);
            var session = new HangarSession(c, H, d);
            Assert.IsTrue(session.Sheet.overBudget);
            var a = session.Advisories.Find(x => x.code == "over-budget");
            Assert.IsNotNull(a, "over-budget advisory");
            Assert.AreEqual(AdvisorySeverity.Warn, a.severity);
            Assert.AreEqual("Over budget", a.headline);
            StringAssert.Contains("costs more than your budget", a.kid);
            StringAssert.Contains("over the 1,000 Kc budget", a.engineer);
            Assert.IsTrue(session.LaunchEnabled, "budget never blocks Launch");

            JsonMut.Set(d, "budgetKc", session.Sheet.cost_Kc / 0.95);
            var near = new HangarSession(c, H, d);
            Assert.IsTrue(near.Sheet.nearBudget);
            Assert.AreEqual(AdvisorySeverity.Note, near.Advisories.Find(x => x.code == "near-budget").severity);
        }

        [Test]
        public void TooHeavy_BlocksWithKidWords()
        {
            var c = Golden.Catalog;
            var d = HangarFixtures.Template("workhorse");
            var s = new HangarSession(c, H, d);
            s.Apply(x => HangarEditor.SetEngineCount(x, PartRef.Stage(0), 1));
            Assert.IsFalse(s.LaunchEnabled);
            var a = s.Advisories[0];
            Assert.AreEqual("twr-low", a.code);
            Assert.AreEqual(AdvisorySeverity.Block, a.severity);
            Assert.AreEqual("Too heavy to lift off", a.headline);
            Assert.AreEqual("Too heavy to lift off. Add engines to the bottom stage or use less fuel.", a.kid);
            StringAssert.Contains("thrust-to-weight", a.engineer);
            Assert.AreEqual(PartRef.Stage(0), a.part);
            Assert.Less(s.Sheet.liftoffTwr, 1);
        }

        [Test]
        public void Unstable_FinnedRocket_WarnsInKidWords()
        {
            var c = Golden.Catalog;
            var s = new HangarSession(c, H, HangarFixtures.Template("hopper"));
            s.Apply(x => HangarEditor.SetFinSpan(x, 0.2));
            Assert.Less(s.Sheet.stabilityCal, 1);
            var a = s.Advisories.Find(x => x.code == "unstable");
            Assert.IsNotNull(a);
            Assert.AreEqual("Wobbly", a.headline);
            StringAssert.Contains("flip", a.kid);
            StringAssert.Contains("cal", a.engineer);
        }

        [Test]
        public void Advisories_AreOrderedBlocksWarningsNotes()
        {
            var c = Golden.Catalog;
            var d = HangarFixtures.Template("workhorse");
            JsonMut.Set(d, "budgetKc", 1d);
            var s = new HangarSession(c, H, d);
            s.Apply(x => HangarEditor.SetEngineCount(x, PartRef.Stage(0), 1));
            var sev = s.Advisories.ConvertAll(a => (int)a.severity);
            var sorted = new List<int>(sev); sorted.Sort();
            CollectionAssert.AreEqual(sorted, sev);
            foreach (var a in s.Advisories)
            {
                Assert.IsFalse(string.IsNullOrEmpty(a.kid), a.code);
                Assert.IsFalse(string.IsNullOrEmpty(a.engineer), a.code);
                Assert.IsFalse(string.IsNullOrEmpty(a.headline), a.code);
            }
        }

        [Test]
        public void KidWords_MatchTheU1Table()
        {
            // The Slice U1 wording (DESIGNER_SPEC §6) must not drift from the Hangar table.
            Assert.AreEqual("A stage or booster has no engines. Give it at least one.", KidWords.Table["zero-engines"]);
            Assert.AreEqual("Too many engines for this width. Use fewer or make it wider.", KidWords.Table["engines-dont-fit"]);
            Assert.AreEqual("You'll just make it. A little more fuel gives room to spare.", KidWords.For(new Issue { code = "dv-margin", msg = "Only 3% Δv margin" }));
            Assert.AreEqual("Not quite enough speed for this trip. More fuel or a lighter cargo.", KidWords.For(new Issue { code = "dv-margin", msg = "About 30 m/s short" }));
            Assert.AreEqual("own words", KidWords.For(new Issue { code = "unstable", kid = "own words" }));
            Assert.AreEqual(KidWords.Fallback, KidWords.For(new Issue { code = "never-heard-of-it" }));
        }
    }
}
