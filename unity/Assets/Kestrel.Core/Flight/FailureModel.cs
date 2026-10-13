// Failure model (§3). Pure and deterministic: Evaluate writes only the scratch struct.
// Random draws are splitmix64 seeded by (design hash, mission seed). Strings are built
// once, when the first hit is explained, not on the step.
// Gentle physics suppresses the three random kinds. Deterministic kinds stay on.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    public static class FailureKind
    {
        public const int None = 0;
        public const int StructuralMaxQ = 1;
        public const int OverG = 2;
        public const int Tumble = 3;
        public const int EngineOverpressure = 4;
        public const int BadMixture = 5;
        public const int Thermal = 6;
        public const int CryoMaterial = 7;
        public const int PropDepletion = 8;
        public const int SeparationCollision = 9;
        public const int LegCollapse = 10;
        public const int EngineOut = 11;

        public static string Name(int kind)
        {
            switch (kind)
            {
                case StructuralMaxQ: return "structural-maxq";
                case OverG: return "overg";
                case Tumble: return "tumble";
                case EngineOverpressure: return "engine-overpressure";
                case BadMixture: return "bad-mixture";
                case Thermal: return "thermal";
                case CryoMaterial: return "cryo-material";
                case PropDepletion: return "prop-depletion";
                case SeparationCollision: return "separation-collision";
                case LegCollapse: return "leg-collapse";
                case EngineOut: return "engine-out";
                default: return "";
            }
        }
    }

    /// <summary>One step of inputs. Value types only, so the evaluator does not allocate.</summary>
    public struct FailureSample
    {
        public double t, qPa, qLimPa, margin, accelMps2, limitG;
        public double alphaRad, stability, car;
        public double pc, pcRated, hazardPerS, mixtureOffsetPct;
        public double noseT, maxTempK;
        public double sepRelSpeed, sepMinSpeed, sepAgeS;
        public double sinkRate, vLeg;
        public double dt;
        public bool cryoFail, baseHeat, hotStaging, bulkhead;
        public bool propEmptyBelowTarget, sepArmed, legsArmed;
        public bool gentle, rollEngines;
        public int stage, engineCount;
        public double reliability;
        public bool redundant;
        public double thrustN, massKg;
    }

    public struct FailureHit
    {
        public bool on;
        public int kind, cause, stage, engine;
        public double value, limit;
    }

    /// <summary>Mutable step state. No heap traffic.</summary>
    public struct FailureScratch
    {
        public bool tumbleOn;
        public double tumbleT0;
        public ulong rng;
        public bool closed;
        public int rolledStage;
        public double thrustFactor;
        public int enginesOut;
        public double qPeak, gPeak, minStability, minCar;
    }

    public sealed class FailureEvent
    {
        public double t;
        public string kind, cause, fixHint, kid, engineer;
        public int stage;
        public int engineIndex = -1;
        public double value, limit;
    }

    public sealed class FlightRisk
    {
        public string kind, level, tab, field, kid, engineer;
        public double margin, value, limit;
        public int stage = -1;
        public int engineIndex = -1;
    }

    public static class FailureModel
    {
        public const double TumbleHoldS = 0.5;
        public const double TumbleAlphaRad = 15.0 * Math.PI / 180.0;
        public const double TumbleQPa = 20000;
        public const double OverpressureFactor = 1.15;
        public const double MixtureHazardPct = 6;
        public const double EngineOutMinCount = 7;
        public const double EngineOutMinTwr = 1.05;
        public const double G0 = 9.80665;

        public static FailureScratch Fresh(ulong designHash, ulong missionSeed)
        {
            ulong rng = designHash ^ (missionSeed * 0x9E3779B97F4A7C15UL);
            if (rng == 0) rng = 0xA5A5A5A5A5A5A5A5UL;
            return new FailureScratch
            {
                rng = rng,
                rolledStage = -1,
                thrustFactor = 1,
                minStability = double.PositiveInfinity,
                minCar = double.PositiveInfinity,
            };
        }

        /// <summary>First matching kind, or none. Updates the tumble timer and the engine-out draw.</summary>
        public static FailureHit Evaluate(in FailureSample s, ref FailureScratch scratch)
        {
            if (s.qPa > scratch.qPeak) scratch.qPeak = s.qPa;
            double g = s.accelMps2 / G0;
            if (g > scratch.gPeak) scratch.gPeak = g;
            if (s.stability < scratch.minStability) scratch.minStability = s.stability;
            if (s.car < scratch.minCar) scratch.minCar = s.car;

            if (scratch.closed) return default;

            if (s.cryoFail) return Seal(ref scratch, Hit(FailureKind.CryoMaterial, 0, s, 1, 0));
            if (s.bulkhead) return Seal(ref scratch, Hit(FailureKind.StructuralMaxQ, 3, s, s.qPa, s.qLimPa));
            if (s.hotStaging) return Seal(ref scratch, Hit(FailureKind.StructuralMaxQ, 2, s, s.qPa, s.qLimPa));
            if (s.qLimPa > 0 && s.qPa > s.qLimPa)
                return Seal(ref scratch, Hit(FailureKind.StructuralMaxQ, 0, s, s.qPa, s.qLimPa));
            if (s.margin < 0 && !double.IsPositiveInfinity(s.margin))
                return Seal(ref scratch, Hit(FailureKind.StructuralMaxQ, 1, s, s.margin, 0));
            if (s.limitG > 0 && s.accelMps2 > s.limitG * G0)
                return Seal(ref scratch, Hit(FailureKind.OverG, 0, s, s.accelMps2 / G0, s.limitG));
            if (s.alphaRad > TumbleAlphaRad && s.qPa > TumbleQPa)
                return Seal(ref scratch, Hit(FailureKind.Tumble, 1, s, s.alphaRad * 180.0 / Math.PI, 15));
            if (s.stability < 0 && s.car < 1)
            {
                if (!scratch.tumbleOn) { scratch.tumbleOn = true; scratch.tumbleT0 = s.t; }
                if (s.t - scratch.tumbleT0 >= TumbleHoldS)
                    return Seal(ref scratch, Hit(FailureKind.Tumble, 0, s, s.stability, 0));
            }
            else scratch.tumbleOn = false;
            if (s.baseHeat || (s.maxTempK > 0 && s.noseT > s.maxTempK))
                return Seal(ref scratch, Hit(FailureKind.Thermal, s.baseHeat ? 1 : 0, s, s.baseHeat ? s.engineCount : s.noseT, s.baseHeat ? 9 : s.maxTempK));
            if (s.propEmptyBelowTarget)
                return Seal(ref scratch, Hit(FailureKind.PropDepletion, 0, s, s.t, 0));
            if (s.sepArmed && s.sepAgeS >= 0 && s.sepAgeS <= RecoveryModel.SeparationWindowS && s.sepRelSpeed < s.sepMinSpeed)
                return Seal(ref scratch, Hit(FailureKind.SeparationCollision, 0, s, s.sepRelSpeed, s.sepMinSpeed));
            if (s.legsArmed && s.sinkRate > s.vLeg)
                return Seal(ref scratch, Hit(FailureKind.LegCollapse, 0, s, s.sinkRate, s.vLeg));
            if (s.pcRated > 0 && s.pc > OverpressureFactor * s.pcRated)
                return Seal(ref scratch, Hit(FailureKind.EngineOverpressure, 1, s, s.pc, s.pcRated));

            if (!s.gentle)
            {
                double dt = s.dt > 0 ? s.dt : 0.05;
                if (s.hazardPerS > 0 && Draw(ref scratch) < 1 - Math.Exp(-s.hazardPerS * dt))
                    return Seal(ref scratch, Hit(FailureKind.EngineOverpressure, 0, s, s.pc, s.pcRated));
                if (Math.Abs(s.mixtureOffsetPct) > MixtureHazardPct)
                {
                    double x = Math.Abs(s.mixtureOffsetPct) / MixtureHazardPct - 1;
                    double lambda = 0.02 * x * x;
                    if (lambda > 0 && Draw(ref scratch) < 1 - Math.Exp(-lambda * dt))
                        return Seal(ref scratch, Hit(FailureKind.BadMixture, 0, s, s.mixtureOffsetPct, MixtureHazardPct));
                }
                if (s.rollEngines && scratch.rolledStage != s.stage && s.engineCount > 0 && s.reliability > 0 && s.reliability < 1)
                {
                    scratch.rolledStage = s.stage;
                    int dead = 0;
                    int first = -1;
                    for (int i = 0; i < s.engineCount; i++)
                    {
                        if (Draw(ref scratch) > s.reliability) { if (first < 0) first = i; dead++; }
                    }
                    scratch.enginesOut = dead;
                    if (dead > 0)
                    {
                        bool tolerate = dead == 1 && s.engineCount >= EngineOutMinCount && s.redundant
                            && s.massKg > 0 && (s.thrustN * (s.engineCount - 1) / s.engineCount) / (s.massKg * G0) > EngineOutMinTwr;
                        if (tolerate) scratch.thrustFactor = (s.engineCount - 1.0) / s.engineCount;
                        else
                        {
                            var hit = Hit(FailureKind.EngineOut, 0, s, dead, s.engineCount);
                            hit.engine = first < 0 ? 0 : first;
                            return Seal(ref scratch, hit);
                        }
                    }
                }
            }
            else if (s.rollEngines)
                scratch.rolledStage = s.stage;

            return default;
        }

        public static FailureEvent Explain(FailureHit hit, double t, double qPeakPa)
        {
            var ev = new FailureEvent
            {
                t = t,
                kind = FailureKind.Name(hit.kind),
                stage = hit.stage,
                engineIndex = hit.engine,
                value = hit.value,
                limit = hit.limit,
                cause = Cause(hit),
            };
            double qShow = hit.kind == FailureKind.StructuralMaxQ && hit.cause == 0 && qPeakPa > hit.value ? qPeakPa : hit.value;
            if (hit.kind == FailureKind.StructuralMaxQ && hit.cause == 0) ev.value = qShow;
            ev.kid = Kid(hit, t, qShow);
            ev.engineer = Engineer(hit, t, qShow);
            ev.fixHint = Fix(hit.kind);
            return ev;
        }

        static FailureHit Seal(ref FailureScratch scratch, FailureHit hit)
        {
            scratch.closed = true;
            return hit;
        }

        static FailureHit Hit(int kind, int cause, in FailureSample s, double value, double limit)
        {
            return new FailureHit
            {
                on = true,
                kind = kind,
                cause = cause,
                stage = s.stage,
                engine = -1,
                value = value,
                limit = limit,
            };
        }

        static double Draw(ref FailureScratch scratch)
        {
            ulong z = scratch.rng += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (z >> 11) * (1.0 / 9007199254740992.0);
        }

        static string Cause(FailureHit hit)
        {
            switch (hit.kind)
            {
                case FailureKind.StructuralMaxQ:
                    if (hit.cause == 1) return "margin";
                    if (hit.cause == 2) return "hot-staging";
                    if (hit.cause == 3) return "common-bulkhead";
                    return "q";
                case FailureKind.Tumble: return hit.cause == 1 ? "alpha" : "stability";
                case FailureKind.EngineOverpressure: return hit.cause == 1 ? "pc-cap" : "hazard";
                case FailureKind.Thermal: return hit.cause == 1 ? "base-heating" : "nose";
                default: return FailureKind.Name(hit.kind);
            }
        }

        static string Fix(int kind)
        {
            switch (kind)
            {
                case FailureKind.StructuralMaxQ: return "raise SF, use a stronger material, lower maxQThrottle";
                case FailureKind.OverG: return "throttle down, add gLimit";
                case FailureKind.Tumble: return "bigger fins, more gimbal, move the mass up";
                case FailureKind.EngineOverpressure: return "lower pc";
                case FailureKind.BadMixture: return "return to nominal MR";
                case FailureKind.Thermal: return "heat shield, different nose";
                case FailureKind.CryoMaterial: return "change material";
                case FailureKind.PropDepletion: return "more propellant / stage";
                case FailureKind.SeparationCollision: return "more sepImpulse, springs";
                case FailureKind.LegCollapse: return "heavier legs, more reserve";
                case FailureKind.EngineOut: return "more engines + redundant avionics";
                default: return "";
            }
        }

        static string Kid(FailureHit hit, double t, double qPa)
        {
            string ts = N0(t);
            switch (hit.kind)
            {
                case FailureKind.StructuralMaxQ when hit.cause == 2:
                    return "The stages slammed together at T+" + ts + " s. Hot staging needs a vented interstage.";
                case FailureKind.StructuralMaxQ when hit.cause == 3:
                    return "The shared tank wall failed on the pad. The pressure across it is too high. Split the tanks or lower the pressure.";
                case FailureKind.StructuralMaxQ when hit.cause == 1:
                    return "Your rocket bent in half at T+" + ts + " s. The stack was loaded past what the tanks can hold. Try a higher safety factor or a stronger material.";
                case FailureKind.StructuralMaxQ:
                    return "Your rocket bent in half at T+" + ts + " s. The air pushed " + N0(qPa / 1000.0) + " kPa, but your tanks could only take " + N0(hit.limit / 1000.0) + " kPa. Try a higher safety factor or a lower max-Q throttle.";
                case FailureKind.OverG:
                    return "Your rocket crushed its payload at T+" + ts + " s. The push hit " + N1(hit.value) + " g and the limit is " + N1(hit.limit) + " g. Throttle down or set a g limit.";
                case FailureKind.Tumble when hit.cause == 1:
                    return "Your rocket flipped at T+" + ts + " s. It was flying " + N0(hit.value) + "° sideways in thick air. Add fins or ease the turn.";
                case FailureKind.Tumble:
                    return "Your rocket flipped at T+" + ts + " s. The heavy end was the wrong way around (stability " + N2(hit.value) + " cal). Add fins, more gimbal, or move mass up.";
                case FailureKind.EngineOverpressure:
                    return "An engine blew its nozzle at T+" + ts + " s. Chamber pressure was " + N0(hit.value) + " bar against a rated " + N0(hit.limit) + " bar. Lower the chamber pressure.";
                case FailureKind.BadMixture:
                    return "An engine sputtered out at T+" + ts + " s. The mixture was " + N1(Math.Abs(hit.value)) + "% off. Put it back near the normal mix.";
                case FailureKind.Thermal when hit.cause == 1:
                    return "The base of the rocket overheated at T+" + ts + " s. More than nine engines need a heat shield.";
                case FailureKind.Thermal:
                    return "The skin burned through at T+" + ts + " s. It reached " + N0(hit.value) + " K and the material handles " + N0(hit.limit) + " K. Add a heat shield or a cooler nose.";
                case FailureKind.CryoMaterial:
                    return "The tanks cracked on the pad. This fuel is far too cold for that material. Pick a tank that can take the cold.";
                case FailureKind.PropDepletion:
                    return "Your rocket ran out of fuel at T+" + ts + " s, short of the target. Load more propellant or add a stage.";
                case FailureKind.SeparationCollision:
                    return "The stages bumped at T+" + ts + " s. They only moved apart at " + N2(hit.value) + " m/s and needed " + N2(hit.limit) + " m/s. Use a stronger separation push.";
                case FailureKind.LegCollapse:
                    return "The legs folded at touchdown. The booster was falling at " + N1(hit.value) + " m/s and the legs take " + N1(hit.limit) + " m/s. Use heavier legs or save more fuel for landing.";
                case FailureKind.EngineOut:
                    return "An engine quit at T+" + ts + " s. The stage could not stay up on the ones left. Add engines and redundant avionics.";
                default:
                    return "";
            }
        }

        static string Engineer(FailureHit hit, double t, double qPa)
        {
            var inv = CultureInfo.InvariantCulture;
            string k = FailureKind.Name(hit.kind);
            switch (hit.kind)
            {
                case FailureKind.StructuralMaxQ when hit.cause == 0:
                    return k + ": q " + qPa.ToString("0.0", inv) + " Pa > q_lim " + hit.limit.ToString("0.0", inv) + " Pa at T+" + t.ToString("0.00", inv) + " s.";
                case FailureKind.StructuralMaxQ when hit.cause == 1:
                    return k + ": MS " + hit.value.ToString("0.000", inv) + " < 0 at T+" + t.ToString("0.00", inv) + " s.";
                case FailureKind.OverG:
                    return k + ": " + hit.value.ToString("0.00", inv) + " g > limit " + hit.limit.ToString("0.00", inv) + " g.";
                case FailureKind.Tumble:
                    return k + " (" + Cause(hit) + "): S " + hit.value.ToString("0.000", inv) + " at T+" + t.ToString("0.00", inv) + " s.";
                default:
                    return k + " (" + Cause(hit) + "): value " + hit.value.ToString("0.000", inv) + " limit " + hit.limit.ToString("0.000", inv) + " at T+" + t.ToString("0.00", inv) + " s.";
            }
        }

        static string N0(double x) => Math.Round(x).ToString(CultureInfo.InvariantCulture);
        static string N1(double x) => x.ToString("0.0", CultureInfo.InvariantCulture);
        static string N2(double x) => x.ToString("0.00", CultureInfo.InvariantCulture);
    }

    /// <summary>Design-side adapter. The integrator calls <see cref="Consider"/> once per step.</summary>
    public sealed class FailureMonitor
    {
        struct Info
        {
            public bool cryo, sepCheck, hotStaging, bulkhead, baseHeat, redundant;
            public double sepImpulse, pc, pcRated, hazard, mixture, reliability, thrustN, fAllow, radius;
            public int engines, designIndex;
        }

        struct Knot { public double frac, stability, carCoeff, cn, cg, cp, mCtrl; }

        readonly Info[] infos;
        readonly Knot[] knots;
        readonly int stages;
        readonly double qLimPa, limitG, maxTempK, heatFactor, vLeg;
        readonly bool legs, gentle;
        FailureScratch scratch;
        double sepV, sepT;
        bool sepLive;
        int sepStage;
        public bool Predict;
        public bool Gentle;
        public FailureEvent Event;
        public List<FlightRisk> Risks;

        FailureMonitor(Info[] infos, Knot[] knots, double qLimPa, double limitG, double maxTempK, double heatFactor, double vLeg, bool legs, bool gentle, ulong hash, ulong seed)
        {
            this.infos = infos;
            this.knots = knots;
            stages = infos.Length;
            this.qLimPa = qLimPa;
            this.limitG = limitG;
            this.maxTempK = maxTempK;
            this.heatFactor = heatFactor;
            this.vLeg = vLeg;
            this.legs = legs;
            this.gentle = gentle;
            Gentle = gentle;
            scratch = FailureModel.Fresh(hash, seed);
        }

        public double ThrustFactor => scratch.thrustFactor > 0 ? scratch.thrustFactor : 1;
        public double QLimPa => qLimPa;
        public double MinStability => scratch.minStability;
        public double MinCar => scratch.minCar;

        public static bool GentleTier(Design d)
        {
            var t = d?.tier;
            return t == "1" || t == "2" || t == "3" || t == "kid";
        }

        public static bool Engages(Design d)
        {
            if (d == null) return false;
            if (d.version >= 3) return true;
            if (d.guidance != null && d.guidance.Any) return true;
            if (d.legsKind != null || !string.IsNullOrEmpty(d.recoveryMode) || d.heatShieldSet || d.boosterSepSet || d.landingReserveSet) return true;
            if (d.gridFinDeployAltSet || !string.IsNullOrEmpty(d.finPlanform) || d.finRootChordSet || d.missionId != null) return true;
            if (StageOn(d.strapOns)) return true;
            for (int i = 0; i < d.stages.Count; i++) if (StageOn(d.stages[i])) return true;
            return false;
        }

        static bool StageOn(StagePart s)
        {
            if (s == null) return false;
            if (s.mixtureOffsetSet || s.sepImpulseSet || s.sepMode != null) return true;
            if (s.engineParams != null && s.engineParams.Any) return true;
            return s.tank != null && s.tank.Any;
        }

        public static FailureMonitor Create(Catalog c, Design d, double targetAlt, ulong designHash = 1, ulong missionSeed = 1)
        {
            var list = new List<Info>();
            if (d != null && d.StrapOnCount > 0 && d.stages.Count > 0)
            {
                list.Add(Make(c, d, d.strapOns, false));
                list.Add(Make(c, d, d.stages[0], true));
                for (int i = 1; i < d.stages.Count; i++) list.Add(Make(c, d, d.stages[i], true));
            }
            else if (d != null)
            {
                for (int i = 0; i < d.stages.Count; i++) list.Add(Make(c, d, d.stages[i], i > 0));
            }
            if (list.Count == 0) list.Add(new Info());
            double qLim = 0;
            if (d != null && c != null)
            {
                for (int i = 0; i < d.stages.Count; i++)
                {
                    double q = StageQ(c, d, d.stages[i]);
                    if (q > 0 && (qLim <= 0 || q < qLim)) qLim = q;
                }
                if (d.StrapOnCount > 0)
                {
                    double q = StageQ(c, d, d.strapOns);
                    if (q > 0 && (qLim <= 0 || q < qLim)) qLim = q;
                }
                double fair = StructureModel.FairingQLimPa(c, d);
                if (fair > 0 && (qLim <= 0 || fair < qLim)) qLim = fair;
            }
            double maxG = 8;
            if (c != null && d != null && d.payloadType != null && c.PayloadById.TryGetValue(d.payloadType, out var pl) && pl.maxG > 0 && pl.maxG < maxG)
                maxG = pl.maxG;
            double maxT = 0;
            if (c != null && d != null && d.fairingMaterial != null && c.MaterialById.TryGetValue(d.fairingMaterial, out var fm))
                maxT = fm.maxTempK;
            double heat = d == null ? 1 : AeroModel.NoseHeatingFactor(d.nose);
            bool land = d != null && d.legs && d.recoveryMode != null && d.recoveryMode != "expendable";
            double vleg = land ? RecoveryModel.VLeg(d.legsKind) : 0;
            var knots = Knots(c, d);
            return new FailureMonitor(list.ToArray(), knots, qLim, maxG, maxT, heat, vleg, land, GentleTier(d), designHash, missionSeed);
        }

        static double StageQ(Catalog c, Design d, StagePart s)
        {
            if (s == null || !c.MaterialById.TryGetValue(s.material ?? "", out var m)) return 0;
            return StructureModel.QLimPa(d.safetyFactor, m.yieldMPa);
        }

        static Info Make(Catalog c, Design d, StagePart s, bool sep)
        {
            var info = new Info { designIndex = -1 };
            if (s == null || c == null) return info;
            c.EngineById.TryGetValue(s.engine ?? "", out var eng);
            var point = EnginePoint.Of(c, s);
            info.engines = s.engineCount > 0 ? s.engineCount : 1;
            if (s.count > 1) info.engines *= s.count;
            info.reliability = eng != null ? eng.reliability : 1;
            info.thrustN = point.known ? point.thrustVac * info.engines : 0;
            info.pc = point.known && eng != null ? (s.engineParams != null && s.engineParams.pcSet ? s.engineParams.pc : eng.pc) : 0;
            if (eng != null)
            {
                var env = EngineModel.EnvelopeFor(eng);
                info.pcRated = env.pcRated;
                info.hazard = EngineModel.OverpressurePerSecond(info.pc > 0 ? info.pc : env.pcRated, env.pcRated);
            }
            info.mixture = s.MixtureOffsetPctOrZero;
            info.cryo = Cryo(c, s);
            info.bulkhead = s.tank != null && s.tank.commonBulkhead && TankModel.CommonBulkheadHazard(true, s.tank.pressureBar);
            info.hotStaging = sep && RecoveryModel.HotStagingOverpressure(s.sepMode, s.interstageShape);
            info.sepCheck = sep && (s.sepImpulseSet || s.sepMode != null || info.hotStaging);
            info.sepImpulse = s.SepImpulseOrZero;
            info.redundant = d != null && d.avionicsTier == "redundant";
            info.baseHeat = d != null && !d.heatShield && info.engines > 9;
            // Hoop margin only when a tank pressure is set. Unpressurized stages break on q_lim,
            // which is the max-Q structural limit. A liftoff axial load would otherwise trip MS first.
            bool pressure = s.tank != null && s.tank.pressureSet && s.tank.pressureBar > 0;
            info.fAllow = pressure ? StructureModel.Allowables(c, s, d != null ? d.safetyFactor : 1.4).fAllowN : 0;
            info.radius = s.diameter > 0 ? s.diameter / 2 : 0;
            return info;
        }

        static bool Cryo(Catalog c, StagePart s)
        {
            if (s == null || !c.PropellantById.TryGetValue(s.propellant ?? "", out var p)) return false;
            if (p.storability == null || p.storability.IndexOf("cryo", StringComparison.Ordinal) < 0) return false;
            if (!c.MaterialById.TryGetValue(s.material ?? "", out var m)) return false;
            return !m.cryoOK;
        }

        static Knot[] Knots(Catalog c, Design d)
        {
            var k = new Knot[5];
            double[] frac = { 1, 0.75, 0.5, 0.25, 0.05 };
            for (int i = 0; i < frac.Length; i++)
            {
                k[i].frac = frac[i];
                if (c == null || d == null || d.stages.Count == 0) continue;
                var b = Balance.Along(c, d, frac[i], 1, 0);
                k[i].stability = b.stabilityCal;
                k[i].cn = b.cnAlpha;
                k[i].cg = b.cg_m;
                k[i].cp = b.cpRaw_m;
                k[i].mCtrl = b.controlMoment;
                k[i].carCoeff = b.aeroMoment;
            }
            return k;
        }

        void Interp(double frac, out double stability, out double car, out double cn, out double cg, out double cp, double qPa)
        {
            stability = 0; car = 0; cn = 0; cg = 0; cp = 0;
            if (knots == null || knots.Length == 0) return;
            int hi = knots.Length - 1;
            int n = -1;
            if (frac >= knots[0].frac) n = 0;
            else if (frac <= knots[hi].frac) n = hi;
            else
            {
                int i = 0;
                while (i < hi && !(frac <= knots[i].frac && frac >= knots[i + 1].frac)) i++;
                if (i >= hi) n = hi;
                else
                {
                    double span = knots[i].frac - knots[i + 1].frac;
                    double u = span > 0 ? (knots[i].frac - frac) / span : 0;
                    stability = knots[i].stability + (knots[i + 1].stability - knots[i].stability) * u;
                    cn = knots[i].cn + (knots[i + 1].cn - knots[i].cn) * u;
                    cg = knots[i].cg + (knots[i + 1].cg - knots[i].cg) * u;
                    cp = knots[i].cp + (knots[i + 1].cp - knots[i].cp) * u;
                    double mCtrl = knots[i].mCtrl + (knots[i + 1].mCtrl - knots[i].mCtrl) * u;
                    double coeff = knots[i].carCoeff + (knots[i + 1].carCoeff - knots[i].carCoeff) * u;
                    car = CarOf(mCtrl, coeff, qPa);
                    return;
                }
            }
            stability = knots[n].stability;
            cn = knots[n].cn;
            cg = knots[n].cg;
            cp = knots[n].cp;
            car = CarOf(knots[n].mCtrl, knots[n].carCoeff, qPa);
        }

        static double CarOf(double mCtrl, double coeff, double qPa)
        {
            double mAero = coeff * (qPa > 0 ? qPa : 0);
            return mAero > 0 ? mCtrl / mAero : (mCtrl > 0 ? double.PositiveInfinity : 0);
        }

        public struct Step
        {
            public double t, dt, alt, qPa, accel, alphaRad, mass, area, cd, vDown;
            public int stageIndex;
            public double propFraction;
            public bool thrusting, emptyLast;
            public double targetAlt;
        }

        /// <summary>True when thrust should stop. Predict records the hit and returns false.</summary>
        public bool Consider(in Step step)
        {
            int idx = step.stageIndex;
            if (idx < 0) idx = 0;
            if (idx >= stages) idx = stages - 1;
            var info = infos[idx];
            Interp(step.propFraction, out double stability, out double car, out double cn, out double cg, out double cp, step.qPa);
            double ms = double.PositiveInfinity;
            if (info.fAllow > 0 && step.mass > 0 && step.qPa > 0)
            {
                double bend = step.qPa * step.area * cn * step.alphaRad * Math.Abs(cp - cg);
                double feq = info.radius > 0 ? Math.Abs(2 * bend / info.radius) : 0;
                double load = step.mass * step.accel + step.qPa * step.area * step.cd + feq;
                if (load > 0) ms = info.fAllow / load - 1;
            }
            double tAtm = step.alt < 11000 ? 288.15 - 0.0065 * Math.Max(0, step.alt) : 216.65;
            double nose = tAtm + heatFactor * (step.qPa / 1000.0) * 2.0;
            bool sepHitArmed = false;
            double sepVnow = 0, sepMin = 0, sepAge = 0;
            if (sepLive)
            {
                sepAge = step.t - sepT;
                sepVnow = sepV;
                sepMin = RecoveryModel.MinRelSpeed(step.qPa / 1000.0);
                sepHitArmed = true;
                if (sepAge > RecoveryModel.SeparationWindowS) sepLive = false;
            }
            var sample = new FailureSample
            {
                t = step.t,
                dt = step.dt,
                qPa = step.qPa,
                qLimPa = qLimPa,
                margin = ms,
                accelMps2 = step.accel,
                limitG = limitG,
                alphaRad = step.alphaRad,
                stability = stability,
                car = car,
                pc = info.pc,
                pcRated = info.pcRated,
                hazardPerS = info.hazard,
                mixtureOffsetPct = info.mixture,
                noseT = nose,
                maxTempK = maxTempK,
                sepRelSpeed = sepVnow,
                sepMinSpeed = sepMin,
                sepAgeS = sepAge,
                sinkRate = step.vDown,
                vLeg = vLeg,
                cryoFail = info.cryo && step.t <= step.dt,
                baseHeat = info.baseHeat && step.qPa > 5000,
                hotStaging = false,
                bulkhead = info.bulkhead && step.t <= step.dt,
                propEmptyBelowTarget = step.emptyLast && step.alt < step.targetAlt,
                sepArmed = sepHitArmed,
                legsArmed = false,
                gentle = Gentle || gentle,
                rollEngines = step.thrusting,
                stage = idx + 1,
                engineCount = info.engines,
                reliability = info.reliability,
                redundant = info.redundant,
                thrustN = info.thrustN,
                massKg = step.mass,
            };
            var hit = FailureModel.Evaluate(sample, ref scratch);
            if (!hit.on) return false;
            if (Event == null) Event = FailureModel.Explain(hit, step.t, scratch.qPeak);
            return !Predict;
        }

        public bool OnEmpty(double t, double alt, bool last, double targetAlt)
        {
            if (!last || !(targetAlt > 0) || !(alt < targetAlt)) return false;
            var step = new Step { t = t, dt = 0.05, alt = alt, targetAlt = targetAlt, emptyLast = true, stageIndex = stages - 1, propFraction = 0 };
            return Consider(step);
        }

        public bool OnSeparation(double t, double alt, double qPa, int spentIndex, double massAft, double massForward)
        {
            int next = spentIndex + 1;
            if (next < 0 || next >= stages) return false;
            var info = infos[next];
            if (info.hotStaging)
            {
                var sample = new FailureSample { t = t, qPa = qPa, qLimPa = qLimPa, hotStaging = true, stage = next + 1, margin = double.PositiveInfinity, car = double.PositiveInfinity };
                var hit = FailureModel.Evaluate(sample, ref scratch);
                if (hit.on && Event == null) Event = FailureModel.Explain(hit, t, scratch.qPeak);
                if (hit.on && !Predict) return true;
            }
            if (!info.sepCheck) return false;
            sepV = RecoveryModel.SeparationRelSpeed(info.sepImpulse, massAft, massForward);
            sepT = t;
            sepLive = true;
            sepStage = next;
            var s = new Step { t = t, dt = 0.05, alt = alt, qPa = qPa, stageIndex = next, propFraction = 1, targetAlt = 1e9 };
            return Consider(s);
        }

        public bool OnImpact(double t, double sink)
        {
            if (!legs || !(vLeg > 0)) return false;
            var sample = new FailureSample
            {
                t = t,
                sinkRate = sink,
                vLeg = vLeg,
                legsArmed = true,
                stage = 1,
                margin = double.PositiveInfinity,
                car = double.PositiveInfinity,
                stability = scratch.minStability,
            };
            var hit = FailureModel.Evaluate(sample, ref scratch);
            if (hit.on && Event == null) Event = FailureModel.Explain(hit, t, scratch.qPeak);
            return hit.on && !Predict;
        }

        public void Finish(double qPeakPa)
        {
            if (Event != null && Event.kind == "structural-maxq" && Event.cause == "q" && qPeakPa > Event.value)
            {
                Event.value = qPeakPa;
                Event.kid = "Your rocket bent in half at T+" + Math.Round(Event.t).ToString(CultureInfo.InvariantCulture)
                    + " s. The air pushed " + Math.Round(qPeakPa / 1000.0).ToString(CultureInfo.InvariantCulture)
                    + " kPa, but your tanks could only take " + Math.Round(Event.limit / 1000.0).ToString(CultureInfo.InvariantCulture)
                    + " kPa. Try a higher safety factor or a lower max-Q throttle.";
                Event.engineer = "structural-maxq: q " + qPeakPa.ToString("0.0", CultureInfo.InvariantCulture)
                    + " Pa > q_lim " + Event.limit.ToString("0.0", CultureInfo.InvariantCulture)
                    + " Pa at T+" + Event.t.ToString("0.00", CultureInfo.InvariantCulture) + " s.";
            }
            Risks = BuildRisks();
        }

        public List<FlightRisk> BuildRisks()
        {
            var list = new List<FlightRisk>(11);
            Add(list, FailureKind.StructuralMaxQ, qLimPa <= 0 ? 1 : (qLimPa - scratch.qPeak) / qLimPa, scratch.qPeak, qLimPa, "structure", "safetyFactor", EventKind(FailureKind.StructuralMaxQ));
            double gLim = limitG > 0 ? limitG : 8;
            Add(list, FailureKind.OverG, (gLim - scratch.gPeak) / gLim, scratch.gPeak, gLim, "guidance", "gLimit", EventKind(FailureKind.OverG));
            double sMin = double.IsPositiveInfinity(scratch.minStability) ? 0 : scratch.minStability;
            double carMin = double.IsPositiveInfinity(scratch.minCar) ? 1 : scratch.minCar;
            bool tumble = EventKind(FailureKind.Tumble) || (sMin < 0 && carMin < 1);
            Add(list, FailureKind.Tumble, tumble ? -1 : Math.Min(sMin, carMin - 1), sMin, 0, "aero", "fins", tumble);
            bool op = false;
            double pc = 0, rated = 1;
            for (int i = 0; i < stages; i++)
            {
                if (infos[i].pcRated <= 0) continue;
                pc = infos[i].pc;
                rated = infos[i].pcRated;
                if (infos[i].hazard > 0 || pc > rated) op = true;
            }
            Add(list, FailureKind.EngineOverpressure, rated > 0 ? (FailureModel.OverpressureFactor * rated - pc) / rated : 1, pc, rated, "engines", "engineParams.pc", op || EventKind(FailureKind.EngineOverpressure));
            bool mix = false;
            double off = 0;
            for (int i = 0; i < stages; i++) if (Math.Abs(infos[i].mixture) > Math.Abs(off)) { off = infos[i].mixture; if (Math.Abs(off) > FailureModel.MixtureHazardPct) mix = true; }
            Add(list, FailureKind.BadMixture, (FailureModel.MixtureHazardPct - Math.Abs(off)) / FailureModel.MixtureHazardPct, off, FailureModel.MixtureHazardPct, "propellants", "mixtureOffsetPct", mix || EventKind(FailureKind.BadMixture));
            bool heat = EventKind(FailureKind.Thermal);
            for (int i = 0; i < stages && !heat; i++) if (infos[i].baseHeat) heat = true;
            Add(list, FailureKind.Thermal, heat ? -1 : 1, heat ? 1 : 0, maxTempK, "aero", "nose", heat);
            bool cryo = false;
            int cryoStage = -1;
            for (int i = 0; i < stages; i++) if (infos[i].cryo) { cryo = true; cryoStage = infos[i].designIndex; }
            var cryoRisk = Risk(FailureKind.CryoMaterial, cryo ? -1 : 1, cryo ? 1 : 0, 0, "tanks", "material", cryo || EventKind(FailureKind.CryoMaterial));
            cryoRisk.stage = cryoStage;
            list.Add(cryoRisk);
            Add(list, FailureKind.PropDepletion, EventKind(FailureKind.PropDepletion) ? -1 : 1, 0, 0, "propellants", "propMass", EventKind(FailureKind.PropDepletion));
            Add(list, FailureKind.SeparationCollision, EventKind(FailureKind.SeparationCollision) ? -1 : 1, sepV, 0, "stages", "sepImpulse", EventKind(FailureKind.SeparationCollision));
            Add(list, FailureKind.LegCollapse, EventKind(FailureKind.LegCollapse) ? -1 : (legs ? 0.2 : 1), vLeg, vLeg, "recovery", "legs.kind", EventKind(FailureKind.LegCollapse));
            bool fatalOut = EventKind(FailureKind.EngineOut);
            Add(list, FailureKind.EngineOut, fatalOut ? -1 : 0.5, scratch.enginesOut, 0, "engines", "engineCount", fatalOut);
            return list;
        }

        bool EventKind(int kind) => Event != null && Event.kind == FailureKind.Name(kind);

        static void Add(List<FlightRisk> list, int kind, double margin, double value, double limit, string tab, string field, bool red)
        {
            list.Add(Risk(kind, margin, value, limit, tab, field, red));
        }

        static FlightRisk Risk(int kind, double margin, double value, double limit, string tab, string field, bool forceRed)
        {
            string level = forceRed || margin < 0 ? "red" : (margin < 0.2 ? "amber" : "green");
            var fake = new FailureHit { on = true, kind = kind, value = value, limit = limit, engine = -1, stage = 1 };
            var explained = level == "red" ? FailureModel.Explain(fake, 0, value) : null;
            return new FlightRisk
            {
                kind = FailureKind.Name(kind),
                level = level,
                margin = margin,
                value = value,
                limit = limit,
                tab = tab,
                field = field,
                kid = explained != null ? explained.kid : "",
                engineer = explained != null ? explained.engineer : FailureKind.Name(kind) + " margin " + margin.ToString("0.00", CultureInfo.InvariantCulture),
            };
        }
    }
}
