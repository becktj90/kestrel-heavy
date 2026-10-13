// Liftoff CG and a Barrowman-style CP. Game approximation, labelled as such.
// Of() is the full-tank liftoff value the readout goldens lock. Along() is S(t): the same
// stations with the propellant level dropped, plus CAR at α = 4°. One remaining fraction
// scales every tank together (the max-Q check, while every stage is still lit).
// Stations match RocketMeshBuilder (metres, +Y up, base of stage 1 at y = 0) so the
// markers sit on the rocket the player sees. CP formulas: Barrowman 1967 as restated
// in the USU MAE notes (cone 0.666 Ln, ogive 0.466 Ln) and Crowell 1996 (von Karman 0.500 Ln).
// Cylinders contribute CNa = 0. The body term is the conical transition on each interstage
// (length 0.6 × the larger diameter) plus a zero-length shoulder where the fairing meets the top stage.
// Fins, when aero.fins is set, are an unswept square (chord = span) with the trailing edge at the base.
// l_F equals the span. CP is the quarter chord of the MAC, x_f metres aft of the root leading edge
// (0.25c for a square fin). The hangar station from the nose is length − (c_r − x_f).
// Grid fins are mass and drag only; they do not move CP.
// cp_m is the hangar marker: clamped to the nose when the raw CP is ahead of the vehicle.
// stabilityCal keeps the raw CP.
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Designer
{
    public struct AeroBalance
    {
        public double cg_m, cp_m, cpRaw_m, stabilityCal, refDiameter_m, length_m, mass_kg;
        public bool cpAheadOfNose;
    }

    /// <summary>§2.5 S(t) = (x_cg(t) − x_cp) / D and §2.7 CAR = M_ctrl / M_aero at α = 4°.</summary>
    public struct BurnPoint
    {
        public double propellantRemaining;
        public double cg_m, cp_m, cpRaw_m, stabilityCal, refDiameter_m, length_m, mass_kg;
        public double cnAlpha, car, controlMoment, aeroMoment;
        public bool cpAheadOfNose;
    }

    public static class Balance
    {
        public const double CarAlphaDeg = 4;
        const double GridFinKgPerM2 = 120, RcsKg = 250;

        public static AeroBalance Of(Catalog c, Design d)
        {
            var b = Build(c, d, 1, 0, 0);
            return new AeroBalance
            {
                cg_m = b.cg_m, cpRaw_m = b.cpRaw_m, cp_m = b.cp_m, cpAheadOfNose = b.cpAheadOfNose,
                refDiameter_m = b.refDiameter_m, length_m = b.length_m, mass_kg = b.mass_kg,
                stabilityCal = b.stabilityCal,
            };
        }

        /// <summary>
        /// Propellant remaining is 1 at liftoff and 0 when the tanks are empty. The liquid sits
        /// in the bottom of each tank, so the centroid moves aft as it drains. At 1 the stations
        /// match Of(). qPa and altitudeM feed CAR; they do not move the CG.
        /// </summary>
        public static BurnPoint Along(Catalog c, Design d, double propellantRemaining, double qPa = 0, double altitudeM = 0)
            => Build(c, d, propellantRemaining, qPa, altitudeM);

        static BurnPoint Build(Catalog c, Design d, double propLeft, double qPa, double altitudeM)
        {
            if (double.IsNaN(propLeft) || propLeft > 1) propLeft = 1;
            if (propLeft < 0) propLeft = 0;
            double mSum = 0, my = 0;
            void Add(double mass, double y) { mSum += mass; my += mass * y; }
            double stage0Y = 0, stage0F = 0, stage0N = 0, stage0Delta = 0;
            double strapY = 0, strapF = 0, strapN = 0, strapDelta = 0;
            double yRcs = 0;
            bool haveStage0 = false, haveStrap = false, haveRcs = false;

            var stages = d.stages;
            var transitions = new List<Transition>();
            var cones = new List<Cone>();
            double y = 0;
            for (int i = 0; i < stages.Count; i++)
            {
                var s = stages[i];
                c.EngineById.TryGetValue(s.engine ?? "", out var eng);
                var point = EnginePoint.Of(eng, s.nozzle);
                double bell = Bell(point.known ? point.diameter : 0);
                double r = s.diameter / 2;
                bool bottom = i == 0, top = i == stages.Count - 1;
                double thrust = 0.6 * r + (bottom ? 0 : bell * 0.7);
                double tank = DesignMath.SizeTank(c, s.propellant, s.propMass, s.diameter).length;
                double yBase = y + (bottom ? bell : 0);
                double yProp0 = yBase + thrust;
                double yTank1 = yProp0 + tank;

                double structure = Mass.StructureMass(c, s, d.safetyFactor);
                double engines = point.known ? point.mass * s.engineCount : 0;
                double av = top ? AvionicsKg(d.avionicsTier) : 0;
                double yEngine = bottom ? bell / 2 : yBase - InterstageAero.Length(stages[i - 1].diameter, s.diameter) / 2;
                if (bottom && point.known)
                {
                    haveStage0 = true;
                    stage0Y = yEngine;
                    stage0F = point.thrustSL;
                    stage0N = s.engineCount;
                    stage0Delta = GimbalDeg(s, point);
                }
                Add(engines, yEngine);
                Add(structure, (yBase + yTank1) / 2);
                double interMass = InterstageAero.MassKg(c, d, i);
                if (!top)
                {
                    double inter = InterstageAero.Length(s.diameter, stages[i + 1].diameter);
                    bool derivedHere = !s.interstageSpecified && i > 0 && !stages[i - 1].interstageSpecified;
                    if (derivedHere) Add(interMass, yBase - InterstageAero.Length(stages[i - 1].diameter, s.diameter) / 2);
                    else Add(interMass, yTank1 + inter / 2);
                    Add(s.separationMass, yTank1);
                    transitions.Add(new Transition { yForward = yTank1 + inter, length = inter, dForward = stages[i + 1].diameter, dAft = s.diameter });
                    y = yTank1 + inter;
                }
                else
                {
                    if (i > 0 && !s.interstageSpecified && !stages[i - 1].interstageSpecified)
                        Add(interMass, yBase - InterstageAero.Length(stages[i - 1].diameter, s.diameter) / 2);
                    else Add(interMass, yTank1);
                    Add(s.separationMass, yTank1);
                    y = yTank1;
                }
                Add(av, yTank1);
                if (bottom)
                {
                    double before = structure + engines + interMass + s.separationMass + av;
                    if (d.legs) Add(RecoveryModel.LegDryFraction(d.legsKind) * before, 0);
                    if (d.gridFins) Add(GridFinKgPerM2 * Math.PI * r * r, yTank1);
                    if (d.rcs) { Add(RcsKg, yTank1); haveRcs = true; yRcs = yTank1; }
                }
                // Full tanks at liftoff, including the landing reserve. The rocket equation holds that reserve as dry.
                // A lower fraction fills from the bottom of the tank, so the centroid moves aft.
                if (propLeft >= 1) Add(s.propMass, (yProp0 + yTank1) / 2);
                else if (propLeft > 0) Add(s.propMass * propLeft, yProp0 + propLeft * (yTank1 - yProp0) / 2);
            }

            double yJoint = y;
            double fairL = d.fairingLength;
            double fairD = d.fairingDiameter;
            if (stages.Count > 0)
                transitions.Add(new Transition { yForward = yJoint, length = 0, dForward = fairD, dAft = stages[stages.Count - 1].diameter });
            double noseFrac = NoseFraction(d.nose);
            Add(Mass.FairingMass(d), yJoint + fairL / 2);
            Add(d.payloadMass, yJoint + fairL * (1 - noseFrac) / 2);

            var so = d.strapOns;
            if (so != null && so.count > 0 && c.EngineById.TryGetValue(so.engine ?? "", out var soEng))
            {
                var sp = EnginePoint.Of(soEng, so.nozzle);
                double sb = Bell(sp.diameter);
                double sr = so.diameter / 2;
                double stank = DesignMath.SizeTank(c, so.propellant, so.propMass, so.diameter).length;
                double snose = 2.2 * sr;
                int n = so.count;
                Add(sp.mass * so.engineCount * n, sb / 2);
                if (sp.known)
                {
                    haveStrap = true;
                    strapY = sb / 2;
                    strapF = sp.thrustSL;
                    strapN = so.engineCount * n;
                    strapDelta = GimbalDeg(so, sp);
                }
                Add(80 * n, sb + stank / 2);
                Add(Mass.StructureMass(c, so, d.safetyFactor) * n, sb + (stank + snose) / 2);
                if (propLeft >= 1) Add(so.propMass * n, sb + stank / 2);
                else if (propLeft > 0) Add(so.propMass * n * propLeft, sb + propLeft * stank / 2);
                cones.Add(new Cone { yTip = sb + stank + snose, length = snose, diameter = so.diameter, count = n });
            }

            double length = yJoint + fairL;
            double dRef = stages.Count > 0 ? Stack.ReferenceDiameter(d) : 0;
            double cg = mSum == 0 ? 0 : my / mSum;
            double cpRaw = Cp(d, stages, length, dRef, fairD, fairL, transitions, cones, out double cna);
            bool ahead = cpRaw > length;
            double mCtrl = 0;
            if (haveStage0) mCtrl += Moment(stage0N, stage0F, stage0Delta, cg, stage0Y);
            if (haveStrap) mCtrl += Moment(strapN, strapF, strapDelta, cg, strapY);
            if (RecoveryModel.RcsActive(haveRcs, altitudeM)) mCtrl += RecoveryModel.RcsThrustN * Math.Abs(cg - yRcs);
            double alpha = CarAlphaDeg * Math.PI / 180.0;
            double sRef = dRef > 0 ? Math.PI * dRef * dRef / 4 : 0;
            double q = qPa > 0 ? qPa : 0;
            double mAero = q * sRef * Math.Abs(cna) * alpha * Math.Abs(cg - cpRaw);
            double car = mAero > 0 ? mCtrl / mAero : (mCtrl > 0 ? double.PositiveInfinity : 0);
            return new BurnPoint
            {
                propellantRemaining = propLeft,
                cg_m = cg, cpRaw_m = cpRaw, cp_m = ahead ? length : cpRaw, cpAheadOfNose = ahead,
                refDiameter_m = dRef, length_m = length, mass_kg = mSum,
                stabilityCal = dRef > 0 ? (cg - cpRaw) / dRef : 0,
                cnAlpha = cna, car = car, controlMoment = mCtrl, aeroMoment = mAero,
            };
        }

        static double GimbalDeg(StagePart s, EnginePoint point)
        {
            double cap = point.gimbal;
            double delta = s.engineParams != null && s.engineParams.gimbalDegSet ? s.engineParams.gimbalDeg : cap;
            if (delta < 0) delta = 0;
            if (delta > cap) delta = cap;
            return delta;
        }

        static double Moment(double count, double thrustEach, double deltaDeg, double cg, double yEngine)
            => count * thrustEach * Math.Sin(deltaDeg * Math.PI / 180.0) * Math.Abs(cg - yEngine);

        struct Transition { public double yForward, length, dForward, dAft; }
        struct Cone { public double yTip, length, diameter; public int count; }

        static double Cp(Design d, List<StagePart> stages, double length, double dRef, double fairD, double fairL, List<Transition> transitions, List<Cone> cones, out double cnaOut)
        {
            cnaOut = 0;
            if (!(dRef > 0) || !(length > 0)) return 0;
            double cna = 0, cnx = 0;
            void Add(double cn, double x) { cna += cn; cnx += cn * x; }

            double ln = fairL * NoseFraction(d.nose);
            double noseD = fairD > 0 ? fairD : (stages.Count > 0 ? stages[stages.Count - 1].diameter : dRef);
            Add(2 * Sq(noseD / dRef), NoseCpFactor(d.nose) * ln);

            foreach (var t in transitions)
            {
                InterstageAero.Conical(t.dAft, t.dForward, t.length, dRef, out double cn, out double xFwd);
                if (cn == 0) continue;
                Add(cn, (length - t.yForward) + xFwd);
            }

            if (AeroModel.IsGridPlanform(d.finPlanform))
            {
                // Grid fins are mass and drag. They do not move CP.
            }
            else if (AeroModel.TryFixedFin(d, out var fin) && stages.Count > 0)
            {
                double radius = stages[0].diameter / 2;
                double cn = AeroModel.FinCnAlpha(fin.count, fin.span, fin.rootChord, fin.tipChord, fin.leSweep, dRef, radius);
                double xf = FinCpAftOfRootLe(fin.rootChord, fin.tipChord, fin.leSweep);
                Add(cn, length - (fin.rootChord - xf));
            }
            else if (d.fins && d.finCount > 0 && d.finSpanM > 0 && stages.Count > 0)
            {
                // Unswept square: l_F is the span. The general mid-chord length
                // sqrt(s^2 + (x_t + (c_t - c_r)/2)^2) equals the span when x_t = 0 and c_r = c_t.
                double s = d.finSpanM, cr = s, ct = s, xt = 0;
                double lf = s;
                int n = d.finCount;
                double cna1 = (4.0 * n * Sq(s / dRef)) / (1 + Math.Sqrt(1 + Sq(2 * lf / (cr + ct))));
                double radius = stages[0].diameter / 2;
                double kfb = 1 + radius / (s + radius);
                double xf = FinCpAftOfRootLe(cr, ct, xt);
                Add(kfb * cna1, length - (cr - xf));
            }

            foreach (var cone in cones)
            {
                // Approximation, not a Barrowman body term: each strap-on nose is a full cone
                // 2 (d/d_ref)^2 at the core reference diameter. Strap-on fins and base drag are omitted.
                double yCp = cone.yTip - (2.0 / 3.0) * cone.length;
                Add(cone.count * 2 * Sq(cone.diameter / dRef), length - yCp);
            }

            cnaOut = cna;
            if (cna == 0) return length / 2;
            return length - cnx / cna;
        }

        /// <summary>
        /// Barrowman fin CP, metres aft of the root leading edge.
        /// x_f = (x_t/3)(c_r+2c_t)/(c_r+c_t) + (1/6)(c_r+c_t − c_r c_t/(c_r+c_t)).
        /// A square unswept fin is 0.25c. With the trailing edge on the base the station
        /// above the base is c_r − x_f (0.75c), not x_f itself.
        /// </summary>
        public static double FinCpAftOfRootLe(double cr, double ct, double xt)
        {
            double sum = cr + ct;
            if (!(sum > 0)) return 0;
            return (xt / 3.0) * (cr + 2 * ct) / sum + (1.0 / 6.0) * (sum - cr * ct / sum);
        }

        static double Sq(double x) => x * x;

        /// <summary>Exposed bell height. Same clamp as RocketMeshBuilder.</summary>
        public static double Bell(double engineDiameter)
        {
            double b = engineDiameter * 1.6;
            if (b < 0.4) return 0.4;
            if (b > 4) return 4;
            return b;
        }

        /// <summary>Fraction of fairing length that is nose. Same split as RocketMeshBuilder.</summary>
        public static double NoseFraction(string nose)
        {
            if (nose == "blunt" || nose == "capsule-tower") return 0.25;
            if (nose == "conic") return 0.4;
            return 0.45;
        }

        /// <summary>
        /// CP station as a fraction of nose length, measured from the tip.
        /// Cone 2/3, ogive 0.466 and von Karman 1/2 are the cited Barrowman factors.
        /// Blunt uses 1/3 as an elliptical-nose approximation; Barrowman does not give that shape.
        /// Biconic (0.55) and hammerhead (0.40) are the v3 noses. Capsule-tower uses the blunt factor.
        /// </summary>
        public static double NoseCpFactor(string nose)
        {
            if (nose == "conic") return 2.0 / 3.0;
            if (nose == "ogive") return 0.466;
            if (nose == "von-karman") return 0.5;
            if (nose == "blunt" || nose == "capsule-tower") return 1.0 / 3.0;
            if (nose == "biconic") return 0.55;
            if (nose == "hammerhead") return 0.40;
            return 0.466;
        }

        static double AvionicsKg(string tier)
        {
            if (tier == "basic") return 60;
            if (tier == "redundant") return 300;
            return 150;
        }
    }
}
