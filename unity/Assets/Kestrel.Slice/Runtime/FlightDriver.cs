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
        public void Hold(bool on)
        {
            Held = on;
            if (on) CancelSkip();
        }

        // ---- Skip to next stage (DECISIONS #78) ----
        bool skipping;
        StageSkipTarget skipTarget = StageSkipTarget.None;
        /// <summary>True while a skip is fast-forwarding the sim.</summary>
        public bool Skipping => skipping;
        /// <summary>The event the current skip is heading for (the HUD label while skipping).</summary>
        public StageSkipTarget SkipTarget => skipTarget;
        /// <summary>Why the last skip stopped (None while one is running or none has run).</summary>
        public StageSkipStop SkipStop { get; private set; } = StageSkipStop.None;
        /// <summary>The event the last skip actually stopped at (None unless it stopped at a target).</summary>
        public StageSkipTarget SkipReached { get; private set; } = StageSkipTarget.None;
        /// <summary>Sim seconds the skip pump advanced this frame (0 when not skipping): SliceApp advances the booster by the same amount, so the two sims stay in lockstep.</summary>
        public double SkipAdvancedSimS { get; private set; }
        /// <summary>
        /// Telemetry sink for skipped time (set by SliceApp to the FailureHost's sampler). The frame sampler
        /// only runs once per rendered frame, so while the skip fast-forwards the sim it would starve the
        /// debrief log; the pump hands this sink one sample per 0.1 sim-seconds from the real stepped state
        /// instead. Null flies without it (the batch and the predictor never set it).
        /// </summary>
        public Action<AscentSample> SkipSampler { get; set; }
        /// <summary>The next ascent event, or None when the flight is over.</summary>
        public StageSkipTarget NextSkipTarget => Running && Sim != null && !Sim.Done ? StageSkip.NextAscentTarget(Sim, checks) : StageSkipTarget.None;
        /// <summary>
        /// True when the button may be offered: the flight is running, nothing is waiting on the player, and a
        /// next event exists. Not while held, not in a cinematic slow-mo, not after a failure.
        /// </summary>
        public bool SkipAvailable => Running && Sim != null && !Sim.Done && !Lost && !Held
            && Kestrel.Kit.CinematicEvents.PresentationScale == 1
            && NextSkipTarget != StageSkipTarget.None
            && (Decisions == null || Decisions.OpenCount == 0);

        /// <summary>Start skipping to the next flight event. No-op when <see cref="SkipAvailable"/> is false.</summary>
        public void StartSkip()
        {
            if (!SkipAvailable) return;
            skipping = true;
            skipTarget = NextSkipTarget;
            SkipStop = StageSkipStop.None;
            SkipReached = StageSkipTarget.None;
        }

        /// <summary>Stop skipping and fly in real time from exactly where the sim is (the tap-to-cancel).</summary>
        public void CancelSkip()
        {
            skipping = false;
            skipTarget = StageSkipTarget.None;
        }

        /// <summary>
        /// Stop skipping because the booster reached its next burn phase or touchdown (SliceApp): the first event
        /// on either timeline wins. No-op when not skipping.
        /// </summary>
        public void HaltSkip(StageSkipTarget reached)
        {
            if (!skipping) return;
            skipping = false;
            skipTarget = StageSkipTarget.None;
            SkipStop = StageSkipStop.Target;
            SkipReached = reached;
        }
        public SimResult Result { get; private set; }
        public bool Lost => Result != null && Result.failure != null;
        /// <summary>PR5: the coast warp the last frame ran at (1 = real time). See Kestrel.Core.Flight.CoastWarp.</summary>
        public double Warp => Running && warp != null ? warp.Factor : 1;
        /// <summary>Sim seconds to the next call or predictable event (CoastWarp), +Infinity with no flight.</summary>
        public double SecondsToNext => Running && warp != null ? warp.SecondsToNext : double.PositiveInfinity;
        /// <summary>Narrator Voice: set each frame while a non-interruptible line holds warp at ×1 (NarrationPlayer.HoldsWarp).</summary>
        public bool NarrationBusy { get; set; }
        /// <summary>Sound: the warp rate changed (from, to, sim time). Only on a real change. See WarpedFlightClock.WarpChanged.</summary>
        public event Action<int, int, double> WarpChanged;
        /// <summary>Sound: a drop to ×1 in about this many real seconds (0 when unforeseen). Always before WarpChanged(x→1).</summary>
        public event Action<int, float> WarpWillDrop;
        /// <summary>PR5: set each frame by the app while the camera follows the returning booster (RecoveryShow.SteerCamera); warp holds ×1.</summary>
        public bool CameraOnBooster { get; set; }
        /// <summary>PR5: the player's warp switch. True keeps real time. Launch reads it from kh.flight.timewarp (TimeWarpSetting).</summary>
        public bool WarpOff { get; set; }
        public double OriginX { get; private set; }   // floating origin in pad-local metres (double)
        public double OriginY { get; private set; }
        public int Rebases { get; private set; }
        /// <summary>Kinematic pitch from the pad frame, degrees from horizontal. The drawn airframe may lag this.</summary>
        public float AttitudeDeg { get; private set; } = 90f;
        public bool HasAttitude { get; private set; }

        double clock; double thrustRef = 1; double prevX, prevY;
        PlayedChecks checks;
        WarpedFlightClock warp;
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

        /// <summary>Widest stage (or strap-on) diameter of the design, m. Drives the size of the blast so it follows the real rocket.</summary>
        internal static double WidestDiameterM(Design d)
        {
            double w = 0;
            if (d == null) return w;
            if (d.stages != null) foreach (var s in d.stages) if (s != null && s.diameter > w) w = s.diameter;
            if (d.strapOns != null && d.strapOns.diameter > w) w = d.strapOns.diameter;
            return w;
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
            CinematicSink.VehicleRadiusM = Kestrel.Kit.FlightSinkMath.VehicleRadiusM(WidestDiameterM(d));
            Running = true;
            clock = 0; Meco = false; Result = null; OriginX = OriginY = 0; Rebases = 0; prevX = prevY = 0;
            skipping = false; skipTarget = StageSkipTarget.None; SkipAdvancedSimS = 0; SkipStop = StageSkipStop.None; SkipReached = StageSkipTarget.None;
            AttitudeDeg = 90f; HasAttitude = false;
            seenEvents = 0; mecoSent = false; warp = null;
            // The origin is back at the pad, so the pad has to be too: after a flight past RebaseDistance the
            // anchored ground, deck and tower were left where the last rebase put them (a displaced pad on relaunch).
            PlaceAnchors();
            try
            {
                // The designer and the launch predictor fly these same checks (#314).
                checks = new PlayedChecks(c, d, stages, opts, Sim);
                warp = new WarpedFlightClock(directed, checks, opts.fairingJettisonAltM);
                warp.WarpChanged += (a, b, t) => WarpChanged?.Invoke(a, b, t);
                warp.WarpWillDrop += (a, r) => WarpWillDrop?.Invoke(a, r);
                WarpOff = TimeWarpSetting.Resolve(PlayerPrefs.HasKey(TimeWarpSetting.Pref), PlayerPrefs.GetInt(TimeWarpSetting.Pref, 0)) == TimeWarpMode.Off;
                thrustRef = Math.Max(1, stages[0].thrustVac);
                omegaAir = opts.rotate ? Earth.SiteRotationSpeed(opts.latitude) / Gravity.R_EARTH : 0;
                UpdateFrame();
                (Sink as IFlightStageSink)?.BeginNarration(NarrationFacts.From(d, hook != null ? hook.SiteId : null).With("mission", aimed != null ? aimed.Id : null), Calls);
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
            SkipAdvancedSimS = 0;
            if (skipping)
            {
                // Skip to next stage (#78): fast-forward the same sim in bounded per-frame chunks. The pump runs
                // DirectedFlight.Step with the same played checks as the real-time loop below, so the state at the
                // event is the state real time would have reached; the clock is kept on the sim so no debt is banked.
                double before = Sim.T;
                var run = StageSkip.Advance(directed, checks, StageSkip.MaxStepsPerFrame, SkipSampler);
                SkipAdvancedSimS = Sim.T - before;
                if (run.Meco) Meco = true;
                if (run.Stop != StageSkipStop.Budget)
                {
                    skipping = false;
                    skipTarget = StageSkipTarget.None;
                    SkipStop = run.Stop;
                    SkipReached = run.Reached;
                }
                clock = Sim.T;
            }
            // PR5 coast warp (Core WarpedFlightClock): more fixed 1/120 s steps per frame while nothing is due, ×1 before every
            // call and event. The steps, director and played checks run exactly as before, so the flight is bit-identical.
            // Skip wins over warp: while a skip is fast-forwarding, the warp clock is frozen and resumes after.
            else if (warp != null)
            {
                warp.Disabled = WarpOff;
                warp.Advance(Math.Min(frame, 0.1f), Kestrel.Kit.CinematicEvents.PresentationScale, CameraOnBooster, NarrationBusy);
                if (warp.MecoThisFrame) Meco = true;
                clock = warp.Clock;
            }
            else
            {
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
                PlaceAnchors();
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

        /// <summary>
        /// One more launch / Restart: put the driver back on the pad before the next count. The old flight stops
        /// (Running was still true after a mid-flight Restart, so the next T-0 resumed it instead of launching), the
        /// result is dropped (a rebuilt FailureHost must not replay it), and the floating origin, the anchored pad
        /// and the vehicle all go back to pad-local zero.
        /// </summary>
        public void ResetToPad()
        {
            Running = false; Held = false;
            Result = null; Meco = false; mecoSent = false; seenEvents = 0;
            skipping = false; skipTarget = StageSkipTarget.None; SkipAdvancedSimS = 0; SkipStop = StageSkipStop.None; SkipReached = StageSkipTarget.None;
            Sim = null; directed = null; checks = null;
            clock = 0; prevX = prevY = 0;
            Downrange = Altitude = Speed = ThrustFraction = 0;
            AttitudeDeg = 90f; HasAttitude = false;
            OriginX = OriginY = 0; Rebases = 0;
            PlaceAnchors();
            if (Vehicle != null) { Vehicle.localPosition = Vector3.zero; Vehicle.localRotation = Quaternion.identity; }
            if (Plume != null) Plume.Apply(0, Atmosphere.P0, 0);
        }

        /// <summary>True when every anchored pad object sits at its pad-local spot for the current origin (tests).</summary>
        public bool AnchorsAtOrigin()
        {
            foreach (var a in anchored)
            {
                if (a.t == null) continue;
                var want = new Vector3((float)(a.x - OriginX), (float)(a.y - OriginY), (float)a.z);
                if ((a.t.localPosition - want).sqrMagnitude > 1e-4f) return false;
            }
            return true;
        }

        void PlaceAnchors()
        {
            foreach (var a in anchored) if (a.t != null) Place(a.t, a.x, a.y, a.z);
        }

        void Place(Transform t, double x, double y, double z) => t.localPosition = new Vector3((float)(x - OriginX), (float)(y - OriginY), (float)z);
    }
}
