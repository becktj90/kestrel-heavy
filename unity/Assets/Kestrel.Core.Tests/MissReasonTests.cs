// F4: the report says why an intact, Ready starter missed, with its own numbers, and what to change.
// KH-3, 4, 6, 8, 12 and 16 stay design puzzles (#313: KH-5 and KH-14 now pass): each real miss (FlightBatch, seed 1)
// is pinned to its exact text, and the advice is checked by flying it. One reason shows, in fix order (orbit, fuel,
// structure, reliability, setup), and the reason counts any other gate that missed with "(+N more issue)" (#324).
// #313 (Physics): KH-3 and KH-4 cut off diving (too flat: a larger pitch kick lands them, and so does the fuel step);
// KH-12's 6° build cuts off climbing (too steep: a smaller pitch kick lands it; its board starter flies 4.5°).
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
        // "fly"), so they moved to PassingStarterHasNoReason. KH-12's board starter is now a fly row at a 4.5° kick (Physics,
        // #313 on #324's 32,000 kg top stage); its 6° build keeps the exact steep text in KH12_SixDegreeBuild_MissSaysWhyAndWhatToChange.
        // Calm-air retune: KH-6's board design is now strength 1.65 and passes (PassingStarterHasNoReason); its 1.35 build
        // keeps the exact apo-high text in KH6_RawBuild_MissSaysWhyAndWhatToChange. KH-3's board design is now an 8.65° kick
        // (Physics, option b) and passes; its 8° build keeps the exact too-flat text in KH3_RawBuild_MissSaysWhyAndWhatToChange.
        [TestCase("m4", "fpa-flat",
            "Missed the 500 km circle: the orbit flew 382 km × 618 km, and the high point needs 475 km to 525 km. The low point, 382 km, needs 475 km to 525 km. The engines cut off while it was already diving, 1.0° down.",
            "Your rocket flew too flat. Use a larger pitch kick at launch (for example 6° → 6.5° in Guidance).")]
        [TestCase("m8", "reliability",
            "Reliability was 95.1%; this mission needs 98%.",
            "Raise the safety factor to 1.5 or more for the reliability bonus, or pick redundant avionics.")]
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
        [TestCase("m3")]
        [TestCase("m5")]
        [TestCase("m6")]
        [TestCase("m7")]
        [TestCase("m11")]
        [TestCase("m12")]
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

        /// <summary>KH-6's auto-built rocket before the calm-air retune (strength 1.35) still misses high and says so; the board
        /// starter is now strength 1.65 and passes (PassingStarterHasNoReason).</summary>
        [Test]
        public void KH6_RawBuild_MissSaysWhyAndWhatToChange()
        {
            var m = Mission("m6");
            var x = Fly(m, Kh6RawBuild(m), out var flight);
            Assert.IsNull(flight.failure, "m6 flies intact");
            Assert.IsFalse(flight.passed, "m6 raw build misses");
            Assert.IsNotNull(x, "m6 raw build has a reason");
            Assert.AreEqual("apo-high", x.Code);
            Assert.AreEqual("Missed the 200 km circle: the orbit flew 188 km × 212 km, and the high point needs 190 km to 210 km. That is about 4 m/s too fast at the low point. The low point, 188 km, needs 190 km to 210 km.", x.Reason);
            Assert.AreEqual("Add fuel to stage 3, about 2% at a time (up to 10%), until the high point comes down into the band.", x.Advice);
            Assert.AreEqual(x.Reason + " " + x.Advice, x.Line);
        }

        Design Kh6RawBuild(CampaignMission m)
        {
            var d = Starter(m);
            d.safetyFactor = 1.35;
            return d;
        }

        /// <summary>KH-3's auto-built rocket before the calm-air retune (8° kick, strength 1.45) still misses too flat and says
        /// so; the board starter is now an 8.65° kick and passes (PassingStarterHasNoReason, KH3_BoardKick_LandsEverySeedAndTheLivePadInCalmAir).</summary>
        [Test]
        public void KH3_RawBuild_MissSaysWhyAndWhatToChange()
        {
            var m = Mission("m3");
            var x = Fly(m, Kh3RawBuild(m), out var flight);
            Assert.IsNull(flight.failure, "m3 flies intact");
            Assert.IsFalse(flight.passed, "m3 raw build misses");
            Assert.IsNotNull(x, "m3 raw build has a reason");
            Assert.AreEqual("fpa-flat", x.Code);
            Assert.AreEqual("Missed the 200 km circle: the orbit flew 72.3 km × 328 km, and the high point needs 190 km to 210 km. The low point, 72.3 km, needs 190 km to 210 km. The engines cut off while it was already diving, 1.1° down.", x.Reason);
            Assert.AreEqual("Your rocket flew too flat. Use a larger pitch kick at launch (for example 6° → 6.5° in Guidance).", x.Advice);
            Assert.AreEqual(x.Reason + " " + x.Advice, x.Line);
        }

        /// <summary>Calm-air retune (Physics, option b): KH-3's board starter flies an 8.65° kick at strength 1.45, the centre
        /// of the calm-robust 8.5°–8.8° window. It lands every batch seed and the live pad in calm air. Live seed-1 wind is
        /// not asserted here: the kick's neighbours flip in and out of the band there (#338, #341).</summary>
        [Test]
        public void KH3_BoardKick_LandsEverySeedAndTheLivePadInCalmAir()
        {
            var m = Mission("m3");
            var board = Starter(m);
            Assert.AreEqual(8.65, board.guidance.pitchKickDeg, 1e-9, "board starter kick");
            Assert.AreEqual(1.45, board.safetyFactor, 1e-9, "board starter strength");
            AssertEverySeed(m, board, "kick 8.65°");
            var calm = LivePadLoop.Fly(catalog, m, board, false);
            Assert.IsTrue(LivePadLoop.InBand(m, calm), "8.65° live calm " + LivePadLoop.Text(calm));
            foreach (double k in new[] { 8.5, 8.8 })
            {
                var d = Starter(m);
                d.guidance.pitchKickDeg = k;
                AssertEverySeed(m, d, "calm window edge kick " + k + "°");
                var edge = LivePadLoop.Fly(catalog, m, d, false);
                Assert.IsTrue(LivePadLoop.InBand(m, edge), k + "° live calm " + LivePadLoop.Text(edge));
            }
        }

        Design Kh3RawBuild(CampaignMission m)
        {
            var d = Starter(m);
            d.guidance.pitchKickDeg = 8;
            return d;
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
        // KH-3 and KH-6 fly their pre-retune builds (8° kick; strength 1.35), the ones that miss.
        // #313 real values: KH-3 1.02 → 1.06 (202 x 198 km), KH-4 1.04 → 1.02 (502 x 498 km), KH-14 1.04 → 1.02 (the
        // starter already passes). KH-5 and KH-8 stay at 1.02 (KH-5 passes already; KH-8 misses on reliability, not
        // altitude). KH-3 and KH-4 get the too-flat advice (a larger pitch kick, FlatAdvice_LargerKickLandsKh3AndKh4); this fuel
        // step also lands them. KH-12's 6° build (too steep) never comes in with fuel (KH12_KickWindow_...), so it gets the steep advice.
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
                var d = id == "m6" ? Kh6RawBuild(m) : id == "m3" ? Kh3RawBuild(m) : Starter(m); // the raw builds that miss
                d.stages[d.stages.Count - 1].propMass *= k;
                var x = Fly(m, d, out var flight);
                if (flight.failure == null && (x == null || (x.Code != "apo-high" && x.Code != "peri-low" && x.Code != "orbit-low" && x.Code != "fpa-steep" && x.Code != "fpa-steep-suborbital" && x.Code != "fpa-flat")))
                    found = k;
            }
            Assert.AreEqual(first, found, 1e-9, id + ": first top-stage step that lands the circle");
        }

        /// <summary>KH-12 under #313 on #324's 32,000 kg top stage (Physics): the pitch kick is the lever, fuel is not. From the
        /// 6° build: 4.5° (the board starter) lands every batch seed and the live pad, calm and in seed-1 wind; 4° misses
        /// calm on every seed (cuts off diving, 740 x 460 km); 5° lands calm but misses the live pad in seed-1 wind
        /// (831 x 369 km, the wind-shear ease tips it steep); extra top-stage fuel alone, +4% to +21%, never lands a seed.</summary>
        [Test]
        public void KH12_KickWindow_FourPointFiveLands_FourAndFiveMiss_FuelAloneNever()
        {
            var m = Mission("m12");
            var board = Starter(m);
            Assert.AreEqual(4.5, board.guidance.pitchKickDeg, 1e-9, "board starter kick");
            AssertEverySeed(m, board, "kick 4.5°");
            var calm = LivePadLoop.Fly(catalog, m, board, false);
            var windy = LivePadLoop.Fly(catalog, m, board, true);
            Assert.IsTrue(LivePadLoop.InBand(m, calm), "4.5° live calm " + LivePadLoop.Text(calm));
            Assert.IsTrue(LivePadLoop.InBand(m, windy), "4.5° live seed-1 wind " + LivePadLoop.Text(windy));

            var four = SixDegree(m);
            four.guidance.pitchKickDeg = 4;
            foreach (ulong seed in campaign.Seeds)
            {
                var f = FlightBatch.Fly(catalog, m, four, seed);
                Assert.IsNull(f.failure, "4° seed " + seed);
                Assert.IsFalse(f.passed, "4° seed " + seed + " " + f.apoKm.ToString("0.0") + " x " + f.periKm.ToString("0.0"));
            }
            Assert.IsFalse(LivePadLoop.InBand(m, LivePadLoop.Fly(catalog, m, four, false)), "4° live calm misses");

            var five = SixDegree(m);
            five.guidance.pitchKickDeg = 5;
            var fiveWind = LivePadLoop.Fly(catalog, m, five, true);
            Assert.IsNull(fiveWind.failure);
            Assert.IsFalse(LivePadLoop.InBand(m, fiveWind), "5° live seed-1 wind misses: " + LivePadLoop.Text(fiveWind));

            var landed = new System.Collections.Generic.List<string>();
            for (int pct = 4; pct <= 21; pct++)
            {
                foreach (ulong seed in campaign.Seeds)
                {
                    var d = SixDegree(m);
                    d.stages[d.stages.Count - 1].propMass *= 1 + pct / 100.0;
                    var flight = FlightBatch.Fly(catalog, m, d, seed);
                    if (flight.passed) landed.Add("+" + pct + "% seed " + seed + " " + flight.periKm.ToString("0") + "x" + flight.apoKm.ToString("0"));
                }
            }
            Assert.That(landed, Is.Empty, "fuel alone never lands KH-12: " + string.Join(" | ", landed));
        }

        /// <summary>KH-12's starter at the template's 6° kick: the board starter with only the kick put back.</summary>
        Design SixDegree(CampaignMission m)
        {
            var d = Starter(m);
            d.guidance.pitchKickDeg = 6;
            return d;
        }

        /// <summary>The 6° build's batch miss (seed 1), pinned to its exact text: too steep, and the advice is the smaller kick.</summary>
        [Test]
        public void KH12_SixDegreeBuild_MissSaysWhyAndWhatToChange()
        {
            var m = Mission("m12");
            var x = Fly(m, SixDegree(m), out var flight);
            Assert.IsNull(flight.failure);
            Assert.IsFalse(flight.passed);
            Assert.AreEqual("fpa-steep", x.Code);
            Assert.AreEqual("Missed the 600 km circle: the orbit flew 364 km × 836 km, and the high point needs 570 km to 630 km. The low point, 364 km, needs 570 km to 630 km. The engines cut off while it was still climbing, 1.9° up.", x.Reason);
            Assert.AreEqual("Your rocket flew too steep. Use a smaller pitch kick at launch (for example 6° → 4.5° in Guidance).", x.Advice);
            Assert.AreEqual(MissReason.SteepAdvice, x.Advice);
            Assert.AreEqual(x.Reason + " " + x.Advice, x.Line);
        }

        /// <summary>The flat advice works: a larger pitch kick lands the KH-3 (8° → 8.5°, its pre-retune 8° build) and KH-4 (6° → 6.5°) starters on every seed.</summary>
        [TestCase("m3", 8.0, 8.5)]
        [TestCase("m4", 6.0, 6.5)]
        public void FlatAdvice_LargerKickLandsKh3AndKh4(string id, double from, double to)
        {
            var m = Mission(id);
            var d = id == "m3" ? Kh3RawBuild(m) : Starter(m);
            var x = Fly(m, d, out _);
            Assert.IsNotNull(x, id + " misses");
            Assert.AreEqual("fpa-flat", x.Code);
            Assert.AreEqual(MissReason.FlatAdvice, x.Advice);
            Assert.AreEqual(from, d.guidance.pitchKickDeg, 1e-9);
            d.guidance.pitchKickDeg = to;
            AssertEverySeed(m, d, id + " kick " + to + "°");
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
                Assert.AreEqual(0, x.Others.Count, "seed " + seed + ": no other gate");
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

        /// <summary>#313 (Physics): KH-12's 6° build gets the steep advice, and its lever is the smaller pitch kick; following
        /// it (4.5°) lands every seed. One more upper engine is no longer advised: on #324's 32,000 kg top stage the upper stage
        /// pushes at 0.584 g, over the 0.5 g weak-upper rule (it was 0.476 g at 40,000 kg), so weak-upper drops and Fix it
        /// no longer adds an engine. Fix it's edit (fins 1.10 → 3.10 m, max-Q throttle 95%) and the old engine advice by
        /// hand both tip it into a dive and miss every seed.</summary>
        [Test]
        public void KH12_SteepAdvice_IsTheSmallerKick_AndFollowingItPassesEverySeed()
        {
            var m = Mission("m12");
            var start = SixDegree(m);
            var x = Fly(m, start, out var raw);
            Assert.AreEqual("fpa-steep", x.Code);
            Assert.AreEqual(MissReason.SteepAdvice, x.Advice);
            Assert.Greater(raw.cutoffGammaDeg, MissReason.FlightPathAngleOffDeg);

            var kick = SixDegree(m);
            kick.guidance.pitchKickDeg = 4.5;
            AssertEverySeed(m, kick, "the advice: kick 6° → 4.5°");

            // weak-upper: 0.584 g on 32,000 kg (no warning), 0.476 g on 40,000 kg (warning). Rule: 0.5 g.
            var json = JsonMut.CloneObject(HomeAutoBuild.Choose(catalog, hangar.templates, m).Design);
            JsonMut.Set(json.Obj("guidance"), "pitchKickDeg", 6.0);
            var quick = Readout.Quick(catalog, json);
            Assert.AreEqual(0.584, quick.stages[quick.stages.Count - 1].twrVac, 0.0005);
            CollectionAssert.DoesNotContain(Validate.Engineering(catalog, json).warnings.ConvertAll(w => w.code), "weak-upper");
            var heavy = JsonMut.CloneObject(json);
            JsonMut.Set((JsonObject)heavy.Arr("stages")[1], "propMass", 40000.0);
            var heavyQuick = Readout.Quick(catalog, heavy);
            Assert.AreEqual(0.476, heavyQuick.stages[heavyQuick.stages.Count - 1].twrVac, 0.0005);
            CollectionAssert.Contains(Validate.Engineering(catalog, heavy).warnings.ConvertAll(w => w.code), "weak-upper");

            // Fix it, exactly as the board runs it, on the 6° build: no engine, and it misses.
            var fix = JsonMut.CloneObject(json);
            Readout.Reference(catalog, fix);
            var report = StepperModel.Evaluate(catalog, fix);
            StepCheck check = null;
            foreach (var c in report.Checks) if (c != null && !string.IsNullOrEmpty(c.Code)) { check = c; break; }
            Assert.IsNotNull(check);
            StepperFix.Apply(catalog, fix, check);
            var fixedByFixIt = Design.FromJson(fix);
            int top = start.stages.Count - 1;
            Assert.AreEqual(start.stages[top].engineCount, fixedByFixIt.stages[top].engineCount, "Fix it adds no upper engine (weak-upper dropped)");
            Assert.IsTrue(fixedByFixIt.guidance.maxQThrottleSet);
            Assert.AreEqual(95, fixedByFixIt.guidance.maxQThrottle, 1e-9, "Fix it sets the max-Q throttle to 95%");
            AssertEverySeedMisses(m, fixedByFixIt, "Fix it's edit");

            var byHand = SixDegree(m);
            byHand.stages[top].engineCount += 1;
            byHand.guidance.maxQThrottleSet = true;
            byHand.guidance.maxQThrottle = 95;
            AssertEverySeedMisses(m, byHand, "the old advice: one more upper engine at 95% max-Q");
        }

        void AssertEverySeedMisses(CampaignMission m, Design d, string what)
        {
            foreach (var seed in campaign.Seeds)
            {
                var f = FlightBatch.Fly(catalog, m, d, seed);
                Assert.IsNull(f.failure, what + " seed " + seed);
                Assert.IsFalse(f.passed, what + " seed " + seed + " " + f.apoKm.ToString("0.0") + " x " + f.periKm.ToString("0.0"));
            }
        }

        /// <summary>#313 (Physics): the advice's lever, a smaller pitch kick (6° to 4.5°), lands KH-12 on every seed. One more
        /// upper engine alone still flies too steep (867 x 333 km).</summary>
        [Test]
        public void KH12_SmallerPitchKickAlsoLands_AndOneEngineAloneIsNotEnough()
        {
            var m = Mission("m12");
            var kick = SixDegree(m);
            Assert.AreEqual(6, kick.guidance.pitchKickDeg, 1e-9);
            kick.guidance.pitchKickDeg = 4.5;
            AssertEverySeed(m, kick, "pitch kick 4.5°");

            var engineOnly = SixDegree(m);
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
            Assert.AreEqual("Your rocket flew too flat. Use a larger pitch kick at launch (for example 6° → 6.5° in Guidance).", flat.Advice);
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
