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
            // The last clause starts the hoverslam while a coast that is already
            // over Haven still has altitude. The suicide test alone shrinks as drag
            // slows the fall, so the burn never opens until the deck.
            bool landing = body.Phase == RecoveryPhase.Landing
                || (falling && alt < suicide && (Math.Abs(gap) < 220 || alt < 420))
                || (falling && Math.Abs(gap) < 1200 && alt < 900);
            // Not a latch. Fire only when the fall will reach the water before the barge,
            // so a boostback that is still short on time can stay in the air.
            double sinkNow = radial < 0 ? -radial : 0;
            double tGround = sinkNow > 40 && alt > 0 ? alt / sinkNow : 1e9;
            double tClose = Math.Abs(east) > 20 ? Math.Abs(gap) / Math.Abs(east) : 1e9;
            // Only a fast return that will splash before it arrives. A slow
            // approach (the assisted test from 20 km) must not chatter this burn.
            // Grid fins on a fast barge run bleed the closing speed in the lower
            // air. Stretch that fall while the stage is still high, or the deck
            // arrives after the horizontal speed is gone.
            bool wantEntry = Math.Abs(east) > 240 && Math.Abs(gap) > 1800 && sinkNow > 200
                && tClose > tGround * 1.12 && alt > 3000 && alt < 40000
                && body.Prop > floor + 400;
            bool holdEntry = body.Phase == RecoveryPhase.Entry && Math.Abs(east) > 200
                && Math.Abs(gap) > 1800 && sinkNow > 160 && alt > 2500
                && body.Prop > floor + 400;
            bool entry = !landing && (wantEntry || holdEntry);
            // Wide at MECO altitude so one burn sets the intercept and the coast does not
            // chase a few metres per second. Tight again once the deck is in sight.
            double enter = alt < 8000 ? 12 : alt < 25000 ? 34 : 70;
            double leave = alt < 8000 ? 3.5 : alt < 25000 ? 6 : 24;
            bool boosting = body.Phase == RecoveryPhase.Boostback;
            // A second burn while the stage is falling through the upper air flips the
            // nose through downward and spends the landing reserve. The first burn is
            // the climb. Trims wait until the approach.
            bool fallingHard = radial < -40 && alt > 18000;
            // Slowing a fall that is still short of Haven throws away the speed that
            // covers the last kilometres. The entry burn is what buys time.
            bool closing = gap * east > 0;
            bool keepSpeed = closing && sinkNow > 250 && alt > 3500 && alt < 22000
                && Math.Abs(gap) > 2500 && Math.Abs(east) > 120;
            bool boost = !landing && !entry && !fallingHard && !keepSpeed && alt > 550 && body.Prop > floor + 1
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
                // Nearly vertical. A 20° tilt on a high-TWR stage adds hundreds of
                // metres per second of closing speed and flies the stage past Haven.
                double mass = body.Mass > 1 ? body.Mass : (craft.DryKg > 1 ? craft.DryKg : 1);
                double thrust = craft.ThrustSlN > 0 ? craft.ThrustSlN : craft.ThrustVacN;
                double accel = thrust > 0 ? thrust / mass : 20;
                double tiltCap = accel > 1 ? 12.0 / accel : 0.12;
                if (tiltCap > 0.12) tiltCap = 0.12;
                if (tiltCap < 0.02) tiltCap = 0.02;
                double bias = eastErr / 400;
                if (bias > tiltCap) bias = tiltCap;
                else if (bias < -tiltCap) bias = -tiltCap;
                cmd.Pitch = Math.PI / 2 + bias;
                // Hold a fall the hoverslam can finish. Full thrust on a high-TWR
                // stage reverses the sink in one second and climbs off the barge.
                // Stay dark until the nose is up so the flip does not add east speed.
                double sink = radial < 0 ? -radial : 0;
                double thr = (sink - 120) / 90;
                double aim = RecoveryFrames.Wrap(cmd.Pitch - body.Pitch);
                if (aim < 0) aim = -aim;
                if (aim > 0.45) thr = 0;
                if (thr < 0 || body.Prop <= floor) thr = 0;
                if (thr > 0.55) thr = 0.55;
                cmd.Throttle = thr;
            }
            else if (phase == RecoveryPhase.Boostback)
            {
                // Above the thick air the burn is horizontal. Pitching 20° up on a
                // high-TWR stage lofts it past the barge and spends the landing fuel.
                // Low: a steeper trim so the burn does not shovel the stage into the ground.
                if (alt > 8000)
                {
                    double noseUp = 0.07;
                    cmd.Pitch = eastErr > 0 ? Math.PI - noseUp : noseUp;
                }
                else
                {
                    double tilt = alt > 7000 ? 1.05 : 0.58;
                    cmd.Pitch = Math.PI / 2 + (eastErr > 0 ? tilt : -tilt);
                }
                double thr = Math.Abs(eastErr) / (alt > 8000 ? 90 : alt > 7000 ? 40 : 24);
                if (thr < 0.16) thr = 0.16;
                if (thr > 1) thr = 1;
                if (alt < 8000 && thr > 0.62) thr = 0.62;
                // Light the engines once the nose is on the burn. A flip through
                // downward thrust is what lofted the early flights off Haven.
                double aim = RecoveryFrames.Wrap(cmd.Pitch - body.Pitch);
                if (aim < 0) aim = -aim;
                if (aim > 0.45) thr = 0;
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
            // 220 m/s under the Karman line keeps the old approach. Above a real MECO
            // the barge was placed on a 0.3 east cut, which is several hundred m/s.
            double cap = EastCap(alt);
            double want = gap / t;
            if (want > cap) want = cap;
            else if (want < -cap) want = -cap;
            return want;
        }

        /// <summary>East-speed limit. High altitude tracks the ballistic intercept. The deck wants a walk.</summary>
        public static double EastCap(double alt)
        {
            if (alt < 1500) return 16;
            if (alt < 6000) return 70;
            if (alt < 22000) return 220;
            if (alt < 55000) return 220 + (alt - 22000) / 33000 * 700;
            return 1400;
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
            if (alt < 40) vTrack = 1.15;
            else if (alt < 900)
            {
                // Blend onto the deck target. A knee at 40 m cannot stop a 100 m/s fall.
                double curve = Math.Sqrt(2 * aNet * altSafe * 0.56);
                if (curve < 5.5) curve = 5.5;
                double u = alt / 900.0;
                vTrack = 1.15 + (curve - 1.15) * u;
                if (vTrack < 1.15) vTrack = 1.15;
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
