// Runs the engine-agnostic core sim (Kestrel.Core.Physics.AscentSim) from liftoff through cutoff at a
// fixed 1/120 s physics step, decoupled from the render rate, and presents it with a floating origin.
// MECO is reported, and the sim keeps going so the launch sequence can call the rest of the flight.
//
// Frames: the sim is 2-D, Earth-centred inertial (double). We convert to a pad-fixed local frame
// (downrange metres east, altitude metres up) in double, then render RELATIVE to a floating origin
// that re-centres whenever the vehicle is > RebaseDistance from it, so float precision near the
// camera stays sub-millimetre at any altitude/downrange.
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using Kestrel.Game.Audio;
using UnityEngine;

namespace Kestrel.Slice
{
    public sealed class FlightDriver : MonoBehaviour, IFlightAudioBus
    {
        public const double PhysicsDt = 1.0 / 120;
        public const float RebaseDistance = 2000f;

        public Transform World;        // pad, ground, scenery: children are placed in local (double) coords minus origin
        public Transform Vehicle;      // rocket root (pivot at engine exit plane)
        public PlumeController Plume;
        public event Action<FlightDriver> OnMeco;
        public IFlightEventSink Sink { get; set; }

        public AscentSim Sim { get; private set; }
        public double SimTime => Sim?.T ?? 0;
        public double Downrange { get; private set; }
        public double Altitude { get; private set; }
        public double Speed { get; private set; }
        public double Mach => Sim?.LastMach ?? 0;
        public double DynPressure => Sim?.LastQ ?? 0;
        public double AmbientPressure => Sim?.LastP ?? Atmosphere.P0;
        public double ThrustFraction { get; private set; }
        public bool Meco { get; private set; }
        public bool Running { get; private set; }
        /// <summary>Pause. The sim clock does not advance until <see cref="Hold"/> is cleared.</summary>
        public bool Held { get; private set; }
        public void Hold(bool on) => Held = on;
        public SimResult Result { get; private set; }
        public bool Lost => Result != null && Result.failure != null;
        public double OriginX { get; private set; }   // floating origin in pad-local metres (double)
        public double OriginY { get; private set; }
        public int Rebases { get; private set; }
        /// <summary>Kinematic pitch from the pad frame, degrees from horizontal. The drawn airframe may lag this.</summary>
        public float AttitudeDeg { get; private set; } = 90f;
        public bool HasAttitude { get; private set; }

        double clock; int mecoStage; double thrustRef = 1; double prevX, prevY;
        bool arcadeBreaks;
        double breakupQPa, liftoffTwr, dvMargin;
        double[] thrustByStage = System.Array.Empty<double>();
        int seenEvents; bool mecoSent;
        readonly List<(Transform t, double x, double y, double z)> anchored = new List<(Transform, double, double, double)>();
        double omegaAir;

        /// <summary>Register a world object at a fixed pad-local position (double precision).</summary>
        public void Anchor(Transform t, double x, double y, double z) { anchored.Add((t, x, y, z)); Place(t, x, y, z); }

        public AscentDirector Decisions { get; private set; }
        GuidanceProfile guidance;
        bool throttleHeld, savedMaxQSet, ascentLost;
        double savedMaxQ;

        public void Launch(Design d) => Launch(d, null);

        public void Launch(Design d, AscentHook hook)
        {
            var c = SliceData.Catalog;
            var (stages, opts) = Stack.SimInputs(c, d, PhysicsDt);
            Decisions = null;
            throttleHeld = false;
            ascentLost = false;
            guidance = opts.guidance;
            if (hook != null)
            {
                if (hook.Monitor != null) opts.monitor = hook.Monitor;
                if (hook.Wind != null && !hook.Wind.Calm) opts.wind = hook.Wind;
                if (hook.HasLatitude) opts.latitude = hook.Latitude;
                guidance = opts.guidance;
                Decisions = hook.Director;
            }
            opts.tMax = 600;
            thrustByStage = new double[stages.Count];
            for (int i = 0; i < stages.Count; i++) thrustByStage[i] = stages[i].thrustVac;
            Sim = new AscentSim(stages, opts);
            arcadeBreaks = PlayedBreakup.Arcade(d);
            breakupQPa = FailureMonitor.StructuralQLimPa(c, d);
            liftoffTwr = stages.Count > 0 && Sim.Y[4] > 1 ? stages[0].thrustSL / (Sim.Y[4] * Atmosphere.G0) : 0;
            double ideal = Headless.StackDeltaV(stages, opts.payload).total;
            double required = Stack.RequiredDeltaV(c, d != null ? d.payloadOrbit : null).required;
            dvMargin = PlayedBreakup.Margin(ideal, required);
            // MECO = burnout of the core first stage (serial entry whose role is core with index 0).
            var serial = Stack.SerialStages(c, d).stages;
            mecoStage = serial.FindIndex(s => s.role == "core" && s.index == 0) + 1;
            thrustRef = Math.Max(1, stages[0].thrustVac);
            omegaAir = opts.rotate ? Earth.SiteRotationSpeed(opts.latitude) / Gravity.R_EARTH : 0;
            clock = 0; Meco = false; Result = null; Running = true; OriginX = OriginY = 0; Rebases = 0; prevX = prevY = 0;
            AttitudeDeg = 90f; HasAttitude = false;
            seenEvents = 0; mecoSent = false;
            UpdateFrame();
            Sink?.OnEvent("flight.begin");
            PumpEvents();
            PublishAudio();
        }

        void Update()
        {
            if (Held || !Running || Sim == null) return;
            clock += Math.Min(Time.deltaTime, 0.1); // clamp hitches; physics catches up at fixed dt
            int guard = 0;
            while (Sim.T < clock && !Sim.Done && guard++ < 64)
            {
                ApplyAscent();
                int before = Sim.Events.Count;
                Sim.Tick();
                if (Decisions != null && !Sim.Done)
                {
                    Decisions.Advance(AscentSample(), PhysicsDt);
                    ApplyAscent();
                }
                if (!Meco)
                {
                    for (int i = before; i < Sim.Events.Count; i++)
                    {
                        var ev = Sim.Events[i];
                        if (ev.type == "burnout" && ev.stage == mecoStage) Meco = true;
                    }
                }
                if (StopForBreakup()) break;
            }
            if (Sim.Done)
            {
                Running = false;
                if (Result == null) Result = Sim.Finish();
            }
            PumpEvents();
            if (Meco && !mecoSent && !Lost) { mecoSent = true; Sink?.OnEvent("meco"); }
            UpdateFrame();
            PublishAudio();
            if (Meco && !Lost) OnMeco?.Invoke(this);
        }

        /// <summary>True when this step ends the played flight in a breakup. The result is published once the sim is done.</summary>
        bool StopForBreakup()
        {
            if (HasEvent("failure")) { Sim.BreakUp(null); return true; }
            FailureEvent ev = null;
            double alt = LiveAlt();
            if (HasEvent("impact")) ev = PlayedBreakup.Impact(Sim.T);
            else if (Meco && dvMargin < 0) ev = PlayedBreakup.Short(Sim.T, dvMargin);
            else if (arcadeBreaks)
            {
                double mass = Sim.Y[4];
                double accel = mass > 1 ? Sim.LastThrust / mass : 0;
                var sample = new PlayedBreakup.Sample
                {
                    T = Sim.T, AltM = alt, QPa = Sim.LastQ, AccelMps2 = accel,
                    QLimPa = breakupQPa, LiftoffTwr = liftoffTwr, Thrusting = Sim.Thrusting,
                };
                ev = PlayedBreakup.At(sample);
            }
            if (ev == null) return false;
            Sim.BreakUp(ev);
            return true;
        }

        double LiveAlt()
        {
            double x = Sim.Y[0], y = Sim.Y[1];
            return Math.Sqrt(x * x + y * y) - Gravity.R_EARTH;
        }

        bool HasEvent(string type)
        {
            var ev = Sim.Events;
            for (int i = ev.Count - 1; i >= 0; i--)
                if (ev[i].type == type) return true;
            return false;
        }

        void PumpEvents()
        {
            if (Sink == null || Sim == null) return;
            var ev = Sim.Events;
            while (seenEvents < ev.Count)
            {
                var type = ev[seenEvents++].type;
                if (!string.IsNullOrEmpty(type)) Sink.OnEvent(type);
            }
        }

        void PublishAudio()
        {
            if (Sink == null || Sim == null) return;
            Sink.SetFlightState((float)Altitude, (float)DynPressure, (float)ThrustFraction);
        }

        void UpdateFrame()
        {
            double x = Sim.Y[0], y = Sim.Y[1];
            double r = Math.Sqrt(x * x + y * y);
            double padAngle = Math.PI / 2 + omegaAir * Sim.T;     // pad co-rotates with Earth (CCW)
            double ang = Math.Atan2(y, x);
            Downrange = Gravity.R_EARTH * (ang - padAngle);       // east positive
            Altitude = r - Gravity.R_EARTH;
            double vx = Sim.Y[2] + omegaAir * y, vy = Sim.Y[3] - omegaAir * x; // air-relative (= ground-relative)
            Speed = Math.Sqrt(vx * vx + vy * vy);
            int stage = Sim.StageIdx;
            if (stage >= 0 && stage < thrustByStage.Length) thrustRef = Math.Max(1, thrustByStage[stage]);
            ThrustFraction = Sim.Thrusting ? Sim.LastThrust / thrustRef : 0;

            // Floating origin: rebase when the vehicle drifts too far from (OriginX, OriginY).
            if (Math.Abs(Downrange - OriginX) > RebaseDistance || Math.Abs(Altitude - OriginY) > RebaseDistance)
            {
                OriginX = Downrange; OriginY = Altitude; Rebases++;
                foreach (var a in anchored) Place(a.t, a.x, a.y, a.z);
            }
            if (Vehicle != null)
            {
                Vehicle.localPosition = new Vector3((float)(Downrange - OriginX), (float)(Altitude - OriginY), 0);
                double dx = Downrange - prevX, dy = Altitude - prevY;
                if (dx * dx + dy * dy > 1e-6 && Altitude > 20)
                {
                    float pitch = (float)(Math.Atan2(dy, dx) * 180 / Math.PI); // from horizontal
                    AttitudeDeg = pitch;
                    HasAttitude = true;
                    Vehicle.localRotation = Quaternion.Euler(0, 0, pitch - 90);
                }
                prevX = Downrange; prevY = Altitude;
            }
            if (Plume != null) Plume.Apply(ThrustFraction, AmbientPressure, Speed);
        }

        void ApplyAscent()
        {
            if (Decisions == null || Sim == null) return;
            var cmd = Decisions.Command;
            double scale = cmd.ThrottleScale;
            if (double.IsNaN(scale) || scale <= 0) scale = 1;
            else if (scale > 1) scale = 1;
            Sim.LiveThrottleScale = scale;
            Sim.LivePitchOffsetRad = cmd.PitchOffsetRad;
            if (guidance != null && cmd.SuppressDesignThrottle)
            {
                if (!throttleHeld)
                {
                    savedMaxQSet = guidance.maxQThrottleSet;
                    savedMaxQ = guidance.maxQThrottle;
                    throttleHeld = true;
                }
                guidance.maxQThrottleSet = false;
            }
            else if (throttleHeld && guidance != null)
            {
                guidance.maxQThrottleSet = savedMaxQSet;
                guidance.maxQThrottle = savedMaxQ;
                throttleHeld = false;
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

        AscentSample AscentSample()
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

        void Place(Transform t, double x, double y, double z) => t.localPosition = new Vector3((float)(x - OriginX), (float)(y - OriginY), (float)z);
    }
}
