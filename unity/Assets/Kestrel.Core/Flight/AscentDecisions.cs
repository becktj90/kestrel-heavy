// Active ascent (stepper S6). Timed calls, seeded anomalies, and engineer nudges.
// Every effect is a guidance input the autopilot already has: throttle scale, pitch offset,
// or the separation scale EventModel drew. Nothing here raises q_lim, adds an engine,
// or invents crossfeed. A skipped call takes the autopilot action and is not an instant fail.
// Gentle flights still fly. Random anomalies stay off. The same seed replays the same calls.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Data;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    public enum AscentCall
    {
        MaxQ = 1,
        Pitch = 2,
        Staging = 3,
        Fairing = 4,
        EngineOut = 5,
        WindShear = 6,
        FuelBalance = 7,
    }

    public enum AscentGrade
    {
        None = 0,
        Perfect = 1,
        Good = 2,
        Late = 3,
    }

    public enum AscentMedal
    {
        None = 0,
        Bronze = 1,
        Silver = 2,
        Gold = 3,
    }

    /// <summary>One telemetry step. The director reads it and writes nothing back to the sim.</summary>
    public struct AscentSample
    {
        public double T, AltM, DownrangeM, SpeedMps, QPa, G, MassKg, ThrustN, PropFraction;
        public double LossGravity, LossDrag, LossSteering;
        public int StageIndex;
        public bool Thrusting;
    }

    /// <summary>What the next integrator step should apply. Scale 1 and offset 0 are a no-op.</summary>
    public struct AscentCommand
    {
        public double ThrottleScale;
        public double PitchOffsetRad;
        /// <summary>
        /// #313 (Kestrel Physics): the design's built-in max-Q throttle (<see cref="GuidanceProfile"/>) is the only max-Q
        /// law. 0 leaves the design's own setting alone. A fraction asks the flight to engage that same law at this
        /// cap: a player who takes the max-Q call on a design without a max-Q throttle.
        /// </summary>
        public double EngageMaxQCap;
        public bool Lost;
        public string LossKind, LossKid, LossEngineer;
    }

    /// <summary>Portrait HUD numbers. Points, not pixels. The action zone is the thumb reach.</summary>
    public static class AscentLayout
    {
        public const float ActionZonePt = 230f;
        public const float ButtonMinPt = 52f;
        public const int MaxPrompts = 2;

        public static float Px(float pt, float unitsPerPoint)
        {
            float u = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            return pt * u;
        }
    }

    public struct AscentPrompt
    {
        public AscentCall Call;
        public string Eyebrow, Quote, Detail, Primary, Alternate;
        public bool ShowAlternate, DangerAlternate;
        public double Marker, GreenStart, GreenEnd, LeftS;
        public bool InGreen;
    }

    /// <summary>Plan vs flown samples for the trajectory card. The plan is a shape, not a second integrator.</summary>
    public sealed class AscentTrack
    {
        public const int Capacity = 48;

        public struct Pt
        {
            public double AltM, DownM, QPa;
        }

        readonly List<Pt> pts = new List<Pt>();

        public int Count => pts.Count;
        public bool Nudged { get; set; }
        public Pt this[int i] => pts[i];

        public void Push(double altM, double downM, double qPa)
        {
            if (pts.Count >= Capacity) pts.RemoveAt(0);
            pts.Add(new Pt { AltM = altM, DownM = downM, QPa = qPa });
        }

        /// <summary>Cyan plan: steep at liftoff, bending over toward the target.</summary>
        public static double PlanDownrange(double altM, double targetAltM)
        {
            if (!(altM > 0) || !(targetAltM > 0)) return 0;
            double u = altM / targetAltM;
            if (u < 0) u = 0;
            else if (u > 1.6) u = 1.6;
            return targetAltM * 0.55 * u * u;
        }
    }

    public sealed class AscentFlightScore
    {
        public AscentMedal Medal;
        public int Perfect, Good, Late, Decided;
        public bool PlayerFlew, PayloadDamaged;
        public bool AltitudeHit, SpeedHit, FuelHit, CleanStaging, PeakGHit;
        public double FairingFlux, PeakAltM, PeakSpeed, PeakG;
        public int StarBonus;
        public string Summary;
    }

    /// <summary>Design facts the calls are allowed to see. Odds and limits come from the design, not from the thumbs.</summary>
    public sealed class AscentVehicle
    {
        public int Stages = 1;
        public int Engines = 1;
        public int AnomalyStage = -1;
        public int AnomalyEngine = -1;
        public bool HasAnomaly, Redundant, HotChamber, Gentle, MaxQSet, HasFairing, CanStage;
        public bool Crossfeed, FuelEvent, CalmWind = true;
        public double MaxQRaw = 100, ThrottleMin, PitchKickDeg = GuidanceProfile.DefaultKickDeg;
        public double SafetyFactor = EventModel.NominalSafetyFactor;
        public double QLimPa, GLimit, JettisonAltM = 110000, MaxHeatFlux, TargetAltM = 200000;
        public double AltitudeTargetM, SpeedTargetMps, BonusMaxG;
        public bool BonusAltitude, BonusSpeed, BonusFuel, BonusClean, BonusPeakG;
        public int StrapOns;
        public string FlightCode = "KH-1";
    }

    public static class AscentDecisions
    {
        public const double WindowS = 8;
        public const double WindowReducedS = 14;
        public const double TimePerfectStart = 0.45;
        public const double TimePerfectEnd = 0.70;
        public const double MaxQGreenStart = 0.33;
        public const double MaxQGreenEnd = 0.90;
        public const double NudgeLimitDeg = 3;
        public const double NudgeStepDeg = 1;
        public const double DefaultBucket = 0.70;
        public const double FuelPenalty = 0.97;
        public const double FuelWasteFraction = 0.25;
        const ulong FuelSalt = 0xF0E1BA1A5C6D7E8FUL;

        public static double OfferedThrottle(bool maxQSet, double raw, double throttleMin)
        {
            double fraction;
            if (!maxQSet || double.IsNaN(raw)) fraction = DefaultBucket;
            else if (raw > 1.5) fraction = raw / 100.0;
            else fraction = raw;
            if (fraction > 1) fraction = 1;
            else if (fraction < 0) fraction = 0;
            double min = throttleMin;
            if (double.IsNaN(min) || min < 0) min = 0;
            else if (min > 1) min = 1;
            if (fraction < min) fraction = min;
            return fraction;
        }

        /// <summary>§2.2. One engine out is survivable only with 7 or more engines, redundant avionics, and TWR after above 1.05.</summary>
        public static bool ToleratesEngineOut(int engines, bool redundant, double thrustN, double massKg)
        {
            if (engines < FailureModel.EngineOutMinCount || !redundant) return false;
            return TwrAfter(engines, thrustN, massKg) > FailureModel.EngineOutMinTwr;
        }

        public static double TwrAfter(int engines, double thrustN, double massKg)
        {
            if (engines < 1 || !(massKg > 0) || !(thrustN > 0)) return 0;
            return (thrustN * (engines - 1.0) / engines) / (massKg * FailureModel.G0);
        }

        /// <summary>½ρv³ from Atmosphere.At. The fairing call compares this with payload.maxHeatFlux.</summary>
        public static double HeatFlux(double altM, double speedMps)
        {
            if (altM < 0 || double.IsNaN(altM)) altM = 0;
            if (speedMps < 0 || double.IsNaN(speedMps)) speedMps = 0;
            double rho = Atmosphere.At(altM).rho;
            return 0.5 * rho * speedMps * speedMps * speedMps;
        }

        /// <summary>Drag and steering losses against gravity loss. Gravity is the part a throttle cannot erase.</summary>
        public static bool FuelEfficient(double gravity, double drag, double steering)
        {
            double waste = (drag > 0 ? drag : 0) + (steering > 0 ? steering : 0);
            double grav = gravity > 0 ? gravity : 0;
            double total = waste + grav;
            if (total < 1) return false;
            return waste / total <= FuelWasteFraction;
        }

        /// <summary>
        /// Separate stream from EventModel and FailureModel. Gentle and single-core rockets draw nothing.
        /// Crossfeed lowers the chance. It does not change the other seeds.
        /// </summary>
        public static bool FuelImbalance(ulong designHash, ulong missionSeed, int strapOns, bool crossfeed, bool gentle)
        {
            if (gentle || strapOns < 2) return false;
            ulong rng = EventModel.Seed(designHash, missionSeed) ^ FuelSalt;
            double u = EventModel.NextUniform(ref rng);
            double p = crossfeed ? 0.08 : 0.35;
            return u < p;
        }

        public static AscentGrade GradeTime(double age, double window)
        {
            if (!(window > 0) || age < 0) return AscentGrade.Late;
            double u = age / window;
            if (u >= 1) return AscentGrade.Late;
            if (u >= TimePerfectStart && u <= TimePerfectEnd) return AscentGrade.Perfect;
            return AscentGrade.Good;
        }

        public static AscentGrade GradeMaxQ(double qPa, double qLimPa, double age, double window)
        {
            if (age >= window) return AscentGrade.Late;
            double lim = qLimPa > 1000 ? qLimPa : GuidanceProfile.MaxQReferencePa;
            if (qPa > lim * 1.02) return AscentGrade.Late;
            double u = (qPa - 0.55 * lim) / (0.45 * lim);
            if (u >= MaxQGreenStart && u <= MaxQGreenEnd) return AscentGrade.Perfect;
            if (u >= 0 && u < 1.2) return AscentGrade.Good;
            return AscentGrade.Good;
        }

        public static double MaxQMarker(double qPa, double qLimPa)
        {
            double lim = qLimPa > 1000 ? qLimPa : GuidanceProfile.MaxQReferencePa;
            double u = (qPa - 0.55 * lim) / (0.45 * lim);
            if (u < 0) return 0;
            if (u > 1) return 1;
            return u;
        }

        public static AscentMedal MedalFrom(int perfect, int good, int late, bool playerFlew)
        {
            if (perfect + good + late <= 0) return AscentMedal.None;
            if (late > 0) return AscentMedal.Bronze;
            if (good == 0 && perfect > 0 && playerFlew) return AscentMedal.Gold;
            return AscentMedal.Silver;
        }

        /// <summary>A gold timing medal adds one star, capped at 3. MissionScore.Stars itself stays the pass formula.</summary>
        public static int StarBonus(AscentMedal medal) => medal == AscentMedal.Gold ? 1 : 0;

        public static int ApplyStars(int stars, AscentMedal medal)
        {
            if (stars < 0) stars = 0;
            if (stars > 3) stars = 3;
            int sum = stars + StarBonus(medal);
            return sum > 3 ? 3 : sum;
        }

        public static AscentVehicle FromDesign(Design design, Catalog catalog, EventSchedule schedule, SeededWind wind, double qLimPa, double targetAltM, bool gentle, IList<BonusGoal> bonuses, ulong designHash, ulong missionSeed)
        {
            var v = new AscentVehicle
            {
                Gentle = gentle,
                QLimPa = qLimPa,
                TargetAltM = targetAltM > 1000 ? targetAltM : 200000,
                SafetyFactor = design != null && design.safetyFactor > 0 ? design.safetyFactor : EventModel.NominalSafetyFactor,
                Redundant = design != null && EventModel.Tier(design.avionicsTier) == "redundant",
                FlightCode = "KH-" + MissionBriefing.FlightNumberOf(missionSeed == 0 ? 1 : missionSeed).ToString(CultureInfo.InvariantCulture),
                CalmWind = wind == null || wind.Calm,
            };
            int cores = design != null && design.stages != null ? design.stages.Count : 0;
            int straps = design != null ? design.StrapOnCount : 0;
            v.StrapOns = straps;
            v.Stages = (cores > 0 ? cores : 1) + (straps > 0 ? 1 : 0);
            v.CanStage = v.Stages > 1;
            v.Crossfeed = straps >= 2 && design.strapOns != null && design.strapOns.crossfeed;
            v.HasFairing = design != null && design.fairingDiameter > 0 && design.fairingLength > 0;
            if (design != null && design.fairingJettisonAltitudeKm > 0) v.JettisonAltM = design.fairingJettisonAltitudeKm * 1000;
            if (design != null && design.guidance != null)
            {
                if (design.guidance.pitchKickDegSet) v.PitchKickDeg = design.guidance.pitchKickDeg;
                if (design.guidance.maxQThrottleSet)
                {
                    v.MaxQSet = true;
                    v.MaxQRaw = design.guidance.maxQThrottle;
                }
                if (design.guidance.gLimitSet) v.GLimit = design.guidance.gLimit;
            }
            StagePart first = null;
            if (straps > 0 && design.strapOns != null) first = design.strapOns;
            else if (cores > 0) first = design.stages[0];
            if (first != null)
            {
                v.Engines = first.engineCount > 0 ? first.engineCount : 1;
                if (first.count > 1) v.Engines *= first.count;
                v.ThrottleMin = ThrottleFloor(catalog, first);
                v.HotChamber = Hot(catalog, first);
            }
            if (schedule != null)
            {
                int n = schedule.stages;
                for (int i = 0; i < n; i++)
                {
                    if (!schedule.EngineAnomaly(i)) continue;
                    v.HasAnomaly = true;
                    v.AnomalyStage = i;
                    v.AnomalyEngine = schedule.EngineIndex(i);
                    StagePart part = PartAt(design, i);
                    if (part != null)
                    {
                        int engines = part.engineCount > 0 ? part.engineCount : 1;
                        if (part.count > 1) engines *= part.count;
                        v.Engines = engines;
                        v.HotChamber = Hot(catalog, part);
                        double floor = ThrottleFloor(catalog, part);
                        if (floor > v.ThrottleMin) v.ThrottleMin = floor;
                    }
                    break;
                }
            }
            if (catalog != null && design != null && !string.IsNullOrEmpty(design.payloadType)
                && catalog.PayloadById.TryGetValue(design.payloadType, out var payload) && payload.maxHeatFlux > 0)
                v.MaxHeatFlux = payload.maxHeatFlux;
            v.FuelEvent = FuelImbalance(designHash, missionSeed, v.StrapOns, v.Crossfeed, gentle);
            if (bonuses != null)
            {
                for (int i = 0; i < bonuses.Count; i++)
                {
                    var b = bonuses[i];
                    if (b == null || string.IsNullOrEmpty(b.Id)) continue;
                    if (b.MaxG > 0) { v.BonusPeakG = true; v.BonusMaxG = b.MaxG; }
                    string id = b.Id;
                    if (id.IndexOf("fuel", StringComparison.Ordinal) >= 0) v.BonusFuel = true;
                    if (id == "karman" || id.IndexOf("alt", StringComparison.Ordinal) >= 0)
                    {
                        v.BonusAltitude = true;
                        v.AltitudeTargetM = id == "karman" ? 100000 : (targetAltM > 1000 ? targetAltM : 100000);
                    }
                    if (id.IndexOf("speed", StringComparison.Ordinal) >= 0)
                    {
                        v.BonusSpeed = true;
                        if (!(v.SpeedTargetMps > 0)) v.SpeedTargetMps = 1500;
                    }
                    if (id == "window" || id.IndexOf("clean", StringComparison.Ordinal) >= 0 || id.IndexOf("stag", StringComparison.Ordinal) >= 0)
                        v.BonusClean = true;
                    if (id.IndexOf("peak", StringComparison.Ordinal) >= 0 || id.IndexOf("soft", StringComparison.Ordinal) >= 0)
                        v.BonusPeakG = true;
                }
            }
            return v;
        }

        static StagePart PartAt(Design design, int simStage)
        {
            if (design == null) return null;
            int i = simStage;
            if (design.StrapOnCount > 0)
            {
                if (i == 0) return design.strapOns;
                i -= 1;
            }
            if (design.stages == null || i < 0 || i >= design.stages.Count) return null;
            return design.stages[i];
        }

        static double ThrottleFloor(Catalog catalog, StagePart part)
        {
            if (part != null && part.engineParams != null && part.engineParams.throttleMinSet && part.engineParams.throttleMin > 0)
                return part.engineParams.throttleMin > 1 ? 1 : part.engineParams.throttleMin;
            if (catalog != null && part != null && catalog.EngineById.TryGetValue(part.engine ?? "", out var eng)
                && eng.throttle != null && eng.throttle.Length > 0 && eng.throttle[0] > 0)
                return eng.throttle[0] > 1 ? 1 : eng.throttle[0];
            return 0.4;
        }

        static bool Hot(Catalog catalog, StagePart part)
        {
            if (catalog == null || part == null || string.IsNullOrEmpty(part.engine)) return false;
            if (!catalog.EngineById.TryGetValue(part.engine, out var eng) || !(eng.pcRated > 0)) return false;
            double pc = part.engineParams != null && part.engineParams.pcSet ? part.engineParams.pc : eng.pc;
            return pc > eng.pcRated;
        }
    }

    public sealed class AscentDirector
    {
        enum Phase { Idle, Open, Done }

        sealed class Call
        {
            public AscentCall Kind;
            public int Stage;
            public Phase Phase;
            public bool Armed;
            public double Age;
            public AscentGrade Grade;
            public bool Accept, Touched;
        }

        readonly AscentVehicle vehicle;
        readonly EventSchedule schedule;
        readonly SeededWind wind;
        readonly List<Call> calls = new List<Call>();
        readonly AscentTrack track = new AscentTrack();
        AscentSample last;
        double lastTrackT = -1;
        double engineScale = 1;
        int engineStage = -1;
        double fuelScale = 1;
        int fuelStage = -1;
        bool maxQDown, lost, payloadDamaged, playerFlew;
        double peakAlt, peakSpeed, peakG, fairingFlux;
        string lossKind, lossKid, lossEngineer;

        public bool AutoAssist { get; set; }
        public bool Engineer { get; private set; }
        public bool ReduceMotion { get; private set; }
        public double NudgeDeg { get; private set; }
        public AscentCommand Command { get; private set; }
        public AscentTrack Track => track;
        public double LiveAlt { get; private set; }
        public double LiveSpeed { get; private set; }
        public double LiveQ { get; private set; }
        public double LiveG { get; private set; }
        public double LiveTwr { get; private set; }
        public double LiveProp { get; private set; }
        public AscentVehicle Vehicle => vehicle;
        public string FlightCode => vehicle != null ? vehicle.FlightCode : "KH-1";

        double Window => ReduceMotion ? AscentDecisions.WindowReducedS : AscentDecisions.WindowS;

        AscentDirector(AscentVehicle vehicle, EventSchedule schedule, SeededWind wind, bool autoAssist, bool engineer, bool reduceMotion)
        {
            this.vehicle = vehicle ?? new AscentVehicle();
            this.schedule = schedule;
            this.wind = wind;
            AutoAssist = autoAssist;
            Engineer = engineer;
            ReduceMotion = reduceMotion;
            BuildCalls();
            Command = new AscentCommand { ThrottleScale = 1 };
        }

        public static AscentDirector Start(AscentVehicle vehicle, EventSchedule schedule, SeededWind wind, bool autoAssist, bool engineer, bool reduceMotion)
            => new AscentDirector(vehicle, schedule, wind, autoAssist, engineer, reduceMotion);

        public void NudgePitch(double deltaDeg)
        {
            if (double.IsNaN(deltaDeg)) return;
            SetNudge(NudgeDeg + deltaDeg);
        }

        public void SetNudge(double deg)
        {
            if (double.IsNaN(deg)) deg = 0;
            if (deg > AscentDecisions.NudgeLimitDeg) deg = AscentDecisions.NudgeLimitDeg;
            else if (deg < -AscentDecisions.NudgeLimitDeg) deg = -AscentDecisions.NudgeLimitDeg;
            NudgeDeg = deg;
            Refresh(last);
        }

        public void Advance(in AscentSample sample, double dt)
        {
            if (double.IsNaN(dt) || dt < 0) dt = 0;
            else if (dt > 0.5) dt = 0.5;
            last = sample;
            LiveAlt = sample.AltM;
            LiveSpeed = sample.SpeedMps;
            LiveQ = sample.QPa;
            LiveG = sample.G;
            LiveProp = sample.PropFraction;
            LiveTwr = sample.MassKg > 1 ? sample.ThrustN / (sample.MassKg * Atmosphere.G0) : 0;
            if (sample.AltM > peakAlt) peakAlt = sample.AltM;
            if (sample.SpeedMps > peakSpeed) peakSpeed = sample.SpeedMps;
            if (sample.G > peakG) peakG = sample.G;
            if (lastTrackT < 0 || sample.T - lastTrackT >= 0.4)
            {
                track.Push(sample.AltM, sample.DownrangeM, sample.QPa);
                lastTrackT = sample.T;
            }
            track.Nudged = Math.Abs(NudgeDeg) > 0.05;
            Arm(sample);
            TryOpen();
            for (int i = 0; i < calls.Count; i++)
            {
                var c = calls[i];
                if (c.Phase != Phase.Open) continue;
                c.Age += dt;
                var grade = GradeFor(c, sample);
                bool fire = AutoAssist && (c.Kind == AscentCall.MaxQ ? grade == AscentGrade.Perfect : c.Age >= AscentDecisions.TimePerfectStart * Window && grade == AscentGrade.Perfect);
                if (fire) Resolve(c, AutopilotAccept(c, sample), AscentGrade.Perfect, false, sample);
                else if (c.Age >= Window) Resolve(c, AutopilotAccept(c, sample), AscentGrade.Late, false, sample);
            }
            Refresh(sample);
        }

        public bool Choose(AscentCall kind, bool accept)
        {
            var c = Find(kind, Phase.Open);
            if (c == null) return false;
            Resolve(c, accept, GradeFor(c, last), true, last);
            Refresh(last);
            return true;
        }

        public bool Skip(AscentCall kind)
        {
            var c = Find(kind, Phase.Open);
            if (c == null) return false;
            Resolve(c, AutopilotAccept(c, last), AscentGrade.Late, false, last);
            Refresh(last);
            return true;
        }

        public int OpenCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < calls.Count; i++) if (calls[i].Phase == Phase.Open) n++;
                return n;
            }
        }

        public AscentPrompt[] OpenPrompts()
        {
            int n = OpenCount;
            if (n > AscentLayout.MaxPrompts) n = AscentLayout.MaxPrompts;
            var list = new AscentPrompt[n];
            int w = 0;
            for (int i = 0; i < calls.Count && w < n; i++)
            {
                if (calls[i].Phase != Phase.Open) continue;
                list[w++] = Describe(calls[i], last);
            }
            return list;
        }

        public AscentGrade GradeOf(AscentCall kind)
        {
            var c = Find(kind, Phase.Done);
            return c != null ? c.Grade : AscentGrade.None;
        }

        public bool IsOpen(AscentCall kind) => Find(kind, Phase.Open) != null;
        public bool IsDone(AscentCall kind) => Find(kind, Phase.Done) != null;

        public AscentFlightScore Score()
        {
            int perfect = 0, good = 0, late = 0;
            bool stagingLate = false, stagingOk = !vehicle.CanStage;
            for (int i = 0; i < calls.Count; i++)
            {
                var c = calls[i];
                if (c.Phase != Phase.Done) continue;
                if (c.Grade == AscentGrade.Perfect) perfect++;
                else if (c.Grade == AscentGrade.Good) good++;
                else if (c.Grade == AscentGrade.Late) late++;
                if (c.Kind == AscentCall.Staging)
                {
                    if (c.Grade == AscentGrade.Late) stagingLate = true;
                    else stagingOk = true;
                }
            }
            var medal = AscentDecisions.MedalFrom(perfect, good, late, playerFlew);
            var score = new AscentFlightScore
            {
                Medal = medal,
                Perfect = perfect,
                Good = good,
                Late = late,
                Decided = perfect + good + late,
                PlayerFlew = playerFlew,
                PayloadDamaged = payloadDamaged,
                FairingFlux = fairingFlux,
                PeakAltM = peakAlt,
                PeakSpeed = peakSpeed,
                PeakG = peakG,
                StarBonus = AscentDecisions.StarBonus(medal),
                CleanStaging = vehicle.BonusClean && stagingOk && !stagingLate,
                FuelHit = vehicle.BonusFuel && AscentDecisions.FuelEfficient(last.LossGravity, last.LossDrag, last.LossSteering),
                AltitudeHit = vehicle.BonusAltitude && vehicle.AltitudeTargetM > 0 && peakAlt >= vehicle.AltitudeTargetM,
                SpeedHit = vehicle.BonusSpeed && vehicle.SpeedTargetMps > 0 && peakSpeed >= vehicle.SpeedTargetMps,
                PeakGHit = vehicle.BonusPeakG && vehicle.BonusMaxG > 0 && peakG > 0.5 && peakG <= vehicle.BonusMaxG,
            };
            score.Summary = Summary(score);
            return score;
        }

        void BuildCalls()
        {
            if (vehicle.HasAnomaly && !vehicle.Gentle)
                calls.Add(new Call { Kind = AscentCall.EngineOut, Stage = vehicle.AnomalyStage < 0 ? 0 : vehicle.AnomalyStage });
            // #338 (Physics, variant B): no wind-shear call. Its 12 s ease (85% throttle, -0.5° pitch) switched on only in
            // wind and moved the high point about 9%, flipping neighbouring designs in and out of the band. The wind still
            // flies through drag, q and alpha (and the structural q·alpha check, FailureMonitor).
            calls.Add(new Call { Kind = AscentCall.MaxQ });
            if (vehicle.FuelEvent)
                calls.Add(new Call { Kind = AscentCall.FuelBalance });
            int seps = vehicle.Stages - 1;
            if (seps < 0) seps = 0;
            if (seps > 3) seps = 3;
            for (int i = 0; i < seps; i++)
                calls.Add(new Call { Kind = AscentCall.Staging, Stage = i });
            if (vehicle.HasFairing)
                calls.Add(new Call { Kind = AscentCall.Fairing });
            calls.Add(new Call { Kind = AscentCall.Pitch });
        }

        void Arm(in AscentSample s)
        {
            double lim = Lim();
            for (int i = 0; i < calls.Count; i++)
            {
                var c = calls[i];
                if (c.Armed || c.Phase != Phase.Idle) continue;
                bool ready = false;
                switch (c.Kind)
                {
                    case AscentCall.MaxQ:
                        ready = s.Thrusting && s.QPa >= 0.55 * lim;
                        break;
                    case AscentCall.Pitch:
                        ready = s.Thrusting && s.StageIndex == 0 && s.SpeedMps > 70 && s.AltM > 1500 && s.AltM < 50000;
                        break;
                    case AscentCall.Staging:
                        ready = s.Thrusting && s.StageIndex == c.Stage && s.PropFraction > 0 && s.PropFraction <= 0.12;
                        break;
                    case AscentCall.Fairing:
                        ready = s.AltM >= vehicle.JettisonAltM - 12000;
                        break;
                    case AscentCall.EngineOut:
                        ready = s.Thrusting && s.StageIndex == c.Stage && s.T > 10 && s.AltM > 1500;
                        break;
                    case AscentCall.WindShear:
                        ready = s.QPa > 5000 && s.AltM > 5000 && s.AltM < 16000 && wind != null && Math.Abs(wind.ShearAt(s.AltM)) >= 0.002;
                        break;
                    case AscentCall.FuelBalance:
                        ready = s.Thrusting && s.StageIndex == 0 && s.AltM > 3000;
                        break;
                }
                if (ready) c.Armed = true;
            }
        }

        void TryOpen()
        {
            int open = 0;
            for (int i = 0; i < calls.Count; i++) if (calls[i].Phase == Phase.Open) open++;
            if (open >= AscentLayout.MaxPrompts) return;
            for (int i = 0; i < calls.Count; i++)
            {
                var c = calls[i];
                if (!c.Armed || c.Phase != Phase.Idle) continue;
                c.Phase = Phase.Open;
                c.Age = 0;
                if (++open >= AscentLayout.MaxPrompts) return;
            }
        }

        AscentGrade GradeFor(Call c, in AscentSample s)
        {
            if (c.Kind == AscentCall.MaxQ) return AscentDecisions.GradeMaxQ(s.QPa, vehicle.QLimPa, c.Age, Window);
            return AscentDecisions.GradeTime(c.Age, Window);
        }

        bool AutopilotAccept(Call c, in AscentSample s)
        {
            // #313 option (b): the default player flies the design's own settings. The autopilot takes the max-Q
            // throttle-down only when the design set a max-Q throttle (and then at that value, OfferedThrottle), and
            // never takes the open-loop nose-down nudge. A player tap (Choose) still does either.
            if (c.Kind == AscentCall.MaxQ) return vehicle.MaxQSet;
            if (c.Kind == AscentCall.Pitch) return false;
            if (c.Kind != AscentCall.EngineOut) return true;
            if (AscentDecisions.ToleratesEngineOut(vehicle.Engines, vehicle.Redundant, s.ThrustN, s.MassKg)) return true;
            return vehicle.HotChamber;
        }

        void Resolve(Call c, bool accept, AscentGrade grade, bool touched, in AscentSample s)
        {
            if (c.Phase == Phase.Done) return;
            c.Phase = Phase.Done;
            c.Accept = accept;
            c.Grade = grade;
            c.Touched = touched;
            if (touched) playerFlew = true;
            switch (c.Kind)
            {
                case AscentCall.MaxQ:
                    if (accept) maxQDown = true;
                    break;
                case AscentCall.Pitch:
                    if (accept && Math.Abs(NudgeDeg) < 0.05) SetNudge(-AscentDecisions.NudgeStepDeg);
                    break;
                case AscentCall.Staging:
                    if (accept && grade != AscentGrade.Late && schedule != null)
                        schedule.SetSep(c.Stage + 1, 1);
                    break;
                case AscentCall.Fairing:
                    fairingFlux = AscentDecisions.HeatFlux(s.AltM, s.SpeedMps);
                    if (vehicle.MaxHeatFlux > 0 && fairingFlux >= vehicle.MaxHeatFlux) payloadDamaged = true;
                    break;
                case AscentCall.EngineOut:
                    ApplyEngine(accept, s);
                    break;
                case AscentCall.WindShear:
                    break; // #338: never built (BuildCalls), and no ease when taken.
                case AscentCall.FuelBalance:
                    // #313 (Kestrel Physics): the penalty throttles only the stage with the imbalance, and ends when
                    // that stage separates or shuts down.
                    fuelScale = accept && vehicle.Crossfeed ? 1 : AscentDecisions.FuelPenalty;
                    fuelStage = s.StageIndex;
                    break;
            }
        }

        void ApplyEngine(bool shutdown, in AscentSample s)
        {
            bool tolerate = AscentDecisions.ToleratesEngineOut(vehicle.Engines, vehicle.Redundant, s.ThrustN, s.MassKg);
            if (shutdown)
            {
                if (tolerate && vehicle.Engines > 1)
                {
                    // Like the fuel penalty (#313 ruling 3): the lost engine costs only its own stage's thrust.
                    engineScale = (vehicle.Engines - 1.0) / vehicle.Engines;
                    engineStage = s.StageIndex;
                }
                else
                    Lose("engine-out",
                        "One engine is out, and this rocket cannot fly on the rest.",
                        "engine-out needs N ≥ 7, redundant avionics, and TWR after > 1.05");
            }
            else if (vehicle.HotChamber)
            {
                Lose("engine-overpressure",
                    "That engine was running hot and came apart.",
                    "pc is above pc rated, so riding the pressure drop trips the overpressure hazard");
            }
        }

        void Lose(string kind, string kid, string engineer)
        {
            if (lost) return;
            lost = true;
            lossKind = kind;
            lossKid = kid;
            lossEngineer = engineer;
        }

        void Refresh(in AscentSample s)
        {
            // #313 (Kestrel Physics): no director bucket. A design with a max-Q throttle already flies it through its
            // guidance; a taken call on a design without one engages that same guidance law at the offered cap.
            double engage = maxQDown && !vehicle.MaxQSet
                ? AscentDecisions.OfferedThrottle(false, vehicle.MaxQRaw, vehicle.ThrottleMin) : 0;
            double scale = 1;
            if (engineStage >= 0 && s.Thrusting && s.StageIndex == engineStage) scale *= engineScale;
            if (fuelStage >= 0 && s.Thrusting && s.StageIndex == fuelStage) scale *= fuelScale;
            if (scale < vehicle.ThrottleMin) scale = vehicle.ThrottleMin;
            if (scale > 1) scale = 1;
            double deg = NudgeDeg;
            if (deg > 4) deg = 4;
            else if (deg < -4) deg = -4;
            Command = new AscentCommand
            {
                ThrottleScale = scale,
                PitchOffsetRad = deg * Math.PI / 180.0,
                EngageMaxQCap = engage,
                Lost = lost,
                LossKind = lossKind,
                LossKid = lossKid,
                LossEngineer = lossEngineer,
            };
        }

        double Lim()
        {
            if (vehicle.QLimPa > 1000) return vehicle.QLimPa;
            return GuidanceProfile.MaxQReferencePa;
        }

        Call Find(AscentCall kind, Phase phase)
        {
            for (int i = 0; i < calls.Count; i++)
                if (calls[i].Kind == kind && calls[i].Phase == phase) return calls[i];
            return null;
        }

        AscentPrompt Describe(Call c, in AscentSample s)
        {
            var inv = CultureInfo.InvariantCulture;
            double left = Window - c.Age;
            if (left < 0) left = 0;
            var p = new AscentPrompt
            {
                Call = c.Kind,
                LeftS = left,
                InGreen = GradeFor(c, s) == AscentGrade.Perfect,
            };
            string sf = vehicle.SafetyFactor.ToString("0.0", inv);
            switch (c.Kind)
            {
                case AscentCall.MaxQ:
                    double lim = Lim();
                    int pct = (int)Math.Round(AscentDecisions.OfferedThrottle(vehicle.MaxQSet, vehicle.MaxQRaw, vehicle.ThrottleMin) * 100);
                    p.Eyebrow = "MISSION CONTROL · MAX-Q";
                    p.Quote = "Approaching max-Q. Throttle down now? q " + Kpa(s.QPa) + " / " + Kpa(lim) + " kPa";
                    p.Detail = "Window " + Clock(left) + " · Design influences risk (SF " + sf + ")";
                    if (pct >= 100) p.Detail += " · This engine cannot throttle down.";
                    p.Primary = "THROTTLE DOWN to " + pct.ToString(inv) + " %";
                    p.Marker = AscentDecisions.MaxQMarker(s.QPa, vehicle.QLimPa);
                    p.GreenStart = AscentDecisions.MaxQGreenStart;
                    p.GreenEnd = AscentDecisions.MaxQGreenEnd;
                    break;
                case AscentCall.Pitch:
                    p.Eyebrow = "MISSION CONTROL · GRAVITY TURN";
                    p.Quote = "Nudge the nose down 1° on the planned kick (" + vehicle.PitchKickDeg.ToString("0.0", inv) + "°)?";
                    p.Detail = "Window " + Clock(left) + " · The nudge stays inside ±" + AscentDecisions.NudgeLimitDeg.ToString("0", inv) + "°.";
                    p.Primary = "NUDGE PITCH −1°";
                    p.Marker = Marker(c);
                    p.GreenStart = AscentDecisions.TimePerfectStart;
                    p.GreenEnd = AscentDecisions.TimePerfectEnd;
                    break;
                case AscentCall.Staging:
                    p.Eyebrow = "MISSION CONTROL · STAGING";
                    p.Quote = "Stage " + (c.Stage + 1).ToString(inv) + " is almost empty. Separate in the window?";
                    p.Detail = "Window " + Clock(left) + " · A short push still needs the sep impulse you built.";
                    p.Primary = "SEPARATE IN THE WINDOW";
                    p.Marker = Marker(c);
                    p.GreenStart = AscentDecisions.TimePerfectStart;
                    p.GreenEnd = AscentDecisions.TimePerfectEnd;
                    break;
                case AscentCall.Fairing:
                    p.Eyebrow = "MISSION CONTROL · FAIRING";
                    p.Quote = "Jettison the fairing near " + (vehicle.JettisonAltM / 1000).ToString("0", inv) + " km?";
                    p.Detail = "Window " + Clock(left) + " · Heat after jettison is ½ρv³ against the payload limit.";
                    p.Primary = "JETTISON FAIRING";
                    p.Marker = Marker(c);
                    p.GreenStart = AscentDecisions.TimePerfectStart;
                    p.GreenEnd = AscentDecisions.TimePerfectEnd;
                    break;
                case AscentCall.EngineOut:
                    int shown = vehicle.AnomalyEngine >= 0 ? vehicle.AnomalyEngine + 1 : 1;
                    int leftEng = vehicle.Engines > 0 ? vehicle.Engines - 1 : 0;
                    double twr = AscentDecisions.TwrAfter(vehicle.Engines, s.ThrustN, s.MassKg);
                    bool ok = AscentDecisions.ToleratesEngineOut(vehicle.Engines, vehicle.Redundant, s.ThrustN, s.MassKg);
                    p.Eyebrow = "ANOMALY · ENGINE " + shown.ToString(inv) + " (seeded, fair)";
                    p.Quote = "Engine " + shown.ToString(inv) + " pressure drop. Shut it down?";
                    p.Detail = leftEng.ToString(inv) + " of " + vehicle.Engines.ToString(inv) + " still burning · TWR after "
                        + twr.ToString("0.00", inv) + (ok ? " > 1.05 · you chose redundant avionics" : " · this design cannot hold an engine-out");
                    p.Primary = "SHUT DOWN ENGINE " + shown.ToString(inv) + (ok ? " (safe)" : " (will not hold)");
                    p.Alternate = "RIDE IT OUT (risk overpressure)";
                    p.ShowAlternate = true;
                    p.DangerAlternate = true;
                    p.Marker = Marker(c);
                    p.GreenStart = AscentDecisions.TimePerfectStart;
                    p.GreenEnd = AscentDecisions.TimePerfectEnd;
                    break;
                case AscentCall.WindShear:
                    p.Eyebrow = "MISSION CONTROL · WIND SHEAR";
                    p.Quote = "Shear on the way up. Ease the throttle and steer?";
                    p.Detail = "Window " + Clock(left) + " · The wind was on the briefing. Fins and throttle still set the limit.";
                    p.Primary = "EASE THROTTLE";
                    p.Alternate = "HOLD THROTTLE";
                    p.ShowAlternate = true;
                    p.Marker = Marker(c);
                    p.GreenStart = AscentDecisions.TimePerfectStart;
                    p.GreenEnd = AscentDecisions.TimePerfectEnd;
                    break;
                default:
                    p.Eyebrow = "ANOMALY · FUEL BALANCE";
                    p.Quote = vehicle.Crossfeed
                        ? "The boosters are burning unevenly. Open the crossfeed?"
                        : "The boosters are burning unevenly. There is no crossfeed line.";
                    p.Detail = vehicle.Crossfeed
                        ? "Window " + Clock(left) + " · You built a shared feed, so the tanks can even out."
                        : "Window " + Clock(left) + " · A button cannot move fuel the design did not plumb.";
                    p.Primary = vehicle.Crossfeed ? "OPEN THE CROSSFEED" : "NO SHARED LINE";
                    p.Alternate = "LEAVE IT";
                    p.ShowAlternate = vehicle.Crossfeed;
                    p.Marker = Marker(c);
                    p.GreenStart = AscentDecisions.TimePerfectStart;
                    p.GreenEnd = AscentDecisions.TimePerfectEnd;
                    break;
            }
            return p;
        }

        double Marker(Call c)
        {
            if (!(Window > 0)) return 0;
            double u = c.Age / Window;
            if (u < 0) return 0;
            if (u > 1) return 1;
            return u;
        }

        static string Summary(AscentFlightScore score)
        {
            string medal = score.Medal == AscentMedal.Gold ? "Gold" : score.Medal == AscentMedal.Silver ? "Silver" : score.Medal == AscentMedal.Bronze ? "Bronze" : "No medal yet";
            var inv = CultureInfo.InvariantCulture;
            return medal + " · " + score.Perfect.ToString(inv) + " perfect · " + score.Good.ToString(inv) + " good · " + score.Late.ToString(inv) + " late";
        }

        static string Kpa(double pa)
        {
            double k = pa / 1000.0;
            var inv = CultureInfo.InvariantCulture;
            return Math.Abs(k) >= 10 ? k.ToString("0", inv) : k.ToString("0.0", inv);
        }

        static string Clock(double seconds)
        {
            int s = (int)Math.Ceiling(seconds - 1e-9);
            if (s < 0) s = 0;
            int m = s / 60;
            int r = s % 60;
            var inv = CultureInfo.InvariantCulture;
            return m.ToString(inv) + ":" + r.ToString("00", inv);
        }
    }
}
