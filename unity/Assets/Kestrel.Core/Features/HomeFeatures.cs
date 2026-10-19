// Home flags for the Phase 1 "Starter" Home (kh-plan PHASED_DESIGNER_PLAN.md, PR 6 feat(home-simple)).
// Starter Home is one primary CTA (CONTINUE with a save, else LAUNCH KH-n · Name), MISSIONS and the Settings gear.
// Design first, the Codex tile, Thrum, the credits chip and the stats card stay in the code behind these flags.
//
// feat(flags) (FeatureFlags + data/features.json, Gameplay Dev) has not landed. Until it does, Source is null and
// every home.* flag reads its Starter default (off). When it lands, set
//     HomeFeatures.Source = name => FeatureFlags.On(name);
// and nothing else changes: the names below are the plan's names.
using System;

namespace Kestrel.Core.Features
{
    public static class HomeFeatures
    {
        public const string DesignFirst = "home.designFirst";
        public const string CodexTile = "home.codexTile";
        public const string Thrum = "home.thrum";
        public const string CreditsChip = "home.creditsChip";
        public const string StatsCard = "home.statsCard";

        public static readonly string[] Names = { DesignFirst, CodexTile, Thrum, CreditsChip, StatsCard };

        /// <summary>Override seam (feat(flags), Settings, tests). Null, or a null answer, keeps the Starter default.</summary>
        public static Func<string, bool?> Source;

        /// <summary>Starter defaults: every home.* extra is hidden.</summary>
        public static bool Default(string name) => false;

        public static bool On(string name)
        {
            bool? v = null;
            if (Source != null)
            {
                try { v = Source(name); }
                catch (Exception) { v = null; }
            }
            return v ?? Default(name);
        }

        public static HomeShow Current() => new HomeShow
        {
            DesignFirst = On(DesignFirst),
            Codex = On(CodexTile),
            Thrum = On(Thrum),
            Credits = On(CreditsChip),
            Stats = On(StatsCard),
        };
    }

    /// <summary>Which Home extras are on. <see cref="Starter"/> is the default; <see cref="Full"/> is the old Home.</summary>
    public struct HomeShow
    {
        public bool DesignFirst, Codex, Thrum, Credits, Stats;
        public static HomeShow Starter => default;
        public static HomeShow Full => new HomeShow { DesignFirst = true, Codex = true, Thrum = true, Credits = true, Stats = true };
        public override string ToString() =>
            (DesignFirst ? "D" : "-") + (Codex ? "C" : "-") + (Thrum ? "T" : "-") + (Credits ? "$" : "-") + (Stats ? "S" : "-");
    }
}
