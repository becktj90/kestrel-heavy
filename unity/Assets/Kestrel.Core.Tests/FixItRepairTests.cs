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
using Kestrel.Core.Physics;
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

        static string Seeds(CampaignMission mission, List<CampaignFlight> flights)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var f in flights)
                sb.Append(" [seed ").Append(f.seed).Append(' ').Append(f.outcome).Append(' ')
                  .Append(f.apoKm.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append(" x ")
                  .Append(f.periKm.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append(" km ")
                  .Append(DesignerRepair.MissKind(mission, f) ?? "pass").Append(']');
            return sb.ToString();
        }

        [Test]
        public void FixIt_WeakAirframe_RepairsUntilEveryGateSeedFlies_GustsIncluded()
        {
            // KH-14's starter with the walls cut to strength 1.0 breaks up at max-Q on every seed. Fixed has to mean
            // fixed (#313): FlightBatch seeds 1-5, the gate's seeds, all pass the mission (no failure and no clean miss),
            // with one strength step of margin. Real values: the stepper's 95% max-Q throttle lofts into a miss, so Fix it
            // takes its next lever, strength on the rocket as it was: Strength 1 → 1.3, 416.2 x 383.8 km on seeds 1-5;
            // 1.25 still passes (409.6 x 390.4 km), 1.20 misses on flight-path angle (516.6 x 283.4 km), 1.15 breaks up.
            const int index = 14;
            var mission = campaign.Mission(index);
            var json = Starter(index);
            JsonMut.Set(json, "safetyFactor", 1.0);
            var before = Gate(json);
            Assert.That(before.state, Is.EqualTo(LaunchGateState.Blocked));
            Assert.That(before.code, Is.EqualTo("structural-maxq"), before.Line);
            Assert.That(Fly(index, json).TrueForAll(f => f.failure == "structural-maxq"), "the weak rocket really breaks up");

            var outcome = DesignerRepair.Fix(catalog, json, before.code, mission);
            Assert.That(outcome.Changed, outcome.Note);
            Assert.That(json.Num("safetyFactor"), Is.GreaterThan(1.0), outcome.Note);
            var flights = Fly(index, json);
            foreach (var f in flights)
            {
                Assert.IsNull(f.failure, "KH-" + index + " seed " + f.seed + " / " + outcome.Note);
                Assert.That(f.passed, "KH-" + index + " seed " + f.seed + " passes the mission, not just flies clean / " + outcome.Note + " full=" + outcome.FullyFixed + " sf=" + json.Num("safetyFactor") + " q=" + json.Obj("guidance")?.Num("maxQThrottle") + Seeds(mission, flights));
            }
            Assert.That(outcome.FullyFixed, outcome.Note);
            Assert.That(outcome.Note, Does.Not.Contain("could not"), outcome.Note);
            var after = Gate(json);
            Assert.That(after.Ready, after.Line);
            // Arcade: one step of margin past the first strength that passes seeds 1-5. One step less still passes all 5
            // (so the last step is margin); two steps less breaks up on a seed (so it is exactly one step).
            double step = DesignerLimits.Active.safetyStep;
            double sf = json.Num("safetyFactor");
            var less = JsonMut.CloneObject(json);
            HangarEditor.SetSafetyFactor(less, sf - step);
            foreach (var f in Fly(index, less)) Assert.That(f.passed, "KH-" + index + " one step under " + sf + ", seed " + f.seed);
            HangarEditor.SetSafetyFactor(less, sf - 2 * step);
            // #313: a clean miss is a failed seed (1.20: flight-path-angle miss 516.6 x 283.4 km).
            Assert.That(Fly(index, less).Exists(f => !f.passed), "KH-" + index + " two steps under " + sf + " is the knife-edge");
        }

        [Test]
        public void FixIt_WeakAirframe_ThatOnlyFliesIntoAMiss_IsNotFixed()
        {
            // KH-16's starter at strength 1.0 breaks up at max-Q on every seed. Before #313 Fix it flew it clean into a
            // miss and called that fixed. A clean miss is unfixed: within StrengthSteps (1.0 → 1.4) the strength sweep only
            // trades the breakup for misses (1.10-1.25: flight-path-angle miss, 635.9 x 364.1 km at 1.20; 1.30-1.45: the
            // 500 km target is reached but reliability is under the 0.98 gate), so Fix it says it could not fix the rocket.
            // The first strength that passes seeds 1-5 is 1.50 (500.6 x 499.4 km), two steps past the budget. #313 (Arcade):
            // the budget stays at 8 steps, and the note says plainly that Fix it can't fix it and names the redesign (1.5).
            // Fix it does not apply that strength. The board's KH-16 row is "strength" (set 1.8 by hand), not Fix it.
            const int index = 16;
            var mission = campaign.Mission(index);
            var json = Starter(index);
            JsonMut.Set(json, "safetyFactor", 1.0);
            var before = Gate(json);
            Assert.That(before.code, Is.EqualTo("structural-maxq"), before.Line);
            Assert.That(Fly(index, json).TrueForAll(f => f.failure == "structural-maxq"), "the weak rocket really breaks up");

            var outcome = DesignerRepair.Fix(catalog, json, before.code, mission);
            var flights = Fly(index, json);
            int failed = flights.FindAll(f => !f.passed).Count;
            Assert.That(failed, Is.EqualTo(5), Seeds(mission, flights));
            Assert.IsFalse(outcome.FullyFixed, outcome.Note);
            Assert.That(outcome.Note, Is.EqualTo("Fix it can't fix this rocket: it still fails 5 of 5 test flights. It needs a redesign with a stronger airframe: strength 1.5. Fix it only goes up to 1.4."));
            Assert.That(json.Num("safetyFactor"), Is.LessThanOrEqualTo(1.0 + DesignerRepair.StrengthSteps * DesignerLimits.Active.safetyStep + 1e-9), "Fix it never applies the redesign strength");
            Assert.AreEqual(8, DesignerRepair.StrengthSteps, "Arcade: the strength budget holds at 8 steps");

            var start = Starter(index);
            double step = DesignerLimits.Active.safetyStep;
            HangarEditor.SetSafetyFactor(start, 1.0 + DesignerRepair.StrengthSteps * step);
            var top = Fly(index, start);
            Assert.That(top.TrueForAll(f => f.failure == null && f.reached && !f.passed), "the last strength in budget reaches the target but fails a gate /" + Seeds(mission, top));
            Assert.That(top.TrueForAll(f => DesignerRepair.MissKind(mission, f) == DesignerRepair.GateMiss), Seeds(mission, top));
            HangarEditor.SetSafetyFactor(start, 1.2);
            var low = Fly(index, start);
            Assert.That(low.TrueForAll(f => DesignerRepair.MissKind(mission, f) == DesignerRepair.FlightPathAngleMiss), Seeds(mission, low));
            HangarEditor.SetSafetyFactor(start, 1.5);
            Assert.That(Fly(index, start).TrueForAll(f => f.passed), "1.50 passes seeds 1-5");
        }

        [Test]
        public void FixIt_WhenTheMaxQThrottleLoftsIntoAMiss_NeverClaimsFixed()
        {
            // #313: KH-14's starter at strength 1.0 with a 95% max-Q throttle no longer breaks up; it lofts (876.0 x
            // -2079.2 km against 400 km) on every seed, and strength does not bring it down (794.8 x -2728.9 km at 1.4).
            // Flying clean is not passing: Fix it reports it could not fix the rocket.
            const int index = 14;
            var mission = campaign.Mission(index);
            var json = Starter(index);
            JsonMut.Set(json, "safetyFactor", 1.0);
            var guidance = json.Obj("guidance");
            if (guidance == null) { guidance = new JsonObject(); JsonMut.Set(json, "guidance", guidance); }
            JsonMut.Set(guidance, "maxQThrottle", 95.0);
            var raw = Fly(index, json);
            Assert.That(raw.TrueForAll(f => f.failure == null && DesignerRepair.MissKind(mission, f) == DesignerRepair.TargetMiss), Seeds(mission, raw));

            var outcome = DesignerRepair.Fix(catalog, json, "structural-maxq", mission);
            var flights = Fly(index, json);
            int failed = flights.FindAll(f => !f.passed).Count;
            Assert.That(failed, Is.EqualTo(5), Seeds(mission, flights));
            Assert.That(flights.TrueForAll(f => f.failure == null), "it flies clean / " + Seeds(mission, flights));
            Assert.IsFalse(outcome.FullyFixed, outcome.Note);
            Assert.That(outcome.Note, Is.EqualTo(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                DesignerRepair.NotFullyFixed, failed, DesignerRepair.GateSeeds)));
        }

        [Test]
        public void FixIt_WeakAirframe16_RepairIsTheMinimalStrengthForTheBreakupQ()
        {
            // #314 (Physics): KH-16's starter with the walls cut to strength 1.0. The repair keeps its strength step, the
            // stepper's: raise strength until the max-Q the designer read sits under the amber line (85% of q_lim). That
            // q is now read at the breakup moment, not taken from the whole run after the stack is already coming apart,
            // so the step is asserted minimal instead of pinned to a value: the repaired strength flies every gate seed
            // with no failure, and one strength step less no longer clears the line for that same breakup q.
            // Plainly: SF 1.20 is minimal against the stepper's 0.85 q_lim line (44.02 kPa needs 1.177, rounded up to the
            // 0.05 grid). It is not the physical knife-edge: FlightBatch alone already flies all 5 seeds at SF 1.10
            // (1.05 breaks up). The 0.85 line is the designer's margin on top of that.
            const int index = 16;
            var json = Starter(index);
            JsonMut.Set(json, "safetyFactor", 1.0);
            var before = Gate(json);
            Assert.That(before.state, Is.EqualTo(LaunchGateState.Blocked));
            Assert.That(before.code, Is.EqualTo("structural-maxq"), before.Line);
            Assert.That(Fly(index, json).TrueForAll(f => f.failure == "structural-maxq"), "the weak rocket really breaks up");

            // The q the repair reads: the designer's reference, frozen at the breakup. It sits at the weak walls' q_lim,
            // far under the uncut full flight's peak.
            var weak = Design.FromJson(json);
            double qLimWeak = FailureMonitor.StructuralQLimPa(catalog, weak);
            var reference = Readout.Reference(catalog, json);
            Assert.IsFalse(reference.maxQStale);
            double qBreak = reference.maxQ_kPa.Value * 1000;
            Assert.That(qBreak, Is.GreaterThanOrEqualTo(qLimWeak), "the reference breaks up at q_lim");
            Assert.That(qBreak, Is.LessThan(qLimWeak * 1.02), "q is read at the breakup moment");
            var setup = MissionFlightSetup.Build(catalog, campaign.Mission(index), weak, MissionFlightSetup.BatchDt);
            Assert.That(Headless.Simulate(setup.Stages, setup.Options).maxQ, Is.GreaterThan(qBreak * 2), "the uncut run's peak is not the breakup q");

            var outcome = DesignerRepair.Fix(catalog, json, before.code, campaign.Mission(index));
            Assert.That(outcome.Changed, outcome.Note);
            Assert.That(outcome.FullyFixed, outcome.Note);
            Assert.That(outcome.Note, Does.Not.Contain("could not"), outcome.Note);
            double sf = json.Num("safetyFactor");
            Assert.That(sf, Is.GreaterThan(1.0), outcome.Note);
            foreach (var f in Fly(index, json)) Assert.IsNull(f.failure, "KH-" + index + " seed " + f.seed + " / " + outcome.Note);
            Assert.That(Gate(json).Ready);

            // Minimal: the repaired strength clears the amber line for the breakup q; one step less does not.
            double step = DesignerLimits.Active.safetyStep;
            double amber = Kestrel.Core.Designer.Stepper.StepperChecks.MaxQAmberFraction;
            Assert.That(qBreak, Is.LessThanOrEqualTo(amber * FailureMonitor.StructuralQLimPa(catalog, Design.FromJson(json)) + 1e-6),
                "KH-16 at strength " + sf + " clears the line");
            var less = JsonMut.CloneObject(json);
            HangarEditor.SetSafetyFactor(less, sf - step);
            Assert.That(qBreak, Is.GreaterThan(amber * FailureMonitor.StructuralQLimPa(catalog, Design.FromJson(less))),
                "KH-16 one step under " + sf + " does not clear the line, so the repair step is minimal");
        }

        [Test]
        public void FixIt_ThatCannotFixTheFlight_SaysSo_NeverClaimsSuccess()
        {
            // KH-6's rocket with half the upper-stage load, asked to fix a structural kind. #313: it no longer runs dry; it
            // cuts off at the target energy at a bad flight-path angle (412.4 x -12.4 km against 200 km; seed 2 loses an
            // engine). Strength cannot fix that, so after its steps Fix it says it could not fully fix the rocket.
            var mission = campaign.Mission(6);
            var json = Starter(6);
            TopStageProp(json, 0.5);
            var raw = Fly(6, json);
            Assert.That(raw.FindAll(f => DesignerRepair.MissKind(mission, f) == DesignerRepair.FlightPathAngleMiss).Count, Is.EqualTo(4), Seeds(mission, raw));
            var outcome = DesignerRepair.Fix(catalog, json, "structural-maxq", mission);
            var flights = Fly(6, json);
            int failed = flights.FindAll(f => DesignerRepair.MissKind(mission, f) != null).Count;
            Assert.That(failed, Is.EqualTo(5), "this rocket still fails every gate seed /" + Seeds(mission, flights));
            foreach (var f in flights)
                Assert.That(DesignerRepair.MissKind(mission, f), Is.EqualTo(DesignerRepair.FlightPathAngleMiss), "seed " + f.seed + Seeds(mission, flights));
            Assert.IsFalse(outcome.FullyFixed);
            Assert.That(outcome.Note, Is.EqualTo("Fix it could not fully fix this rocket. It still fails 5 of 5 test flights. Lighten the cargo or change the rocket."));
            // #313: the report text for the repaired rocket, pinned. It cuts off climbing and falls back (412.0 x -12.0 km).
            var x = MissReason.Explain(mission, MissReason.FromFlight(flights[0], json.Arr("stages").Count));
            Assert.AreEqual("fpa-steep-suborbital", x.Code);
            Assert.AreEqual("Missed the 200 km circle: the orbit flew -12 km × 412 km, and the high point needs 190 km to 210 km. The low point, -12 km, needs 190 km to 210 km. The engines cut off while it was still climbing, 1.8° up. Your rocket flew too steep and fell back down. Give stage 3 much more fuel (it can take double or more), so it burns long enough to level out.", x.Line);
            // Following it: double the top-stage fuel and seeds 1, 3, 4, 5 land the circle (seed 2 is its seeded engine-out).
            // The steep levers do not move this one: one more upper engine with 95% max-Q is still 412.8 x -12.8 km.
            var doubled = JsonMut.CloneObject(json);
            TopStageProp(doubled, 2.0);
            foreach (var f in Fly(6, doubled))
                if (f.seed == 2) Assert.That(f.failure, Is.EqualTo("engine-out"), Seeds(mission, Fly(6, doubled)));
                else Assert.That(f.passed, "seed " + f.seed + Seeds(mission, Fly(6, doubled)));
        }

        [Test]
        public void ReportFixIt_RanOutOfFuel_AddsFuelUntilItNoLongerRunsDry_ThenSaysTheMissRemains()
        {
            // #313: KH-6's rocket at half the upper-stage load no longer runs dry (it misses on flight-path angle). It runs
            // dry at 16% of the load, on every seed. The report's Fix it adds propellant until the test ascent is not short
            // and no seed runs dry. Under the new rule the mission still has to pass: KH-6's starter then cuts off at the
            // target energy at a bad flight-path angle (413.9 x -13.9 km against 200 km), so Fix it reports not fixed.
            var mission = campaign.Mission(6);
            var json = Starter(6);
            TopStageProp(json, 0.16);
            var raw = Fly(6, json);
            Assert.That(raw.TrueForAll(f => f.failure == "prop-depletion"), Seeds(mission, raw));
            Assert.That(DesignerRepair.FlightShort(catalog, json), "the designer's test ascent runs dry");

            var plain = JsonMut.CloneObject(json);
            var fuel = DesignerRepair.Fix(catalog, plain, DesignerRepair.ReportKind("prop-depletion", "prop-depletion"));
            Assert.That(fuel.Changed, fuel.Note);
            Assert.That(fuel.Note, Is.EqualTo(DesignerRepair.DeltaVAdded));
            Assert.IsFalse(DesignerRepair.FlightShort(catalog, plain));
            Assert.IsFalse(fuel.FullyFixed, "no mission, no full-fix claim");
            var fueled = Fly(6, plain);
            foreach (var f in fueled) Assert.That(f.failure, Is.Not.EqualTo("prop-depletion"), "seed " + f.seed);

            var outcome = DesignerRepair.Fix(catalog, json, DesignerRepair.ReportKind("prop-depletion", "prop-depletion"), mission);
            Assert.That(outcome.Changed, outcome.Note);
            var flights = Fly(6, json);
            foreach (var f in flights)
                Assert.That(DesignerRepair.MissKind(mission, f), Is.EqualTo(DesignerRepair.FlightPathAngleMiss), "seed " + f.seed + Seeds(mission, flights));
            Assert.IsFalse(outcome.FullyFixed, outcome.Note);
            Assert.That(outcome.Note, Is.EqualTo("Fix it could not fully fix this rocket. It still fails 5 of 5 test flights. Lighten the cargo or change the rocket."));
            // #313: the report text after Fix it, pinned (413.9 x -13.9 km), and following it: about 3x the fuel lands all 5.
            var x = MissReason.Explain(mission, MissReason.FromFlight(flights[0], json.Arr("stages").Count));
            Assert.AreEqual("fpa-steep-suborbital", x.Code);
            Assert.AreEqual("Missed the 200 km circle: the orbit flew -13.9 km × 414 km, and the high point needs 190 km to 210 km. The low point, -13.9 km, needs 190 km to 210 km. The engines cut off while it was still climbing, 1.8° up. Your rocket flew too steep and fell back down. Give stage 3 much more fuel (it can take double or more), so it burns long enough to level out.", x.Line);
            var more = JsonMut.CloneObject(json);
            TopStageProp(more, 3.06);
            foreach (var f in Fly(6, more)) Assert.That(f.passed, "seed " + f.seed + Seeds(mission, Fly(6, more)));
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
