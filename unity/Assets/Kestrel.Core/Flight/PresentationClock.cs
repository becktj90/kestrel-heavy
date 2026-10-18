// Presentation time sits outside the sim step. Slow motion scales how much wall-clock
// time is fed to the accumulator. The physics step is FixedStepAccumulator.Step and is
// never multiplied by the scale. A 0.4x play test that scaled the integrator dt made
// KH-1 fail; this clock is the replacement.
using System;

namespace Kestrel.Core.Flight
{
    /// <summary>
    /// How fast presentation time runs. 1 is realtime. 0.4 feeds 0.4 seconds of sim time per wall second.
    /// The value never becomes a physics dt.
    /// </summary>
    public readonly struct PresentationTimeScale
    {
        public readonly double Scale;

        public PresentationTimeScale(double scale)
        {
            if (double.IsNaN(scale) || scale < 0) scale = 0;
            Scale = scale;
        }

        public static PresentationTimeScale Realtime => new PresentationTimeScale(1);

        /// <summary>Sim seconds to accumulate for this wall-clock slice.</summary>
        public double ToSimSeconds(double wallSeconds)
        {
            if (double.IsNaN(wallSeconds) || wallSeconds <= 0 || Scale == 0) return 0;
            return wallSeconds * Scale;
        }
    }

    /// <summary>
    /// Fixed-step accumulator. Advance adds scaled wall time, then releases whole steps of <see cref="Step"/>.
    /// Time is counted in microseconds so two different frame splits of the same sim time take the same steps.
    /// Backlog is kept: a long frame runs more steps and does not drop sim time.
    /// </summary>
    public struct FixedStepAccumulator
    {
        public const long UnitsPerSecond = 1000000;

        public readonly double Step;
        readonly long stepUnits;
        long residual;

        public FixedStepAccumulator(double stepSeconds)
        {
            if (!(stepSeconds > 0) || double.IsNaN(stepSeconds))
                throw new ArgumentOutOfRangeException(nameof(stepSeconds));
            Step = stepSeconds;
            stepUnits = ToUnits(stepSeconds);
            if (stepUnits < 1) stepUnits = 1;
            residual = 0;
        }

        public static long ToUnits(double seconds)
        {
            if (double.IsNaN(seconds) || seconds <= 0) return 0;
            double units = seconds * UnitsPerSecond;
            if (units >= long.MaxValue) return long.MaxValue;
            return (long)Math.Round(units, MidpointRounding.AwayFromZero);
        }

        /// <summary>How many physics steps to run. Each step uses <see cref="Step"/>, not a scaled dt.</summary>
        public int Advance(double wallSeconds, PresentationTimeScale scale)
        {
            residual += ToUnits(scale.ToSimSeconds(wallSeconds));
            int n = 0;
            while (residual >= stepUnits)
            {
                residual -= stepUnits;
                n++;
            }
            return n;
        }
    }
}
