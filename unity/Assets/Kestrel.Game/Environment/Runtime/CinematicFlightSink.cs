// Maps every Kestrel.Core.Flight.IFlightSimSink hook onto Kestrel.Kit.CinematicEvents, so one assignment
// (AscentSim.Sink = new CinematicFlightSink { Anchor = ... }) lights up explosions, debris, shockwave, camera beats
// and staging flashes. Observer only: it never touches the sim and a null sink changes nothing.
//
// Positions: the sim's FlightVec is an Earth-centred ascent frame, not scene space (the scene is floating-origin).
// Scene positions therefore come from the Anchor delegate (the drawn vehicle), and EngineAnchor when set.
// What fires, and who does the rest:
//   OnCrash / OnFailure / OnEngineFailure / OnImpact -> CinematicEvents.Crash / Failure / EngineFailure; ExplosionFx,
//        DebrisFx, ShockwaveFx and CameraFx each already subscribe to those, so one raise gives fire + debris + ring + haze + shake.
//   OnIgnition -> staging ignition glow (+ pad overpressure ring and haze for stage 1) + a small camera beat
//   OnLiftoff -> camera beat, pad haze
//   OnMaxQ -> camera beat scaled by q
//   OnStageSeparation / OnFairingSeparation -> staging flash + staging camera beat. The physical stage and fairing
//        debris stay with FlightStageShow (it owns the meshes), so no separation debris is raised here.
//   OnTouchdown -> touchdown camera beat, grounded ring when hard on deck or ground
//   OnRecoveryPhase -> no visual of its own (audio and HUD listen); counted for tests
// No allocation per hook: events are structs, the delegates are cached, no strings are built.
using System;
using Kestrel.Core.Flight;
using Kestrel.Kit;
using UnityEngine;

namespace Kestrel.Game.Environment
{
    public sealed class CinematicFlightSink : IFlightSimSink
    {
        /// <summary>Scene position of the drawn vehicle. Null falls back to the origin.</summary>
        public Func<Vector3> Anchor;
        /// <summary>Scene position of engine <c>i</c>'s bell (optional; falls back to Anchor).</summary>
        public Func<int, Vector3> EngineAnchor;
        public float VehicleRadiusM = 1.5f;
        public double FullEnergyJoules = FlightSinkMath.FullEnergyJoules;
        /// <summary>Reduce-motion / reduce-flashing. Defaults to CameraFx's switch (the same PlayerPrefs the Slice comfort menu writes).</summary>
        public Func<bool> Reduce = () => CameraFx.ReduceMotion();

        public bool Bound { get; private set; }
        public int Hooks { get; private set; }
        public int LastPhase { get; private set; }
        bool blown;   // a crash or failure already raised this flight: OnImpact then adds nothing

        /// <summary>Mark a live sim as sink-driven so the legacy host raises stand down. Call once per flight; pair with Detach.</summary>
        public void Attach() { if (Bound) return; Bound = true; blown = false; CinematicEvents.SinkAttached(); }
        public void Detach() { if (!Bound) return; Bound = false; CinematicEvents.SinkDetached(); }

        Vector3 At() => Anchor != null ? Anchor() : Vector3.zero;
        bool Red() => Reduce != null && Reduce();

        public void OnIgnition(int stage, double t)
        {
            Hooks++;
            var p = At(); bool r = Red(); float s = FlightSinkMath.StageScale01(stage);
            CinematicEvents.StagingFlash(StagingFlashKind.Ignition, p.x, p.y, p.z, s, r);
            if (stage <= 1)
            {
                CinematicEvents.Shockwave(p.x, p.y, p.z, 0.12f, true, r, VehicleRadiusM);
                CinematicEvents.HeatHaze(p.x, p.y, p.z, 0.4f, 4f, r);
            }
            CinematicEvents.CameraBeat(CameraBeat.Ignition, 0.3f + 0.4f * s, r);
        }

        public void OnLiftoff(double t, FlightVec padPos)
        {
            Hooks++;
            var p = At(); bool r = Red();
            CinematicEvents.HeatHaze(p.x, p.y, p.z, 0.6f, 5f, r);
            CinematicEvents.CameraBeat(CameraBeat.Liftoff, 0.7f, r, 0.5f);   // FlightPresenter already shakes on liftoff
        }

        public void OnMaxQ(double q, double t)
        {
            Hooks++;
            CinematicEvents.CameraBeat(CameraBeat.MaxQ, 0.25f + 0.5f * FlightSinkMath.MaxQ01(q), Red(), 0.5f);
        }

        public void OnStageSeparation(int stage, FlightVec pos, FlightVec vel)
        {
            Hooks++;
            var p = At(); bool r = Red();
            CinematicEvents.StagingFlash(StagingFlashKind.Separation, p.x, p.y, p.z, FlightSinkMath.StageScale01(stage), r);
            CinematicEvents.CameraBeat(CameraBeat.Staging, 0.6f, r, 0.5f);
        }

        public void OnFairingSeparation(double t, FlightVec pos, FlightVec vel)
        {
            Hooks++;
            var p = At(); bool r = Red();
            CinematicEvents.StagingFlash(StagingFlashKind.Separation, p.x, p.y, p.z, 0.45f, r);
            CinematicEvents.CameraBeat(CameraBeat.Staging, 0.3f, r, 0.5f);
        }

        public void OnRecoveryPhase(RecoveryPhase phase, double t) { Hooks++; LastPhase = (int)phase; }

        public void OnTouchdown(double vz, double tilt, FlightSurface surface)
        {
            Hooks++;
            var p = At(); bool r = Red();
            CinematicEvents.CameraBeat(CameraBeat.Touchdown, FlightSinkMath.Touchdown01(vz, tilt), r);
            float ring = FlightSinkMath.TouchdownShockwave01(vz, surface != FlightSurface.Sea);
            if (ring > 0f) CinematicEvents.Shockwave(p.x, p.y, p.z, ring, true, r, VehicleRadiusM);
        }

        public void OnCrash(string kind, FlightVec pos, FlightVec vel, double energy)
        {
            Hooks++; blown = true;
            var p = At(); bool r = Red();
            CinematicEvents.Crash(p.x, p.y, p.z, FlightSinkMath.CrashEnergy01(energy, FullEnergyJoules), VehicleRadiusM, true, r, (float)vel.X, (float)vel.Y, 0f);
        }

        public void OnFailure(string kind, string cause, int engineIndex, FlightVec pos, FlightVec vel, double t)
        {
            Hooks++;
            if (FlightEngineMode.IsEngine(kind)) return;   // OnEngineFailure follows with the bell picture; a breakup is the whole-vehicle one
            blown = true;
            var p = At();
            CinematicEvents.Failure(p.x, p.y, p.z, 1f, Mathf.Max(0.4f, VehicleRadiusM), Red(), (float)vel.X, (float)vel.Y, 0f);
        }

        public void OnEngineFailure(int engineIndex, string mode)
        {
            Hooks++;
            var p = EngineAnchor != null ? EngineAnchor(engineIndex) : At();
            CinematicEvents.EngineFailure(p.x, p.y, p.z, engineIndex, mode == FlightEngineMode.Overpressure, Red());
        }

        public void OnImpact(double speed, FlightVec pos)
        {
            Hooks++;
            if (blown) return;
            blown = true;
            var p = At();
            CinematicEvents.Crash(p.x, p.y, p.z, FlightSinkMath.ImpactEnergy01(speed), VehicleRadiusM, true, Red());
        }
    }
}
