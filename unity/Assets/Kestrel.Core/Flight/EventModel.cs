// Seeded fair events for a flight (stepper S5). Odds are functions of the design.
// The draw is splitmix64 on (design hash, mission seed), a different stream from FailureModel,
// so the existing engine-out rolls and the golden vectors stay put.
// Gentle physics reports the odds as off and does not fire. Deterministic failures stay in FailureModel.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Kestrel.Core.Flight
{
    /// <summary>One burn that can throw an engine anomaly. Chamber pressure is bar, same as the engine card.</summary>
    public struct EventStage
    {
        public int engineCount;
        public double pc, pcRated;
    }

    /// <summary>Vehicle choices that move the three probabilities. Reserve is a fraction of first-stage propellant.</summary>
    public struct EventVehicle
    {
        public string avionicsTier;
        public double safetyFactor;
        public double reserveFraction;
        public bool landing;
        public bool canStage;
    }

    /// <summary>Latent probabilities in [0, 1]. Gentle flights still carry these; the shown odds are zero.</summary>
    public struct EventOdds
    {
        public double engineAnomaly;
        public double stagingJitter;
        public double recoveryGust;
        public int engineCount;
        public int engineStage;
        public string avionicsTier;
        public double pcMargin;
        public double safetyFactor;
        public double reserveFraction;
        public bool landing;
        public bool canStage;
    }

    /// <summary>One replayable draw. Built once per flight, then read. Scales are 1 and the gust is 0 when nothing fired.</summary>
    public sealed class EventSchedule
    {
        public EventOdds odds;
        public bool gentle;
        public int stages;
        readonly bool[] engineHit;
        readonly int[] engineIndex;
        readonly double[] sepScale;

        public double GustMps { get; private set; }

        public EventSchedule(int stages, bool gentle)
        {
            if (stages < 1) stages = 1;
            if (stages > EventModel.MaxStages) stages = EventModel.MaxStages;
            this.stages = stages;
            this.gentle = gentle;
            engineHit = new bool[stages];
            engineIndex = new int[stages];
            sepScale = new double[stages];
            for (int i = 0; i < stages; i++)
            {
                engineIndex[i] = -1;
                sepScale[i] = 1;
            }
        }

        public bool EngineAnomaly(int stageIndex)
        {
            if (gentle || engineHit == null || stageIndex < 0 || stageIndex >= engineHit.Length) return false;
            return engineHit[stageIndex];
        }

        public int EngineIndex(int stageIndex)
        {
            if (engineIndex == null || stageIndex < 0 || stageIndex >= engineIndex.Length) return -1;
            return engineIndex[stageIndex];
        }

        /// <summary>Multiplier on the separation impulse into this stage. 1 when the push is nominal.</summary>
        public double SepScale(int stageIndex)
        {
            if (gentle || sepScale == null || stageIndex < 0 || stageIndex >= sepScale.Length) return 1;
            double s = sepScale[stageIndex];
            if (!(s > 0) || s > 1) return 1;
            return s;
        }

        internal void SetEngine(int stageIndex, bool hit, int index)
        {
            if (stageIndex < 0 || stageIndex >= stages) return;
            engineHit[stageIndex] = hit && !gentle;
            engineIndex[stageIndex] = index;
        }

        internal void SetSep(int stageIndex, double scale)
        {
            if (gentle || stageIndex < 0 || stageIndex >= stages) return;
            if (!(scale > 0) || scale > 1) scale = 1;
            sepScale[stageIndex] = scale;
        }

        internal void SetGust(double mps)
        {
            GustMps = gentle || !(mps > 0) ? 0 : mps;
        }
    }

    public static class EventModel
    {
        public const int MaxStages = 8;
        public const int ReferenceEngines = 9;
        public const double ReferenceBurnOdds = 0.04;
        public const double NominalSafetyFactor = 1.4;
        public const double StagingBase = 0.04;
        public const double GustBase = 0.25;
        public const double ReserveEFold = 0.08;
        public const double JitterScaleMin = 0.55;
        public const double JitterScaleSpan = 0.40;
        public const double GustMinMps = 0.5;
        public const double GustSpanMps = 2.0;
        /// <summary>A chance at or above this is an amber row. It is not a red failure.</summary>
        public const double AmberAt = 0.02;
        const double PcCreditSpan = 0.15;
        const double PerEngineCap = 0.25;
        const ulong Salt = 0xE17E017E17E017E1UL;

        static readonly double NominalPerEngine = 1 - Math.Pow(1 - ReferenceBurnOdds, 1.0 / ReferenceEngines);

        public static string Tier(string tier)
        {
            if (tier == "basic" || tier == "standard" || tier == "redundant") return tier;
            return "standard";
        }

        /// <summary>basic 3, standard 1, redundant 0.2. Same shape as the avionics reliability gaps.</summary>
        public static double AvionicsFactor(string tier)
        {
            tier = Tier(tier);
            if (tier == "basic") return 3;
            if (tier == "redundant") return 0.2;
            return 1;
        }

        /// <summary>(pcRated − pc) / pcRated. 0 when the burn is at rated, or when pressure is unknown.</summary>
        public static double PcMargin(double pc, double pcRated)
        {
            if (!(pcRated > 0) || !(pc > 0) || double.IsNaN(pc) || double.IsNaN(pcRated)) return 0;
            return (pcRated - pc) / pcRated;
        }

        /// <summary>1 at rated. 0.7 at 15% under rated. 4 at the 1.15 overpressure cap. Flat past that cap.</summary>
        public static double ChamberFactor(double pcMargin)
        {
            if (double.IsNaN(pcMargin)) return 1;
            if (pcMargin >= 0)
            {
                double credit = pcMargin / PcCreditSpan;
                if (credit > 1) credit = 1;
                return 1 - 0.3 * credit;
            }
            double over = -pcMargin;
            if (over > PcCreditSpan) over = PcCreditSpan;
            double x = over / PcCreditSpan;
            return 1 + 3 * x * x;
        }

        /// <summary>(1.4 / SF)². 1 at the nominal safety factor.</summary>
        public static double SafetyWeight(double sf)
        {
            if (!(sf > 0) || double.IsNaN(sf) || double.IsInfinity(sf)) sf = NominalSafetyFactor;
            double x = NominalSafetyFactor / sf;
            return x * x;
        }

        /// <summary>Chance at least one engine anomaly fires on this burn.</summary>
        public static double EngineAnomaly(int engines, string tier, double pc, double pcRated)
        {
            if (engines < 1) return 0;
            if (engines > 40) engines = 40;
            double p = NominalPerEngine * AvionicsFactor(tier) * ChamberFactor(PcMargin(pc, pcRated));
            if (p < 0) p = 0;
            if (p > PerEngineCap) p = PerEngineCap;
            return 1 - Math.Pow(1 - p, engines);
        }

        /// <summary>Chance one separation push comes up short. Zero when the rocket does not stage.</summary>
        public static double StagingJitter(string tier, double sf, bool canStage)
        {
            if (!canStage) return 0;
            double p = StagingBase * AvionicsFactor(tier) * SafetyWeight(sf);
            if (p < 0) return 0;
            if (p > 0.5) return 0.5;
            return p;
        }

        /// <summary>Chance a landing gust gets past the reserve. Zero when the stage is not landing.</summary>
        public static double RecoveryGust(string tier, double sf, double reserve, bool landing)
        {
            if (!landing) return 0;
            if (double.IsNaN(reserve) || reserve < 0) reserve = 0;
            if (reserve > 0.5) reserve = 0.5;
            double av = Tier(tier) == "redundant" ? 0.7 : Tier(tier) == "basic" ? 1.3 : 1;
            double p = GustBase * Math.Exp(-reserve / ReserveEFold) * SafetyWeight(sf) * av;
            if (p < 0) return 0;
            if (p > 0.5) return 0.5;
            return p;
        }

        public static EventOdds Odds(EventStage[] stages, in EventVehicle vehicle)
        {
            var odds = new EventOdds
            {
                avionicsTier = Tier(vehicle.avionicsTier),
                safetyFactor = vehicle.safetyFactor > 0 && !double.IsNaN(vehicle.safetyFactor) ? vehicle.safetyFactor : NominalSafetyFactor,
                reserveFraction = vehicle.reserveFraction,
                landing = vehicle.landing,
                canStage = vehicle.canStage,
                engineStage = 1,
            };
            int n = stages != null ? stages.Length : 0;
            if (n > MaxStages) n = MaxStages;
            double worst = 0;
            for (int i = 0; i < n; i++)
            {
                double p = EngineAnomaly(stages[i].engineCount, odds.avionicsTier, stages[i].pc, stages[i].pcRated);
                if (i == 0 || p > worst)
                {
                    worst = p;
                    odds.engineCount = stages[i].engineCount > 0 ? stages[i].engineCount : 0;
                    odds.engineStage = i + 1;
                    odds.pcMargin = PcMargin(stages[i].pc, stages[i].pcRated);
                }
            }
            odds.engineAnomaly = worst;
            odds.canStage = vehicle.canStage && n > 1;
            odds.stagingJitter = StagingJitter(odds.avionicsTier, odds.safetyFactor, odds.canStage);
            odds.recoveryGust = RecoveryGust(odds.avionicsTier, odds.safetyFactor, vehicle.reserveFraction, vehicle.landing);
            return odds;
        }

        public static ulong Seed(ulong designHash, ulong missionSeed)
        {
            ulong rng = designHash ^ (missionSeed * 0x9E3779B97F4A7C15UL) ^ Salt;
            if (rng == 0) rng = 0xC3A5C3A5C3A5C3A5UL;
            return rng;
        }

        /// <summary>Same mixer FailureModel uses, so a seed replays bit for bit. Returns a uniform in [0, 1).</summary>
        public static double NextUniform(ref ulong rng)
        {
            ulong z = rng += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (z >> 11) * (1.0 / 9007199254740992.0);
        }

        /// <summary>
        /// Draws every stage, then each separation, then the landing gust.
        /// Gentle still consumes the stream, then discards the hits, so the seed stays aligned.
        /// </summary>
        public static EventSchedule Roll(ulong designHash, ulong missionSeed, EventStage[] stages, in EventVehicle vehicle, bool gentle)
        {
            int n = stages != null && stages.Length > 0 ? stages.Length : 1;
            if (n > MaxStages) n = MaxStages;
            var odds = Odds(stages, vehicle);
            var schedule = new EventSchedule(n, gentle);
            schedule.odds = odds;
            ulong rng = Seed(designHash, missionSeed);
            for (int i = 0; i < n; i++)
            {
                int count = stages != null && i < stages.Length && stages[i].engineCount > 0 ? stages[i].engineCount : 0;
                if (count > 40) count = 40;
                // One draw per burn, at the cluster odds the Review line shows.
                double p = count > 0 && stages != null && i < stages.Length
                    ? EngineAnomaly(count, odds.avionicsTier, stages[i].pc, stages[i].pcRated)
                    : 0;
                double u = NextUniform(ref rng);
                double uIndex = NextUniform(ref rng);
                bool hit = !gentle && p > 0 && u < p;
                int index = -1;
                if (count > 0)
                {
                    index = (int)(uIndex * count);
                    if (index >= count) index = count - 1;
                    if (index < 0) index = 0;
                }
                schedule.SetEngine(i, hit, hit ? index : -1);
            }
            double jitter = odds.stagingJitter;
            for (int i = 1; i < n; i++)
            {
                double u = NextUniform(ref rng);
                double uScale = NextUniform(ref rng);
                bool hit = !gentle && jitter > 0 && u < jitter;
                double scale = hit ? JitterScaleMin + JitterScaleSpan * uScale : 1;
                schedule.SetSep(i, scale);
            }
            double uGust = NextUniform(ref rng);
            double uMag = NextUniform(ref rng);
            bool gust = !gentle && odds.recoveryGust > 0 && uGust < odds.recoveryGust;
            schedule.SetGust(gust ? GustMinMps + GustSpanMps * uMag : 0);
            return schedule;
        }

        /// <summary>Writes the three odds onto the predict risk list. Red deterministic rows keep their own sentences.</summary>
        public static void Disclose(List<FlightRisk> risks, EventSchedule schedule, bool gentle)
        {
            if (risks == null || schedule == null) return;
            var o = schedule.odds;
            if (gentle)
            {
                Note(risks, "engine-out", 0, true, "engine-out · gentle: random events off", GentleKid);
                Note(risks, "separation-collision", 0, true, "separation-collision · gentle: random events off", GentleKid);
                Note(risks, "leg-collapse", 0, true, "leg-collapse · gentle: random events off", GentleKid);
                return;
            }
            Note(risks, "engine-out", o.engineAnomaly, false, EngineLine(o), EngineKid(o));
            Note(risks, "separation-collision", o.stagingJitter, false, StagingLine(o), StagingKid(o));
            Note(risks, "leg-collapse", o.recoveryGust, false, GustLine(o), GustKid(o));
        }

        const string GentleKid = "Random surprises are off. A weak rocket can still fail.";

        static void Note(List<FlightRisk> risks, string kind, double p, bool gentle, string engineer, string kid)
        {
            for (int i = 0; i < risks.Count; i++)
            {
                var risk = risks[i];
                if (risk == null || risk.kind != kind) continue;
                risk.odds = p;
                risk.oddsLine = engineer;
                if (risk.level == "red") continue;
                risk.engineer = engineer;
                risk.kid = kid;
                if (!gentle && p >= AmberAt && risk.level == "green") risk.level = "amber";
            }
        }

        public static string Percent(double p)
        {
            var inv = CultureInfo.InvariantCulture;
            if (double.IsNaN(p) || p < 0) p = 0;
            double pct = p * 100.0;
            if (pct < 0.05) return "~0%";
            double whole = Math.Round(pct);
            if (pct >= 10 || Math.Abs(pct - whole) < 0.05)
                return "~" + whole.ToString("0", inv) + "%";
            return "~" + pct.ToString("0.0", inv) + "%";
        }

        static string PlainPercent(double p)
        {
            string s = Percent(p);
            return s.Length > 0 && s[0] == '~' ? s.Substring(1) : s;
        }

        static string EngineLine(EventOdds o)
        {
            var inv = CultureInfo.InvariantCulture;
            string where = o.engineStage > 1 ? "stage " + o.engineStage.ToString(inv) + ", " : "";
            string line = "engine-out " + Percent(o.engineAnomaly) + "/burn (" + where + o.engineCount.ToString(inv)
                + " engines, " + o.avionicsTier + " avionics";
            if (Math.Abs(o.pcMargin) >= 0.005)
                line += ", pc margin " + (o.pcMargin * 100.0).ToString("+0.0;-0.0", inv) + "%";
            return line + ")";
        }

        static string EngineKid(EventOdds o)
        {
            var inv = CultureInfo.InvariantCulture;
            return "One engine may quit on the way up. With " + o.engineCount.ToString(inv)
                + " engines that is about " + PlainPercent(o.engineAnomaly) + ".";
        }

        static string StagingLine(EventOdds o)
        {
            var inv = CultureInfo.InvariantCulture;
            string why = o.canStage ? o.avionicsTier + " avionics, SF " + o.safetyFactor.ToString("0.00", inv) : "no separation";
            return "separation-collision " + Percent(o.stagingJitter) + "/sep (" + why + ")";
        }

        static string StagingKid(EventOdds o)
        {
            if (!o.canStage) return "This rocket does not stage, so the stages cannot bump.";
            return "The stages might push apart short. About " + PlainPercent(o.stagingJitter) + " at this safety factor.";
        }

        static string GustLine(EventOdds o)
        {
            var inv = CultureInfo.InvariantCulture;
            if (!o.landing) return "leg-collapse " + Percent(0) + "/landing (no landing)";
            double pct = Math.Round((o.reserveFraction > 0 ? o.reserveFraction : 0) * 100.0);
            return "leg-collapse " + Percent(o.recoveryGust) + "/landing (reserve " + pct.ToString("0", inv)
                + "%, SF " + o.safetyFactor.ToString("0.00", inv) + ")";
        }

        static string GustKid(EventOdds o)
        {
            if (!o.landing) return "This flight does not land, so a landing gust cannot fold the legs.";
            return "A gust at landing can add sink speed. About " + PlainPercent(o.recoveryGust) + " with this fuel reserve.";
        }
    }
}
