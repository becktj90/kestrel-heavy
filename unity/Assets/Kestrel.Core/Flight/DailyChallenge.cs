// Daily challenge (ENGAGEMENT_SPEC §3): local calendar seed, same for everyone that day.
// No FOMO, no expiry rewards, no network — only a deterministic pick from unlocked campaign missions.
using System;
using System.Globalization;

namespace Kestrel.Core.Flight
{
    public readonly struct DailyPick
    {
        public readonly string DayKey;
        public readonly ulong Seed;
        public readonly string MissionId;
        public readonly int MissionIndex;
        public readonly string Title;

        public DailyPick(string dayKey, ulong seed, string missionId, int missionIndex, string title)
        {
            DayKey = dayKey;
            Seed = seed;
            MissionId = missionId;
            MissionIndex = missionIndex;
            Title = title;
        }
    }

    public static class DailyChallenge
    {
        public const string PrefsKey = "kh.daily.day";
        public const string ClearedKey = "kh.daily.cleared";

        static string pendingMissionId;
        static ulong? pendingSeed;

        /// <summary>Arm the board's daily pick so Launch/Design use today's seed, not the campaign default.</summary>
        public static void Arm(in DailyPick pick)
        {
            pendingMissionId = pick.MissionId;
            pendingSeed = string.IsNullOrEmpty(pick.MissionId) ? (ulong?)null : pick.Seed;
        }

        public static void ClearArm()
        {
            pendingMissionId = null;
            pendingSeed = null;
        }

        /// <summary>Pending daily seed for this mission, if any. Does not clear.</summary>
        public static ulong? PeekSeed(string missionId)
        {
            if (string.IsNullOrEmpty(missionId) || pendingMissionId != missionId || !pendingSeed.HasValue)
                return null;
            return pendingSeed;
        }

        /// <summary>Consume a pending daily seed when the mission matches; otherwise leave the campaign default.</summary>
        public static ulong? TakeSeed(string missionId)
        {
            ulong? seed = PeekSeed(missionId);
            if (seed.HasValue) ClearArm();
            return seed;
        }

        /// <summary>UTC calendar day key (yyyy-MM-dd). Everyone on Earth shares the same daily.</summary>
        public static string DayKey(DateTime utc)
            => utc.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public static ulong SeedFor(DateTime utc)
        {
            string key = DayKey(utc);
            unchecked
            {
                ulong h = 14695981039346656037UL;
                for (int i = 0; i < key.Length; i++)
                {
                    h ^= key[i];
                    h *= 1099511628211UL;
                }
                // Mix with a fixed tag so the seed is never a bare date int.
                h ^= 0x4B48UL; // "KH"
                h *= 1099511628211UL;
                // Career saves the seed as a JSON number, exact only through 2^53-1.
                // A wider hash (2026-11-30 is one) throws in RecordAttempt and drops the report.
                return FlightSeed.Fit(h);
            }
        }

        /// <summary>
        /// Pick one unlocked campaign mission for the day. Unlocked = index ≤ cleared+1 (the next open slot).
        /// When nothing is unlocked yet, KH-1 is the daily.
        /// </summary>
        public static DailyPick Pick(Campaign campaign, int cleared, DateTime utc)
        {
            string day = DayKey(utc);
            ulong seed = SeedFor(utc);
            if (campaign == null || campaign.Missions == null || campaign.Missions.Count == 0)
                return new DailyPick(day, seed, null, 0, "Daily");

            int openThrough = cleared < 0 ? 1 : cleared + 1;
            int pool = 0;
            for (int i = 0; i < campaign.Missions.Count; i++)
            {
                var m = campaign.Missions[i];
                if (m != null && m.Index >= 1 && m.Index <= openThrough) pool++;
            }
            if (pool <= 0)
            {
                var first = campaign.Mission(1) ?? campaign.Missions[0];
                return Of(day, seed, first);
            }

            int pick = (int)(seed % (ulong)pool);
            int seen = 0;
            for (int i = 0; i < campaign.Missions.Count; i++)
            {
                var m = campaign.Missions[i];
                if (m == null || m.Index < 1 || m.Index > openThrough) continue;
                if (seen == pick) return Of(day, seed, m);
                seen++;
            }
            return Of(day, seed, campaign.Mission(1) ?? campaign.Missions[0]);
        }

        static DailyPick Of(string day, ulong seed, CampaignMission m)
        {
            if (m == null) return new DailyPick(day, seed, null, 0, "Daily");
            string title = string.IsNullOrEmpty(m.Name) ? MissionContext.LabelFor(m.Index) : MissionContext.LabelFor(m.Index) + "  " + m.Name.Trim();
            return new DailyPick(day, seed, m.Id, m.Index, title);
        }
    }
}
