// Engine-free entry point for cinematic flight VFX (explosions, fire/smoke; later debris, shockwave, camera).
// Gameplay raises an event with plain numbers; Environment/ExplosionFx (and later areas) subscribe.
// Positions are scene metres, the space the vehicle is drawn in. Nothing here allocates per call:
// events are structs and the delegate list is only touched at subscribe time.
using System;

namespace Kestrel.Kit
{
    public enum CinematicKind { Crash = 0, Failure = 1, EngineFailure = 2 }

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
    }

    public static class CinematicEvents
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

        /// <summary>Test seam: drop every subscriber.</summary>
        public static void ClearSubscribers() { Raised = null; RaisedCount = 0; }
    }
}
