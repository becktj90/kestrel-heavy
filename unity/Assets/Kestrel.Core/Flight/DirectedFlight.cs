// One directed ascent (#313): the AscentSim stepped together with the AscentDirector, the way the
// live pad flies it. FlightDriver, FlightBatch, the designer practice flight (ReferenceAscent) and
// LaunchPredictor all step through this class, so prediction and play fly one throttle model. The max-Q
// throttle is always the design's own guidance law (GuidanceProfile), never a second director bucket.
// Deterministic: no clock, no randomness beyond the seeded schedule the caller passes in.
using System;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    public sealed class DirectedFlight
    {
        readonly SimOptions opts;
        readonly double dt;
        readonly double omegaAir;
        bool ascentLost, maxQEngaged;

        public AscentSim Sim { get; }
        /// <summary>Null flies the bare sim (no calls, no throttle commands).</summary>
        public AscentDirector Director { get; }

        public DirectedFlight(AscentSim sim, AscentDirector director, SimOptions opts)
        {
            Sim = sim ?? throw new ArgumentNullException(nameof(sim));
            Director = director;
            this.opts = opts;
            dt = opts != null && opts.dt > 0 ? opts.dt : MissionFlightSetup.BatchDt;
            omegaAir = opts != null && opts.rotate ? Earth.SiteRotationSpeed(opts.latitude) / Gravity.R_EARTH : 0;
        }

        /// <summary>
        /// One physics step: apply the director's command, tick the sim, then let the director read the new
        /// state and re-apply. Returns false once the sim is done.
        /// </summary>
        public bool Step()
        {
            if (Sim.Done) return false;
            Apply();
            int events = Sim.Events.Count;
            Sim.Tick();
            if (Director != null && !Sim.Done)
            {
                Director.Advance(Sample(), dt);
                Apply();
            }
            // #314: the played checks, in the pad's order (tick, director, checks). The batch leaves them off.
            if (Checks != null)
            {
                Checks.NoteEvents(Sim, events);
                if (Checks.StopForBreakup(Sim)) return false;
            }
            return !Sim.Done;
        }

        /// <summary>#314: the checks that end a played flight (PlayedChecks). The designer and predictor set them; null flies without.</summary>
        public PlayedChecks Checks { get; set; }

        /// <summary>Fly to the end and return the result.</summary>
        public SimResult Run()
        {
            while (Step()) { }
            return Sim.Finish();
        }

        /// <summary>Hands the director's throttle, pitch and loss command to the sim.</summary>
        public void Apply()
        {
            if (Director == null) return;
            var cmd = Director.Command;
            double scale = cmd.ThrottleScale;
            if (double.IsNaN(scale) || scale <= 0) scale = 1;
            else if (scale > 1) scale = 1;
            Sim.LiveThrottleScale = scale;
            Sim.LivePitchOffsetRad = cmd.PitchOffsetRad;
            if (cmd.EngageMaxQCap > 0 && !maxQEngaged && opts != null)
            {
                // A taken max-Q call on a design without a max-Q throttle: run the design's own guidance law
                // (same trigger, depth and duration as a designed cap) at the offered cap.
                maxQEngaged = true;
                if (opts.guidance == null) opts.guidance = GuidanceProfile.Builtin(opts.targetAlt);
                opts.guidance.maxQThrottleSet = true;
                opts.guidance.maxQThrottle = cmd.EngageMaxQCap * 100;
            }
            if (cmd.Lost && !ascentLost)
            {
                ascentLost = true;
                Sim.BreakUp(new FailureEvent
                {
                    t = Sim.T,
                    kind = cmd.LossKind,
                    kid = cmd.LossKid,
                    engineer = cmd.LossEngineer,
                });
            }
        }

        /// <summary>The director's view of the sim, in the pad frame.</summary>
        public AscentSample Sample()
        {
            double x = Sim.Y[0], y = Sim.Y[1];
            double r = Math.Sqrt(x * x + y * y);
            double padAngle = Math.PI / 2 + omegaAir * Sim.T;
            double down = Gravity.R_EARTH * (Math.Atan2(y, x) - padAngle);
            double alt = r - Gravity.R_EARTH;
            double vx = Sim.Y[2] + omegaAir * y, vy = Sim.Y[3] - omegaAir * x;
            double speed = Math.Sqrt(vx * vx + vy * vy);
            double mass = Sim.Y[4];
            double prop = Sim.PropellantFraction;
            if (double.IsNaN(prop)) prop = 1;
            double g = mass > 1 && Sim.LastThrust > 0 ? Sim.LastThrust / mass / Atmosphere.G0 : 0;
            return new AscentSample
            {
                T = Sim.T,
                AltM = alt,
                DownrangeM = down,
                SpeedMps = speed,
                QPa = Sim.LastQ,
                G = g,
                MassKg = mass,
                ThrustN = Sim.LastThrust,
                PropFraction = prop,
                LossGravity = Sim.LossGravity,
                LossDrag = Sim.LossDrag,
                LossSteering = Sim.LossSteering,
                StageIndex = Sim.StageIdx,
                Thrusting = Sim.Thrusting,
            };
        }
    }
}
