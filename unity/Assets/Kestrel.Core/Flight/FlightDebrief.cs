// Post-flight report (S7 narrator, G5 rewards). One pass through MissionScore, MedalShelf, and Career.Settle.
// The cause line and the narrator quote the same flight numbers. A free build keeps medals and writes no Kc.
// No timer, no streak, no purchase.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Economy;
using Kestrel.Core.Hangar;

namespace Kestrel.Core.Flight
{
    /// <summary>
    /// Optional gates that make the result card agree with <see cref="FlightBatch"/>.
    /// NaN and the known-flags left false skip that gate, so older callers stay on altitude and g.
    /// </summary>
    public sealed class GoalCheck
    {
        public double IdealDv = double.NaN;
        /// <summary>#313 Physics A: Δv left at the parking orbit and the departing stage's T/W (SimResult.departureDv/TW).</summary>
        public double DepartureDv = double.NaN, DepartureTW = double.NaN;
        public double C3Km2PerS2 = double.NaN;
        public double Reliability = double.NaN;
        public double Margin = double.NaN;
        public double BoilOffKg = double.NaN;
        public bool RedKnown, RedRisk;
        public string BoosterGrade;
        public bool BoosterKnown;
        public bool AirframeKnown, Gentle, PayloadMatch, FairingFits, UnderBudget;
    }

    /// <summary>What the flight actually did. The report does not invent a second score.</summary>
    public sealed class FlightFacts
    {
        public bool Passed;
        /// <summary>When false, Commit records the attempt and does not move the bank.</summary>
        public bool EconomyKnown;
        public double Margin;
        public double CostKc;
        public double BudgetKc;
        public bool LandingRequired;
        public string LandingGrade;
        /// <summary>#79: the player flew the landing burn (BoosterLanding.PlayerFlown). Only the score's
        /// landing bonus reads it; the grade, medal and stars never do.</summary>
        public bool PlayerFlownBurn;
        public double PeakG;
        public double GLimit;
        public long CostMinor;
        public long RecoveredHardwareMinor;
        /// <summary>The strap-on boosters landed well (KH-10, KH-19), so their hardware joins the reuse refund.</summary>
        public bool BoostersHome;
        public string ArticleKey;
        public string DesignId;
        public ulong Seed;
        public FailureEvent Failure;
        public AscentMedal TimingMedal;
        public bool RedundantAvionics;
        /// <summary>The flown design has no fins (design.fins false). False when unknown, so a "finless" line never plays by accident.</summary>
        public bool Finless;
        public int EngineCount;
        public double ApoKm;
        public double GoalKm;
        /// <summary>Playthrough 2026-11-11: a GTO or C3 mission (MissionRamp.DepartsFromParking). The sim flies the rocket to
        /// its parking orbit and books the transfer or departure burn on the Δv left there; that burn is not flown.</summary>
        public bool Departs;
        public string TargetKind;
        public double PeriKm = double.NaN;
        /// <summary>Δv left in the parking orbit (SimResult.departureDv) and the burn it must cover (MissionScore.DepartureRequiredMps).</summary>
        public double DepartureDvLeft = double.NaN, DepartureNeedMps = double.NaN;
        /// <summary>The flown orbit itself met the target (an ellipse apogee or a C3 actually reached).</summary>
        public bool ReachedDirectly;
        /// <summary>The flight ended parked, with the transfer or departure burn still to come.</summary>
        public bool Parked => Departs && !ReachedDirectly && !double.IsNaN(DepartureDvLeft) && !double.IsNaN(DepartureNeedMps);
        /// <summary>"transfer" (an ellipse such as GTO) or "departure" (a C3 escape).</summary>
        public string BurnName => TargetKind == "c3" ? "departure" : "transfer";

        /// <summary>Fill the departure facts from the mission row, the flown orbit, and the goal check (PostFlightSource).</summary>
        public void FillDeparture(CampaignMission row, double apoMeters, double periMeters, GoalCheck extra)
        {
            if (row == null || !row.DepartsFromParking) return;
            Departs = true;
            TargetKind = row.TargetKind;
            PeriKm = periMeters / 1000.0;
            DepartureDvLeft = extra != null ? extra.DepartureDv : double.NaN;
            DepartureNeedMps = extra != null
                ? MissionScore.DepartureRequiredMps(row.TargetKind, row.ApoKm, row.C3Km2PerS2, row.GuidanceAltM, extra.DepartureTW)
                : double.NaN;
            double c3 = extra != null ? extra.C3Km2PerS2 : double.NaN;
            ReachedDirectly = MissionScore.TargetMet(row.TargetKind, row.ApoKm, row.PeriKm, row.C3Km2PerS2, apoMeters, periMeters, c3, false, out _);
        }
        public double RequiredMps;
        /// <summary>F4: why an intact flight missed (<see cref="MissReason.Explain"/>). Null on a pass or a failure event.</summary>
        public MissExplanation Miss;
        /// <summary>DECISIONS #102: what a test flight did, for its result card. Null on a counted flight.</summary>
        public TestFlightFacts Test;

        /// <summary>
        /// Every design tag true of the flight: the avionics/engine one, finless when the design has no fins, and
        /// <see cref="ClusterOutTag"/> when an engine-out hit a stage that had three or more engines lit.
        /// </summary>
        public string[] DesignTags
        {
            get
            {
                var tags = new List<string> { DesignTag };
                if (Finless) tags.Add("finless");
                if (FailedStageEngines >= ClusterOutMinEngines) tags.Add(ClusterOutTag);
                return tags.ToArray();
            }
        }

        /// <summary>"The cluster lost an engine" is only true of a stage with at least this many engines.</summary>
        public const int ClusterOutMinEngines = 3;
        /// <summary>Design tag of the engine-out line that claims the failed stage's engine count (narr.engine.out.2).</summary>
        public const string ClusterOutTag = "engine-out-cluster";

        /// <summary>
        /// Engines lit on the stage that lost one: the engine-out failure's limit. 0 for any other failure or
        /// when unknown, so the cluster line fails closed.
        /// </summary>
        public int FailedStageEngines
            => Failure != null && Failure.kind == "engine-out" && Failure.limit >= 1 ? (int)Math.Round(Failure.limit) : 0;

        public string DesignTag
        {
            get
            {
                if (RedundantAvionics) return "redundant";
                if (EngineCount <= 1) return "single";
                return "cluster";
            }
        }
    }

    public sealed class FlightDebrief
    {
        public string CauseTitle = "Flight ended";
        public string CauseFamily = "";
        public string Kind = "";
        public string Numbers = "";
        public string Kid = "";
        public string Engineer = "";
        /// <summary>F4: <see cref="MissExplanation.Code"/> and advice when an intact flight missed. Empty otherwise.</summary>
        public string MissCode = "";
        public string MissAdvice = "";
        public string NarratorId = "";
        public string NarratorKid = "";
        public string NarratorEng = "";
        public int Stars;
        public double Score;
        public readonly List<string> Medals = new List<string>();
        public string StarsLine = "No stars this flight.";
        public string MedalsLine = "No medal this flight.";
        public string KcLine = "";
        /// <summary>Pass card hero money, "+2.0M Kc" (KcFormat.Signed of DeltaKc). Empty on a fail or before Kc settles.</summary>
        public string KcHeadline = "";
        public string CosmeticLine = "";
        public string UnlockLine = "";
        public bool Passed;
        public bool FreeBuild;
        /// <summary>DECISIONS #102: a test flight. Nothing was written, and <see cref="TestCard"/> is the card.</summary>
        public bool TestFlight;
        /// <summary>The Test Flight result card (real sim numbers). Null unless <see cref="TestFlight"/>.</summary>
        public TestFlightCard TestCard;
        public bool EconomySettled;
        public bool TriedSettle;
        public bool WroteCareer;
        public long RewardMinor, CostMinor, RefundMinor, DeltaMinor, BankMinor;
        /// <summary>Poly Bridge / DECISIONS #100: unspent-budget bonus (budget − cost) on a pass. Part of DeltaMinor.</summary>
        public long UnspentBonusMinor;
        /// <summary>DECISIONS #87: what the range covered so the bank stays at the campaign floor. Part of DeltaMinor.</summary>
        public long RangeCreditMinor;
        public double RangeCreditKc;
        public double UnspentBonusKc;
        public double RewardKc, CostKc, RefundKc, DeltaKc, BankKc;
        /// <summary>Credits this flight paid. Zero on a miss, a free build, or when no wallet is attached.</summary>
        public long CreditsEarned;
        /// <summary>Credits on hand after this flight. Zero when no wallet is attached.</summary>
        public long CreditsBalance;
        public ulong Seed;
        public string MissionId;
        public readonly List<string> Unlocks = new List<string>();
        public readonly List<string> Cosmetics = new List<string>();

        public static FlightDebrief Latest { get; private set; }

        public static void ClearLatest() => Latest = null;

        public static string TitleOf(string kind)
        {
            switch (kind)
            {
                case "structural-maxq": return "Structural breakup";
                case "engine-out":
                case "engine-overpressure":
                case "bad-mixture": return "Engine failure";
                case "tumble": return "Tumble";
                case "prop-depletion": return "Out of fuel";
                case "separation-collision": return "Stage collision";
                case "overg": return "Too many g";
                case "thermal": return "Overheating";
                case "cryo-material": return "Cold tank crack";
                case "leg-collapse": return "Legs folded";
                case "recovery-fuel": return "Landing fuel ran out";
                case "recovery-fast": return "Too fast to land";
                case "recovery-tilt": return "Tilted touchdown";
                case "recovery-miss": return "Missed Haven";
                case "pass": return "She flew";
                case "miss": return "Wrong place";
                default: return "Flight ended";
            }
        }

        public static string FamilyOf(string kind)
        {
            switch (kind)
            {
                case "structural-maxq": return "structural-breakup";
                case "engine-out":
                case "engine-overpressure":
                case "bad-mixture": return "engine-failure";
                case "tumble": return "tumble";
                case "prop-depletion": return "out-of-fuel";
                case "separation-collision": return "stage-collision";
                case "recovery-fuel": return "out-of-fuel";
                case "recovery-fast": return "too-fast";
                case "recovery-tilt": return "tilted";
                case "recovery-miss": return "missed-barge";
                default: return kind ?? "";
            }
        }

        /// <summary>
        /// Intact and on the mission target. Landing and g limits use the mission row.
        /// A breakup is not a pass. Budget is a star term, not this gate, unless <paramref name="extra"/>
        /// carries the airframe flags the batch scorer uses.
        /// </summary>
        public static bool GoalMet(CampaignMission row, bool intact, double apoMeters, double periMeters, double maxG, string landingGrade)
            => GoalMet(row, intact, apoMeters, periMeters, maxG, landingGrade, null);

        /// <summary>
        /// Same gates as <see cref="FlightBatch"/> once <paramref name="extra"/> is filled:
        /// ellipse and C3 may pass on the parking periapsis plus the departure Δv actually left there (#313 Physics A,
        /// <see cref="DepartureMet"/>), C3 may pass on energy,
        /// and reliability, red risks, margin, coast, and booster grade are mission gates.
        /// A null extra keeps the altitude, g, and landing checks only.
        /// </summary>
        public static bool GoalMet(CampaignMission row, bool intact, double apoMeters, double periMeters, double maxG, string landingGrade, GoalCheck extra)
        {
            if (!intact || row == null) return false;
            bool dv = DepartureMet(row, extra);
            double c3 = extra != null ? extra.C3Km2PerS2 : double.NaN;
            if (!MissionScore.TargetMet(row.TargetKind, row.ApoKm, row.PeriKm, row.C3Km2PerS2, apoMeters, periMeters, c3, dv, out _)) return false;
            if (!double.IsNaN(row.MaxG) && maxG > row.MaxG) return false;
            if (row.LandingRequired && !MissionScore.IsGood(landingGrade)) return false;
            string boosters = extra != null && extra.BoosterKnown ? extra.BoosterGrade : landingGrade;
            if (row.LandBoosters && !MissionScore.IsGood(boosters)) return false;
            if (extra == null) return true;
            if (row.MarginNonNegative && !double.IsNaN(extra.Margin) && !(extra.Margin >= 0)) return false;
            if (!double.IsNaN(row.ReliabilityMin) && !double.IsNaN(extra.Reliability) && !(extra.Reliability >= row.ReliabilityMin)) return false;
            if (row.NoRedRisks && extra.RedKnown && extra.RedRisk) return false;
            if (row.CoastHours > 0 && !double.IsNaN(extra.BoilOffKg) && !(extra.BoilOffKg >= 0)) return false;
            if (extra.AirframeKnown && (extra.Gentle || !extra.PayloadMatch || !extra.FairingFits || !extra.UnderBudget)) return false;
            return true;
        }

        /// <summary>
        /// #313 Physics A: an ellipse or C3 target passes on its parking perigee only if the Δv actually left there covers
        /// the impulsive departure burn plus the finite-burn loss (<see cref="MissionScore.DepartureRequiredMps"/>).
        /// </summary>
        public static bool DepartureMet(CampaignMission row, GoalCheck extra)
        {
            if (row == null || extra == null || !row.DepartsFromParking || double.IsNaN(extra.DepartureDv)) return false;
            double need = MissionScore.DepartureRequiredMps(row.TargetKind, row.ApoKm, row.C3Km2PerS2, row.GuidanceAltM, extra.DepartureTW);
            return !double.IsNaN(need) && extra.DepartureDv + 1e-6 >= need;
        }

        /// <summary>The batch row, in the shape <see cref="GoalMet"/> reads. Call after the flight fields are filled.</summary>
        public static GoalCheck FromFlight(CampaignFlight flight)
        {
            if (flight == null) return new GoalCheck();
            return new GoalCheck
            {
                IdealDv = flight.idealDv,
                DepartureDv = flight.departDv,
                DepartureTW = flight.departTW,
                C3Km2PerS2 = flight.c3Km2PerS2,
                Reliability = flight.reliability,
                Margin = flight.margin,
                BoilOffKg = flight.boilOffKg,
                RedKnown = true,
                RedRisk = flight.redRisk,
                BoosterKnown = true,
                BoosterGrade = flight.boosterGrade,
                AirframeKnown = true,
                Gentle = flight.gentle,
                PayloadMatch = flight.payloadMatch,
                FairingFits = flight.fairingFits,
                UnderBudget = flight.underBudget,
            };
        }

        public static FlightDebrief Compose(CareerStore store, MissionContext mission, FlightFacts facts, NarrationPool pool, long minorPerKc, HangarData look)
            => Build(store, mission, facts, pool, minorPerKc, look, false);

        public static FlightDebrief Commit(CareerStore store, MissionContext mission, FlightFacts facts, NarrationPool pool, long minorPerKc, HangarData look)
            => Build(store, mission, facts, pool, minorPerKc, look, true);

        static FlightDebrief Build(CareerStore store, MissionContext mission, FlightFacts facts, NarrationPool pool, long minorPerKc, HangarData look, bool write)
        {
            if (facts == null) facts = new FlightFacts();
            if (pool == null) pool = NarrationPool.Empty;
            if (minorPerKc < 1) minorPerKc = 1;
            var ev = facts.Failure;
            // DECISIONS #102: a test flight (Home's Test Flight or a mission's "Test it") writes nothing, like a free build,
            // and earns no stars or medals.
            bool test = mission != null && mission.TestFlight;
            bool free = test || mission == null || mission.IsFree || mission.Mode == MissionMode.Free;
            // A booster miss explains itself. It fails the mission when landing was the job.
            // An orbit that did not ask for the booster still counts.
            bool recovery = ev != null && ev.kind != null && ev.kind.StartsWith("recovery-", System.StringComparison.Ordinal);
            bool passed = facts.Passed && (ev == null || (recovery && !facts.LandingRequired));
            string kind = ev != null && !string.IsNullOrEmpty(ev.kind) ? ev.kind : (passed ? "pass" : "miss");
            string cause = ev != null ? ev.cause : "";
            var debrief = new FlightDebrief
            {
                Kind = kind,
                CauseTitle = TitleOf(kind),
                CauseFamily = FamilyOf(kind),
                Kid = ev != null && !string.IsNullOrEmpty(ev.kid) ? ev.kid : "",
                Engineer = ev != null && !string.IsNullOrEmpty(ev.engineer) ? ev.engineer : "",
                Passed = passed,
                FreeBuild = free,
                TestFlight = test,
                Seed = facts.Seed != 0 ? facts.Seed : (mission != null ? mission.Seed : 0),
                MissionId = mission != null ? mission.MissionId : null,
            };
            if (pool.Title(kind) != "Flight ended") debrief.CauseTitle = pool.Title(kind);
            if (!string.IsNullOrEmpty(pool.Family(kind))) debrief.CauseFamily = pool.Family(kind);

            // F4: an intact miss says why, with the flight's numbers, and what to change. The pool's
            // "Reached {apo} km" lines would contradict an orbit that overshot or a gate past the target.
            var miss = !passed && ev == null ? facts.Miss : null;
            debrief.Numbers = miss != null ? miss.Reason : NumberLine(ev, facts, passed);
            if (miss != null)
            {
                debrief.Engineer = miss.Advice;
                debrief.MissCode = miss.Code;
                debrief.MissAdvice = miss.Advice;
            }
            bool perfectLanding = MissionScore.IsPerfect(facts.LandingGrade);
            double costKc = facts.CostKc;
            if (costKc == 0 && facts.CostMinor > 0) costKc = facts.CostMinor / (double)minorPerKc;
            int stars = test ? 0 : MissionScore.Stars(passed, facts.Margin, costKc, facts.BudgetKc, facts.LandingRequired, perfectLanding, facts.PeakG, facts.GLimit);
            if (passed && !test) stars = AscentDecisions.ApplyStars(stars, facts.TimingMedal);
            double score = MissionScore.Points(passed, facts.Margin, costKc, facts.BudgetKc, facts.LandingGrade, facts.PlayerFlownBurn);
            debrief.Stars = stars;
            debrief.Score = score;
            // Trevor 11-07: "No stars this flight." next to "Gold for timing." read as a contradiction. Stars come from the
            // mission goal; a medal is for how the calls were timed (or the landing), and a missed goal can still earn one.
            debrief.StarsLine = stars <= 0
                ? (passed ? "No stars this flight." : MissedStarsLine)
                : stars == 1
                    ? "1 star."
                    : stars.ToString(CultureInfo.InvariantCulture) + " stars.";
            if (!test) CollectMedals(facts, debrief);
            debrief.MedalsLine = MedalSentence(debrief.Medals, passed);

            string narrKind = kind;
            // A parked GTO/C3 win picks the "departure" pass lines: the generic ones quote apogee against the goal.
            string narrCause = passed && ev == null && facts.Parked ? "departure" : cause;
            var line = pool.Pick(narrKind, narrCause, facts.DesignTags, debrief.Seed);
            if (miss != null)
            {
                debrief.NarratorKid = "";
                debrief.NarratorEng = "";
            }
            else if (line != null)
            {
                debrief.NarratorId = line.Id;
                debrief.NarratorKid = Fill(line.Kid, ev, facts, debrief);
                debrief.NarratorEng = Fill(line.Eng, ev, facts, debrief);
            }
            else
            {
                debrief.NarratorKid = debrief.Numbers.Length > 0 ? debrief.Numbers : debrief.Kid;
                debrief.NarratorEng = debrief.Engineer;
            }

            long costMinor = facts.CostMinor;
            if (costMinor == 0 && facts.CostKc > 0) costMinor = Career.KcToMinor(facts.CostKc, minorPerKc);
            debrief.CostMinor = costMinor;
            debrief.CostKc = minorPerKc > 0 ? costMinor / (double)minorPerKc : costKc;

            bool campaign = !free && mission != null && (mission.Mode == MissionMode.Campaign || mission.Mode == MissionMode.Tutorial);
            CampaignMission row = null;
            if (campaign && store != null && store.Career != null && store.Career.Campaign != null)
                row = store.Career.Campaign.ById(mission.MissionId);
            string label = mission != null && !string.IsNullOrEmpty(mission.Label) ? mission.Label : (row != null ? MissionContext.LabelFor(row.Index) : "");

            if (write && store != null && campaign && row != null && facts.EconomyKnown)
            {
                debrief.TriedSettle = true;
                var before = new HashSet<string>(store.Career.Unlocks);
                var settled = store.Career.Settle(row.Index, passed, costMinor, passed ? facts.RecoveredHardwareMinor : 0, facts.ArticleKey);
                debrief.EconomySettled = settled.accepted;
                debrief.RewardMinor = settled.rewardMinor;
                debrief.UnspentBonusMinor = settled.unspentBonusMinor;
                debrief.RefundMinor = settled.refundMinor;
                debrief.RangeCreditMinor = settled.rangeCreditMinor;
                debrief.DeltaMinor = settled.deltaMinor;
                debrief.BankMinor = settled.accepted ? settled.bankMinor : store.Career.BankMinor;
                if (settled.accepted) debrief.CostMinor = settled.costMinor;
                foreach (var id in store.Career.Unlocks)
                    if (!before.Contains(id)) debrief.Unlocks.Add(id);
            }
            else if (store != null)
                debrief.BankMinor = store.Career.BankMinor;

            if (write && store != null && !free)
                GrantLooks(look, store.Career, label, passed, debrief.Cosmetics);

            if (write && store != null && store.Wallet != null && campaign && row != null && passed)
            {
                debrief.CreditsEarned = store.Wallet.Award(debrief.MissionId, row.Index, true, debrief.Medals);
                debrief.CreditsBalance = store.Wallet.Balance;
                store.NoteMission(debrief.MissionId);
            }
            else if (store != null && store.Wallet != null)
                debrief.CreditsBalance = store.Wallet.Balance;

            if (write && store != null && !free && !string.IsNullOrEmpty(debrief.MissionId))
            {
                long? seed = debrief.Seed <= (ulong)long.MaxValue ? (long?)debrief.Seed : null;
                store.RecordAttempt(debrief.MissionId, debrief.Stars, debrief.Score, debrief.Medals, seed, facts.DesignId);
                debrief.WroteCareer = true;
                debrief.BankMinor = store.Career.BankMinor;
            }

            debrief.RewardKc = debrief.RewardMinor / (double)minorPerKc;
            debrief.UnspentBonusKc = debrief.UnspentBonusMinor / (double)minorPerKc;
            debrief.RefundKc = debrief.RefundMinor / (double)minorPerKc;
            debrief.RangeCreditKc = debrief.RangeCreditMinor / (double)minorPerKc;
            debrief.DeltaKc = debrief.DeltaMinor / (double)minorPerKc;
            debrief.BankKc = debrief.BankMinor / (double)minorPerKc;
            if (debrief.EconomySettled)
                debrief.CostKc = debrief.CostMinor / (double)minorPerKc;
            debrief.KcLine = test ? TestFlightCopy.Rules : KcSentence(debrief, facts, free);
            if (test)
            {
                debrief.TestCard = Kestrel.Core.Flight.TestFlight.Card(facts.Test ?? new TestFlightFacts { Failure = facts.Failure });
                debrief.StarsLine = "";
                debrief.MedalsLine = "";
            }
            debrief.KcHeadline = KcHeadlineOf(debrief, free);
            debrief.UnlockLine = debrief.Unlocks.Count > 0 ? "New part: " + string.Join(", ", debrief.Unlocks) + "." : "";
            debrief.CosmeticLine = CosmeticSentence(debrief.Cosmetics);

            Latest = debrief;
            return debrief;
        }

        /// <summary>
        /// Playthrough 2026-11-11: "Air pushed 25 kPa. The walls held 25 kPa." from 25.06 vs 25.00 kPa. Two values shown
        /// side by side are rounded to whole numbers unless that makes them read equal; then one decimal, then two, then
        /// three (1 Pa), the first precision where they differ. Values that are equal to 1 Pa read equal.
        /// </summary>
        public static void PairText(double a, double b, out string aText, out string bText) => PairText(a, b, 0, out aText, out bText);

        /// <summary>
        /// <see cref="PairText(double, double, out string, out string)"/> from <paramref name="decimals"/> places: both values
        /// always print at the same precision ("8.04 g past the 8.00 g limit", never "8.00 g past 8.0 g"), and up to three
        /// more places are added only while the two would read equal.
        /// </summary>
        public static void PairText(double a, double b, int decimals, out string aText, out string bText)
        {
            if (decimals < 0) decimals = 0;
            aText = Fixed(a, decimals);
            bText = Fixed(b, decimals);
            if (aText != bText || double.IsNaN(a) || double.IsNaN(b) || a == b) return;
            for (int d = decimals + 1; d <= decimals + 3; d++)
            {
                aText = Fixed(a, d);
                bText = Fixed(b, d);
                if (aText != bText) return;
            }
        }

        static string Fixed(double x, int decimals)
            => decimals == 0 ? N0(x) : x.ToString("0." + new string('0', decimals), CultureInfo.InvariantCulture);

        /// <summary>Air load against the wall, in kPa, as <see cref="PairText"/> prints the pair.</summary>
        public static void KpaPair(double valuePa, double limitPa, out string q, out string qlim) =>
            PairText(valuePa / 1000.0, limitPa / 1000.0, out q, out qlim);

        public static string NumberLine(FailureEvent ev, FlightFacts facts, bool passed)
        {
            if (ev == null)
            {
                // Playthrough 2026-11-11: a GTO or C3 win read "Apogee 204 km. Goal 35786 km." (or "Goal 0 km."). The rocket
                // is parked; say where, and the Δv left for the burn the sim books but does not fly.
                if (facts != null && facts.Parked)
                {
                    PairText(facts.DepartureDvLeft, facts.DepartureNeedMps, out var left, out var need);
                    string burn = facts.BurnName;
                    if (passed)
                        return "Parked " + ParkText(facts) + " with " + left + " m/s left. The " + burn + " burn needs " + need + " m/s and isn't flown.";
                    return "Parked " + ParkText(facts) + " with " + left + " m/s left. The " + burn + " burn needs " + need + " m/s.";
                }
                double apo = facts != null ? facts.ApoKm : 0;
                double goal = facts != null ? facts.GoalKm : 0;
                if (passed) return "Apogee " + N0(apo) + " km. Goal " + N0(goal) + " km.";
                return "Reached " + N0(apo) + " km. The goal was " + N0(goal) + " km.";
            }
            switch (ev.kind)
            {
                case "structural-maxq" when ev.cause == "q":
                    KpaPair(ev.value, ev.limit, out var airQ, out var wallQ);
                    return "Air pushed " + airQ + " kPa. The walls held " + wallQ + " kPa.";
                case "structural-maxq":
                    return "At T+" + N0(ev.t) + " s. Load margin " + N2(ev.value) + ".";
                case "engine-overpressure":
                    PairText(ev.value, ev.limit, 0, out var bar, out var rated);
                    return "Chamber pressure " + bar + " bar. Rated " + rated + " bar.";
                case "bad-mixture":
                    PairText(Math.Abs(ev.value), Math.Abs(ev.limit), 1, out var mixOff, out var band);
                    return "Mixture " + mixOff + "% off. The band is " + band + "%.";
                case "engine-out":
                    return N0(ev.value) + (Math.Abs(ev.value - 1) < 0.001 ? " engine quit out of " : " engines quit out of ") + N0(ev.limit) + ".";
                case "tumble" when ev.cause == "alpha":
                    PairText(ev.value, ev.limit, 0, out var side, out var sideLim);
                    return side + " degrees sideways. The limit is " + sideLim + " degrees.";
                case "tumble":
                    return "Stability " + N2(ev.value) + " cal at T+" + N0(ev.t) + " s.";
                case "prop-depletion":
                    string fuel = "Out of fuel at T+" + N0(ev.t) + " s.";
                    if (facts != null && facts.Margin < 0 && facts.RequiredMps > 0)
                        fuel += " Short " + N0(-facts.Margin * facts.RequiredMps) + " m/s.";
                    return fuel;
                case "separation-collision":
                    PairText(ev.value, ev.limit, 2, out var parted, out var needed);
                    return "Parted at " + parted + " m/s. Needed " + needed + " m/s.";
                case "overg":
                    PairText(ev.value, ev.limit, 1, out var peakG, out var limG);
                    return "The push reached " + peakG + " g, past the " + limG + " g limit.";
                case "thermal" when ev.cause != "base-heating":
                    PairText(ev.value, ev.limit, 0, out var skin, out var handles);
                    return skin + " K. The material handles " + handles + " K.";
                case "thermal":
                    return "Base heating at T+" + N0(ev.t) + " s. More than " + N0(ev.limit) + " engines.";
                case "leg-collapse":
                    PairText(ev.value, ev.limit, 1, out var falling, out var legs);
                    return "Falling at " + falling + " m/s. The legs take " + legs + " m/s.";
                case "recovery-fuel":
                    PairText(ev.value, ev.limit, 1, out var sink, out var hold);
                    return "Landing fuel gone. Sink " + sink + " m/s. The legs hold " + hold + " m/s.";
                case "recovery-fast":
                    PairText(ev.value, ev.limit, 1, out var touch, out var holds);
                    return "Touchdown " + touch + " m/s. The legs hold " + holds + " m/s.";
                case "recovery-tilt":
                    PairText(ev.value, ev.limit, 1, out var tilt, out var tiltLim);
                    return "Tilt " + tilt + "°. The limit is " + tiltLim + "°.";
                case "recovery-miss":
                    PairText(System.Math.Abs(ev.value), ev.limit, 0, out var missed, out var deck);
                    return "Missed by " + missed + " m. The deck reaches " + deck + " m.";
                case "cryo-material":
                    return "The tank cracked on the pad. T+" + N0(ev.t) + " s.";
                default:
                    return "Value " + N2(ev.value) + ". Limit " + N2(ev.limit) + ". T+" + N0(ev.t) + " s.";
            }
        }

        /// <summary>"at 204 km", or "in a 180 × 204 km orbit" when perigee and apogee round apart.</summary>
        public static string ParkText(FlightFacts facts)
        {
            string apo = N0(facts.ApoKm);
            string peri = double.IsNaN(facts.PeriKm) ? apo : N0(facts.PeriKm);
            return peri == apo ? "at " + apo + " km" : "in a " + peri + " × " + apo + " km orbit";
        }

        public static string Fill(string template, FailureEvent ev, FlightFacts facts, FlightDebrief debrief)
        {
            if (string.IsNullOrEmpty(template)) return "";
            double value = ev != null ? ev.value : 0;
            double limit = ev != null ? ev.limit : 0;
            double t = ev != null ? ev.t : 0;
            double apo = facts != null ? facts.ApoKm : 0;
            double goal = facts != null ? facts.GoalKm : 0;
            int stars = debrief != null ? debrief.Stars : 0;
            string s = template;
            KpaPair(value, limit, out var qText, out var qlimText);
            s = s.Replace("{q}", qText);
            s = s.Replace("{qlim}", qlimText);
            s = s.Replace("{qPa}", value.ToString("0.0", CultureInfo.InvariantCulture));
            s = s.Replace("{qlimPa}", limit.ToString("0.0", CultureInfo.InvariantCulture));
            s = s.Replace("{t}", N0(t));
            // Playthrough 2026-11-11: a value and its limit print at one precision, with more places only while they read equal.
            PairText(value, limit, 2, out var valueText, out var limitText);
            PairText(value, limit, 0, out var n0Text, out var n0LimText);
            PairText(value, limit, 1, out var gText, out var gLimText);
            s = s.Replace("{value}", valueText);
            s = s.Replace("{limit}", limitText);
            s = s.Replace("{n0lim}", n0LimText);
            s = s.Replace("{n0}", n0Text);
            PairText(Math.Abs(value), Math.Abs(limit), 1, out var n1Text, out var n1LimText);
            s = s.Replace("{n1lim}", n1LimText);
            s = s.Replace("{n1}", n1Text);
            s = s.Replace("{glim}", gLimText);
            s = s.Replace("{g}", gText);
            s = s.Replace("{stars}", stars.ToString(CultureInfo.InvariantCulture));
            s = s.Replace("{apo}", N0(apo));
            s = s.Replace("{goal}", N0(goal));
            if (s.IndexOf('{') >= 0)
            {
                // The departure lines are picked only for a parked GTO/C3 pass (FlightFacts.Parked); elsewhere the tokens
                // still fill, so no brace ever reaches the player.
                var f = facts ?? new FlightFacts();
                double dvLeft = double.IsNaN(f.DepartureDvLeft) ? 0 : f.DepartureDvLeft;
                double dvNeed = double.IsNaN(f.DepartureNeedMps) ? 0 : f.DepartureNeedMps;
                PairText(dvLeft, dvNeed, out var left, out var need);
                s = s.Replace("{park}", ParkText(f));
                s = s.Replace("{dvleft}", left);
                s = s.Replace("{dvneed}", need);
                s = s.Replace("{burn}", f.BurnName);
            }
            return s;
        }

        public static string CosmeticName(string id)
        {
            switch (id)
            {
                case "decal_thrums_toolbox": return "Thrum's Toolbox";
                case "decal_tidy_pin": return "Tidy Pin";
                case "decal_homecoming": return "Homecoming";
                case "decal_moon_patch": return "Moon patch";
                default: return id ?? "";
            }
        }

        static void CollectMedals(FlightFacts facts, FlightDebrief debrief)
        {
            string landing = LandingMedal(facts.LandingGrade);
            if (landing != null) debrief.Medals.Add(landing);
            string timing = TimingMedal(facts.TimingMedal);
            if (timing != null) debrief.Medals.Add(timing);
        }

        static string LandingMedal(string grade)
        {
            if (string.IsNullOrEmpty(grade)) return null;
            string name = grade;
            if (MissionScore.IsPerfect(grade)) name = "Perfect";
            else if (grade == "good" || grade == "Good") name = "Good";
            else if (grade == "hard" || grade == "Hard") name = "Hard";
            else if (grade == "salvage" || grade == "Salvage") name = "Salvage";
            else return null;
            return MedalShelf.From(new TouchdownGrade(name, default)).Medal;
        }

        static string TimingMedal(AscentMedal medal)
        {
            if (medal == AscentMedal.Gold) return "timing-gold";
            if (medal == AscentMedal.Silver) return "timing-silver";
            if (medal == AscentMedal.Bronze) return "timing-bronze";
            return null;
        }

        /// <summary>The stars line of a flight that missed its goal: stars are for the goal, so say that, not "this flight".</summary>
        public const string MissedStarsLine = "No stars: stars need the mission goal.";

        /// <summary>"Gold for the landing. Silver for timing." on a pass. On a miss the same medals read as still earned
        /// ("You still earned gold for timing."), so the line does not contradict the stars line above it.</summary>
        public static string MedalSentence(List<string> medals, bool passed = true)
        {
            if (medals == null || medals.Count == 0) return "No medal this flight.";
            var parts = new List<string>();
            for (int i = 0; i < medals.Count; i++)
            {
                string medal = medals[i];
                if (medal == "gold") parts.Add("gold for the landing");
                else if (medal == "silver") parts.Add("silver for the landing");
                else if (medal == "bronze") parts.Add("bronze for the landing");
                else if (medal == "timing-gold") parts.Add("gold for timing");
                else if (medal == "timing-silver") parts.Add("silver for timing");
                else if (medal == "timing-bronze") parts.Add("bronze for timing");
            }
            if (parts.Count == 0) return "No medal this flight.";
            if (!passed) return "You still earned " + string.Join(" and ", parts) + ".";
            for (int i = 0; i < parts.Count; i++) parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1) + ".";
            return string.Join(" ", parts);
        }

        static void GrantLooks(HangarData look, Career career, string label, bool passed, List<string> into)
        {
            if (look == null || career == null || !passed || string.IsNullOrEmpty(label)) return;
            for (int i = 0; i < look.decals.Count; i++)
            {
                var decal = look.decals[i];
                if (decal == null || decal.earn == null || decal.earn.type != "mission") continue;
                if (decal.earn.missionId != label || string.IsNullOrEmpty(decal.id)) continue;
                if (career.HasUnlock(decal.id)) continue;
                career.Unlocks.Add(decal.id);
                into.Add(decal.id);
            }
        }

        static string CosmeticSentence(List<string> ids)
        {
            if (ids == null || ids.Count == 0) return "";
            var names = new List<string>();
            for (int i = 0; i < ids.Count; i++) names.Add(CosmeticName(ids[i]));
            return "New look: " + string.Join(", ", names) + ". Free, earned, yours.";
        }

        static string KcSentence(FlightDebrief d, FlightFacts facts, bool free)
        {
            if (free) return "Free build. No Kc, and no unlocks. Medals still count.";
            if (facts == null || !facts.EconomyKnown) return "Kc waits until the build cost is known.";
            // DECISIONS #87: when the range covered part of the bill, the card says so in the same plain words.
            string range = d.RangeCreditMinor > 0 ? " Range credit " + Kc(d.RangeCreditKc) + "." : "";
            if (d.EconomySettled && d.Passed)
            {
                string bonus = d.UnspentBonusMinor > 0 ? " Unspent bonus " + Kc(d.UnspentBonusKc) + "." : "";
                return "Reward " + Kc(d.RewardKc) + "." + bonus + " Hardware " + Kc(d.CostKc) + ". Recovery " + Kc(d.RefundKc) + "." + range + " Bank " + Kc(d.BankKc) + ".";
            }
            if (d.EconomySettled)
                return "No reward. Hardware " + Kc(d.CostKc) + "." + range + " Bank " + Kc(d.BankKc) + ".";
            if (d.TriedSettle)
                return "Stars saved. Kc was settled the first time this mission opened.";
            return "Kc is not posted from this screen.";
        }

        /// <summary>Trevor 11-07: "Hardware 7612.17 Kc. Bank 8846363.3 Kc." printed raw doubles. The card writes Kc the way Home
        /// and the designer strip do (HomeRecord.Kc, CostModel.FormatKc): whole Kc with separators under 1,000, then
        /// "7.6k Kc", "8.85M Kc".</summary>
        public static string Kc(double kc)
            => double.IsNaN(kc) || double.IsInfinity(kc) ? "0 Kc" : HomeRecord.Kc((long)Math.Round(kc, MidpointRounding.AwayFromZero));

        /// <summary>#416 item 3: the pass card's big money line, "+2.0M Kc": empty unless the pass settled Kc.</summary>
        static string KcHeadlineOf(FlightDebrief d, bool free)
            => !free && d.EconomySettled && d.Passed ? KcFormat.Signed(d.DeltaKc) + " Kc" : "";

        static string N0(double x) => Math.Round(x).ToString(CultureInfo.InvariantCulture);
        static string N1(double x) => x.ToString("0.0", CultureInfo.InvariantCulture);
        static string N2(double x) => x.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
