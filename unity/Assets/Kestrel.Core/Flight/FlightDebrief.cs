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
        public double PeakG;
        public double GLimit;
        public long CostMinor;
        public long RecoveredHardwareMinor;
        public string ArticleKey;
        public string DesignId;
        public ulong Seed;
        public FailureEvent Failure;
        public AscentMedal TimingMedal;
        public bool RedundantAvionics;
        public int EngineCount;
        public double ApoKm;
        public double GoalKm;
        public double RequiredMps;

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
        public string NarratorId = "";
        public string NarratorKid = "";
        public string NarratorEng = "";
        public int Stars;
        public double Score;
        public readonly List<string> Medals = new List<string>();
        public string StarsLine = "No stars this flight.";
        public string MedalsLine = "No medal this flight.";
        public string KcLine = "";
        public string CosmeticLine = "";
        public string UnlockLine = "";
        public bool Passed;
        public bool FreeBuild;
        public bool EconomySettled;
        public bool TriedSettle;
        public bool WroteCareer;
        public long RewardMinor, CostMinor, RefundMinor, DeltaMinor, BankMinor;
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
        /// ellipse and C3 may pass on periapsis plus ideal Δv, C3 may pass on energy,
        /// and reliability, red risks, margin, coast, and booster grade are mission gates.
        /// A null extra keeps the altitude, g, and landing checks only.
        /// </summary>
        public static bool GoalMet(CampaignMission row, bool intact, double apoMeters, double periMeters, double maxG, string landingGrade, GoalCheck extra)
        {
            if (!intact || row == null) return false;
            bool apo = MissionScore.AltitudeWithin(apoMeters, row.ApoKm * 1000);
            bool peri = MissionScore.AltitudeWithin(periMeters, row.PeriKm * 1000);
            bool dv = extra != null && !double.IsNaN(extra.IdealDv) && extra.IdealDv + 1e-6 >= row.RequiredDeltaV();
            bool hit = false;
            if (row.TargetKind == "apogee") hit = apo;
            else if (row.TargetKind == "circular") hit = apo && peri;
            else if (row.TargetKind == "ellipse") hit = (apo && peri) || (peri && dv);
            else if (row.TargetKind == "c3")
            {
                bool energy = extra != null && !double.IsNaN(extra.C3Km2PerS2)
                    && MissionScore.WithinFraction(extra.C3Km2PerS2, row.C3Km2PerS2, MissionScore.AltitudeFraction);
                hit = energy || (peri && dv);
            }
            if (!hit) return false;
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

        /// <summary>The batch row, in the shape <see cref="GoalMet"/> reads. Call after the flight fields are filled.</summary>
        public static GoalCheck FromFlight(CampaignFlight flight)
        {
            if (flight == null) return new GoalCheck();
            return new GoalCheck
            {
                IdealDv = flight.idealDv,
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
            bool free = mission == null || mission.IsFree || mission.Mode == MissionMode.Free;
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
                Seed = facts.Seed != 0 ? facts.Seed : (mission != null ? mission.Seed : 0),
                MissionId = mission != null ? mission.MissionId : null,
            };
            if (pool.Title(kind) != "Flight ended") debrief.CauseTitle = pool.Title(kind);
            if (!string.IsNullOrEmpty(pool.Family(kind))) debrief.CauseFamily = pool.Family(kind);

            debrief.Numbers = NumberLine(ev, facts, passed);
            bool perfectLanding = MissionScore.IsPerfect(facts.LandingGrade);
            double costKc = facts.CostKc;
            if (costKc == 0 && facts.CostMinor > 0) costKc = facts.CostMinor / (double)minorPerKc;
            int stars = MissionScore.Stars(passed, facts.Margin, costKc, facts.BudgetKc, facts.LandingRequired, perfectLanding, facts.PeakG, facts.GLimit);
            if (passed) stars = AscentDecisions.ApplyStars(stars, facts.TimingMedal);
            double score = MissionScore.Points(passed, facts.Margin, costKc, facts.BudgetKc, facts.LandingGrade);
            debrief.Stars = stars;
            debrief.Score = score;
            debrief.StarsLine = stars <= 0
                ? "No stars this flight."
                : stars == 1
                    ? "1 star."
                    : stars.ToString(CultureInfo.InvariantCulture) + " stars.";
            CollectMedals(facts, debrief);
            debrief.MedalsLine = MedalSentence(debrief.Medals);

            string narrKind = kind;
            var line = pool.Pick(narrKind, cause, facts.DesignTag, debrief.Seed);
            if (line != null)
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
                debrief.RefundMinor = settled.refundMinor;
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
            debrief.RefundKc = debrief.RefundMinor / (double)minorPerKc;
            debrief.DeltaKc = debrief.DeltaMinor / (double)minorPerKc;
            debrief.BankKc = debrief.BankMinor / (double)minorPerKc;
            if (debrief.EconomySettled)
                debrief.CostKc = debrief.CostMinor / (double)minorPerKc;
            debrief.KcLine = KcSentence(debrief, facts, free);
            debrief.UnlockLine = debrief.Unlocks.Count > 0 ? "New part: " + string.Join(", ", debrief.Unlocks) + "." : "";
            debrief.CosmeticLine = CosmeticSentence(debrief.Cosmetics);

            Latest = debrief;
            return debrief;
        }

        public static string NumberLine(FailureEvent ev, FlightFacts facts, bool passed)
        {
            if (ev == null)
            {
                double apo = facts != null ? facts.ApoKm : 0;
                double goal = facts != null ? facts.GoalKm : 0;
                if (passed) return "Apogee " + N0(apo) + " km. Goal " + N0(goal) + " km.";
                return "Reached " + N0(apo) + " km. The goal was " + N0(goal) + " km.";
            }
            switch (ev.kind)
            {
                case "structural-maxq" when ev.cause == "q":
                    return "Air pushed " + N0(ev.value / 1000.0) + " kPa. The walls held " + N0(ev.limit / 1000.0) + " kPa.";
                case "structural-maxq":
                    return "At T+" + N0(ev.t) + " s. Load margin " + N2(ev.value) + ".";
                case "engine-overpressure":
                    return "Chamber pressure " + N0(ev.value) + " bar. Rated " + N0(ev.limit) + " bar.";
                case "bad-mixture":
                    return "Mixture " + N1(Math.Abs(ev.value)) + "% off. The band is " + N1(Math.Abs(ev.limit)) + "%.";
                case "engine-out":
                    return N0(ev.value) + (Math.Abs(ev.value - 1) < 0.001 ? " engine quit out of " : " engines quit out of ") + N0(ev.limit) + ".";
                case "tumble" when ev.cause == "alpha":
                    return N0(ev.value) + " degrees sideways. The limit is " + N0(ev.limit) + " degrees.";
                case "tumble":
                    return "Stability " + N2(ev.value) + " cal at T+" + N0(ev.t) + " s.";
                case "prop-depletion":
                    string fuel = "Out of fuel at T+" + N0(ev.t) + " s.";
                    if (facts != null && facts.Margin < 0 && facts.RequiredMps > 0)
                        fuel += " Short " + N0(-facts.Margin * facts.RequiredMps) + " m/s.";
                    return fuel;
                case "separation-collision":
                    return "Parted at " + N2(ev.value) + " m/s. Needed " + N2(ev.limit) + " m/s.";
                case "overg":
                    string peakG = N1(ev.value);
                    string limG = N1(ev.limit);
                    if (peakG == limG) peakG = ev.value.ToString("0.00", CultureInfo.InvariantCulture);
                    return "The push reached " + peakG + " g, past the " + limG + " g limit.";
                case "thermal" when ev.cause != "base-heating":
                    return N0(ev.value) + " K. The material handles " + N0(ev.limit) + " K.";
                case "thermal":
                    return "Base heating at T+" + N0(ev.t) + " s. More than " + N0(ev.limit) + " engines.";
                case "leg-collapse":
                    return "Falling at " + N1(ev.value) + " m/s. The legs take " + N1(ev.limit) + " m/s.";
                case "recovery-fuel":
                    return "Landing fuel gone. Sink " + N1(ev.value) + " m/s. The legs hold " + N1(ev.limit) + " m/s.";
                case "recovery-fast":
                    return "Touchdown " + N1(ev.value) + " m/s. The legs hold " + N1(ev.limit) + " m/s.";
                case "recovery-tilt":
                    return "Tilt " + N1(ev.value) + "°. The limit is " + N1(ev.limit) + "°.";
                case "recovery-miss":
                    return "Missed by " + N0(System.Math.Abs(ev.value)) + " m. The deck reaches " + N0(ev.limit) + " m.";
                case "cryo-material":
                    return "The tank cracked on the pad. T+" + N0(ev.t) + " s.";
                default:
                    return "Value " + N2(ev.value) + ". Limit " + N2(ev.limit) + ". T+" + N0(ev.t) + " s.";
            }
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
            s = s.Replace("{q}", N0(value / 1000.0));
            s = s.Replace("{qlim}", N0(limit / 1000.0));
            s = s.Replace("{qPa}", value.ToString("0.0", CultureInfo.InvariantCulture));
            s = s.Replace("{qlimPa}", limit.ToString("0.0", CultureInfo.InvariantCulture));
            s = s.Replace("{t}", N0(t));
            s = s.Replace("{value}", N2(value));
            s = s.Replace("{limit}", N2(limit));
            s = s.Replace("{n0}", N0(value));
            s = s.Replace("{n0lim}", N0(limit));
            s = s.Replace("{n1}", N1(Math.Abs(value)));
            s = s.Replace("{g}", N1(value));
            s = s.Replace("{glim}", N1(limit));
            s = s.Replace("{stars}", stars.ToString(CultureInfo.InvariantCulture));
            s = s.Replace("{apo}", N0(apo));
            s = s.Replace("{goal}", N0(goal));
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

        static string MedalSentence(List<string> medals)
        {
            if (medals == null || medals.Count == 0) return "No medal this flight.";
            var parts = new List<string>();
            for (int i = 0; i < medals.Count; i++)
            {
                string medal = medals[i];
                if (medal == "gold") parts.Add("Gold for the landing.");
                else if (medal == "silver") parts.Add("Silver for the landing.");
                else if (medal == "bronze") parts.Add("Bronze for the landing.");
                else if (medal == "timing-gold") parts.Add("Gold for timing.");
                else if (medal == "timing-silver") parts.Add("Silver for timing.");
                else if (medal == "timing-bronze") parts.Add("Bronze for timing.");
            }
            return parts.Count == 0 ? "No medal this flight." : string.Join(" ", parts);
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
            if (d.EconomySettled && d.Passed)
                return "Reward " + Kc(d.RewardKc) + " Kc. Hardware " + Kc(d.CostKc) + " Kc. Recovery " + Kc(d.RefundKc) + " Kc. Bank " + Kc(d.BankKc) + " Kc.";
            if (d.EconomySettled)
                return "No reward. Hardware " + Kc(d.CostKc) + " Kc. Bank " + Kc(d.BankKc) + " Kc.";
            if (d.TriedSettle)
                return "Stars saved. Kc was settled the first time this mission opened.";
            return "Kc is not posted from this screen.";
        }

        static string Kc(double kc) => kc.ToString("0.##", CultureInfo.InvariantCulture);

        static string N0(double x) => Math.Round(x).ToString(CultureInfo.InvariantCulture);
        static string N1(double x) => x.ToString("0.0", CultureInfo.InvariantCulture);
        static string N2(double x) => x.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
