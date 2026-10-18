// Observer hooks for flight VFX and audio. The sim calls these and never reads them back.
// A null sink and any sink leave the step, the event list, and the result identical.
using System;

namespace Kestrel.Core.Flight
{
    /// <summary>Planar position or velocity. Metres, or metres per second. The ascent frame is Earth-centred; recovery uses the pad frame.</summary>
    public readonly struct FlightVec
    {
        public readonly double X, Y;
        public FlightVec(double x, double y) { X = x; Y = y; }
    }

    /// <summary>Where a booster meets the surface. Deck is Haven or the pad. RTLS off the pad is ground. A barge miss is sea.</summary>
    public enum FlightSurface
    {
        Deck = 0,
        Ground = 1,
        Sea = 2,
    }

    public static class FlightSurfaces
    {
        public static FlightSurface Choose(double missM, double deckRadiusM, string mode)
        {
            if (!double.IsNaN(missM) && Math.Abs(missM) <= deckRadiusM) return FlightSurface.Deck;
            if (mode == "rtls") return FlightSurface.Ground;
            return FlightSurface.Sea;
        }
    }

    /// <summary>Engine-failure picture. Overpressure is the chamber rupture. The other engine kinds sputter.</summary>
    public static class FlightEngineMode
    {
        public const string Sputter = "sputter";
        public const string Overpressure = "overpressure";

        public static bool IsEngine(string kind)
            => kind == "engine-out" || kind == "bad-mixture" || kind == "engine-overpressure";

        public static string Of(string kind)
            => kind == "engine-overpressure" ? Overpressure : Sputter;
    }

    /// <summary>
    /// Flight-sim observer. Payloads match the flight-event VFX audit.
    /// Implementations must not write sim state. The ascent calls stage-1 ignition on the first tick.
    /// </summary>
    public interface IFlightSimSink
    {
        /// <summary>Sim ignition, including stage 1 before liftoff. <paramref name="stage"/> is 1-based. <paramref name="t"/> is sim seconds.</summary>
        void OnIgnition(int stage, double t);

        /// <summary>Altitude has passed 1 m. <paramref name="padPos"/> is the pad in the ascent frame.</summary>
        void OnLiftoff(double t, FlightVec padPos);

        /// <summary>Peak dynamic pressure, pascals, and the sim time of that peak. Emitted once, from the ascent, not from a presenter.</summary>
        void OnMaxQ(double q, double t);

        /// <summary>A stage has separated. <paramref name="stage"/> is the 1-based index of the stage that left. Position and velocity are metres and metres per second.</summary>
        void OnStageSeparation(int stage, FlightVec pos, FlightVec vel);

        /// <summary>Fairing jettison. One sim event, after max-Q, once altitude reaches the jettison gate.</summary>
        void OnFairingSeparation(double t, FlightVec pos, FlightVec vel);

        /// <summary>Recovery phase changed. <paramref name="t"/> is recovery sim time.</summary>
        void OnRecoveryPhase(RecoveryPhase phase, double t);

        /// <summary>Touchdown. <paramref name="vz"/> is sink speed (m/s), <paramref name="tilt"/> is degrees from vertical.</summary>
        void OnTouchdown(double vz, double tilt, FlightSurface surface);

        /// <summary>Hard touchdown. <paramref name="kind"/> is the boom kind. <paramref name="energy"/> is kinetic energy, joules.</summary>
        void OnCrash(string kind, FlightVec pos, FlightVec vel, double energy);

        /// <summary>In-flight failure. <paramref name="engineIndex"/> is -1 when the failure is not an engine.</summary>
        void OnFailure(string kind, string cause, int engineIndex, FlightVec pos, FlightVec vel, double t);

        /// <summary>Engine-out, bad mixture, or overpressure. <paramref name="mode"/> is <see cref="FlightEngineMode.Sputter"/> or <see cref="FlightEngineMode.Overpressure"/>.</summary>
        void OnEngineFailure(int engineIndex, string mode);

        /// <summary>Unplanned ground hit. <paramref name="speed"/> is the downward radial speed (m/s).</summary>
        void OnImpact(double speed, FlightVec pos);
    }
}
