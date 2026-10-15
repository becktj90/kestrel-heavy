// Thumb steering already has an expo curve (SteerTune). This is the time response on top of that
// axis: a full shove takes a moment to arrive, and letting go comes back quicker than it went out.
// It does not change the pixel-to-axis curve the golden vectors lock.
using System;

namespace Kestrel.Core.Feel
{
    public static class StickResponse
    {
        /// <summary>Approach rate while the command is growing, per second.</summary>
        public const double Attack = 6.5;
        /// <summary>Approach rate while the command is shrinking, per second. Faster than attack.</summary>
        public const double Release = 11.0;
        /// <summary>Hard cap on axis change per second, so a tap cannot snap the ship.</summary>
        public const double MaxRate = 5.5;

        /// <summary>One step of the stick toward <paramref name="command"/> in [-1, 1].</summary>
        public static double Step(double current, double command, double dt)
        {
            if (double.IsNaN(current) || double.IsInfinity(current)) current = 0;
            if (double.IsNaN(command) || double.IsInfinity(command)) command = 0;
            if (command > 1) command = 1;
            else if (command < -1) command = -1;
            if (double.IsNaN(dt) || dt < 0) dt = 0;
            if (dt > 0.1) dt = 0.1;
            if (dt == 0) return current;
            double err = command - current;
            bool growing = Math.Abs(command) + 1e-9 >= Math.Abs(current);
            double rate = growing ? Attack : Release;
            double step = err * (1 - Math.Exp(-rate * dt));
            double cap = MaxRate * dt;
            if (step > cap) step = cap;
            else if (step < -cap) step = -cap;
            double next = current + step;
            if (next > 1) return 1;
            if (next < -1) return -1;
            return next;
        }
    }
}
