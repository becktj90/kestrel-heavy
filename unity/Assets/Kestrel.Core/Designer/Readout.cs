// Instant designer readout. Port of quickReadout() in web/js/designer/readout.js,
// plus the v2 fields (masses, sea-level Δv, CG/CP, cached max-Q).
// Quick() does not integrate. Max-Q is filled only when ReferenceAscent already cached this design.
// simReadout() is simulate(); data/golden/simulate.json and catalog-flight cover it.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Designer
{
    public sealed class StageReadout
    {
        public string label, role;
        public int designIndex = -1;
        public double m0, twr, twrVac;
        /// <summary>Δv (vac). Vacuum-Isp rocket equation for this serial phase. Not a loss model.</summary>
        public double deltaV;
        /// <summary>Serial-stage dry mass, including the landing reserve held as dry.</summary>
        public double dryMass_kg;
        /// <summary>Usable propellant burned in this serial phase.</summary>
        public double propMass_kg;
        /// <summary>
        /// Δv (SL, pad stage). ispSL held for the whole burn: a sea-level bound, not a loss model.
        /// Null except the phase that lights on the pad.
        /// </summary>
        public double? deltaVSL_mps;
        /// <summary>TWR at burnout of this phase, same thrust basis as <see cref="twr"/> (sea level on the pad phase).</summary>
        public double twrEnd;
        /// <summary>propellant / vacuum mass flow. Not shortened by a throttle cap.</summary>
        public double burnTime_s;
        /// <summary>m0 / (m0 − propellant).</summary>
        public double massRatio;
        /// <summary>Kilograms lost in one hour on the pad. Insulation uses the tank factor. Zero when the propellant does not boil.</summary>
        public double boilOff_kg;
        /// <summary>Liftoff axial margin, F_allow / (m·a) − 1. NaN when the stage has no allowable.</summary>
        public double msMin;
    }

    public sealed class QuickReadout
    {
        public VehicleMass mass;
        public List<StageReadout> stages = new List<StageReadout>();
        public double liftoffTWR, totalDeltaV, margin, required, targetAlt, liftoffMass;
        public long cost;
        public double reliability;
        public string orbitLabel;
        /// <summary>
        /// Liftoff CG, metres from the vehicle base, positive up. Full tanks, including landing reserve.
        /// TODO: a CG that follows the burn, and the stability at max-Q (about 60% of the propellant burned).
        /// </summary>
        public double cg_m;
        /// <summary>CP at the liftoff shape, same datum. Barrowman-style nose, body transitions and fins.</summary>
        public double cp_m;
        /// <summary>
        /// Liftoff stability, (liftoff CG − CP) / reference diameter. Positive means the CG is above the CP.
        /// TODO: the same margin at max-Q, once the CG follows the burn.
        /// </summary>
        public double stabilityCal;
        /// <summary>
        /// Peak dynamic pressure of the powered flight, from the cached reference ascent. Null while stale.
        /// A dense descent reopens the early stop so a lofted fall under thrust is included.
        /// Unpowered re-entry is not: the integrator stops when thrust ends.
        /// </summary>
        public double? maxQ_kPa;
        /// <summary>Geometric altitude at that peak. Null while stale.</summary>
        public double? maxQAlt_m;
        /// <summary>True until the debounced reference ascent for this design hash has finished.</summary>
        public bool maxQStale;
        /// <summary>Lowest stage or fairing q_lim, kPa. Null when no material yield is known.</summary>
        public double? qLim_kPa;
        /// <summary>min(payload maxG, 8). The over-g limit the failure model uses.</summary>
        public double gLimit;
        /// <summary>Peak sensed g from the cached predict ascent. Null while stale.</summary>
        public double? peakG;
        /// <summary>Lowest stability sampled on that ascent. Null while stale.</summary>
        public double? minStability;
        /// <summary>Lowest control-to-aero ratio sampled on that ascent. Null while stale.</summary>
        public double? minCar;
        public double? lossGravity_mps, lossDrag_mps, lossSteering_mps;
        /// <summary>Δv the landing reserve can actually buy. Zero when the stage holds none.</summary>
        public double landingReserve_mps;
        /// <summary>Boost-back Δv from the cached MECO speed. Null until that ascent exists, or when the vehicle is expendable (then 0).</summary>
        public double? landingNeed_mps;
        public bool? fairingFits;
        public double? fairingVolumeMargin_m3, fairingDiameterMargin_m;
        /// <summary>No budget field on the design yet. Stays null until campaign contracts land.</summary>
        public long? budget;
        public List<FlightRisk> risks;
        /// <summary>First predicted failure from the cached ascent. Null when the predict run stayed intact, or while stale.</summary>
        public FailureEvent predictedFailure;
    }

    public static class Readout
    {
        const double G0 = 9.80665;

        /// <summary>
        /// Instant path. O(stages) mass and rocket equation, one CG/CP pass, cost and reliability.
        /// Does not run Headless. Budget: p95 &lt; 0.5 ms (ReadoutBench).
        /// </summary>
        public static QuickReadout Quick(Catalog c, JsonObject design)
        {
            var d = Design.FromJson(design);
            var mass = Mass.Of(c, d);
            var serial = Stack.SerialStages(c, d);
            var sims = serial.stages.ConvertAll(s => s.ToSim());
            var dv = Headless.StackDeltaV(sims, serial.payload);
            var stages = new List<StageReadout>();
            for (int i = 0; i < serial.stages.Count; i++)
            {
                var s = serial.stages[i];
                double above = serial.payload;
                for (int j = i + 1; j < serial.stages.Count; j++) above += serial.stages[j].propMass + serial.stages[j].dryMass;
                double m0 = above + s.propMass + s.dryMass;
                double thrust = i == 0 ? s.thrustSL : s.thrustVac;
                // Only the first serial phase lights on the pad (stage 1, or core+strap-ons).
                // The core continuation after booster burnout is not a pad lighting.
                double? dvSl = null;
                if (i == 0) dvSl = RocketEq.DeltaV(s.ispSL, m0, m0 - s.propMass);
                double mEnd = m0 - s.propMass;
                double mdot = s.ispVac > 0 && s.thrustVac > 0 ? s.thrustVac / (G0 * s.ispVac) : 0;
                stages.Add(new StageReadout
                {
                    label = s.label, role = s.role, designIndex = s.index, m0 = m0,
                    twr = NonNegativeTwr(thrust, m0), twrVac = NonNegativeTwr(s.thrustVac, m0), deltaV = dv.perStage[i],
                    dryMass_kg = s.dryMass, propMass_kg = s.propMass, deltaVSL_mps = dvSl,
                    twrEnd = NonNegativeTwr(thrust, mEnd),
                    burnTime_s = mdot > 0 ? s.propMass / mdot : 0,
                    massRatio = mEnd > 0 ? m0 / mEnd : 0,
                    boilOff_kg = BoilOff(c, d, s),
                    msMin = AxialMargin(c, d, s, m0, thrust),
                });
            }
            var req = Stack.RequiredDeltaV(c, d.payloadOrbit);
            var price = CostModel.PriceDesign(c, d);
            var rel = Reliability.Of(c, d);
            var bal = Balance.Of(c, d);
            var readout = new QuickReadout
            {
                mass = mass, stages = stages,
                liftoffTWR = stages.Count > 0 ? ShownTwr(stages[0].twr) : 0,
                liftoffMass = stages.Count > 0 ? stages[0].m0 : mass.liftoffMass,
                totalDeltaV = dv.total, required = req.required, targetAlt = req.targetAlt,
                margin = (dv.total - req.required) / req.required,
                cost = price.recurring, reliability = rel.reliability, orbitLabel = req.orbit.label,
                cg_m = bal.cg_m, cp_m = bal.cp_m, stabilityCal = bal.stabilityCal,
                maxQStale = true,
                qLim_kPa = QLimKPa(c, d),
                gLimit = GLimit(c, d),
                landingReserve_mps = LandingReserve(c, d, mass),
                landingNeed_mps = d.recoveryMode == null || d.recoveryMode == "expendable" ? (double?)0 : null,
            };
            Fairing(d, c, readout);
            if (ReferenceAscent.TryGet(ReferenceAscent.CanonicalKey(c, design), out var flight) && flight.hit)
            {
                readout.maxQ_kPa = flight.qPa / 1000.0;
                readout.maxQAlt_m = flight.altM;
                readout.maxQStale = false;
                if (flight.flown)
                {
                    readout.peakG = flight.maxG;
                    readout.lossGravity_mps = flight.lossGravity;
                    readout.lossDrag_mps = flight.lossDrag;
                    readout.lossSteering_mps = flight.lossSteering;
                    if (!double.IsNaN(flight.minStability) && !double.IsPositiveInfinity(flight.minStability)) readout.minStability = flight.minStability;
                    if (!double.IsNaN(flight.minCar) && !double.IsPositiveInfinity(flight.minCar)) readout.minCar = flight.minCar;
                    readout.risks = flight.risks;
                    readout.predictedFailure = flight.failure;
                    if (d.recoveryMode != null && d.recoveryMode != "expendable" && !double.IsNaN(flight.burnoutSpeed))
                        readout.landingNeed_mps = RecoveryModel.BoostbackDeltaV(d.recoveryMode, flight.burnoutSpeed);
                }
            }
            return readout;
        }

        /// <summary>Thrust over weight. Zero when the inputs cannot make a positive finite ratio.</summary>
        public static double NonNegativeTwr(double thrust, double mass)
        {
            const double G0 = 9.80665;
            if (!(thrust > 0) || !(mass > 0)) return 0;
            if (double.IsNaN(thrust) || double.IsInfinity(thrust) || double.IsNaN(mass) || double.IsInfinity(mass)) return 0;
            double twr = thrust / (mass * G0);
            return ShownTwr(twr);
        }

        /// <summary>A TWR shown on the strip. Never negative and never NaN.</summary>
        public static double ShownTwr(double twr)
        {
            if (double.IsNaN(twr) || double.IsInfinity(twr) || twr < 0) return 0;
            return twr;
        }

        /// <summary>"+42 m/s dv · -0.03 TWR". ASCII hyphen and "dv" so both designer fonts can draw it. A zero change is blank.</summary>
        public static string DeltaChip(double deltaVMps, double deltaTwr)
        {
            if (double.IsNaN(deltaVMps) || double.IsInfinity(deltaVMps)) deltaVMps = 0;
            if (double.IsNaN(deltaTwr) || double.IsInfinity(deltaTwr)) deltaTwr = 0;
            if (Math.Abs(Math.Round(deltaVMps)) < 0.5 && Math.Abs(deltaTwr) < 0.005) return "";
            var inv = CultureInfo.InvariantCulture;
            string Sign(double x) => x < 0 ? "-" : "+";
            return Sign(deltaVMps) + Math.Abs(Math.Round(deltaVMps)).ToString(inv) + " m/s dv · "
                + Sign(deltaTwr) + Math.Abs(deltaTwr).ToString("0.00", inv) + " TWR";
        }

        static double BoilOff(Catalog c, Design d, SerialStage phase)
        {
            double loss = 0;
            if (phase.role != null && phase.role.IndexOf("strap", StringComparison.Ordinal) >= 0 && d.StrapOnCount > 0)
                loss += OneBoil(c, d.strapOns);
            if (phase.index >= 0 && phase.index < d.stages.Count) loss += OneBoil(c, d.stages[phase.index]);
            return loss;
        }

        static double OneBoil(Catalog c, StagePart part)
        {
            if (part == null) return 0;
            double rate = 0;
            if (c.PropellantById.TryGetValue(part.propellant ?? "", out var prop)) rate = prop.boiloffPerHour;
            string insulation = part.tank != null ? part.tank.InsulationOrNone : "none";
            return TankModel.BoilOffLossKg(part.propMass, rate, insulation, 1);
        }

        static double AxialMargin(Catalog c, Design d, SerialStage phase, double m0, double thrust)
        {
            StagePart part = phase.index >= 0 && phase.index < d.stages.Count ? d.stages[phase.index] : null;
            if (part == null || !(m0 > 0) || !(thrust > 0)) return double.NaN;
            var allow = StructureModel.Allowables(c, part, d.safetyFactor);
            if (!(allow.fAllowN > 0)) return double.NaN;
            double ms = StructureModel.Margin(new LoadState
            {
                fAllowN = allow.fAllowN,
                massAboveKg = m0,
                aAxial = thrust / m0,
                radiusM = part.diameter > 0 ? part.diameter / 2 : 0,
            });
            return double.IsPositiveInfinity(ms) ? double.NaN : ms;
        }

        static double? QLimKPa(Catalog c, Design d)
        {
            double qLim = 0;
            void Take(StagePart s)
            {
                if (s == null || !c.MaterialById.TryGetValue(s.material ?? "", out var mat)) return;
                double q = StructureModel.QLimPa(d.safetyFactor, mat.yieldMPa);
                if (q > 0 && (qLim <= 0 || q < qLim)) qLim = q;
            }
            for (int i = 0; i < d.stages.Count; i++) Take(d.stages[i]);
            if (d.StrapOnCount > 0) Take(d.strapOns);
            double fair = StructureModel.FairingQLimPa(c, d);
            if (fair > 0 && (qLim <= 0 || fair < qLim)) qLim = fair;
            return qLim > 0 ? (double?)(qLim / 1000.0) : null;
        }

        static double GLimit(Catalog c, Design d)
        {
            double g = 8;
            if (d.payloadType != null && c.PayloadById.TryGetValue(d.payloadType, out var pl) && pl.maxG > 0 && pl.maxG < g) g = pl.maxG;
            return g;
        }

        static double LandingReserve(Catalog c, Design d, VehicleMass mass)
        {
            if (d.stages.Count == 0) return 0;
            var s = d.stages[0];
            if (!(s.landingPropMass > 0) || !c.EngineById.TryGetValue(s.engine ?? "", out var eng)) return 0;
            var point = EnginePoint.Of(c, s);
            double isp = point.known ? point.ispVac : eng.ispVac;
            double dry = mass != null && mass.stages != null && mass.stages.Count > 0 ? mass.stages[0].dry : 0;
            if (!(dry > 0)) return 0;
            return RecoveryModel.ReserveDeltaV(isp, dry, s.landingPropMass);
        }

        static void Fairing(Design d, Catalog c, QuickReadout readout)
        {
            if (d.payloadType == null || !c.PayloadById.TryGetValue(d.payloadType, out var pl)) return;
            // Catalog payloads do not carry a diameter yet. The volume check still runs; diameter margin uses 0.
            var fit = PayloadFit.Check(d, new PayloadSpec { id = pl.id, volume = pl.volume, diameter = 0 });
            readout.fairingFits = fit.fits;
            readout.fairingVolumeMargin_m3 = fit.volumeMargin;
            readout.fairingDiameterMargin_m = fit.diameterMargin;
        }

        /// <summary>
        /// Reference ascent for max-Q. Runs the Headless path once per design hash and caches it.
        /// Subsequent Quick() calls for the same hash return maxQStale false.
        /// Cost: one Headless ascent at dt 0.2 s, through thrust cutoff. Not for the edit frame.
        /// </summary>
        public static QuickReadout Reference(Catalog c, JsonObject design) => ReferenceAscent.Run(c, design);
    }
}
