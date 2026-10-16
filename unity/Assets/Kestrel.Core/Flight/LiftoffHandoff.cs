// The player's countdown. The webcast opens on the last few seconds, then this clock
// walks to liftoff and keeps asking for the ascent until the sim is actually running.
// A throw on the first liftoff frame used to clear the count and never try again, so the
// HUD sat on T+0:00 / COUNT with the rocket still on the pad.
// Audio, a clip load, and the guide voice gate are not part of this clock.
using System;
using System.Globalization;
using Kestrel.Core.Timeline;

namespace Kestrel.Core.Flight
{
    /// <summary>What the device may be doing while the count runs. None of these hold liftoff.</summary>
    public sealed class LiftoffBlockers
    {
        public bool AudioDeviceReady = true;
        public bool ClipLoadFinished = true;
        public bool VoiceGateOpen = true;
        public bool GuideLineHolding;
    }

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
        /// <summary>boot, counting, committed (T+0, sim not running), ascending.</summary>
        public string Phase { get; private set; } = "boot";
        public event Action<string> Transitioned;

        string lastLine;
        bool releasedHold;

        public void Reset()
        {
            Sequence = null;
            Counting = false;
            Ascending = false;
            StartAttempts = 0;
            LastError = null;
            releasedHold = false;
            Move("boot", null);
        }

        /// <summary>Open the count. Does not start the ascent; the caller ticks.</summary>
        public void Enter(LaunchSequence sequence)
        {
            Sequence = sequence;
            Ascending = false;
            StartAttempts = 0;
            LastError = null;
            releasedHold = false;
            if (sequence != null && !sequence.Liftoff)
            {
                if (sequence.Time < OpenAt) sequence.Step(OpenAt - sequence.Time);
                Counting = !sequence.Liftoff;
            }
            else Counting = false;
            if (Counting) Move("counting", null);
            else if (sequence != null && sequence.Liftoff) Move("committed", null);
            else Move("boot", null);
        }

        /// <summary>
        /// One frame. <paramref name="startAscent"/> returns true once the sim is running.
        /// A throw or a false return leaves the handoff short of ascent so the next frame tries again.
        /// <paramref name="blockers"/> is recorded by the caller and does not delay this tick.
        /// </summary>
        public void Tick(double dt, Func<bool> startAscent, LiftoffBlockers blockers = null)
        {
            if (Ascending) return;
            if (WaitsOn(blockers)) return;
            if (Counting)
            {
                if (Sequence == null) Counting = false;
                else
                {
                    ReleaseHold();
                    Sequence.Step(Clamp(dt));
                    if (!Sequence.Liftoff) return;
                    Counting = false;
                    Move("committed", null);
                }
            }
            TryStart(startAscent);
        }

        /// <summary>Skip the rest of the count and start the ascent. Works after T+0 as well as during the count.</summary>
        public void Skip(Func<bool> startAscent, LiftoffBlockers blockers = null)
        {
            if (Ascending) return;
            if (WaitsOn(blockers)) return;
            if (Sequence != null)
            {
                ReleaseHold();
                if (!Sequence.Liftoff) Sequence.Step(1e6);
            }
            Counting = false;
            Move("committed", "skip");
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

        /// <summary>Liftoff does not wait on a missing audio device, a clip that never finishes, or a closed guide voice gate.</summary>
        public static bool WaitsOn(LiftoffBlockers blockers)
        {
            if (blockers == null) return false;
            _ = blockers.AudioDeviceReady;
            _ = blockers.ClipLoadFinished;
            _ = blockers.VoiceGateOpen;
            _ = blockers.GuideLineHolding;
            return false;
        }

        void ReleaseHold()
        {
            if (Sequence == null || !Sequence.Held) return;
            Sequence.Release();
            if (releasedHold) return;
            releasedHold = true;
            Emit(Phase + " release-hold t=" + Clock());
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
                    Move("ascending", null);
                }
                else if (StartAttempts == 1)
                    Move("committed", "retry start returned false");
            }
            catch (Exception e)
            {
                LastError = e;
                if (StartAttempts == 1)
                    Move("committed", "retry " + e.GetType().Name + ": " + e.Message);
            }
        }

        void Move(string next, string detail)
        {
            string line = Phase + "->" + next + " t=" + Clock();
            if (!string.IsNullOrEmpty(detail)) line += " " + detail;
            Phase = next;
            if (line == lastLine) return;
            Emit(line);
        }

        void Emit(string line)
        {
            lastLine = line;
            Transitioned?.Invoke(line);
        }

        string Clock()
        {
            double t = Sequence != null ? Sequence.Time : 0;
            if (double.IsNaN(t) || double.IsInfinity(t)) return "0";
            return t.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
