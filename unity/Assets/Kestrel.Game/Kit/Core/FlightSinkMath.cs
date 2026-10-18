// Engine-free number mapping for CinematicFlightSink: turns sim units (joules, m/s, Pa) into the 0..1 intensities
// CinematicEvents take, and holds the "a flight sink is bound" flag the legacy hosts check so nothing fires twice.
using System;

namespace Kestrel.Kit
{
    public static partial class CinematicEvents
    {
        static int sinkRefs;
        /// <summary>True while a CinematicFlightSink is attached to a live sim. FailureHost and RecoveryShow then skip their own raises.</summary>
        public static bool FlightSinkActive => sinkRefs > 0;
        public static void SinkAttached() { sinkRefs++; }
        public static void SinkDetached() { if (sinkRefs > 0) sinkRefs--; }
        public static void ResetSinkState() { sinkRefs = 0; }
    }

    public static class FlightSinkMath
    {
        /// <summary>Kinetic energy of a full-vehicle crash that reads as energy 1 (20 t at 45 m/s).</summary>
        public const double FullEnergyJoules = 2.0e7;
        public const double MaxQRefPa = 40000.0;
        public const double HardTouchdownMps = 3.0;

        static float C01(double v) => double.IsNaN(v) ? 0f : v < 0 ? 0f : v > 1 ? 1f : (float)v;

        public static float Energy01(double joules, double fullJoules = FullEnergyJoules)
            => C01(joules / Math.Max(1.0, fullJoules));

        public static float CrashEnergy01(double joules, double fullJoules = FullEnergyJoules)
            => Math.Max(0.15f, Energy01(joules, fullJoules));

        public static float ImpactEnergy01(double speedMps, double fullSpeedMps = 120.0)
            => Math.Max(0.15f, C01(speedMps * speedMps / (fullSpeedMps * fullSpeedMps)));

        public static float MaxQ01(double qPa) => C01(qPa / MaxQRefPa);

        public static float Touchdown01(double vz, double tiltDeg)
            => C01(Math.Abs(vz) / 8.0 + Math.Abs(tiltDeg) / 90.0 * 0.4);

        public static bool HardTouchdown(double vz) => Math.Abs(vz) >= HardTouchdownMps;

        /// <summary>A shockwave rides a touchdown only on solid ground and only when it is hard.</summary>
        public static float TouchdownShockwave01(double vz, bool solid)
            => solid && HardTouchdown(vz) ? C01(0.1 + Math.Abs(vz) / 20.0) : 0f;

        public static float StageScale01(int stage) => stage <= 1 ? 1f : stage == 2 ? 0.7f : 0.5f;
    }
}
