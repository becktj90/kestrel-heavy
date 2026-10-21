// F4: the report says why an intact, Ready starter missed, with its own numbers, and what to change.
// KH-3, 4, 6, 8, 12 and 16 stay design puzzles (#313: KH-5 and KH-14 now pass): each real miss (FlightBatch, seed 1)
// is pinned to its exact text, and the advice is checked by flying it. One reason shows, in fix order (orbit, fuel,
// structure, reliability, setup), and the reason counts any other gate that missed with "(+N more issue)" (#324).
// #313: KH-3 and KH-4 cut off diving (too flat: the fuel step lands them); KH-12 cuts off climbing (too steep: Fix it's
// engine + max-Q throttle edit, or a smaller kick, lands it).
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class MissReasonTests
    {
        Catalog catalog;
        Campaign campaign;
        HangarData hangar;

        [OneTimeSetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
        }

        CampaignMission Mission(string id)
        {
            foreach (var m in campaign.Missions) if (m.Id == id) return m;
            Assert.Fail("no mission " + id);
            return null;
        }

        Design Starter(CampaignMission m) => Design.FromJson(HomeAutoBuild.Choose(catalog, hangar.templates, m).Design);

        MissExplanation Fly(CampaignMission m, Design d, out CampaignFlight flight)
        {
            flight = FlightBatch.Fly(catalog, m, d, 1);
            return MissReason.Explain(m, MissReason.FromFlight(flight, d.stages.Count));
        }

        // #313 (closed-loop director, energy cutoff): the starters' misses are new numbers. KH-3, 4, 6 and 12 now cut off at
        // the target energy at a bad flight-path angle, so both the high and the low point are out of the band and the
        // reason names both. KH-8 now flies in the band and misses on reliability. KH-5 and KH-14 now pass (board rows
        // "fly"), so they moved to PassingStarterHasNoReason.
        [TestCase("m3", "fpa-flat",
            "Missed the 200 km circle: the orbit flew 72.3 km × 328 km, and the high point needs 190 km to 210 km. The low point, 72.3 km, needs 190 km to 210 km. The engines cut off while it was already diving, 1.1° down.",
            "Your rocket flew too flat. Add fuel to stage 2, about 2% at a time (up to 10%), until the orbit rounds off.")]
        [TestCase("m4", "fpa-flat",
            "Missed the 500 km circle: the orbit flew 382 km × 618 km, and the high point needs 475 km to 525 km. The low point, 382 km, needs 475 km to 525 km. The engines cut off while it was already diving, 1.0° down.",
            "Your rocket flew too flat. Add fuel to stage 2, about 2% at a time (up to 10%), until the orbit rounds off.")]
        [TestCase("m6", "apo-high",
            "Missed the 200 km circle: the orbit flew 188 km × 212 km, and the high point needs 190 km to 210 km. That is about 4 m/s too fast at the low point. The low point, 188 km, needs 190 km to 210 km.",
            "Add fuel to stage 3, about 2% at a time (up to 10%), until the high point comes down into the band.")]
        [TestCase("m8", "reliability",
            "Reliability was 95.1%; this mission needs 98%.",
            "Raise the safety factor to 1.5 or more for the reliability bonus, or pick redundant avionics.")]
        [TestCase("m12", "fpa-steep",
            "Missed the 600 km circle: the orbit flew 343 km × 857 km, and the high point needs 570 km to 630 km. The low point, 343 km, needs 570 km to 630 km. The engines cut off while it was still climbing, 1.9° up.",
            "Your rocket flew too steep. Give the upper stage more push (one more engine, with the max-Q throttle at 95%), or use a smaller pitch kick at launch (for example 6° → 4° in Guidance).")]
        [TestCase("m16", "reliability",
            "Reliability was 97.9%; this mission needs 98%.",
            "Raise the safety factor to 1.5 or more for the reliability bonus, or pick redundant avionics.")]
        public void StarterMissSaysWhyAndWhatToChange(string id, string code, string reason, string advice)
        {
            var m = Mission(id);
            var x = Fly(m, Starter(m), out var flight);
            Assert.IsNull(flight.failure, id + " flies intact");
            Assert.IsFalse(flight.passed, id + " misses");
            Assert.IsNotNull(x, id + " has a reason");
            Assert.AreEqual(code, x.Code);
            Assert.AreEqual(reason, x.Reason);
            Assert.AreEqual(advice, x.Advice);
            Assert.AreEqual(reason + " " + advice, x.Line);
        }

        [TestCase("m1")]
        [TestCase("m2")]
        [TestCase("m5")]
        [TestCase("m7")]
        [TestCase("m11")]
        [TestCase("m13")]
        [TestCase("m14")]
        [TestCase("m15")]
        public void PassingStarterHasNoReason(string id)
        {
            var m = Mission(id);
            var x = Fly(m, Starter(m), out var flight);
            Assert.IsTrue(flight.passed, id + " passes");
            Assert.IsNull(x);
        }

        /// <summary>A failure event explains itself: a batch row with a failure kind gets no miss reason.</summary>
        [Test]
        public void FailureEventExplainsItselfNotTheMissReason()
        {
            var m = Mission("m4");
            var d = Starter(m);
            var flight = FlightBatch.Fly(catalog, m, d, 1);
            flight.failure = "structural-maxq";
            var input = MissReason.FromFlight(flight, d.stages.Count);
            Assert.IsFalse(input.Intact);
            Assert.IsNull(MissReason.Explain(m, input));
        }

        /// <summary>The overshoot advice works: some +2% step up to +10% on the top stage brings the high point into the band.</summary>
        // #313 real values: KH-3 1.02 → 1.06 (202 x 198 km), KH-4 1.04 → 1.02 (502 x 498 km), KH-14 1.04 → 1.02 (the
        // starter already passes). KH-5 and KH-8 stay at 1.02 (KH-5 passes already; KH-8 misses on reliability, not
        // altitude). KH-3 and KH-4 now get the too-flat advice, which is this same fuel step. KH-12 (too steep, 857 x 343 km)
        // does not come in with fuel (1.10 still flies 866 x 334), so it gets the steep advice instead (below).
        [TestCase("m3", 1.06)]
        [TestCase("m4", 1.02)]
        [TestCase("m5", 1.02)]
        [TestCase("m6", 1.02)]
        [TestCase("m8", 1.02)]
        [TestCase("m14", 1.02)]
        public void MoreTopStageFuelBringsTheHighPointIn(string id, double first)
        {
            var m = Mission(id);
            double found = double.NaN;
            for (int step = 1; step <= 5 && double.IsNaN(found); step++)
            {
                double k = 1 + 0.02 * step;
                var d = Starter(m);
                d.stages[d.stages.Count - 1].propMass *= k;
                var x = Fly(m, d, out var flight);
                if (flight.failure == null && (x == null || (x.Code != "apo-high" && x.Code != "peri-low" && x.Code != "orbit-low" && x.Code != "fpa-steep" && x.Code != "fpa-steep-suborbital" && x.Code != "fpa-flat")))
                    found = k;
            }
            Assert.AreEqual(first, found, 1e-9, id + ": first top-stage step that lands the circle");
        }

        /// <summary>
        /// KH-12's starter misses high on every seed, and a wide band of top-stage fuel lands the 600 km circle:
        /// every whole step from +4% to +21% passes on seeds 1 to 5 (data/board-starters.json, 32,000 kg top stage).
        /// Below that it is not a window: +1% lands, +2% tops out at 630 km, and +3% misses on seed 5.
        /// </summary>
        [Test]
        public void KhTwelveLandsOnAWideFuelWindow()
        {
            var m = Mission("m12");
            foreach (ulong seed in campaign.Seeds)
            {
                var d = Starter(m);
                var flight = FlightBatch.Fly(catalog, m, d, seed);
                var x = MissReason.Explain(m, MissReason.FromFlight(flight, d.stages.Count));
                Assert.IsNull(flight.failure, "starter seed " + seed + " flies intact");
                Assert.IsFalse(flight.passed, "starter seed " + seed + " keeps its real miss");
                Assert.AreEqual("fpa-steep", x?.Code, "starter seed " + seed);
            }
            var bad = new System.Collections.Generic.List<string>();
            for (int pct = 4; pct <= 21; pct++)
            {
                foreach (ulong seed in campaign.Seeds)
                {
                    var d = Starter(m);
                    d.stages[d.stages.Count - 1].propMass *= 1 + pct / 100.0;
                    var flight = FlightBatch.Fly(catalog, m, d, seed);
                    if (!flight.passed) bad.Add("+" + pct + "% seed " + seed + " " + (flight.failure ?? (flight.periKm.ToString("0") + "x" + flight.apoKm.ToString("0"))));
                }
            }
            Assert.That(bad, Is.Empty, string.Join(" | ", bad));
        }

        /// <summary>KH-8 under #313 (closed-loop director, energy cutoff): the starter's orbit is inside the 400 km band on every
        /// seed, so reliability (95.1% vs 98%) is the only reason, with no other gate counted. The old pre-fuel-fix state (an
        /// apo-high miss plus reliability) no longer exists, so the fix-order rule is held by ReasonsComeInFixOrderAndTheRestAreCounted;
        /// the +2% fuel step that used to clear the orbit keeps the same single reason.</summary>
        [Test]
        public void KhEightOrbitIsInTheBand_ReliabilityIsTheOnlyReason()
        {
            var m = Mission("m8");
            foreach (var seed in campaign.Seeds)
            {
                var d = Starter(m);
                var flight = FlightBatch.Fly(catalog, m, d, seed);
                var x = MissReason.Explain(m, MissReason.FromFlight(flight, d.stages.Count));
                Assert.IsNull(flight.failure, "seed " + seed);
                Assert.IsTrue(flight.reached, "seed " + seed + " orbit in the band: " + flight.apoKm.ToString("0.0") + " x " + flight.periKm.ToString("0.0"));
                Assert.That(flight.apoKm, Is.InRange(m.ApoKm * 0.95, m.ApoKm * 1.05), "seed " + seed + " high point");
                Assert.That(flight.periKm, Is.InRange(m.PeriKm * 0.95, m.PeriKm * 1.05), "seed " + seed + " low point");
                Assert.IsFalse(flight.passed, "seed " + seed + " still misses on reliability");
                Assert.AreEqual("reliability", x.Code, "seed " + seed + ": " + x.Line);
                Assert.AreEqual(0, x.More, "seed " + seed + ": reliability is the only reason");
                CollectionAssert.IsEmpty(x.Others, "seed " + seed);
                Assert.AreEqual("Reliability was 95.1%; this mission needs 98%.", x.Reason, "seed " + seed);
            }

            var fuel = Starter(m);
            fuel.stages[fuel.stages.Count - 1].propMass *= 1.02;
            var after = Fly(m, fuel, out var fuelFlight);
            Assert.IsNull(fuelFlight.failure);
            Assert.AreEqual("reliability", after.Code, "after +2% fuel: " + after.Line);
            Assert.AreEqual(0, after.More);
            Assert.AreEqual("Reliability was 95.1%; this mission needs 98%.", after.Reason);
        }

        /// <summary>Fix order: orbit, fuel, structure, reliability, setup. The first gate is the reason; the rest are counted.</summary>
        [Test]
        public void ReasonsComeInFixOrderAndTheRestAreCounted()
        {
            var row = Row("circular", 400, 400);
            row.MaxG = 4;
            row.ReliabilityMin = 0.98;
            row.MarginNonNegative = true;
            var extra = new GoalCheck { Reliability = 0.95, Margin = -0.02 };

            var all = MissReason.Explain(row, new MissInput { ApoM = 250e3, PeriM = 120e3, MaxG = 5, Stages = 2, Extra = extra });
            Assert.AreEqual("orbit-low", all.Code);
            Assert.AreEqual(3, all.More);
            CollectionAssert.AreEqual(new[] { "margin", "g", "reliability" }, all.Others);
            StringAssert.EndsWith(" (+3 more issues)", all.Reason);

            var onOrbit = MissReason.Explain(row, new MissInput { ApoM = 400e3, PeriM = 400e3, MaxG = 5, Stages = 2, Extra = new GoalCheck { Reliability = 0.95, Margin = 0.01 } });
            Assert.AreEqual("g", onOrbit.Code, "structure before reliability");
            Assert.AreEqual("Peak load was 5.0 g; this cargo is rated for 4.0 g. (+1 more issue)", onOrbit.Reason);

            var one = MissReason.Explain(row, new MissInput { ApoM = 400e3, PeriM = 400e3, MaxG = 3, Stages = 2, Extra = new GoalCheck { Reliability = 0.95, Margin = 0.01 } });
            Assert.AreEqual("reliability", one.Code);
            Assert.AreEqual(0, one.More);
            Assert.AreEqual("Reliability was 95%; this mission needs 98%.", one.Reason);
        }

        [Test]
        public void MoreNoteSaysIssueOrIssues()
        {
            Assert.AreEqual("", MissReason.MoreNote(0));
            Assert.AreEqual("(+1 more issue)", MissReason.MoreNote(1));
            Assert.AreEqual("(+2 more issues)", MissReason.MoreNote(2));
        }

        /// <summary>Inclination is not scored in Phase 1: a starter that passes flies the reference pad's equatorial plane, nowhere near its stated target inclination.</summary>
        [TestCase("m11")]
        [TestCase("m13")]
        public void InclinationIsNotScored(string id)
        {
            var m = Mission(id);
            var flight = FlightBatch.Fly(catalog, m, Starter(m), 1);
            Assert.IsTrue(flight.passed, id);
            Assert.That(Math.Abs(flight.incDeg - m.IncDeg), Is.GreaterThan(MissionScore.InclinationDeg), id + " flew " + flight.incDeg + " deg");
        }

        /// <summary>Because inclination is not scored, no campaign briefing, report goal or board target line states one, or promises anything that depends on the orbital plane (sun-sync, same hour, ground track).</summary>
        [Test]
        public void NoMissionTextStatesAnInclination()
        {
            var deg = new System.Text.RegularExpressions.Regex(@"\d\s*(°|degree)|sun-?sync|same hour|ground track", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var bad = new System.Collections.Generic.List<string>();
            foreach (var m in campaign.Missions)
            {
                foreach (var text in new[] { m.Goal, m.Control, m.Why, m.WhatItIs, m.WhatItDoes, MissionBoardFacts.OrbitLine(m) })
                    if (!string.IsNullOrEmpty(text) && deg.IsMatch(text)) bad.Add(m.Id + ": " + text);
            }
            Assert.That(bad, Is.Empty, string.Join(" | ", bad));
        }

        void AssertEverySeed(CampaignMission m, Design d, string what)
        {
            foreach (var seed in campaign.Seeds)
            {
                var f = FlightBatch.Fly(catalog, m, d, seed);
                Assert.IsTrue(f.passed, what + " seed " + seed + " " + (f.failure ?? "miss") + " " + f.apoKm.ToString("0.0") + " x " + f.periKm.ToString("0.0"));
            }
        }

        /// <summary>#313: KH-12 gets the steep advice, and its first lever is Fix it's own edit for KH-12 (board row "fix"):
        /// one more upper engine with the max-Q throttle at 95%. Following it lands the circle on all five seeds.</summary>
        [Test]
        public void KH12_SteepAdvice_IsFixItsEdit_AndFollowingItPassesEverySeed()
        {
            var m = Mission("m12");
            var choice = HomeAutoBuild.Choose(catalog, hangar.templates, m);
            var start = Design.FromJson(choice.Design);
            var x = Fly(m, start, out var raw);
            Assert.AreEqual("fpa-steep", x.Code);
            Assert.AreEqual(MissReason.SteepAdvice, x.Advice);
            Assert.Greater(raw.cutoffGammaDeg, MissReason.FlightPathAngleOffDeg);

            // Fix it, exactly as the board runs it.
            var json = JsonMut.CloneObject(choice.Design);
            Readout.Reference(catalog, json);
            var report = StepperModel.Evaluate(catalog, json);
            StepCheck check = null;
            foreach (var c in report.Checks) if (c != null && !string.IsNullOrEmpty(c.Code)) { check = c; break; }
            Assert.IsNotNull(check);
            StepperFix.Apply(catalog, json, check);
            var fixedByFixIt = Design.FromJson(json);
            int top = start.stages.Count - 1;
            Assert.AreEqual(start.stages[top].engineCount + 1, fixedByFixIt.stages[top].engineCount, "Fix it adds one upper engine");
            Assert.IsTrue(fixedByFixIt.guidance.maxQThrottleSet);
            Assert.AreEqual(95, fixedByFixIt.guidance.maxQThrottle, 1e-9, "Fix it sets the max-Q throttle to 95%");
            AssertEverySeed(m, fixedByFixIt, "Fix it's edit");

            // The advice by hand on the starter: one more upper engine, max-Q throttle 95%.
            var byHand = Starter(m);
            byHand.stages[top].engineCount += 1;
            byHand.guidance.maxQThrottleSet = true;
            byHand.guidance.maxQThrottle = 95;
            AssertEverySeed(m, byHand, "one more upper engine at 95% max-Q");
        }

        /// <summary>#313: the advice's other lever, a smaller pitch kick (6° to 4°), also lands KH-12 on every seed. The advice
        /// names the throttle because one more engine alone still flies too steep (903 x 297 km), and 5° is still too steep.</summary>
        [Test]
        public void KH12_SmallerPitchKickAlsoLands_AndOneEngineAloneIsNotEnough()
        {
            var m = Mission("m12");
            var kick = Starter(m);
            Assert.AreEqual(6, kick.guidance.pitchKickDeg, 1e-9);
            kick.guidance.pitchKickDeg = 4;
            AssertEverySeed(m, kick, "pitch kick 4°");

            var engineOnly = Starter(m);
            engineOnly.stages[engineOnly.stages.Count - 1].engineCount += 1;
            var x = Fly(m, engineOnly, out var f);
            Assert.IsFalse(f.passed);
            Assert.AreEqual("fpa-steep", x.Code, x.Line);
        }

        [Test]
        public void FlightPathAngleMiss_NeedsACleanTiltedCutoff()
        {
            var row = Row("circular", 600, 600);
            var climbing = Flown(857, 343); climbing.CutoffGammaDeg = 1.9;
            Assert.AreEqual("fpa-steep", MissReason.Explain(row, climbing).Code);
            var diving = Flown(857, 343); diving.CutoffGammaDeg = -1.1;
            var flat = MissReason.Explain(row, diving);
            Assert.AreEqual("fpa-flat", flat.Code);
            Assert.AreEqual("Your rocket flew too flat. Add fuel to stage 2, about 2% at a time (up to 10%), until the orbit rounds off.", flat.Advice);
            StringAssert.EndsWith("The engines cut off while it was already diving, 1.1° down.", flat.Reason);
            var level = Flown(857, 343); level.CutoffGammaDeg = 0.1;
            Assert.AreEqual("apo-high", MissReason.Explain(row, level).Code, "near level: a plain overshoot");
            var burnout = Flown(857, 343);
            Assert.AreEqual("apo-high", MissReason.Explain(row, burnout).Code, "no clean cutoff (NaN): a plain overshoot");
            var fell = Flown(412, -12); fell.CutoffGammaDeg = 1.8;
            var back = MissReason.Explain(Row("circular", 200, 200), fell);
            Assert.AreEqual("fpa-steep-suborbital", back.Code);
            Assert.AreEqual("Your rocket flew too steep and fell back down. Give stage 2 much more fuel (it can take double or more), so it burns long enough to level out.", back.Advice);
            var lowOk = Flown(857, 600); lowOk.CutoffGammaDeg = 1.9;
            Assert.AreEqual("apo-high", MissReason.Explain(row, lowOk).Code, "the low point is on target");
        }

        [TestCase("m8")]
        [TestCase("m16")]
        public void SafetyFactorAdviceClearsTheReliabilityGate(string id)
        {
            var m = Mission(id);
            var d = Starter(m);
            d.safetyFactor = MissReason.SafetyBonusAt;
            var x = Fly(m, d, out var flight);
            Assert.IsTrue(flight.passed, id + " passes at safety factor " + MissReason.SafetyBonusAt + ": " + x?.Line);
        }

        [Test]
        public void SafetyBonusMatchesTheEconomy()
            => Assert.AreEqual(catalog.Econ.Obj("reliability").Num("highSafetyFactor"), MissReason.SafetyBonusAt, 1e-12);

        static CampaignMission Row(string kind, double apoKm, double periKm)
            => new CampaignMission { Id = "t", TargetKind = kind, ApoKm = apoKm, PeriKm = periKm };

        static MissInput Flown(double apoKm, double periKm, int stages = 2)
            => new MissInput { ApoM = apoKm * 1000, PeriM = periKm * 1000, MaxG = 3, Stages = stages };

        [Test]
        public void HopShortSaysHowHighItGot()
        {
            var x = MissReason.Explain(Row("apogee", 100, 0), Flown(36, -6000, 1));
            Assert.AreEqual("altitude-low", x.Code);
            Assert.AreEqual("Topped out at 36 km of 100 km (it needs 95 km to 105 km).", x.Reason);
            Assert.AreEqual("Add fuel to stage 1, or a bigger engine.", x.Advice);
        }

        [Test]
        public void CircleTooLowSaysTheSpeedShortfall()
        {
            var x = MissReason.Explain(Row("circular", 400, 400), Flown(250, 120));
            Assert.AreEqual("orbit-low", x.Code);
            StringAssert.StartsWith("Missed the 400 km circle by about ", x.Reason);
            StringAssert.Contains("the orbit flew 120 km × 250 km", x.Reason);
            Assert.AreEqual("Add fuel to stage 2, or another stage.", x.Advice);
        }

        [Test]
        public void SuborbitalCircleSaysWhereItToppedOut()
        {
            var x = MissReason.Explain(Row("circular", 200, 200), Flown(36, -6000));
            Assert.AreEqual("orbit-low", x.Code);
            StringAssert.Contains("it topped out at 36 km of 200 km", x.Reason);
            StringAssert.Contains("km/s", x.Reason);
        }

        [Test]
        public void LowPointShortSaysTheSpeedAtTheHighPoint()
        {
            var x = MissReason.Explain(Row("circular", 400, 400), Flown(405, 300));
            Assert.AreEqual("peri-low", x.Code);
            StringAssert.Contains("the low point needs 380 km to 420 km", x.Reason);
            StringAssert.Contains("m/s short at the high point", x.Reason);
        }

        [Test]
        public void NothingToExplainOnAPassAFailureOrNoMission()
        {
            Assert.IsNull(MissReason.Explain(Row("circular", 400, 400), Flown(402, 398)));
            Assert.IsNull(MissReason.Explain(Row("circular", 400, 400), new MissInput { Intact = false, ApoM = 1, PeriM = 1 }));
            Assert.IsNull(MissReason.Explain(Row("circular", 400, 400), new MissInput { FailureKind = "overg", ApoM = 1, PeriM = 1 }));
            Assert.IsNull(MissReason.Explain(null, Flown(1, 1)));
        }

        /// <summary>Same verdict as the score, for every board mission: a reason exactly when GoalMet says miss on an intact flight.</summary>
        [Test]
        public void ReasonExactlyWhenTheScoreSaysMiss()
        {
            foreach (var m in campaign.Missions)
            {
                var d = Starter(m);
                var x = Fly(m, d, out var flight);
                bool explainable = flight.failure == null && !flight.passed;
                Assert.AreEqual(explainable, x != null, m.Id);
            }
        }

        /// <summary>The report card text: Numbers carries the reason, Engineer the advice, and no pool line contradicts them.</summary>
        [Test]
        public void DebriefShowsTheReasonAndTheAdviceOnAMiss()
        {
            var m = Mission("m4");
            var d = Starter(m);
            var x = Fly(m, d, out var flight);
            var facts = new FlightFacts { Passed = false, ApoKm = flight.apoKm, GoalKm = m.ApoKm, Miss = x, Seed = 1 };
            var ctx = MissionContext.FromCampaign(campaign, "m4");
            var pool = NarrationPool.Load(Golden.Root);
            var card = FlightDebrief.Compose(null, ctx, facts, pool, 100000, null);
            Assert.AreEqual("miss", card.Kind);
            Assert.AreEqual(x.Reason, card.Numbers);
            Assert.AreEqual(x.Advice, card.Engineer);
            Assert.AreEqual("fpa-flat", card.MissCode);
            Assert.AreEqual(x.Advice, card.MissAdvice);
            Assert.AreEqual("", card.NarratorKid);
            Assert.AreEqual("", card.NarratorEng);
        }

        [Test]
        public void DebriefWithoutAMissReasonKeepsTheOldLines()
        {
            var facts = new FlightFacts { Passed = false, ApoKm = 22, GoalKm = 10, Seed = 1 };
            var ctx = MissionContext.FromCampaign(campaign, "m1");
            var card = FlightDebrief.Compose(null, ctx, facts, NarrationPool.Load(Golden.Root), 100000, null);
            Assert.AreEqual("Reached 22 km. The goal was 10 km.", card.Numbers);
            Assert.AreEqual("", card.MissCode);
            StringAssert.Contains("22", card.NarratorKid);
        }

        [Test]
        public void AFailureEventKeepsItsOwnLinesEvenWithAMissAttached()
        {
            var ev = new FailureEvent { kind = "overg", value = 7.2, limit = 6, kid = "Too hard a push.", engineer = "Lower the g limit." };
            var facts = new FlightFacts { Failure = ev, Miss = new MissExplanation { Code = "g", Reason = "r", Advice = "a" }, Seed = 1 };
            var card = FlightDebrief.Compose(null, MissionContext.FromCampaign(campaign, "m1"), facts, NarrationPool.Empty, 100000, null);
            Assert.AreEqual("overg", card.Kind);
            Assert.AreEqual("", card.MissCode);
            Assert.AreEqual("Lower the g limit.", card.Engineer);
        }
    }
}
