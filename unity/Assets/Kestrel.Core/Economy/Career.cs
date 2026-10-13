// Career bank (§4). Reward minus cost is credited on a pass, plus the reuse refund.
// A miss still pays the build cost and pays no reward. Unlocks arrive in mission order.
// No timers, no energy, no ads.
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;

namespace Kestrel.Core.Economy
{
    /// <summary>ENGAGEMENT_SPEC no-dark-patterns. These stay off; the campaign file must say the same.</summary>
    public static class DarkPatterns
    {
        public const bool Timers = false;
        public const bool Energy = false;
        public const bool Ads = false;
    }

    public sealed class CareerResult
    {
        public bool accepted, passed;
        public long rewardMinor, costMinor, refundMinor, deltaMinor, bankMinor;
        public int cleared;
    }

    public sealed class Career
    {
        public readonly Campaign Campaign;
        public long BankMinor;
        /// <summary>Highest mission index passed in order. 0 means none yet. Mission 1 is open at the start.</summary>
        public int Cleared;
        public readonly HashSet<string> Unlocks = new HashSet<string>();
        public readonly Dictionary<string, int> ArticleFlights = new Dictionary<string, int>();
        readonly long minorPerKc;

        Career(Campaign campaign, long minorPerKc)
        {
            Campaign = campaign;
            this.minorPerKc = minorPerKc;
        }

        public static Career Start(Campaign campaign, long minorPerKc)
        {
            if (campaign == null) throw new ArgumentNullException(nameof(campaign));
            if (campaign.Timers || campaign.Energy || campaign.Ads)
                throw new InvalidOperationException("Campaign dark patterns are off.");
            var career = new Career(campaign, minorPerKc)
            {
                BankMinor = KcToMinor(campaign.StartingBankKc, minorPerKc),
            };
            if (campaign.StarterUnlocks != null)
                foreach (var id in campaign.StarterUnlocks) career.Unlocks.Add(id);
            return career;
        }

        public static long KcToMinor(double kc, long minorPerKc)
            => (long)Math.Round(kc * minorPerKc, MidpointRounding.AwayFromZero);

        public bool MissionOpen(int index)
            => index >= 1 && index <= Campaign.Missions.Count && index == Cleared + 1;

        public bool HasUnlock(string id) => id != null && Unlocks.Contains(id);

        /// <summary>
        /// Settle one launch of the next open mission. Recovered hardware refunds
        /// 70% minus refurbishment (10% + 5% × prior flights of that article).
        /// </summary>
        public CareerResult Settle(int missionIndex, bool pass, long costMinor, long recoveredHardwareMinor, string articleKey)
        {
            var result = new CareerResult { costMinor = costMinor < 0 ? 0 : costMinor };
            if (!MissionOpen(missionIndex)) return result;
            var mission = Campaign.Mission(missionIndex);
            result.accepted = true;
            result.passed = pass;
            int prior = 0;
            if (!string.IsNullOrEmpty(articleKey)) ArticleFlights.TryGetValue(articleKey, out prior);
            if (pass)
            {
                result.rewardMinor = KcToMinor(mission.RewardKc, minorPerKc);
                result.refundMinor = CostModel.ReuseRefundMinor(recoveredHardwareMinor, prior);
                if (!string.IsNullOrEmpty(articleKey) && recoveredHardwareMinor > 0)
                    ArticleFlights[articleKey] = prior + 1;
                Cleared = missionIndex;
                if (mission.Unlocks != null)
                    foreach (var id in mission.Unlocks) Unlocks.Add(id);
            }
            result.deltaMinor = result.rewardMinor - result.costMinor + result.refundMinor;
            BankMinor += result.deltaMinor;
            result.bankMinor = BankMinor;
            result.cleared = Cleared;
            return result;
        }
    }
}
