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
using Kestrel.Core.Flight.Narration;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using Kestrel.Game.Audio;
using UnityEngine;

namespace Kestrel.Slice
{
    public sealed class FlightDriver : MonoBehaviour, IFlightAudioBus
    {
        public const double PhysicsDt = MissionFlightSetup.LiveDt;
        public const float RebaseDistance = 2000f;

        public Transform World;        // pad, ground, scenery: children are placed in local (double) coords minus origin
        public Transform Vehicle;      // rocket root (pivot at engine exit plane)
        public PlumeController Plume;
        public event Action<FlightDriver> OnMeco;
        public IFlightEventSink Sink { get; set; }
        /// <summary>When the timeline expects its calls (from the launch sequence). Null: unknown, so no color exchange is offered.</summary>
        public CallSchedule Calls { get; set; }

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

        double clock; double thrustRef = 1; double prevX, prevY;
        PlayedChecks checks;
        double[] thrustByStage = System.Array.Empty<double>();
        int seenEvents; bool mecoSent;
        readonly List<(Transform t, double x, double y, double z)> anchored = new List<(Transform, double, double, double)>();
        double omegaAir;

        /// <summary>Register a world object at a fixed pad-local position (double precision).</summary>
        public void Anchor(Transform t, double x, double y, double z) { anchored.Add((t, x, y, z)); Place(t, x, y, z); }

        public AscentDirector Decisions { get; private set; }
        /// <summary>The sim stepped with its director, the same Core stepper the batch, designer and timeline fly (#313).</summary>
        DirectedFlight directed;

        /// <summary>Cinematic hooks for the live sim (explosions, debris, shockwave, camera, staging flash). Observer only.</summary>
        public Kestrel.Game.Environment.CinematicFlightSink CinematicSink { get; private set; }

        void BindCinematicSink()
        {
            if (CinematicSink == null)
            {
                CinematicSink = new Kestrel.Game.Environment.CinematicFlightSink();
                CinematicSink.Anchor = VehicleScenePosition;
            }
            CinematicSink.Attach();
            Sim.Sink = CinematicSink;
        }

        UnityEngine.Vector3 VehicleScenePosition() => Vehicle != null ? Vehicle.position : UnityEngine.Vector3.zero;

        void OnDisable()
        {
            if (CinematicSink != null) CinematicSink.Detach();
        }

        void OnDestroy()
        {
            if (CinematicSink != null) CinematicSink.Detach();
        }

        public void Launch(Design d) => Launch(d, null);

        public void Launch(Design d, AscentHook hook)
        {
            var c = SliceData.Catalog;
            // The campaign hop on the pad (null for a free build) and the briefing's pad and wind.
            var aimed = hook != null && hook.Mission != null ? hook.Mission : AscentFlight.CurrentMission();
            var site = hook != null ? new MissionFlightSite { HasLatitude = hook.HasLatitude, LatitudeDeg = hook.Latitude, Wind = hook.Wind } : null;
            List<SimStage> stages;
            SimOptions opts;
            try
            {
                // Same setup as FlightBatch (B-01, B-03): tMax from Stack.SimInputs (>= 2500 s), aim and
                // cutoff, coast boil-off and strap-on landing fuel. Only the step and the monitor differ.
                var setup = MissionFlightSetup.Build(c, aimed, d, PhysicsDt, site);
                stages = setup.Stages;
                opts = setup.Options;
            }
            catch (Exception e)
            {
                string orbit = d != null ? d.payloadOrbit : null;
                throw new InvalidOperationException("SimInputs failed for orbit " + (orbit ?? "?") + ": " + e.Message, e);
            }
            if (stages == null || stages.Count == 0)
                throw new InvalidOperationException("Launch has no stages.");
            Decisions = null;
            if (hook != null)
            {
                if (hook.Monitor != null) opts.monitor = hook.Monitor;
                Decisions = hook.Director;
            }
            thrustByStage = new double[stages.Count];
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i] == null) throw new InvalidOperationException("Launch stage " + i + " is missing.");
                thrustByStage[i] = stages[i].thrustVac;
            }
            // The sim is live before any presentation. A throw in the guide, the plume, or the
            // audio bus must not leave this flight without a clock.
            Sim = new AscentSim(stages, opts);
            directed = new DirectedFlight(Sim, Decisions, opts);
            BindCinematicSink();
            Running = true;
            clock = 0; Meco = false; Result = null; OriginX = OriginY = 0; Rebases = 0; prevX = prevY = 0;
            AttitudeDeg = 90f; HasAttitude = false;
            seenEvents = 0; mecoSent = false;
            try
            {
                // The designer and the launch predictor fly these same checks (#314).
                checks = new PlayedChecks(c, d, stages, opts, Sim);
                thrustRef = Math.Max(1, stages[0].thrustVac);
                omegaAir = opts.rotate ? Earth.SiteRotationSpeed(opts.latitude) / Gravity.R_EARTH : 0;
                UpdateFrame();
                (Sink as IFlightStageSink)?.BeginNarration(NarrationFacts.From(d, hook != null ? hook.SiteId : null), Calls);
                Sink?.OnEvent("flight.begin");
                PumpEvents();
                PublishAudio();
                GuideBinder.Listen(this);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void Update()
        {
            if (Held || !Running || Sim == null) return;
            float frame = Time.deltaTime;
            if (frame <= 0f && Time.unscaledDeltaTime > 0f) frame = Time.unscaledDeltaTime;
            // Slow-mo scales only how much wall-clock feeds the clock (Core PresentationTimeScale); Sim.Tick and PhysicsDt are untouched.
            clock += new Kestrel.Core.Flight.PresentationTimeScale(Kestrel.Kit.CinematicEvents.PresentationScale).ToSimSeconds(Math.Min(frame, 0.1f)); // clamp hitches; physics catches up at fixed dt
            int guard = 0;
            while (Sim.T < clock && !Sim.Done && guard++ < 64)
            {
                int before = Sim.Events.Count;
                directed.Step();
                if (checks != null && checks.NoteEvents(Sim, before)) Meco = true;
                if (checks != null && checks.StopForBreakup(Sim)) break;
            }
            if (Sim.Done)
            {
                Running = false;
                if (Result == null) Result = directed != null ? directed.Finish() : Sim.Finish();
            }
            PumpEvents();
            if (Meco && !mecoSent && !Lost) { mecoSent = true; Sink?.OnEvent("meco"); }
            UpdateFrame();
            PublishAudio();
            if (Meco && !Lost) OnMeco?.Invoke(this);
        }

        void PumpEvents()
        {
            if (Sink == null || Sim == null) return;
            var ev = Sim.Events;
            var staged = Sink as IFlightStageSink;
            while (seenEvents < ev.Count)
            {
                var e = ev[seenEvents++];
                if (string.IsNullOrEmpty(e.type)) continue;
                // Stage-aware sinks route ignitions by the sim stack entry (ses1 for a later core stage); the rest get the plain id.
                if (staged != null) staged.OnStagedEvent(e.type, e.stage ?? 0);
                else Sink.OnEvent(e.type);
            }
        }

        void PublishAudio()
        {
            if (Sink == null || Sim == null) return;
            Sink.SetFlightState((float)Altitude, (float)DynPressure, (float)ThrustFraction);
            (Sink as IFlightStageSink)?.SetFlightClock(SimTime, Altitude);
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

        void Place(Transform t, double x, double y, double z) => t.localPosition = new Vector3((float)(x - OriginX), (float)(y - OriginY), (float)z);
    }
}
