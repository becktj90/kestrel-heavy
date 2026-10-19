// Engine-free entry point for cinematic flight VFX (explosions, fire/smoke; later debris, shockwave, camera).
// Gameplay raises an event with plain numbers; Environment/ExplosionFx (and later areas) subscribe.
// Positions are scene metres, the space the vehicle is drawn in. Nothing here allocates per call:
// events are structs and the delegate list is only touched at subscribe time.
using System;

namespace Kestrel.Kit
{
    public enum CinematicKind { Crash = 0, Failure = 1, EngineFailure = 2, StageSeparation = 3, FairingSeparation = 4, DebrisBurst = 5, DebrisClear = 6 }

    public struct CinematicEvent
    {
        public CinematicKind Kind;
        public float X, Y, Z;             // world position of the blast centre
        public float VelX, VelY, VelZ;    // vehicle velocity at the event (m/s); smoke and embers inherit a share
        public float Energy01;            // 0 small .. 1 full vehicle
        public float Radius;              // vehicle radius at the blast (m)
        public int EngineIndex;           // EngineFailure only, else -1
        public bool Overpressure;         // engine failure that is a fireball, not a sputter
        public bool Grounded;             // touching deck or ground: smoke and sparks stay low, fire spreads
        public bool Reduce;               // reduce-motion / reduce-flashing at the moment of the event
        // Separation payload (StageSeparation / FairingSeparation). Spin is derived: omega = LeverM x ImpulseNs / I.
        public int Stage;                 // stage index
        public float MassKg, LengthM;     // body mass and length (a fairing event describes ONE half; two are spawned)
        public float ImpulseNs;           // separation impulse magnitude (N s), pushing the body away from the stack
        public float LeverM;              // distance of the impulse line from the centre of mass (m)
        public float FrameAccel;          // m/s^2 the upper stack accelerates away from the body (0 for a free frame)
        public int Seed;                  // deterministic variation
    }

    public static partial class CinematicEvents
    {
        public static event Action<CinematicEvent> Raised;
        /// <summary>Total events raised since start; tests and debug overlays read it.</summary>
        public static int RaisedCount { get; private set; }

        public static void Raise(in CinematicEvent e)
        {
            RaisedCount++;
            var h = Raised;
            if (h != null) h(e);
        }

        /// <summary>Hard touchdown or ground impact (Gameplay: OnCrash / OnImpact).</summary>
        public static void Crash(float x, float y, float z, float energy01, float radius, bool grounded, bool reduce, float vx = 0f, float vy = 0f, float vz = 0f)
        {
            Raise(new CinematicEvent { Kind = CinematicKind.Crash, X = x, Y = y, Z = z, VelX = vx, VelY = vy, VelZ = vz, Energy01 = energy01, Radius = radius, EngineIndex = -1, Grounded = grounded, Reduce = reduce });
        }

        /// <summary>In-flight breakup / RUD (Gameplay: OnFailure).</summary>
        public static void Failure(float x, float y, float z, float energy01, float radius, bool reduce, float vx = 0f, float vy = 0f, float vz = 0f)
        {
            Raise(new CinematicEvent { Kind = CinematicKind.Failure, X = x, Y = y, Z = z, VelX = vx, VelY = vy, VelZ = vz, Energy01 = energy01, Radius = radius, EngineIndex = -1, Reduce = reduce });
        }

        /// <summary>One engine lost: a sputter or an overpressure fireball at the bell (Gameplay: OnEngineFailure).</summary>
        public static void EngineFailure(float x, float y, float z, int engineIndex, bool overpressure, bool reduce, float vx = 0f, float vy = 0f, float vz = 0f)
        {
            Raise(new CinematicEvent { Kind = CinematicKind.EngineFailure, X = x, Y = y, Z = z, VelX = vx, VelY = vy, VelZ = vz, Energy01 = overpressure ? 0.6f : 0.3f, Radius = 1f, EngineIndex = engineIndex, Overpressure = overpressure, Reduce = reduce });
        }

        /// <summary>
        /// Spent stage leaves the stack (Gameplay: OnStageSeparation). Position and velocity are in the frame the
        /// debris is drawn in; the stage tumbles from the impulse, it is not given a spin rate.
        /// </summary>
        public static void StageSeparation(float x, float y, float z, float vx, float vy, float vz, int stage, float massKg, float lengthM, float radiusM,
            float impulseNs, float leverM, float frameAccel, bool reduce, int seed = 1)
        {
            Raise(new CinematicEvent { Kind = CinematicKind.StageSeparation, X = x, Y = y, Z = z, VelX = vx, VelY = vy, VelZ = vz, Stage = stage, MassKg = massKg, LengthM = lengthM, Radius = radiusM,
                ImpulseNs = impulseNs, LeverM = leverM, FrameAccel = frameAccel, EngineIndex = -1, Reduce = reduce, Seed = seed });
        }

        /// <summary>Fairing jettison (Gameplay: OnFairingSeparation). MassKg/length/radius describe one half; the halves leave along +X and -X.</summary>
        public static void FairingSeparation(float x, float y, float z, float vx, float vy, float vz, float halfMassKg, float lengthM, float radiusM,
            float impulseNs, float leverM, float frameAccel, bool reduce, int seed = 1)
        {
            Raise(new CinematicEvent { Kind = CinematicKind.FairingSeparation, X = x, Y = y, Z = z, VelX = vx, VelY = vy, VelZ = vz, MassKg = halfMassKg, LengthM = lengthM, Radius = radiusM,
                ImpulseNs = impulseNs, LeverM = leverM, FrameAccel = frameAccel, EngineIndex = -1, Reduce = reduce, Seed = seed });
        }

        /// <summary>Hot chunks and sparks thrown from a point (Gameplay: OnCrash / OnFailure raise this too through ExplosionFx's own binding).</summary>
        public static void DebrisBurst(float x, float y, float z, float vx, float vy, float vz, float energy01, float radiusM, bool grounded, bool reduce, int seed = 1)
        {
            Raise(new CinematicEvent { Kind = CinematicKind.DebrisBurst, X = x, Y = y, Z = z, VelX = vx, VelY = vy, VelZ = vz, Energy01 = energy01, Radius = radiusM, Grounded = grounded, EngineIndex = -1, Reduce = reduce, Seed = seed });
        }

        /// <summary>Flight reset or retry: drop all live debris.</summary>
        public static void DebrisClear() => Raise(new CinematicEvent { Kind = CinematicKind.DebrisClear, EngineIndex = -1 });

        /// <summary>Deck and ground the debris collides with. Gameplay/Slice sets it once the pad is built; defaults to a ground plane at y = 0 and no deck.</summary>
        public static DebrisSurfaces Surfaces;

        /// <summary>Handle of the rigid body the last separation event created (written by the subscriber, read right after Raise). A fairing event sets both: left half, right half.</summary>
        public static int LastDebrisHandle, LastDebrisHandleB;

        /// <summary>Test seam: drop every subscriber.</summary>
        public static void ClearSubscribers() { Raised = null; RaisedCount = 0; LastDebrisHandle = 0; LastDebrisHandleB = 0; Surfaces = default; }
    }
}
