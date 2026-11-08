// Arcade P1 (rule from Kestrel Physics): the stage banner said "Short on speed" whenever speed was under 40 % of the
// whole Δv budget, a fitted share that read every orbital starter short for minutes of a flight that made its orbit
// (KH-7 from MECO into the S3 burn). It now reads LiveSpare: Δv on board − (speed still to gain + the sheet's loss term
// less the sim's gravity + drag + steering loss tallies so far, inertial frame), and says short only once that has
// stayed below zero for LiveSpare.HoldS.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class LiveSpareShortOnSpeedTests
    {
        /// <summary>The live pad's ascent (briefing card wind and site for the design hash and seed, seed 1 by default as the app flies, shared director), not yet flown.</summary>
        static DirectedFlight StartLive(Design d, CampaignMission m, bool autoAssist, ulong seed = 1)
        {
            var c = StarterFixture.Catalog;
            ulong hash = FlightBatch.DesignHash(d);
            var card = MissionBriefing.Load(Golden.Root, m.Index, hash, seed);
            double target = Stack.RequiredDeltaV(c, d.payloadOrbit).targetAlt;
            if (MissionAim.IsApogeeHop(m)) target = m.GuidanceAltM;
            double qLim = FailureMonitor.StructuralQLimPa(c, d);
            FailureMonitor monitor = FailureMonitor.Engages(d) ? FailureMonitor.Create(c, d, target, hash, seed) : null;
            var director = MissionFlightSetup.Director(c, d, monitor?.Events, card?.Wind, qLim, FailureMonitor.GentleTier(d),
                target, hash, seed, autoAssist, false, false, card?.Bonuses);
            var site = new MissionFlightSite { Wind = card?.Wind };
            if (card != null && card.Site != null) { site.HasLatitude = true; site.LatitudeDeg = card.Site.LatitudeDeg; }
            var setup = MissionFlightSetup.Build(c, m, d, MissionFlightSetup.LiveDt, site);
            if (monitor != null) setup.Options.monitor = monitor;
            var flight = MissionFlightSetup.Start(setup, director);
            flight.Checks = new PlayedChecks(c, d, setup.Stages, setup.Options, flight.Sim);
            return flight;
        }

        internal sealed class Reading
        {
            public double ShortS, OldShortS, MinSpare = double.PositiveInfinity, MinT, Spare0, SheetSpare, EndSpare;
            public double FirstShortT = double.NaN, OldFirstT = double.NaN, OldLastT = double.NaN;
            public bool Pass; public string Outcome = "";
            public StringBuilder Trace;
        }

        /// <summary>Fly the design on the live pad and read the banner every step the way SliceApp.RefreshWebcast does.</summary>
        internal static Reading Read(JsonObject json, CampaignMission m, bool autoAssist, double traceEvery = 0, ulong seed = 1)
        {
            var c = StarterFixture.Catalog;
            var d = Design.FromJson(json);
            var q = Readout.Quick(c, json);
            double required = q.required, hopGoal = 0;
            if (m.TargetKind == "apogee") { hopGoal = m.ApoKm * 1000.0; required = m.RequiredDeltaV(); }
            var live = LiveSpare.For(c, d.payloadOrbit);
            var f = StartLive(d, m, autoAssist, seed);
            var r = new Reading { SheetSpare = q.totalDeltaV - q.required, Spare0 = live.Spare(f.Sim) };
            if (traceEvery > 0)
            {
                r.Trace = new StringBuilder();
                r.Trace.AppendLine("     T      alt_km  v_mps  onboard  to_go  accrued(g+D+S)  allowance  needed  spare  verdict");
            }
            double nextTrace = 0, spare = r.Spare0;
            while (f.Step())
            {
                var s = f.Sample();
                var sim = f.Sim;
                spare = live.Spare(sim);
                if (s.AltM >= 10000 && spare < r.MinSpare) { r.MinSpare = spare; r.MinT = sim.T; }
                bool isShort = live.Short(sim.T, spare);
                var pulse = WebcastBoard.Pulse(true, s.AltM, s.SpeedMps, required, r.SheetSpare, 0, null, "", hopGoal, shortOnSpeed: isShort);
                if (pulse.Verdict == WebcastBoard.VerdictShort)
                {
                    r.ShortS += MissionFlightSetup.LiveDt;
                    if (double.IsNaN(r.FirstShortT)) r.FirstShortT = sim.T;
                }
                // The rule this replaces, verdict order as it was: speed under 40 % of the required Δv, from 10 km, not a hop below or past its goal.
                double frac = s.SpeedMps / Math.Max(1, required);
                bool underHop = hopGoal > 0 && s.AltM <= hopGoal * 1.05, pastHop = hopGoal > 0 && s.AltM > hopGoal * 1.08;
                if (frac < 1 && !underHop && s.AltM >= 10000 && !pastHop && frac < 0.40)
                {
                    r.OldShortS += MissionFlightSetup.LiveDt;
                    if (double.IsNaN(r.OldFirstT)) r.OldFirstT = sim.T;
                    r.OldLastT = sim.T;
                }
                if (r.Trace != null && sim.T >= nextTrace)
                {
                    nextTrace = sim.T + traceEvery;
                    var Y = sim.Y;
                    double rr = Math.Sqrt(Y[0] * Y[0] + Y[1] * Y[1]);
                    double togo = live.SpeedToGo(rr, Y[2], Y[3], Y[0], Y[1]), acc = LiveSpare.AccruedLoss(sim);
                    r.Trace.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{0,7:0.0} {1,8:0.0} {2,6:0} {3,8:0} {4,6:0} {5,12:0} {6,10:0} {7,7:0} {8,6:0}  {9}",
                        sim.T, s.AltM / 1000, Math.Sqrt(Y[2] * Y[2] + Y[3] * Y[3]), sim.RemainingVacDeltaV(), togo, acc,
                        live.LossAllowance(acc), live.Needed(togo, acc), spare, pulse.Verdict));
                }
            }
            r.EndSpare = spare;
            var res = f.Finish();
            r.Pass = LivePadLoop.Passes(c, m, d, res) || LivePadLoop.IsM13HardButWins(c, m, d, res);
            r.Outcome = res.outcome + (res.failure != null ? " " + res.failure.kind : "");
            return r;
        }

        internal static IEnumerable<CampaignMission> Starters()
        {
            StarterFixture.EnsureBases();
            foreach (var m in StarterFixture.Campaign.Missions)
                if (StarterBases.For(m.Id) != null) yield return m;
        }

        internal static CampaignMission Mission(string id)
        {
            foreach (var m in Starters()) if (m.Id == id) return m;
            Assert.Fail("no starter for " + id);
            return null;
        }

        internal static JsonObject Variant(string id, StarterEngine engine, int stages, int fuel, int boosters)
        {
            var r = StarterFixture.Rocket(id);
            r.Engine = engine; r.Stages = stages; r.FuelNotch = fuel; r.Boosters = boosters;
            return r.BuildDesign();
        }

        [Test]
        public void TheHold_IgnoresADipShorterThanThreeSeconds()
        {
            var live = LiveSpare.For(StarterFixture.Catalog, "leo400");
            Assert.AreEqual(3, LiveSpare.HoldS);
            Assert.IsFalse(live.Short(0, 5));
            Assert.IsFalse(live.Short(1, -10), "a dip starts: not short yet");
            Assert.IsFalse(live.Short(3.9, -10), "2.9 s below zero: not short yet");
            Assert.IsTrue(live.Short(4.0, -10), "3 s below zero: short");
            Assert.IsTrue(live.Short(9.0, -1), "still below zero: still short");
            Assert.IsFalse(live.Short(9.2, 0), "zero is not below zero");
            Assert.IsFalse(live.Short(9.4, -1), "a new dip starts the hold again");
            Assert.IsFalse(live.Short(12.3, -1), "2.9 s into the new dip");
            Assert.IsTrue(live.Short(12.4, -1), "3 s into the new dip");
            live.Reset();
            Assert.IsFalse(live.Short(20, -1), "a new flight starts the hold again");
        }

        [Test]
        public void TheSpare_IsOnBoardMinusSpeedToGoMinusTheUnspentLossTerm_WithNoSlack()
        {
            var live = LiveSpare.For(StarterFixture.Catalog, "leo400");
            Assert.AreEqual(Stack.LOSS_BUDGET, live.SheetLossMps, 1e-6);
            Assert.AreEqual(Stack.LOSS_BUDGET - 600, live.LossAllowance(600), 1e-9, "the losses flown come off the allowance");
            Assert.AreEqual(0, live.LossAllowance(Stack.LOSS_BUDGET + 400), "past the sheet's loss term nothing more is held back");
            Assert.AreEqual(5000 - (3000 + 900), live.Spare(5000, 3000, 600), 1e-9);
            Assert.AreEqual(-1, live.Spare(3899, 3000, 600), 1e-9, "one m/s under is short: no slack");
            Assert.AreEqual(12, live.Spare(12, 0, 0), 1e-9, "with no speed left to gain nothing more is needed");
            // A coast above the perigee costs nothing: the height is credited by vis-viva.
            double rp = live.PerigeeRadiusM, vp = live.GoalSpeedMps;
            Assert.AreEqual(0, live.SpeedToGo(rp, 0, vp, rp, 0), 1e-6);
            double r2 = rp + 50000, v2 = Math.Sqrt(vp * vp - 2 * Gravity.MU_EARTH * (1 / rp - 1 / r2));
            Assert.AreEqual(0, live.SpeedToGo(r2, 0, v2, r2, 0), 1e-6);
        }

        [Test]
        public void AtLiftoff_TheLiveSpareIsTheSheetsSpare()
        {
            var c = StarterFixture.Catalog;
            int n = 0;
            foreach (var m in Starters())
            {
                var rocket = StarterFixture.Rocket(m.Id);
                if (rocket.Check().Blocked || !rocket.Verdict().Survives) continue;
                var json = rocket.BuildDesign();
                var d = Design.FromJson(json);
                var q = Readout.Quick(c, json);
                var live = LiveSpare.For(c, d.payloadOrbit);
                Assert.AreEqual(live.Hop ? 1000 : Stack.LOSS_BUDGET, live.SheetLossMps, 1e-6, m.Id);
                var f = StartLive(d, m, true);
                // The pad's rotation differs from the sheet's 28.5° pad by the site latitude only, and the on-board Δv
                // from the sheet's total by the parallel-booster Isp blend only.
                Assert.AreEqual(q.totalDeltaV - q.required, live.Spare(f.Sim), 60, m.Id + ": liftoff spare is the sheet's spare");
                n++;
            }
            Assert.GreaterOrEqual(n, 20);
        }

        [Test]
        public void NoCampaignWinner_ReadsShortOnSpeed_OnTheLivePad()
        {
            int flown = 0;
            var bad = new List<string>();
            foreach (var m in Starters())
            {
                var rocket = StarterFixture.Rocket(m.Id);
                if (rocket.Check().Blocked || !rocket.Verdict().Survives) continue;
                var r = Read(rocket.BuildDesign(), m, MissionFlightSetup.DefaultAutoAssist);
                if (!r.Pass) continue;
                if (r.ShortS > 0) bad.Add(m.Id + " " + rocket.Choice.Key + ": " + r.ShortS.ToString("0.0") + " s short from T+"
                    + r.FirstShortT.ToString("0.0") + " (min live spare " + r.MinSpare.ToString("0") + " m/s at T+" + r.MinT.ToString("0") + ")");
                flown++;
            }
            Assert.IsEmpty(bad, "a winner is never short on speed:\n" + string.Join("\n", bad));
            Assert.GreaterOrEqual(flown, 20, "every campaign winner was flown and won");
        }

        /// <summary>
        /// Kestrel Physics' windy-seed check: every campaign winner on the windy briefing seeds (#75's known windy rows for
        /// KH-5, plus seed 1). Many of these flights miss; a miss may read short, a flight that makes it never does.
        /// One case per seed so each stays inside the Unity runner's 180 s per-test timeout: all eight seeds in one test
        /// ran 348 s on the Mono EditMode runner (about 112 s on CoreCLR). The floors are one under each seed's count on
        /// CoreCLR (passes 20/13/12/7/12/11/11/3, short misses 0/4/4/7/4/0/1/4), so together they still require more than
        /// 60 winners checked (at least 81) and some windy misses that read short, as the single test did.
        /// </summary>
        [TestCase(1UL, 19, 0)]
        [TestCase(2UL, 12, 1)]
        [TestCase(5UL, 11, 1)]
        [TestCase(6UL, 6, 1)]
        [TestCase(7UL, 11, 1)]
        [TestCase(9UL, 10, 0)]
        [TestCase(10UL, 10, 0)]
        [TestCase(12UL, 2, 1)]
        public void NoPassingFlight_ReadsShortOnSpeed_OnTheWindySeeds(ulong seed, int minPasses, int minShortMisses)
        {
            var shown = new List<string>();
            int passes = 0, shortMisses = 0;
            foreach (var m in Starters())
            {
                var r = Read(StarterFixture.Rocket(m.Id).BuildDesign(), m, MissionFlightSetup.DefaultAutoAssist, 0, seed);
                if (!r.Pass) { if (r.ShortS > 0) shortMisses++; continue; }
                passes++;
                if (r.ShortS > 0) shown.Add($"seed {seed} {m.Id}: {r.ShortS:0.0} s from T+{r.FirstShortT:0.0}, min spare {r.MinSpare:0}");
            }
            Assert.IsEmpty(shown, "a flight that wins read \"Short on speed\"");
            Assert.GreaterOrEqual(passes, minPasses, $"seed {seed} still has winners to check");
            Assert.GreaterOrEqual(shortMisses, minShortMisses, $"seed {seed}: windy misses that are short on speed say so");
        }

        [Test]
        public void KH7_TheStarterThatReadShortForMinutes_NoLongerDoes()
        {
            var m = Mission("m7");
            var r = Read(StarterFixture.Rocket("m7").BuildDesign(), m, MissionFlightSetup.DefaultAutoAssist);
            Assert.IsTrue(r.Pass, "the KH-7 starter wins");
            Assert.Greater(r.OldShortS, 30, "the old 40 % rule read it short");
            Assert.AreEqual(0, r.ShortS, "the live spare never goes below zero for 3 s");
        }

        [Test]
        public void ADesignCutDownToBeShort_ReadsShortOnSpeed()
        {
            // One kerosene stage for a 200 km circular orbit: far short on the sheet, short from early in the flight.
            var one = Read(Variant("m10", StarterEngine.Kerosene, 1, 0, 2), Mission("m10"), MissionFlightSetup.DefaultAutoAssist);
            Assert.IsFalse(one.Pass);
            Assert.Greater(one.ShortS, 30, "m10 kerosene-1-f0-b2");
            // Two stages at the lowest fuel notch for KH-17's C3 departure: misses, and the banner says why.
            var low = Read(Variant("m17", StarterEngine.Kerosene, 2, 0, 0), Mission("m17"), MissionFlightSetup.DefaultAutoAssist);
            Assert.IsFalse(low.Pass);
            Assert.Greater(low.ShortS, 30, "m17 kerosene-2-f0-b0");
        }
    }
}
