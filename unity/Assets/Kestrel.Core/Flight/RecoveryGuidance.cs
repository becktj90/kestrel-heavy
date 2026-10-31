// Closed-loop recovery law. Command(state) → pitch, throttle, fin and leg orders.
// The law steers on the predicted touchdown point (RecoverySim.Predict: the unpowered fall
// with drag, fins and the planned entry burn), not on a vacuum fall-time guess:
// one boostback burn until the predicted point sits on Haven or the pad, an entry burn
// that brings the fall down to a speed the landing burn can stop, fin steering on the
// predicted miss, then the hoverslam on the suicide curve so it does not hover and drift
// off the deck. The live flight hands the booster over in the ground frame (FlightDriver's
// air-relative speed); this law lands that state (t-booster-land: the old east-speed law
// was tuned on inertial states and crashed every steep live hand-over but KH-7's).
// A manual stick replaces pitch and throttle and still gets the fin, leg and phase gates.
using System;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    public static class RecoveryGuidance
    {
        public const double FinAltM = 52000;
        public const double LegAltM = 650;
        /// <summary>The sink the hoverslam settles onto the deck at. Under 0.55 of the light-leg limit (2 m/s) is a Perfect catch.</summary>
        public const double TouchdownSinkMps = 0.8;

        /// <summary>The entry burn window: it starts below this height on the way down.</summary>
        public const double EntryTopM = 42000;
        /// <summary>No entry burn below this height; the air and the landing burn take over.</summary>
        public const double EntryFloorM = 9000;
        /// <summary>No boostback below this height: a horizontal burn low down shovels the stage into the sea.</summary>
        public const double BoostFloorM = 15000;
        /// <summary>A predicted miss wider than this opens a boostback burn.</summary>
        public const double BoostEnterM = 300;
        /// <summary>The boostback ends once the predicted miss is inside this.</summary>
        public const double BoostLeaveM = 25;
        /// <summary>The hoverslam never opens above this height.</summary>
        public const double LandingTopM = 9000;
        /// <summary>An open entry burn holds on this far under the target speed, so gravity does not reopen it every few seconds.</summary>
        public const double EntryUndershoot = 120;

        public static RecoveryCommand Command(in RecoveryBody body, in RecoveryCraft craft, in RecoveryStick stick)
        {
            var auto = Plan(body, craft);
            if (stick.Auto) return auto;
            auto.Pitch = RecoveryFrames.Wrap(stick.PitchRad);
            double thr = stick.Throttle;
            if (double.IsNaN(thr) || thr < 0) thr = 0;
            if (thr > 1) thr = 1;
            auto.Throttle = thr;
            return auto;
        }

        /// <summary>
        /// The speed the entry burn holds the fall to at <paramref name="alt"/>: 700 m/s at the top of the window,
        /// 320 m/s at its floor. Thick air does the rest before the landing burn.
        /// </summary>
        public static double EntryTargetSpeed(double alt)
        {
            double u = (alt - EntryFloorM) / (EntryTopM - EntryFloorM);
            if (u < 0) u = 0;
            if (u > 1) u = 1;
            return 320 + 380 * u;
        }

        /// <summary>True while a falling stage inside the entry window is faster than <see cref="EntryTargetSpeed"/> and
        /// still holds more than the landing burn's share. The predictor and the law share this test.</summary>
        public static bool EntryWanted(double alt, double radialUp, double speed, double prop, double floorKg, double over)
        {
            if (!(radialUp < -60) || alt >= EntryTopM || alt <= EntryFloorM) return false;
            if (!(prop > floorKg * 1.2 + 300)) return false;
            return speed > EntryTargetSpeed(alt) + over;
        }

        public static double EntryThrottle(double speed, double alt)
        {
            double thr = (speed - EntryTargetSpeed(alt)) / 150;
            if (thr < 0.15) thr = 0.15;
            if (thr > 0.6) thr = 0.6;
            return thr;
        }

        /// <summary>Predicted touchdown arc minus the Haven arc, metres. Positive lands beyond Haven (further east).</summary>
        public static double PredictedMiss(in RecoveryBody body, in RecoveryCraft craft)
        {
            double arc = body.PredArc;
            if (double.IsNaN(body.PredStamp) || double.IsNaN(arc))
                RecoverySim.Predict(body, craft, out arc, out _);
            return arc - body.BargeArc;
        }

        static RecoveryCommand Plan(in RecoveryBody body, in RecoveryCraft craft)
        {
            RecoveryFrames.Axes(body.X, body.Y, out _, out double alt, out double arc, out _, out _, out _, out _);
            RecoveryFrames.Split(body.X, body.Y, body.Vx, body.Vy, out double east, out double radial);
            double speed = Math.Sqrt(body.Vx * body.Vx + body.Vy * body.Vy);
            double g = Gravity.Accel(Gravity.R_EARTH + (alt > 0 ? alt : 0));
            double floor = RecoverySim.LandingReserveKg(craft);
            double gap = body.BargeArc - arc;
            double suicide = SuicideAlt(craft, radial, g, body.Mass);
            if (suicide > LandingTopM) suicide = LandingTopM;
            double mass = body.Mass > 1 ? body.Mass : (craft.DryKg > 1 ? craft.DryKg : 1);
            double spare = body.Prop - floor;

            bool falling = radial < 1.5;
            // The hoverslam opens on the suicide curve wherever the stage is: a fall that is still off the deck steers
            // in during the burn (Landing), and a stage that never lights hits the sea at full speed. The last clause
            // starts a slow approach that is already over Haven while it still has height — only when slow, or a fast
            // stage wastes fuel fighting gravity for hundreds of metres instead of waiting for the suicide curve.
            bool landing = body.Phase == RecoveryPhase.Landing
                || (falling && alt < suicide)
                || (falling && Math.Abs(gap) < 1200 && alt < 900 && speed < 60);
            bool inPlay = !landing && !(alt <= 0 && falling);
            double miss = inPlay ? PredictedMiss(body, craft) : -gap;

            bool entry = false;
            if (inPlay)
            {
                if (body.Phase == RecoveryPhase.Entry)
                    entry = EntryWanted(alt, radial, speed, body.Prop, floor, -EntryUndershoot);
                else
                    entry = EntryWanted(alt, radial, speed, body.Prop, floor, 30);
            }
            bool boosting = body.Phase == RecoveryPhase.Boostback;
            bool boost = inPlay && !entry && alt > BoostFloorM && spare > 1
                && Math.Abs(miss) > (boosting ? BoostLeaveM : BoostEnterM);

            RecoveryPhase phase;
            if (alt <= 0 && falling) phase = RecoveryPhase.Down;
            else if (landing) phase = RecoveryPhase.Landing;
            else if (entry) phase = RecoveryPhase.Entry;
            else if (boost) phase = RecoveryPhase.Boostback;
            else if (alt < FinAltM) phase = RecoveryPhase.Descent;
            else phase = RecoveryPhase.Coast;

            var cmd = new RecoveryCommand
            {
                Phase = phase,
                Fins = craft.GridFins && alt < FinAltM && alt > 0,
                Legs = craft.Legs && alt < LegAltM && alt > 0,
            };

            if (phase == RecoveryPhase.Landing)
                Landing(east, radial, alt, gap, g, craft, mass, out cmd.Pitch, out cmd.Throttle);
            else if (phase == RecoveryPhase.Entry)
            {
                // Retrograde, leaning a little toward Haven: a positive miss wants more westward thrust (pitch toward π).
                double retro = Math.Atan2(-radial, -east);
                double bias = miss / 1500;
                if (bias > 0.25) bias = 0.25;
                else if (bias < -0.25) bias = -0.25;
                cmd.Pitch = retro + bias;
                double thr = EntryThrottle(speed, alt);
                double aim = Math.Abs(RecoveryFrames.Wrap(cmd.Pitch - body.Pitch));
                if (aim > 0.45 || spare <= 0) thr = 0;
                cmd.Throttle = thr;
            }
            else if (phase == RecoveryPhase.Boostback)
            {
                // Horizontal, a touch nose-up. The burn closes the predicted miss: the speed change it needs is the miss
                // over the time left to the surface, spread over about a second and a half of thrust.
                const double noseUp = 0.07;
                cmd.Pitch = miss > 0 ? Math.PI - noseUp : noseUp;
                double thrust = craft.ThrustVacN > 0 ? craft.ThrustVacN : craft.ThrustSlN;
                double aMax = thrust / mass;
                double tLeft = body.PredTime > 20 ? body.PredTime : 20;
                double dv = Math.Abs(miss) / tLeft;
                double thr = aMax > 0.1 ? dv / (aMax * 1.5) : 1;
                if (thr < 0.04) thr = 0.04;
                if (thr > 1) thr = 1;
                // Light the engines once the nose is on the burn. A flip through downward thrust lofts the stage.
                double aim = Math.Abs(RecoveryFrames.Wrap(cmd.Pitch - body.Pitch));
                if (aim > 0.45 || spare <= 0) thr = 0;
                cmd.Throttle = thr;
            }
            else
            {
                // Unpowered: nose along the flight path, with fin lift toward Haven. Falling, a positive angle of attack
                // pushes the stage east; climbing, west. The lean grows with the miss so the stage does not hunt.
                double alpha = 0;
                if (Math.Abs(miss) > 4)
                {
                    double want = miss > 0 ? -1 : 1; // falling: a positive miss wants westward lift
                    if (radial > 0) want = -want;
                    double u = Math.Abs(miss) / 1500;
                    if (u > 1) u = 1;
                    alpha = 0.38 * want * u;
                }
                if (!craft.GridFins) alpha *= 0.35;
                cmd.Pitch = DescentPitch(east, radial, alt, alpha);
                cmd.Throttle = 0;
            }
            if (body.Prop <= 1e-6) cmd.Throttle = 0;
            cmd.Pitch = RecoveryFrames.Wrap(cmd.Pitch);
            return cmd;
        }

        /// <summary>The unpowered attitude: on the flight path plus <paramref name="alpha"/> (tail first once falling), swinging upright below 8 km
        /// for the landing burn. The predictor flies the same attitude with no lean.</summary>
        public static double DescentPitch(double east, double radial, double alt, double alpha)
        {
            double along = Math.Abs(east) < 1 && Math.Abs(radial) < 1 ? 1 : east;
            double gamma = Math.Atan2(radial, along);
            // Falling, the stage rides engines first (retrograde), the attitude of the
            // entry and landing burns, so no half-turn flip throws the predicted point off. The lean flips sign with it:
            // a body at retrograde − α makes the same lift as one at the flight path + α.
            // #369/#400: at the 10°/s pitch rate the 180° flip needs a head start — begin it as soon as the
            // stage falls, not at the 42 km entry window, or the entry burn's aim gate never opens in time
            // on steep trajectories (m13 SSO).
            if (radial < 0) gamma = gamma + Math.PI - 2 * alpha;
            double vertical = 0;
            if (alt < 8000) vertical = (8000 - Math.Max(0, alt)) / 8000;
            if (vertical > 1) vertical = 1;
            return RecoveryFrames.Wrap((1 - vertical) * (gamma + alpha) + vertical * (Math.PI / 2));
        }

        public static double SuicideAlt(in RecoveryCraft craft, double radialUp, double g, double mass)
        {
            double sink = -radialUp;
            if (sink < 8) return 0;
            if (!(g > 0.1)) g = Atmosphere.G0;
            if (!(mass > 1)) mass = craft.DryKg > 1 ? craft.DryKg : 1;
            // Sea-level thrust: the hoverslam is in the thick air, and the tanks are not empty yet.
            double thrust = craft.ThrustSlN > 0 ? craft.ThrustSlN : craft.ThrustVacN;
            double aNet = thrust / mass - g;
            if (aNet < 0.8) aNet = 0.8;
            return 1.18 * sink * sink / (2 * aNet);
        }

        static void Landing(double east, double radial, double alt, double gap, double g, in RecoveryCraft craft, double mass,
            out double pitch, out double throttle)
        {
            if (!(g > 0.1)) g = Atmosphere.G0;
            if (!(mass > 1)) mass = craft.DryKg > 1 ? craft.DryKg : 1;
            double sink = radial < 0 ? -radial : 0;
            double thrust = craft.ThrustSlN > 0 ? craft.ThrustSlN : craft.ThrustVacN;
            double aThr = thrust / mass;
            if (aThr < 1) aThr = 1;
            double aNet = aThr - g;
            if (aNet < 1) aNet = 1;

            double altSafe = alt > 1 ? alt : 1;
            double tGo = sink > 3 ? altSafe / sink : 5;
            if (tGo < 1.5) tGo = 1.5;
            double cap = alt < 48 ? 3.2 : alt < 400 ? 9 : 20;
            if (Math.Abs(gap) > 80 && alt > 80)
            {
                double hurry = Math.Abs(gap) / 8.0;
                if (hurry > cap) cap = hurry;
                if (cap > 36) cap = 36;
            }
            double eastWant = gap / tGo;
            if (eastWant > cap) eastWant = cap;
            else if (eastWant < -cap) eastWant = -cap;
            double eErr = east - eastWant;
            double tiltMax = alt < 48 ? 0.2 : 0.55;
            double tilt = eErr / (Math.Abs(eErr) + 6.5);
            tilt *= tiltMax;
            pitch = Math.PI / 2 + tilt;

            double vTrack;
            if (alt < 40) vTrack = TouchdownSinkMps;
            else if (alt < 900)
            {
                // Blend onto the deck target. A knee at 40 m cannot stop a 100 m/s fall.
                double curve = Math.Sqrt(2 * aNet * altSafe * 0.56);
                if (curve < 5.5) curve = 5.5;
                double u = alt / 900.0;
                vTrack = TouchdownSinkMps + (curve - TouchdownSinkMps) * u;
                if (vTrack < TouchdownSinkMps) vTrack = TouchdownSinkMps;
            }
            else
            {
                // Stay on a fraction of the suicide curve so the stage keeps descending.
                vTrack = Math.Sqrt(2 * aNet * altSafe * 0.56);
                if (vTrack < 5.5) vTrack = 5.5;
            }
            double tau = alt < 40 ? 0.25 : 0.38;
            double need = g + (sink - vTrack) / tau;
            double vert = Math.Cos(tilt);
            if (vert < 0.55) vert = 0.55;
            throttle = need / (aThr * vert);
            if (radial > 1.5 && alt > 25) throttle = 0;
            // A miss of hundreds of metres cannot be taxied along the surface.
            if (alt < 40 && Math.Abs(gap) > 180) throttle = 0;
            if (throttle < 0) throttle = 0;
            if (throttle > 1) throttle = 1;
        }
    }
}
