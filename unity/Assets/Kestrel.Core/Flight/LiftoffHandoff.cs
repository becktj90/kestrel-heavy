// The player's countdown. The webcast opens on the last few seconds, then this clock
// walks to liftoff and keeps asking for the ascent until the sim is actually running.
// A throw on the first liftoff frame used to clear the count and never try again, so the
// HUD sat on T+0:00 / COUNT with the rocket still on the pad.
using System;
using Kestrel.Core.Timeline;

namespace Kestrel.Core.Flight
{
    public sealed class LiftoffHandoff
    {
        /// <summary>Game seconds before liftoff. The full count is longer; the player opens here.</summary>
        public const double OpenAt = -12;
        /// <summary>Same hitch cap as the ascent integrator. One slow frame must not skip the count.</summary>
        public const double MaxStep = 0.1;

        public LaunchSequence Sequence { get; private set; }
        public bool Counting { get; private set; }
        public bool Ascending { get; private set; }
        public int StartAttempts { get; private set; }
        public Exception LastError { get; private set; }

        public void Reset()
        {
            Sequence = null;
            Counting = false;
            Ascending = false;
            StartAttempts = 0;
            LastError = null;
        }

        /// <summary>Open the count. Does not start the ascent; the caller ticks.</summary>
        public void Enter(LaunchSequence sequence)
        {
            Sequence = sequence;
            Ascending = false;
            StartAttempts = 0;
            LastError = null;
            if (sequence != null && !sequence.Liftoff)
            {
                if (sequence.Time < OpenAt) sequence.Step(OpenAt - sequence.Time);
                Counting = !sequence.Liftoff;
            }
            else Counting = false;
        }

        /// <summary>
        /// One frame. <paramref name="startAscent"/> returns true once the sim is running.
        /// A throw or a false return leaves the handoff short of ascent so the next frame tries again.
        /// </summary>
        public void Tick(double dt, Func<bool> startAscent)
        {
            if (Ascending) return;
            if (Counting)
            {
                if (Sequence == null) Counting = false;
                else
                {
                    Sequence.Step(Clamp(dt));
                    if (!Sequence.Liftoff) return;
                    Counting = false;
                }
            }
            TryStart(startAscent);
        }

        /// <summary>Skip the rest of the count and start the ascent.</summary>
        public void Skip(Func<bool> startAscent)
        {
            if (Ascending) return;
            if (Sequence != null && !Sequence.Liftoff) Sequence.Step(1e6);
            Counting = false;
            TryStart(startAscent);
        }

        /// <summary>Scaled time, or unscaled time when the scale is zero but frames are still arriving.</summary>
        public static double FrameDt(double delta, double unscaled)
        {
            double dt = delta;
            if (dt <= 0 && unscaled > 0) dt = unscaled;
            return Clamp(dt);
        }

        public static double Clamp(double dt)
        {
            if (dt < 0 || double.IsNaN(dt)) return 0;
            if (dt > MaxStep) return MaxStep;
            return dt;
        }

        void TryStart(Func<bool> startAscent)
        {
            if (startAscent == null) return;
            StartAttempts++;
            try
            {
                if (startAscent())
                {
                    Ascending = true;
                    LastError = null;
                }
            }
            catch (Exception e)
            {
                LastError = e;
            }
        }
    }
}
