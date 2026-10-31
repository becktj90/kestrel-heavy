// Fixed-step booster from separation to the surface. Gravity is inverse-square toward
// Earth's centre. Drag and grid-fin lift use Atmosphere.At. Thrust comes only from the
// landing reserve. The same inputs and the same stick always replay the same samples.
using System;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    public struct RecoveryBody
    {
        public double T, X, Y, Vx, Vy, Mass, Prop, Pitch, Fins, Legs, Throttle;
        public RecoveryPhase Phase;
        public bool FuelEmpty;
        public double BargeArc;
        /// <summary>The predicted touchdown arc (RecoverySim.Predict), the time to it, and the body clock it was taken at
        /// (NaN: not taken yet). Refreshed every RecoverySim.PredictBurning s while the engines burn, otherwise every <see cref="RecoverySim.PredictEvery"/> s.</summary>
        public double PredArc, PredTime, PredStamp;
    }

    public static class RecoverySim
    {
        public const double Dt = 0.05;
        public const double NearDt = 0.0125;
        public const double NearAlt = 2500;
        public const double MaxTime = 900;
        // #351: 10 deg/s max pitch rate, matching real booster attitude rates
        // (was 0.55 rad/s ~= 31.5 deg/s, faster than real vehicles).
        public const double PitchRate = 0.1745;
        public const double FinRate = 1.0 / 1.6;
        public const double LegRate = 1.0 / 1.4;
        public const double DeckRadiusM = 36;
        public const double TiltLimitDeg = 8;

        public static bool Offered(string mode) => mode == "barge" || mode == "rtls";

        public static RecoveryBody Begin(in RecoveryCraft craft, double downrangeM, double altM, double vEast, double vUp)
        {
            RecoveryFrames.FromPad(downrangeM, altM, vEast, vUp, out double x, out double y, out double vx, out double vy);
            var body = new RecoveryBody
            {
                X = x, Y = y, Vx = vx, Vy = vy,
                Mass = craft.DryKg + Math.Max(0, craft.ReserveKg),
                Prop = Math.Max(0, craft.ReserveKg),
                Pitch = Math.PI / 2,
                Phase = RecoveryPhase.Coast,
                PredArc = double.NaN,
                PredTime = double.NaN,
                PredStamp = double.NaN,
            };
            body.BargeArc = PlaceBarge(body, craft);
            Refresh(ref body, craft);
            return body;
        }

        public static double PlaceBarge(RecoveryBody body, in RecoveryCraft craft)
        {
            if (craft.Mode == "rtls") return 0;
            RecoveryFrames.Split(body.X, body.Y, body.Vx, body.Vy, out double east, out double radial);
            double cut = RecoveryModel.BargeFactor * Math.Abs(east);
            double eastAfter = east - Math.Sign(east) * cut;
            Aim(body.X, body.Y, eastAfter, radial, out double vx, out double vy);
            var slowed = body;
            slowed.Vx = vx;
            slowed.Vy = vy;
            // Haven sits where the guidance's own prediction puts the slowed stage, so a 0.3 east cut really lands there.
            Predict(slowed, craft, out double arc, out _);
            return arc;
        }

        /// <summary>How often (body seconds) an unpowered stage refreshes its predicted touchdown. A burning one refreshes every step.</summary>
        public const double PredictEvery = 1.0;
        /// <summary>The refresh while the engines burn (boostback, entry).</summary>
        public const double PredictBurning = 0.2;

        /// <summary>Take a fresh touchdown prediction when the last one is stale. Never once the hoverslam has opened.</summary>
        public static void Refresh(ref RecoveryBody body, in RecoveryCraft craft)
        {
            if (body.Phase == RecoveryPhase.Landing || body.Phase == RecoveryPhase.Down) return;
            double every = body.Throttle > 0 ? PredictBurning : PredictEvery;
            bool stale = double.IsNaN(body.PredStamp) || body.T - body.PredStamp >= every - 1e-9;
            if (!stale) return;
            Predict(body, craft, out double arc, out double t);
            body.PredArc = arc;
            body.PredTime = t;
            body.PredStamp = body.T;
        }

        /// <summary>
        /// Where the stage comes down if the law flies no more boostback: nose on the flight path (no lift), grid fins out
        /// below <see cref="RecoveryGuidance.FinAltM"/>, and the planned entry burn (retrograde, <see cref="RecoveryGuidance.EntryWanted"/>)
        /// paid from the reserve. Drag-aware, so the return through the thick air is not a vacuum guess. Pure.
        /// </summary>
        public static void Predict(RecoveryBody b, in RecoveryCraft craft, out double arc, out double tImpact)
        {
            double floor = LandingReserveKg(craft);
            double t = 0;
            int guard = 0;
            bool burning = b.Phase == RecoveryPhase.Entry;
            b.Throttle = 0;
            while (guard++ < 8000 && t < 900)
            {
                RecoveryFrames.Axes(b.X, b.Y, out _, out double alt, out _, out _, out _, out _, out _);
                if (alt <= 0) break;
                RecoveryFrames.Split(b.X, b.Y, b.Vx, b.Vy, out double east, out double radial);
                double speed = Math.Sqrt(b.Vx * b.Vx + b.Vy * b.Vy);
                double pitch = RecoveryGuidance.DescentPitch(east, radial, alt, 0);
                double thr = 0;
                burning = burning
                    ? RecoveryGuidance.EntryWanted(alt, radial, speed, b.Prop, floor, -RecoveryGuidance.EntryUndershoot)
                    : RecoveryGuidance.EntryWanted(alt, radial, speed, b.Prop, floor, 30);
                if (burning)
                {
                    thr = RecoveryGuidance.EntryThrottle(speed, alt);
                    pitch = Math.Atan2(-radial, -east);
                }
                b.Fins = craft.GridFins && alt < RecoveryGuidance.FinAltM ? 1 : 0;
                double dt = thr > 0 ? 0.1 : alt > 40000 ? 1.0 : alt > 8000 ? 0.25 : 0.1;
                Advance(ref b, craft, pitch, thr, dt, true);
                t += dt;
            }
            arc = Arc(b);
            tImpact = t;
        }

        public static RecoveryVerdict Fly(in RecoveryCraft craft, double downrangeM, double altM, double vEast, double vUp, in RecoveryStick stick, IFlightSimSink sink = null)
        {
            var body = Begin(craft, downrangeM, altM, vEast, vUp);
            return Fly(craft, body, stick, sink);
        }

        public static RecoveryVerdict Fly(in RecoveryCraft craft, RecoveryBody body, in RecoveryStick stick, IFlightSimSink sink = null)
        {
            sink = GuardedFlightSink.Wrap(sink);
            var marks = new RecoveryMark[8];
            int n = 0;
            Remember(ref marks, ref n, body);
            if (sink != null) sink.OnRecoveryPhase(body.Phase, body.T);
            int guard = 0;
            while (body.Phase != RecoveryPhase.Down && body.T < MaxTime && guard++ < 80000)
            {
                double alt = Altitude(body);
                double dt = alt < NearAlt ? NearDt : Dt;
                var prev = body.Phase;
                Step(ref body, craft, stick, dt);
                if (n == 0 || marks[n - 1].Phase != body.Phase) Remember(ref marks, ref n, body);
                if (sink != null && body.Phase != prev) sink.OnRecoveryPhase(body.Phase, body.T);
            }
            if (body.Phase != RecoveryPhase.Down)
            {
                body.Phase = RecoveryPhase.Down;
                Remember(ref marks, ref n, body);
                if (sink != null) sink.OnRecoveryPhase(body.Phase, body.T);
            }
            var kept = new RecoveryMark[n];
            for (int i = 0; i < n; i++) kept[i] = marks[i];
            var sample = Sample(body, craft);
            var verdict = RecoveryScore.Judge(sample, craft, kept);
            verdict.Attempted = craft.Recoverable;
            if (sink != null && sample.Phase == RecoveryPhase.Down)
            {
                sink.OnTouchdown(sample.Sink, sample.TiltDeg, FlightSurfaces.Choose(sample.MissM, DeckRadiusM, craft.Mode));
                string boom = RecoveryViews.Boom(verdict.Cause, sample.Sink, craft.VLeg);
                if (!string.IsNullOrEmpty(boom))
                {
                    double energy = 0.5 * sample.Mass * sample.Speed * sample.Speed;
                    sink.OnCrash(boom, new FlightVec(body.X, body.Y), new FlightVec(body.Vx, body.Vy), energy);
                }
            }
            return verdict;
        }

        public static void Step(ref RecoveryBody body, in RecoveryCraft craft, in RecoveryStick stick, double dt)
        {
            if (!(dt > 0) || double.IsNaN(dt)) return;
            if (body.Phase == RecoveryPhase.Down) return;
            Refresh(ref body, craft);
            var cmd = RecoveryGuidance.Command(body, craft, stick);
            double pitchErr = RecoveryFrames.Wrap(cmd.Pitch - body.Pitch);
            double pitchStep = PitchRate * dt;
            if (pitchErr > pitchStep) pitchErr = pitchStep;
            else if (pitchErr < -pitchStep) pitchErr = -pitchStep;
            body.Pitch = RecoveryFrames.Wrap(body.Pitch + pitchErr);
            double throttle = cmd.Throttle;
            if (double.IsNaN(throttle) || throttle < 0) throttle = 0;
            if (throttle > 1) throttle = 1;
            if (body.Prop <= 1e-8)
            {
                if (throttle > 0.05) body.FuelEmpty = true;
                throttle = 0;
            }
            Advance(ref body, craft, body.Pitch, throttle, dt);
            body.T += dt;
            double finTarget = cmd.Fins ? 1 : 0;
            body.Throttle = throttle;
            body.Fins = Move(body.Fins, finTarget, FinRate * dt);
            double legTarget = cmd.Legs ? 1 : 0;
            body.Legs = Move(body.Legs, legTarget, LegRate * dt);
            body.Phase = cmd.Phase;
            if (Altitude(body) <= 0 && Radial(body) < 1.5)
                body.Phase = RecoveryPhase.Down;
        }

        public static double ImpactArc(RecoveryBody body, in RecoveryCraft craft)
        {
            body.Prop = 0;
            double elapsed = 0;
            int guard = 0;
            while (guard++ < 4000 && elapsed < 700)
            {
                double alt = Altitude(body);
                if (alt <= 0) return Arc(body);
                double dt = alt < 4000 ? 0.1 : 0.25;
                Advance(ref body, craft, body.Pitch, 0, dt);
                elapsed += dt;
            }
            return Arc(body);
        }

        public static RecoverySample Sample(in RecoveryBody body, in RecoveryCraft craft)
        {
            RecoveryFrames.Axes(body.X, body.Y, out _, out double alt, out double arc, out _, out _, out _, out _);
            RecoveryFrames.Split(body.X, body.Y, body.Vx, body.Vy, out double east, out double radial);
            double speed = Math.Sqrt(body.Vx * body.Vx + body.Vy * body.Vy);
            double tilt = Math.Abs(RecoveryFrames.Wrap(body.Pitch - Math.PI / 2)) * 180.0 / Math.PI;
            double sink = radial < 0 ? -radial : 0;
            var atm = Atmosphere.At(alt > 0 ? alt : 0);
            double q = 0.5 * atm.rho * speed * speed;
            return new RecoverySample
            {
                T = body.T,
                Alt = alt,
                Speed = speed,
                Mass = body.Mass,
                Prop = body.Prop,
                Pitch = body.Pitch,
                Throttle = body.Throttle,
                Q = q,
                Sink = sink,
                TiltDeg = tilt,
                MissM = arc - body.BargeArc,
                East = east,
                Radial = radial,
                Arc = arc,
                Phase = body.Phase,
                Fins = body.Fins,
                Legs01 = body.Legs,
                FuelEmpty = body.FuelEmpty || body.Prop <= 1e-6,
            };
        }

        static void Advance(ref RecoveryBody body, in RecoveryCraft craft, double pitch, double throttle, double dt, bool fast = false)
        {
            Deriv(body, craft, pitch, throttle, out double ax1, out double ay1, out double mdot1, fast);
            var mid = body;
            mid.Vx = body.Vx + ax1 * dt * 0.5;
            mid.Vy = body.Vy + ay1 * dt * 0.5;
            mid.X = body.X + body.Vx * dt * 0.5;
            mid.Y = body.Y + body.Vy * dt * 0.5;
            double dm1 = mdot1 * dt * 0.5;
            if (dm1 > mid.Prop) dm1 = mid.Prop;
            if (dm1 < 0) dm1 = 0;
            mid.Prop -= dm1;
            mid.Mass -= dm1;
            if (mid.Mass < craft.DryKg) mid.Mass = craft.DryKg;
            Deriv(mid, craft, pitch, throttle, out double ax2, out double ay2, out double mdot2, fast);
            double vx0 = body.Vx, vy0 = body.Vy;
            body.Vx += ax2 * dt;
            body.Vy += ay2 * dt;
            body.X += (vx0 + body.Vx) * 0.5 * dt;
            body.Y += (vy0 + body.Vy) * 0.5 * dt;
            double dm = mdot2 * dt;
            if (dm > body.Prop) dm = body.Prop;
            if (dm < 0) dm = 0;
            body.Prop -= dm;
            body.Mass -= dm;
            if (body.Mass < craft.DryKg) body.Mass = craft.DryKg;
            if (body.Prop < 1e-9) body.Prop = 0;
        }

        static void Deriv(in RecoveryBody body, in RecoveryCraft craft, double pitch, double throttle,
            out double ax, out double ay, out double mdot, bool fast = false)
        {
            RecoveryFrames.Axes(body.X, body.Y, out double r, out double alt, out _, out double eastX, out double eastY, out double upX, out double upY);
            double g = Gravity.MU_EARTH / (r * r);
            ax = -g * upX;
            ay = -g * upY;
            double mass = body.Mass > 1 ? body.Mass : 1;
            double speed = Math.Sqrt(body.Vx * body.Vx + body.Vy * body.Vy);
            double bx = Math.Cos(pitch) * eastX + Math.Sin(pitch) * upX;
            double by = Math.Cos(pitch) * eastY + Math.Sin(pitch) * upY;
            double thr = throttle;
            if (thr < 0 || double.IsNaN(thr)) thr = 0;
            if (thr > 1) thr = 1;
            if (body.Prop <= 1e-8) thr = 0;
            double z = alt > 0 ? alt : 0;
            AtmState atm;
            if (fast) atm = PredictAir.At(z);
            else atm = Atmosphere.At(z);
            double sl = craft.ThrustSlN > 0 ? craft.ThrustSlN : craft.ThrustVacN;
            double vac = craft.ThrustVacN > 0 ? craft.ThrustVacN : sl;
            double pFrac = Atmosphere.P0 > 0 ? atm.p / Atmosphere.P0 : 0;
            if (pFrac < 0) pFrac = 0;
            if (pFrac > 1) pFrac = 1;
            double thrust = (sl + (vac - sl) * (1 - pFrac)) * thr;
            ax += thrust * bx / mass;
            ay += thrust * by / mass;
            mdot = thr > 0 && craft.IspSec > 1 ? thrust / (craft.IspSec * Atmosphere.G0) : 0;
            if (!(speed > 1) || !(atm.rho > 0)) return;
            double vhx = body.Vx / speed, vhy = body.Vy / speed;
            double dot = bx * vhx + by * vhy;
            if (dot > 1) dot = 1;
            else if (dot < -1) dot = -1;
            double alpha = Math.Acos(dot);
            if (vhx * by - vhy * bx < 0) alpha = -alpha;
            double aAbs = Math.Abs(alpha);
            if (aAbs > 1.15) aAbs = 1.15;
            double cd = (craft.Cd > 0.02 ? craft.Cd : 0.45) + 1.15 * aAbs * aAbs;
            if (body.Fins > 0) cd += AeroModel.GridDeployedCd * body.Fins;
            double area = Aero.RefArea(craft.DiameterM > 0.2 ? craft.DiameterM : 1);
            double q = 0.5 * atm.rho * speed * speed;
            double drag = q * cd * area / mass;
            ax -= drag * vhx;
            ay -= drag * vhy;
            double cl = 1.2 * Math.Sin(alpha) * (0.25 + 0.75 * body.Fins);
            if (cl > 0.85) cl = 0.85;
            else if (cl < -0.85) cl = -0.85;
            double lift = q * cl * area / mass;
            ax += lift * (-vhy);
            ay += lift * vhx;
        }

        public static double LandingReserveKg(in RecoveryCraft craft)
        {
            double dry = craft.DryKg > 1 ? craft.DryKg : 1;
            double g = Atmosphere.G0;
            double thrust = craft.ThrustVacN > 0 ? craft.ThrustVacN : craft.ThrustSlN;
            double twr = thrust / (dry * g);
            if (twr < 1.05) twr = 1.05;
            double area = Aero.RefArea(craft.DiameterM > 0.2 ? craft.DiameterM : 1);
            double cd = (craft.Cd > 0.02 ? craft.Cd : 0.45) + (craft.GridFins ? AeroModel.GridDeployedCd : 0);
            double vTerm = Math.Sqrt(2 * dry * g / Math.Max(1e-3, 1.225 * cd * area));
            // The entry burn and the fins are what kill the raw terminal speed.
            // This floor is the hoverslam share the earlier burns are not allowed to spend.
            if (vTerm > 85) vTerm = 85;
            double dv = RecoveryModel.LandingDeltaV(vTerm, g, 4, twr);
            if (double.IsNaN(dv) || double.IsInfinity(dv) || dv < 0) return 0;
            if (!(craft.IspSec > 1)) return craft.ReserveKg;
            double prop = dry * (Math.Exp(dv / (craft.IspSec * g)) - 1);
            if (prop < 0) prop = 0;
            double cap = craft.ReserveKg * 0.82;
            if (cap < 0) cap = 0;
            if (prop > cap) prop = cap;
            return prop;
        }

        static void Aim(double x, double y, double east, double radial, out double vx, out double vy)
        {
            RecoveryFrames.Axes(x, y, out _, out _, out _, out double ex, out double ey, out double ux, out double uy);
            vx = east * ex + radial * ux;
            vy = east * ey + radial * uy;
        }

        static double Altitude(in RecoveryBody body)
        {
            RecoveryFrames.Axes(body.X, body.Y, out _, out double alt, out _, out _, out _, out _, out _);
            return alt;
        }

        static double Arc(in RecoveryBody body)
        {
            RecoveryFrames.Axes(body.X, body.Y, out _, out _, out double arc, out _, out _, out _, out _);
            return arc;
        }

        static double Radial(in RecoveryBody body)
        {
            RecoveryFrames.Split(body.X, body.Y, body.Vx, body.Vy, out _, out double radial);
            return radial;
        }

        static double Move(double cur, double target, double step)
        {
            if (cur < target)
            {
                cur += step;
                if (cur > target) cur = target;
            }
            else if (cur > target)
            {
                cur -= step;
                if (cur < target) cur = target;
            }
            if (cur < 0) return 0;
            if (cur > 1) return 1;
            return cur;
        }

        static void Remember(ref RecoveryMark[] marks, ref int n, in RecoveryBody body)
        {
            if (n >= marks.Length)
            {
                var grown = new RecoveryMark[marks.Length * 2];
                for (int i = 0; i < marks.Length; i++) grown[i] = marks[i];
                marks = grown;
            }
            RecoveryFrames.Axes(body.X, body.Y, out _, out double alt, out double arc, out _, out _, out _, out _);
            RecoveryFrames.Split(body.X, body.Y, body.Vx, body.Vy, out _, out double radial);
            double speed = Math.Sqrt(body.Vx * body.Vx + body.Vy * body.Vy);
            marks[n++] = new RecoveryMark
            {
                Phase = body.Phase,
                T = body.T,
                Alt = alt,
                Speed = speed,
                Sink = radial < 0 ? -radial : 0,
                TiltDeg = Math.Abs(RecoveryFrames.Wrap(body.Pitch - Math.PI / 2)) * 180.0 / Math.PI,
                MissM = arc - body.BargeArc,
                Prop = body.Prop,
            };
        }
    }

    /// <summary>
    /// The predictor's air: Atmosphere.At sampled every 100 m to 120 km once, then interpolated (log-linear density and
    /// pressure). About a hundred times cheaper than the full ISA call; the flown step still uses Atmosphere.At.
    /// </summary>
    static class PredictAir
    {
        const double Step = 100;
        const int N = 1201;
        static double[] lnRho, lnP;

        static void Build()
        {
            var r = new double[N];
            var p = new double[N];
            for (int i = 0; i < N; i++)
            {
                var a = Atmosphere.At(i * Step);
                r[i] = Math.Log(a.rho > 1e-300 ? a.rho : 1e-300);
                p[i] = Math.Log(a.p > 1e-300 ? a.p : 1e-300);
            }
            lnP = p;
            lnRho = r;
        }

        public static AtmState At(double z)
        {
            if (lnRho == null || lnP == null) Build();
            if (z < 0) z = 0;
            double u = z / Step;
            int i = (int)u;
            if (i >= N - 1) { i = N - 2; u = N - 1; }
            double f = u - i;
            var rho = Math.Exp(lnRho[i] + (lnRho[i + 1] - lnRho[i]) * f);
            var pr = Math.Exp(lnP[i] + (lnP[i + 1] - lnP[i]) * f);
            return new AtmState { rho = rho, p = pr };
        }
    }
}
