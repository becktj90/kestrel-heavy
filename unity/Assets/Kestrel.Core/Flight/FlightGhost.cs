// H3 last-attempt ghost. Session-scoped: relaunch shows where the previous try peaked
// or failed on the trajectory rail. No network. Cleared only when the player leaves the mission.
using System;

namespace Kestrel.Core.Flight
{
    /// <summary>Translucent last-attempt mark for the ascent rail (peer loop: SFS / Juno ghost).</summary>
    public static class FlightGhost
    {
        public const int SampleCap = 48;

        static string missionId;
        static float failAlong = float.NaN;
        static float peakAlong;
        static readonly float[] along = new float[SampleCap];
        static int count;
        static string liveMissionId;
        static TrajectoryPlan livePlan;

        public static bool Has => !string.IsNullOrEmpty(missionId) && count > 0;
        public static string MissionId => missionId;
        public static float FailAlong => failAlong;
        public static float PeakAlong => peakAlong;
        public static int Count => count;

        /// <summary>Remember the live rail each frame so the report can commit it without owning the HUD.</summary>
        public static void RememberLive(string id, TrajectoryPlan plan)
        {
            liveMissionId = id;
            livePlan = plan;
        }

        /// <summary>Commit the remembered live rail as the next attempt's ghost.</summary>
        public static void CommitLive(bool failed)
        {
            if (string.IsNullOrEmpty(liveMissionId) || livePlan == null) return;
            Capture(liveMissionId, livePlan, failed);
        }

        public static float AlongAt(int index)
        {
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
            return along[index];
        }

        public static void Clear()
        {
            missionId = null;
            failAlong = float.NaN;
            peakAlong = 0f;
            count = 0;
        }

        /// <summary>Remember this flight's rail progress for the next attempt on the same mission.</summary>
        public static void Capture(string id, TrajectoryPlan plan, bool failed)
        {
            if (string.IsNullOrEmpty(id) || plan == null) return;
            missionId = id;
            count = 0;
            peakAlong = 0f;
            failAlong = float.NaN;
            float progress = plan.Progress;
            if (progress < 0f) progress = 0f;
            else if (progress > 1f) progress = 1f;
            peakAlong = progress;
            if (failed) failAlong = progress;
            var marks = plan.Marks;
            if (marks != null && marks.Length > 0)
            {
                int n = Math.Min(SampleCap, marks.Length);
                for (int i = 0; i < n; i++)
                {
                    float a = (float)marks[i].Along;
                    if (a < 0f) a = 0f;
                    else if (a > 1f) a = 1f;
                    along[count++] = a;
                    if (marks[i].Passed && a > peakAlong) peakAlong = a;
                }
            }
            if (count == 0)
            {
                along[0] = progress;
                count = 1;
            }
        }

        public static bool TryFor(string id, out float ghostFail, out float ghostPeak)
        {
            ghostFail = float.NaN;
            ghostPeak = 0f;
            if (string.IsNullOrEmpty(id) || missionId != id || count == 0) return false;
            ghostFail = failAlong;
            ghostPeak = peakAlong;
            return true;
        }
    }
}
