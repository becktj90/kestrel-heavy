// Closed-loop recovery law. Command(state) → pitch, throttle, fin and leg orders.
// One boostback tracks the east speed that reaches Haven, then the hoverslam
// follows the suicide curve so it does not hover and drift off the deck.
// A manual stick replaces pitch and throttle and still gets the fin, leg and phase gates.
using System;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    public static class RecoveryGuidance
    {
        public const double MissBandM = 140;
        public const double EntryAltM = 62000;
        public const double EntrySpeed = 300;
        public const double FinAltM = 52000;
        public const double LegAltM = 650;

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

        static RecoveryCommand Plan(in RecoveryBody body, in RecoveryCraft craft)
        {
            RecoveryFrames.Axes(body.X, body.Y, out _, out double alt, out double arc, out _, out _, out _, out _);
            RecoveryFrames.Split(body.X, body.Y, body.Vx, body.Vy, out double east, out double radial);
            double speed = Math.Sqrt(body.Vx * body.Vx + body.Vy * body.Vy);
            double g = Gravity.Accel(Gravity.R_EARTH + (alt > 0 ? alt : 0));
            double floor = RecoverySim.LandingReserveKg(craft);
            double gap = body.BargeArc - arc;
            double eastTarget = EastTarget(body.BargeArc, arc, alt, radial, g);
            double eastErr = east - eastTarget;
            double suicide = SuicideAlt(craft, radial, g, body.Mass);
            if (suicide > 2600) suicide = 2600;

            bool falling = radial < 1.5;
            bool landing = body.Phase == RecoveryPhase.Landing
                || (falling && alt < suicide && (Math.Abs(gap) < 220 || alt < 420));
            // Entry only when the fall is too fast for the hoverslam. Retrograde would
            // throw away the east speed the boostback just bought.
            bool entry = !landing && body.Prop > floor + 400 && (
                (body.Phase == RecoveryPhase.Entry && speed > 200 && alt > 7000)
                || (alt < 42000 && alt > 11000 && speed > 500 && radial < -180));
            double enter = alt < 8000 ? 12 : 34;
            double leave = alt < 8000 ? 3.5 : 6;
            bool boosting = body.Phase == RecoveryPhase.Boostback;
            bool boost = !landing && !entry && alt > 550 && body.Prop > floor + 1
                && Math.Abs(eastErr) > (boosting ? leave : enter);

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
                Landing(east, radial, alt, gap, g, craft, body.Mass, out cmd.Pitch, out cmd.Throttle);
            else if (phase == RecoveryPhase.Entry)
            {
                double bias = eastErr / 90;
                if (bias > 0.35) bias = 0.35;
                else if (bias < -0.35) bias = -0.35;
                cmd.Pitch = Math.PI / 2 + bias;
                double sink = radial < 0 ? -radial : 0;
                cmd.Throttle = sink > 80 && body.Prop > floor ? 1 : 0;
            }
            else if (phase == RecoveryPhase.Boostback)
            {
                // High: nearly horizontal, the real boostback. Low: a trim so the
                // burn does not shovel the stage into the ground.
                double tilt = alt > 7000 ? 1.2 : 0.58;
                cmd.Pitch = Math.PI / 2 + (eastErr > 0 ? tilt : -tilt);
                double thr = Math.Abs(eastErr) / (alt > 7000 ? 40 : 24);
                if (thr < 0.16) thr = 0.16;
                if (thr > 1) thr = 1;
                if (alt < 8000 && thr > 0.62) thr = 0.62;
                if (body.Prop <= floor) thr = 0;
                cmd.Throttle = thr;
            }
            else
            {
                double along = Math.Abs(east) < 1 && Math.Abs(radial) < 1 ? 1 : east;
                double gamma = Math.Atan2(radial, along);
                double alpha = 0;
                if (Math.Abs(eastErr) > 4)
                {
                    alpha = eastErr > 0 ? -0.38 : 0.38;
                    if (Math.Abs(eastErr) < 24) alpha *= Math.Abs(eastErr) / 24;
                }
                if (!craft.GridFins) alpha *= 0.35;
                double vertical = 0;
                if (alt < 8000) vertical = (8000 - Math.Max(0, alt)) / 8000;
                if (vertical > 1) vertical = 1;
                cmd.Pitch = RecoveryFrames.Wrap((1 - vertical) * (gamma + alpha) + vertical * (Math.PI / 2));
                cmd.Throttle = 0;
            }
            if (body.Prop <= 1e-6) cmd.Throttle = 0;
            cmd.Pitch = RecoveryFrames.Wrap(cmd.Pitch);
            return cmd;
        }

        /// <summary>East speed that reaches the barge before the deck, and stays gentle in the last kilometres.</summary>
        public static double EastTarget(double bargeArc, double arc, double alt, double radialUp, double g)
        {
            double gap = bargeArc - arc;
            double sink = radialUp < 0 ? -radialUp : 0;
            double t = sink > 25 && alt > 0 ? alt / sink : RecoveryFrames.FallTime(alt, radialUp, g);
            if (t < 4) t = 4;
            double cap = alt < 1500 ? 16 : alt < 6000 ? 70 : 220;
            double want = gap / t;
            if (want > cap) want = cap;
            else if (want < -cap) want = -cap;
            return want;
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
            double eastWant = gap / tGo;
            if (eastWant > cap) eastWant = cap;
            else if (eastWant < -cap) eastWant = -cap;
            double eErr = east - eastWant;
            double tiltMax = alt < 48 ? 0.2 : 0.55;
            double tilt = eErr / (Math.Abs(eErr) + 6.5);
            tilt *= tiltMax;
            pitch = Math.PI / 2 + tilt;

            double vTrack;
            if (alt < 40) vTrack = 1.15;
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
            if (throttle < 0) throttle = 0;
            if (throttle > 1) throttle = 1;
        }
    }
}
