// The Kid / Builder / Engineer switch on the Starter screen (docs/DESIGNER_TIERS_SPEC.md). Kid is ExperienceTier.Starter
// and is always offered. Builder and Engineer are offered only when their flag is on (designer.tierBuilder,
// designer.tierEngineer; off in data/features.json and off when the file is missing), so the switch stays hidden until a
// later PR ships their controls. The tier is stored in kh.settings.tier through ExperienceSettings (legacy engineer prefs
// read as Engineer). A stored tier whose chip is not offered shows Kid and is left in the prefs untouched.
using System;
using System.Collections.Generic;
using Kestrel.Core.Features;

namespace Kestrel.Core.Designer
{
    public static class DesignerTiers
    {
        public static readonly IReadOnlyList<ExperienceTier> Order = new[] { ExperienceTier.Starter, ExperienceTier.Builder, ExperienceTier.Engineer };

        /// <summary>The flag that offers a tier's chip; null for Kid (always offered).</summary>
        public static string FlagFor(ExperienceTier t)
        {
            switch (t)
            {
                case ExperienceTier.Builder: return FeatureKeys.DesignerTierBuilder;
                case ExperienceTier.Engineer: return FeatureKeys.DesignerTierEngineer;
                default: return null;
            }
        }

        public static bool Offered(FeatureFlags flags, ExperienceTier t)
        {
            string key = FlagFor(t);
            return key == null || (flags != null && flags.IsOn(key));
        }

        /// <summary>The chips on the switch, in order. Always starts with Kid.</summary>
        public static List<ExperienceTier> Shown(FeatureFlags flags)
        {
            var o = new List<ExperienceTier>();
            foreach (var t in Order) if (Offered(flags, t)) o.Add(t);
            return o;
        }

        /// <summary>The switch is drawn only when it has a choice to offer.</summary>
        public static bool ShowSwitch(FeatureFlags flags) => Shown(flags).Count > 1;

        /// <summary>The tier the screen shows: the stored one when its chip is offered, else Kid. Read-only on the prefs.</summary>
        public static ExperienceTier Resolve(FeatureFlags flags, Func<string, bool> has, Func<string, int> read)
        {
            var stored = ExperienceSettings.Tier(has, read);
            return Offered(flags, stored) ? stored : ExperienceTier.Starter;
        }

        /// <summary>A tap on a chip. Stores the tier (ExperienceSettings.SetTier) and mirrors it on the flags. False, and
        /// nothing written, when the chip is not offered or the tier is already the stored one.</summary>
        public static bool Choose(FeatureFlags flags, ExperienceTier t, Func<string, bool> has, Func<string, int> read, Action<string, int> write)
        {
            if (!Offered(flags, t) || write == null) return false;
            if (ExperienceSettings.Tier(has, read) == t && Stored(has, read) == (int)t) return false;
            ExperienceSettings.SetTier(t, write);
            if (flags != null) flags.Tier = t;
            return true;
        }

        static int Stored(Func<string, bool> has, Func<string, int> read)
        {
            if (read == null || (has != null && !has(ExperienceSettings.TierPref))) return -1;
            return read(ExperienceSettings.TierPref);
        }
    }
}
