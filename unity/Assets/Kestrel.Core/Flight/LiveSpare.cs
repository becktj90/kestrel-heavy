// The stage banner's live spare (Arcade P1, rule from Kestrel Physics). It replaces the fitted "speed under 40 % of the
// budget" rule behind "Short on speed".
//
//   live spare = Δv on board − Δv still needed
//   Δv on board     = the rocket equation over the stack still attached, at its current masses, vacuum Isp
//                     (AscentSim.RemainingVacDeltaV).
//   Δv still needed = the ideal speed still to gain + the loss allowance, or 0 once there is no speed left to gain.
//   loss allowance  = the sheet's loss term − the losses accrued so far in this flight, never below 0. No slack.
//                     Accrued = ∫(g·sinγ + D/m)dt + ∫a_T(1 − cos θ)dt: the sim's own powered-flight gravity, drag and
//                     steering tallies (AscentSim.LossGravity, LossDrag, LossSteering, LossAccount.Add). The steering term
//                     is in because the sim keeps its books in inertial speed: the vertical rise off a rotating pad is
//                     thrust across a 400 m/s eastward velocity, so most of what the sheet's lump loss term pays for early
//                     is booked there (KH-6 starter: 1,911 m/s steering against 1,293 gravity + drag). Leaving it out
//                     would hold back loss allowance for losses already paid and read five campaign winners short.
//                     The sim keeps no pressure-loss tally (vacuum vs. local Isp), so back-pressure loss stays inside
//                     the allowance: the spare is slightly pessimistic, which can only add a pill, never hide one.
//
// The sheet's loss term is its required Δv minus its ideal speed from the pad (Stack.RequiredDeltaV: LOSS_BUDGET for an
// orbit, 1000 m/s for a hop), so at liftoff the live spare is the sheet's spare. Nothing is fitted: every number is the
// sheet's or the sim's. The banner says short only once the live spare has stayed below zero for HoldS of sim time.
// Read only: nothing here feeds back into the flight. No Unity types.
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    public sealed class LiveSpare
    {
        /// <summary>The spare must stay below zero this long (seconds of sim time) before the banner says "Short on speed".</summary>
        public const double HoldS = 3.0;

        /// <summary>True for an apogee hop (the sheet's required is sqrt(2·g0·h) + its loss term).</summary>
        public readonly bool Hop;
        /// <summary>The hop's goal height, m (0 for an orbit).</summary>
        public readonly double HopAltM;
        /// <summary>Perigee radius of the full target orbit, m (orbital targets).</summary>
        public readonly double PerigeeRadiusM;
        /// <summary>The target orbit's speed at perigee, m/s. For an ellipse or C3 target this is the speed after the
        /// departure burn, so the departure burn is inside the spare.</summary>
        public readonly double GoalSpeedMps;
        /// <summary>The sheet's loss term, m/s: its required Δv minus its ideal speed from the pad.</summary>
        public readonly double SheetLossMps;

        double negSince = double.NaN;

        LiveSpare(bool hop, double hopAltM, double rp, double goal, double loss)
        {
            Hop = hop; HopAltM = hopAltM; PerigeeRadiusM = rp; GoalSpeedMps = goal; SheetLossMps = loss;
        }

        /// <summary>The live spare for a design's target, built from the same Stack.RequiredDeltaV the sheet uses.</summary>
        public static LiveSpare For(Catalog c, string orbitId)
            => From(Stack.RequiredDeltaV(c, orbitId), Stack.LAUNCH_LATITUDE);

        /// <summary>From a sheet requirement taken at <paramref name="sheetLatitudeDeg"/> (the sheet's own pad).</summary>
        public static LiveSpare From(Stack.Required req, double sheetLatitudeDeg)
        {
            var o = req.orbit;
            if (o.periKm < 0)
            {
                double h = o.apoKm * 1000;
                return new LiveSpare(true, h, 0, 0, req.required - Math.Sqrt(2 * Atmosphere.G0 * h));
            }
            double rp = Gravity.R_EARTH + o.periKm * 1000, ra = Gravity.R_EARTH + o.apoKm * 1000;
            double vPeri = Math.Sqrt(Gravity.MU_EARTH * (2 / rp - 2 / (rp + ra)));
            double loss = req.required - (vPeri - Earth.SiteRotationSpeed(sheetLatitudeDeg));
            return new LiveSpare(false, 0, rp, vPeri, loss);
        }

        /// <summary>
        /// Ideal speed still to gain, m/s, from radius <paramref name="rM"/> with inertial velocity (vx, vy) at (x, y).
        /// Orbital: the goal perigee speed minus the speed now; above the perigee the extra height is credited as the
        /// speed it gives back on the way down to it (vis-viva, so a coast above the perigee costs nothing). Below the
        /// perigee the climb is the loss term's job, as on the sheet. Hop: the vertical speed that still has to be added
        /// for a coast to the goal height. Never below 0.
        /// </summary>
        public double SpeedToGo(double rM, double vx, double vy, double x, double y)
        {
            double alt = rM - Gravity.R_EARTH;
            if (Hop)
            {
                if (alt >= HopAltM) return 0;
                double vr = rM > 0 ? (vx * x + vy * y) / rM : 0;
                return Math.Max(0, Math.Sqrt(2 * Atmosphere.G0 * (HopAltM - alt)) - vr);
            }
            double v2 = vx * vx + vy * vy;
            if (rM > PerigeeRadiusM) v2 += 2 * Gravity.MU_EARTH * (1 / PerigeeRadiusM - 1 / rM);
            return Math.Max(0, GoalSpeedMps - Math.Sqrt(v2));
        }

        /// <summary>The loss allowance still held back, m/s: the sheet's loss term less the losses already flown, never below 0.</summary>
        public double LossAllowance(double accruedLossMps) => Math.Max(0, SheetLossMps - accruedLossMps);

        /// <summary>Losses accrued so far, m/s: ∫(g·sinγ + D/m)dt plus the steering term, the sim's own powered-flight tallies.</summary>
        public static double AccruedLoss(AscentSim sim) => sim.LossGravity + sim.LossDrag + sim.LossSteering;

        /// <summary>Δv still needed, m/s: speed still to gain plus the loss allowance; nothing once there is no speed left to gain.</summary>
        public double Needed(double speedToGoMps, double accruedLossMps)
            => speedToGoMps > 0 ? speedToGoMps + LossAllowance(accruedLossMps) : 0;

        /// <summary>Live spare, m/s, from its parts.</summary>
        public double Spare(double onboardMps, double speedToGoMps, double accruedLossMps)
            => onboardMps - Needed(speedToGoMps, accruedLossMps);

        /// <summary>The live spare of a flying sim (inertial state, ideal vacuum Δv still on board).</summary>
        public double Spare(AscentSim sim)
        {
            var Y = sim.Y;
            double r = Math.Sqrt(Y[0] * Y[0] + Y[1] * Y[1]);
            return Spare(sim.RemainingVacDeltaV(), SpeedToGo(r, Y[2], Y[3], Y[0], Y[1]), AccruedLoss(sim));
        }

        /// <summary>
        /// True once the live spare has stayed below zero for <see cref="HoldS"/> of sim time (a shorter dip does not
        /// flash the banner). Call with increasing <paramref name="t"/>; any reading at or above zero restarts the hold.
        /// </summary>
        public bool Short(double t, double spare)
        {
            if (!(spare < 0)) { negSince = double.NaN; return false; }
            if (double.IsNaN(negSince)) negSince = t;
            return t - negSince >= HoldS - 1e-9;
        }

        /// <summary>Forget the hold (a new flight).</summary>
        public void Reset() => negSince = double.NaN;
    }
}
