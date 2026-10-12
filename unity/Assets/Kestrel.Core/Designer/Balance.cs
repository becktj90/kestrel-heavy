// Liftoff CG and a Barrowman-style CP. Game approximation, labelled as such.
// Stations match RocketMeshBuilder (metres, +Y up, base of stage 1 at y = 0) so the
// markers sit on the rocket the player sees. CP formulas: Barrowman 1967 as restated
// in the USU MAE notes (cone 0.666 Ln, ogive 0.466 Ln) and Crowell 1996 (von Karman 0.500 Ln).
// Cylinders contribute CNa = 0. The body term is the conical transition (shoulders and interstages).
// Grid fins are mass and drag only; they do not move CP.
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Designer
{
    public struct AeroBalance
    {
        public double cg_m, cp_m, stabilityCal, refDiameter_m, length_m, mass_kg;
    }

    public static class Balance
    {
        const double LegFraction = 0.06, GridFinKgPerM2 = 120, RcsKg = 250;

        public static AeroBalance Of(Catalog c, Design d)
        {
            double mSum = 0, my = 0;
            void Add(double mass, double y) { mSum += mass; my += mass * y; }

            var stages = d.stages;
            var transitions = new List<Transition>();
            var cones = new List<Cone>();
            double y = 0;
            for (int i = 0; i < stages.Count; i++)
            {
                var s = stages[i];
                c.EngineById.TryGetValue(s.engine ?? "", out var e);
                double bell = Bell(e != null ? e.diameter : 0);
                double r = s.diameter / 2;
                bool bottom = i == 0, top = i == stages.Count - 1;
                double thrust = 0.6 * r + (bottom ? 0 : bell * 0.7);
                double tank = DesignMath.SizeTank(c, s.propellant, s.propMass, s.diameter).length;
                double yBase = y + (bottom ? bell : 0);
                double yProp0 = yBase + thrust;
                double yTank1 = yProp0 + tank;

                double structure = Mass.StructureMass(c, s, d.safetyFactor);
                double engines = Mass.EnginesMass(c, s);
                double av = top ? AvionicsKg(d.avionicsTier) : 0;
                double yEngine = bottom ? bell / 2 : yBase - InterLength(stages[i - 1].diameter, s.diameter) / 2;
                Add(engines, yEngine);
                Add(structure, (yBase + yTank1) / 2);
                if (!top)
                {
                    double inter = InterLength(s.diameter, stages[i + 1].diameter);
                    Add(s.interstageMass, yTank1 + inter / 2);
                    Add(s.separationMass, yTank1);
                    transitions.Add(new Transition { yForward = yTank1 + inter, length = inter, dForward = stages[i + 1].diameter, dAft = s.diameter });
                    y = yTank1 + inter;
                }
                else
                {
                    Add(s.interstageMass, yTank1);
                    Add(s.separationMass, yTank1);
                    y = yTank1;
                }
                Add(av, yTank1);
                if (bottom)
                {
                    double before = structure + engines + s.interstageMass + s.separationMass + av;
                    if (d.legs) Add(LegFraction * before, 0);
                    if (d.gridFins) Add(GridFinKgPerM2 * Math.PI * r * r, yTank1);
                    if (d.rcs) Add(RcsKg, yTank1);
                }
                // Full tanks at liftoff, including the landing reserve. The rocket equation holds that reserve as dry.
                Add(s.propMass, (yProp0 + yTank1) / 2);
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
            if (so != null && so.count > 0 && c.EngineById.ContainsKey(so.engine ?? ""))
            {
                double sb = Bell(c.EngineById[so.engine].diameter);
                double sr = so.diameter / 2;
                double stank = DesignMath.SizeTank(c, so.propellant, so.propMass, so.diameter).length;
                double snose = 2.2 * sr;
                int n = so.count;
                Add(Mass.EnginesMass(c, so) * n, sb / 2);
                Add(80 * n, sb + stank / 2);
                Add(Mass.StructureMass(c, so, d.safetyFactor) * n, sb + (stank + snose) / 2);
                Add(so.propMass * n, sb + stank / 2);
                cones.Add(new Cone { yTip = sb + stank + snose, length = snose, diameter = so.diameter, count = n });
            }

            double length = yJoint + fairL;
            double dRef = stages.Count > 0 ? Stack.ReferenceDiameter(d) : 0;
            double cg = mSum == 0 ? 0 : my / mSum;
            double cp = Cp(d, stages, length, dRef, fairD, fairL, transitions, cones);
            return new AeroBalance
            {
                cg_m = cg, cp_m = cp, refDiameter_m = dRef, length_m = length, mass_kg = mSum,
                stabilityCal = dRef > 0 ? (cg - cp) / dRef : 0,
            };
        }

        struct Transition { public double yForward, length, dForward, dAft; }
        struct Cone { public double yTip, length, diameter; public int count; }

        static double Cp(Design d, List<StagePart> stages, double length, double dRef, double fairD, double fairL, List<Transition> transitions, List<Cone> cones)
        {
            if (!(dRef > 0) || !(length > 0)) return 0;
            double cna = 0, cnx = 0;
            void Add(double cn, double x) { cna += cn; cnx += cn * x; }

            double ln = fairL * NoseFraction(d.nose);
            double noseD = fairD > 0 ? fairD : (stages.Count > 0 ? stages[stages.Count - 1].diameter : dRef);
            Add(2 * Sq(noseD / dRef), NoseCpFactor(d.nose) * ln);

            foreach (var t in transitions)
            {
                if (!(t.dForward > 0) || !(t.dAft > 0)) continue;
                double cn = 2 * (Sq(t.dAft / dRef) - Sq(t.dForward / dRef));
                if (cn == 0) continue;
                double x = length - t.yForward;
                if (t.length > 0)
                {
                    double ratio = t.dForward / t.dAft;
                    double denom = 1 - ratio * ratio;
                    if (denom != 0) x += (t.length / 3) * (1 + (1 - ratio) / denom);
                }
                Add(cn, x);
            }

            if (d.fins && d.finCount > 0 && d.finSpanM > 0 && stages.Count > 0)
            {
                double s = d.finSpanM, cr = s, ct = s, lf = 0, xr = 0;
                int n = d.finCount;
                double cna1 = (4.0 * n * Sq(s / dRef)) / (1 + Math.Sqrt(1 + Sq(2 * lf / (cr + ct))));
                double radius = stages[0].diameter / 2;
                double kfb = 1 + radius / (s + radius);
                double xb = length - cr;
                double xf = xb + (xr / 3) * (cr + 2 * ct) / (cr + ct) + (1.0 / 6) * (cr + ct - cr * ct / (cr + ct));
                Add(kfb * cna1, xf);
            }

            foreach (var cone in cones)
            {
                // Conical nose, CP at 2/3 of the length aft of its own tip. Same factor as the core cone.
                double yCp = cone.yTip - (2.0 / 3.0) * cone.length;
                Add(cone.count * 2 * Sq(cone.diameter / dRef), length - yCp);
            }

            if (cna == 0) return length / 2;
            return length - cnx / cna;
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

        static double InterLength(double dLower, double dUpper) => 1.2 * Math.Max(dLower / 2, dUpper / 2);

        /// <summary>Fraction of fairing length that is nose. Same split as RocketMeshBuilder.</summary>
        public static double NoseFraction(string nose)
        {
            if (nose == "blunt") return 0.25;
            if (nose == "conic") return 0.4;
            return 0.45;
        }

        /// <summary>CP station as a fraction of nose length, measured from the tip.</summary>
        public static double NoseCpFactor(string nose)
        {
            if (nose == "conic") return 2.0 / 3.0;
            if (nose == "ogive") return 0.466;
            if (nose == "von-karman") return 0.5;
            if (nose == "blunt") return 1.0 / 3.0;
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
