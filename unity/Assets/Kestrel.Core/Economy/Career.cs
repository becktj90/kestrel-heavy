// Career bank (§4). Reward minus cost is credited on a pass, plus the reuse refund.
// A miss still pays the build cost and pays no reward, but never below the range-credit floor
// (DECISIONS #87, data/campaign.json rangeFloorKc). Unlocks arrive in mission order.
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
        /// <summary>What the range covered so the bank stays at the campaign floor (DECISIONS #87). Included in deltaMinor.</summary>
        public long rangeCreditMinor;
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
            long settled = result.rewardMinor - result.costMinor + result.refundMinor;
            // Range credit (DECISIONS #87): the range covers whatever would take the bank below the floor, so the
            // next Starter default is always affordable. Free and automatic: no timer, nothing to buy or wait for.
            long floor = FloorMinor;
            long after = BankMinor + settled;
            if (floor > 0 && after < floor) result.rangeCreditMinor = floor - after;
            result.deltaMinor = settled + result.rangeCreditMinor;
            BankMinor += result.deltaMinor;
            result.bankMinor = BankMinor;
            result.cleared = Cleared;
            return result;
        }

        /// <summary>The range-credit floor in minor units (campaign rangeFloorKc). 0 when the campaign has none.</summary>
        public long FloorMinor => Campaign.RangeFloorKc > 0 ? KcToMinor(Campaign.RangeFloorKc, minorPerKc) : 0;

        /// <summary>
        /// Rescale a bank saved under an older economy (data/campaign.json bankMigration): multiply by
        /// (new start + new rewards of the cleared missions) / (old start + old rewards of those missions),
        /// round to whole minor units, then lift it to the range floor. Returns the bank unchanged when the
        /// campaign has no migration or the old table is unusable.
        /// </summary>
        public static long MigrateBankMinor(Campaign campaign, long bankMinor, int cleared, long minorPerKc)
        {
            if (campaign == null || campaign.Migration == null) return bankMinor;
            double factor = MigrationFactor(campaign, cleared);
            if (!(factor > 0) || double.IsInfinity(factor)) return bankMinor;
            long scaled = (long)Math.Round(bankMinor * factor, MidpointRounding.AwayFromZero);
            long floor = campaign.RangeFloorKc > 0 ? KcToMinor(campaign.RangeFloorKc, minorPerKc) : 0;
            return scaled < floor ? floor : scaled;
        }

        /// <summary>New paid-in Kc over old paid-in Kc for a career that cleared <paramref name="cleared"/> missions. NaN when unusable.</summary>
        public static double MigrationFactor(Campaign campaign, int cleared)
        {
            var mig = campaign != null ? campaign.Migration : null;
            if (mig == null || !(mig.StartingBankKc > 0) || !(campaign.StartingBankKc > 0)) return double.NaN;
            int n = cleared < 0 ? 0 : cleared;
            if (n > campaign.Missions.Count) n = campaign.Missions.Count;
            if (n > mig.RewardKc.Count) n = mig.RewardKc.Count;
            double oldPaid = mig.StartingBankKc, newPaid = campaign.StartingBankKc;
            for (int i = 1; i <= n; i++)
            {
                var mission = campaign.Mission(i);
                if (mission == null) return double.NaN;
                oldPaid += mig.RewardKc[i - 1];
                newPaid += mission.RewardKc;
            }
            return newPaid / oldPaid;
        }
    }
}
