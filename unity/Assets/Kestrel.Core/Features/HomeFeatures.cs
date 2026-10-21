// Starter Home (PHASED_DESIGNER_PLAN.md PR 6, feat(home-simple)): one primary CTA (CONTINUE with a save, else
// LAUNCH KH-n · Name), MISSIONS and the Settings gear. Design first, the Codex tile, Thrum, the credits chip and
// the stats card keep their code behind the home.* flags of FeatureFlags (feat(flags)). This is only a typed view
// of those five flags: the defaults live in data/features.json and FeatureFlags, never here.
namespace Kestrel.Core.Features
{
    public static class HomeFeatures
    {
        public const string DesignFirst = FeatureKeys.HomeDesignFirst;
        public const string CodexTile = FeatureKeys.HomeCodexTile;
        public const string Thrum = FeatureKeys.HomeThrum;
        public const string CreditsChip = FeatureKeys.HomeCreditsChip;
        public const string StatsCard = FeatureKeys.HomeStatsCard;

        public static readonly string[] Names = { DesignFirst, CodexTile, Thrum, CreditsChip, StatsCard };

        public static bool On(string key) => FeatureFlags.Active != null && FeatureFlags.Active.IsOn(key);

        public static HomeShow Current() => Read(FeatureFlags.Active);

        public static HomeShow Read(FeatureFlags flags)
        {
            if (flags == null) return default;
            return new HomeShow
            {
                DesignFirst = flags.IsOn(DesignFirst),
                Codex = flags.IsOn(CodexTile),
                Thrum = flags.IsOn(Thrum),
                Credits = flags.IsOn(CreditsChip),
                Stats = flags.IsOn(StatsCard),
            };
        }

        /// <summary>A FeatureFlags with the home.* flags set to <paramref name="show"/> (tests and previews).</summary>
        public static FeatureFlags With(HomeShow show)
        {
            var f = new FeatureFlags();
            f.SetOverride(DesignFirst, show.DesignFirst);
            f.SetOverride(CodexTile, show.Codex);
            f.SetOverride(Thrum, show.Thrum);
            f.SetOverride(CreditsChip, show.Credits);
            f.SetOverride(StatsCard, show.Stats);
            return f;
        }
    }

    /// <summary>Which Home extras are on. <see cref="Full"/> is the pre-Starter Home; <see cref="None"/> hides all five.</summary>
    public struct HomeShow
    {
        public bool DesignFirst, Codex, Thrum, Credits, Stats;
        public static HomeShow None => default;
        public static HomeShow Full => new HomeShow { DesignFirst = true, Codex = true, Thrum = true, Credits = true, Stats = true };
        public override string ToString() =>
            (DesignFirst ? "D" : "-") + (Codex ? "C" : "-") + (Thrum ? "T" : "-") + (Credits ? "$" : "-") + (Stats ? "S" : "-");
    }
}
