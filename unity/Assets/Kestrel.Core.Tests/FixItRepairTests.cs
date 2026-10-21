// F2 (NAV_BUGS_LIVE): "Fix it" did nothing, in the designer's launch row and on the flight end screen.
// The gate's Fix it only switched tabs; the report's Fix it asked the Δv budget, which can say "enough" while the
// flight runs dry; and a miss with no failure event sent an empty kind. These drive DesignerRepair.Fix, the one
// repair every Fix it now runs, on rockets the tests weaken on purpose, and check the result with FlightBatch.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FixItRepairTests
    {
        Catalog catalog;
        Campaign campaign;
        HangarData hangar;

        [SetUp]
        public void Load()
        {
            ReferenceAscent.Reset();
            catalog = Catalog.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
        }

        [TearDown]
        public void Clear() => ReferenceAscent.Reset();

        JsonObject Starter(int index) => JsonMut.CloneObject(HomeAutoBuild.Choose(catalog, hangar.templates, campaign.Mission(index)).Design);

        LaunchGateResult Gate(JsonObject json)
        {
            ReferenceAscent.Reset();
            ReferenceAscent.Run(catalog, json);
            return LaunchGate.Evaluate(Validate.Engineering(catalog, json), Readout.Quick(catalog, json));
        }

        List<CampaignFlight> Fly(int index, JsonObject json)
        {
            var list = new List<CampaignFlight>();
            for (ulong seed = 1; seed <= 5; seed++) list.Add(FlightBatch.Fly(catalog, campaign.Mission(index), Design.FromJson(json), seed));
            return list;
        }

        static void TopStageProp(JsonObject json, double factor)
        {
            var stages = json.Arr("stages");
            var top = (JsonObject)stages[stages.Count - 1];
            JsonMut.Set(top, "propMass", top.Num("propMass") * factor);
        }

        [Test]
        public void GateFixIt_RepairsAWeakAirframe_ThenTheGateIsReady()
        {
            // KH-8's rocket with the walls cut to strength 1.0: it bends in half at max-Q on every seed.
            const int index = 8;
            var json = Starter(index);
            JsonMut.Set(json, "safetyFactor", 1.0);
            var before = Gate(json);
            Assert.That(before.state, Is.EqualTo(LaunchGateState.Blocked));
            Assert.That(before.code, Is.EqualTo("structural-maxq"), before.Line);
            Assert.That(Fly(index, json).TrueForAll(f => f.failure == "structural-maxq"), "the weak rocket really breaks up");
            Assert.That(DesignerRepair.GateRepairs(Validate.Engineering(catalog, json), before), "the gate's Fix it repairs, not just opens a tab");

            var outcome = DesignerRepair.Fix(catalog, json, before.code);
            Assert.That(outcome.Changed, outcome.Note);
            Assert.That(json.Num("safetyFactor"), Is.GreaterThan(1.0), outcome.Note);
            var after = Gate(json);
            Assert.That(after.Ready, after.Line);
            // The failure Fix it named is gone on every seed. (With F1's shared MissionFlightSetup predict, #309, this
            // rocket also flies 5/5; on main the designer's predict still misses the upper stage running dry.)
            foreach (var f in Fly(index, json)) Assert.That(f.failure, Is.Not.EqualTo("structural-maxq"), "seed " + f.seed + " / " + outcome.Note);
        }

        [TestCase(14)]
        [TestCase(16)]
        public void FixIt_WeakAirframe_RepairsUntilEveryGateSeedFlies_GustsIncluded(int index)
        {
            // The starter with the walls cut to strength 1.0 breaks up at max-Q on every seed. The stepper's repair alone
            // read Ready but still broke up on one gusty seed (KH-14 seed 3, KH-16 seed 5). Fixed has to mean fixed:
            // FlightBatch seeds 1-5, the gate's seeds, all fly with no failure, with one strength step of margin.
            var json = Starter(index);
            JsonMut.Set(json, "safetyFactor", 1.0);
            var before = Gate(json);
            Assert.That(before.state, Is.EqualTo(LaunchGateState.Blocked));
            Assert.That(before.code, Is.EqualTo("structural-maxq"), before.Line);
            Assert.That(Fly(index, json).TrueForAll(f => f.failure == "structural-maxq"), "the weak rocket really breaks up");

            var outcome = DesignerRepair.Fix(catalog, json, before.code, campaign.Mission(index));
            Assert.That(outcome.Changed, outcome.Note);
            Assert.That(json.Num("safetyFactor"), Is.GreaterThan(1.0), outcome.Note);
            foreach (var f in Fly(index, json)) Assert.IsNull(f.failure, "KH-" + index + " seed " + f.seed + " / " + outcome.Note);
            Assert.That(outcome.FullyFixed, outcome.Note);
            Assert.That(outcome.Note, Does.Not.Contain("could not"), outcome.Note);
            var after = Gate(json);
            Assert.That(after.Ready, after.Line);
            // Arcade: one step of margin past the first strength that flies seeds 1-5. One step less still flies all 5
            // (so the last step is margin); two steps less breaks up on a seed (so it is exactly one step).
            double step = DesignerLimits.Active.safetyStep;
            double sf = json.Num("safetyFactor");
            var less = JsonMut.CloneObject(json);
            HangarEditor.SetSafetyFactor(less, sf - step);
            foreach (var f in Fly(index, less)) Assert.IsNull(f.failure, "KH-" + index + " one step under " + sf + ", seed " + f.seed);
            HangarEditor.SetSafetyFactor(less, sf - 2 * step);
            Assert.That(Fly(index, less).Exists(f => f.failure != null), "KH-" + index + " two steps under " + sf + " is the knife-edge");
        }

        [Test]
        public void FixIt_ThatCannotFixTheFlight_SaysSo_NeverClaimsSuccess()
        {
            // KH-6's rocket with half the upper-stage load, asked to fix a structural kind. Strength cannot stop it
            // running dry, so after its steps Fix it says it could not fully fix the rocket instead of a success note.
            var json = Starter(6);
            TopStageProp(json, 0.5);
            var outcome = DesignerRepair.Fix(catalog, json, "structural-maxq", campaign.Mission(6));
            var flights = Fly(6, json);
            int failed = flights.FindAll(f => f.failure != null).Count;
            Assert.That(failed, Is.GreaterThan(0), "this rocket still fails a gate seed");
            Assert.IsFalse(outcome.FullyFixed);
            Assert.That(outcome.Note, Is.EqualTo(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                DesignerRepair.NotFullyFixed, failed, DesignerRepair.GateSeeds)));
        }

        [Test]
        public void ReportFixIt_RanOutOfFuel_AddsFuelUntilTheFlightReachesOrbit()
        {
            // KH-6's rocket with half the upper-stage load. The Δv budget still says it has the speed
            // (DvMarginFix alone changes nothing), but the flight runs dry.
            var json = Starter(6);
            TopStageProp(json, 0.5);
            Assert.That(Fly(6, json)[0].failure, Is.EqualTo("prop-depletion"));
            Assert.IsFalse(DvMarginFix.Solve(catalog, json).Changed, "the budget alone sees nothing to add");
            Assert.That(DesignerRepair.FlightShort(catalog, json), "the designer's test ascent runs dry");

            var outcome = DesignerRepair.Fix(catalog, json, DesignerRepair.ReportKind("prop-depletion", "prop-depletion"));
            Assert.That(outcome.Changed, outcome.Note);
            Assert.That(outcome.Note, Is.EqualTo(DesignerRepair.DeltaVAdded));
            Assert.IsFalse(DesignerRepair.FlightShort(catalog, json));
            foreach (var f in Fly(6, json)) Assert.IsNull(f.failure, "seed " + f.seed);
        }

        [Test]
        public void ReportFixIt_AHopThatToppedOutShort_WithNoFailureEvent_AddsSpeed()
        {
            // KH-2's hopper with 70% of its load climbs to about 36 km of 100 km. No failure event: the report is a "miss".
            var json = Starter(2);
            TopStageProp(json, 0.7);
            var miss = Fly(2, json)[0];
            Assert.IsNull(miss.failure);
            Assert.That(miss.apoKm, Is.LessThan(95));
            string kind = DesignerRepair.ReportKind(null, "miss");
            Assert.That(kind, Is.EqualTo("dv-margin"), "a miss repairs the speed; an empty kind changed nothing");

            var outcome = DesignerRepair.Fix(catalog, json, kind);
            Assert.That(outcome.Changed, outcome.Note);
            foreach (var f in Fly(2, json))
            {
                Assert.IsNull(f.failure, "seed " + f.seed);
                Assert.That(f.apoKm, Is.GreaterThanOrEqualTo(95), "seed " + f.seed + " reaches the 100 km band");
            }
        }

        [Test]
        public void FixIt_OnARocketWithNothingToFix_SaysSoAndChangesNothing()
        {
            var json = Starter(8);
            string before = HangarSession.Snapshot(json);
            var outcome = DesignerRepair.Fix(catalog, json, "dv-margin");
            Assert.IsFalse(outcome.Changed);
            Assert.That(outcome.Note, Is.EqualTo(DesignerRepair.DeltaVAlready), "it no longer claims it added propellant");
            Assert.That(HangarSession.Snapshot(json), Is.EqualTo(before));
        }

        [Test]
        public void AValidationBlockOnlyOpensItsTab()
        {
            var rep = new EngineeringReport { ok = true };
            rep.blocks.Add(new Issue { code = "no-engine", kid = "Add an engine.", msg = "no engine", tab = "engines" });
            var gate = LaunchGate.Evaluate(rep, new QuickReadout { margin = 0.2, fairingFits = true });
            Assert.IsFalse(DesignerRepair.GateRepairs(rep, gate));
            var soft = LaunchGate.Evaluate(new EngineeringReport { ok = true }, new QuickReadout { margin = -0.1, fairingFits = true });
            Assert.That(DesignerRepair.GateRepairs(new EngineeringReport { ok = true }, soft));
            Assert.IsFalse(DesignerRepair.GateRepairs(new EngineeringReport { ok = true }, new LaunchGateResult { state = LaunchGateState.Ready }));
        }

        [Test]
        public void ReportKindAndFixTab()
        {
            Assert.That(DesignerRepair.ReportKind("tumble", "tumble"), Is.EqualTo("tumble"));
            Assert.That(DesignerRepair.ReportKind(null, "miss"), Is.EqualTo("dv-margin"));
            Assert.That(DesignerRepair.ReportKind(null, "pass"), Is.EqualTo(""));
            Assert.That(DesignerRepair.ReportKind("", null), Is.EqualTo(""));
            Assert.That(DesignerRepair.FixTab("prop-depletion", "propellants"), Is.EqualTo("tanks"), "the fuel load lives on Tanks");
            Assert.That(DesignerRepair.FixTab("dv-margin", "tanks"), Is.EqualTo("tanks"));
            Assert.That(DesignerRepair.FixTab("bad-mixture", "propellants", "mixtureOffsetPct"), Is.EqualTo("propellants"));
            Assert.That(DesignerRepair.FixTab("structural-maxq", "structure"), Is.EqualTo("structure"));
        }
    }
}
