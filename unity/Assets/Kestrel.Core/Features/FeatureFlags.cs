// Phase 1 feature flags (docs/plans/PHASED_DESIGNER_PLAN.md, PR 3). data/features.json holds the
// defaults; Settings can override any flag. A hidden feature keeps its code.
// Every flag defaults ON, so this PR changes nothing on screen. The Starter screen and the simple
// Home (PRs 5/6) flip the starter defaults and read the flags. The tier lives in ExperienceSettings.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Json;

namespace Kestrel.Core.Features
{
    /// <summary>The single designer tier. Replaces the old Simple/Standard/Engineer layer and the two engineer prefs.</summary>
    public enum ExperienceTier { Starter = 0, Builder = 1, Engineer = 2 }

    /// <summary>Flag ids as written in data/features.json.</summary>
    public static class FeatureKeys
    {
        public const string DesignerStepper = "designer.stepper";
        public const string DesignerFree = "designer.free";
        public const string DesignerModeTabs = "designer.modeTabs";
        public const string DesignerHangar = "designer.hangar";
        public const string DesignerTemplates = "designer.templates";
        public const string DesignerFixPill = "designer.fixPill";
        public const string DesignerWarningsList = "designer.warningsList";
        public const string DesignerMakeItFly = "designer.makeItFly";
        public const string DesignerPayloadStepper = "designer.payloadStepper";
        public const string DesignerV3Knobs = "designer.v3Knobs";
        public const string DesignerLook = "designer.look";
        public const string DesignerRecoveryControls = "designer.recoveryControls";
        public const string DesignerUndoRedo = "designer.undoRedo";
        /// <summary>#341: the optional closed-loop guidance computer part. Off in the Starter designer (data/features.json);
        /// it is also gated on a campaign unlock (Flight.GuidanceComputer.Available).</summary>
        public const string DesignerGuidanceComputer = "designer.guidanceComputer";
        public const string HomeDesignFirst = "home.designFirst";
        public const string HomeCodexTile = "home.codexTile";
        public const string HomeThrum = "home.thrum";
        public const string HomeCreditsChip = "home.creditsChip";
        public const string HomeStatsCard = "home.statsCard";

        public static readonly IReadOnlyList<string> All = new[]
        {
            DesignerStepper, DesignerFree, DesignerModeTabs, DesignerHangar, DesignerTemplates, DesignerFixPill,
            DesignerWarningsList, DesignerMakeItFly, DesignerPayloadStepper, DesignerV3Knobs, DesignerLook,
            DesignerRecoveryControls, DesignerUndoRedo, DesignerGuidanceComputer,
            HomeDesignFirst, HomeCodexTile, HomeThrum, HomeCreditsChip, HomeStatsCard,
        };

        /// <summary>Settings pref for the "Show full designer" toggle (off by default).</summary>
        public const string ShowFullDesignerPref = ExperienceSettings.ShowFullDesignerPref;
        /// <summary>Settings pref for the tier: 0 starter, 1 builder, 2 engineer.</summary>
        public const string TierPref = ExperienceSettings.TierPref;
    }

    public sealed class FeatureFlags
    {
        readonly Dictionary<string, bool> defaults = new Dictionary<string, bool>();
        readonly Dictionary<string, bool> overrides = new Dictionary<string, bool>();
        ExperienceTier tier;
        bool showFullDesigner;

        /// <summary>The flags the game reads. The host sets it at boot (checkout file or Resources/KestrelData/features).</summary>
        public static FeatureFlags Active = new FeatureFlags();

        /// <summary>Raised after an override, the tier or the full-designer toggle changes.</summary>
        public event Action<FeatureFlags> Changed;

        /// <summary>Built-in defaults: every listed flag on, tier Starter. Matches data/features.json.</summary>
        public FeatureFlags()
        {
            foreach (var k in FeatureKeys.All) defaults[k] = true;
        }

        /// <summary>Copy the player's Settings (tier and Show full designer) from prefs. Read-only on the prefs.</summary>
        public void ApplyPrefs(Func<string, bool> has, Func<string, int> read)
        {
            Tier = ExperienceSettings.Tier(has, read);
            ShowFullDesigner = ExperienceSettings.ShowFullDesigner(has, read);
        }

        public static FeatureFlags Load(string repoRoot) => Parse(File.ReadAllText(Path.Combine(repoRoot, "data", "features.json")));

        public static FeatureFlags Parse(string json)
        {
            var f = new FeatureFlags();
            var o = MiniJson.ParseObject(json);
            if (TryTier(o.Str("tier"), out var t)) f.tier = t;
            var flags = o.Obj("flags");
            if (flags != null)
                foreach (var k in flags.Order)
                    if (flags[k] is bool b) f.defaults[k] = b;
            return f;
        }

        public ExperienceTier Tier
        {
            get => tier;
            set { if (tier == value) return; tier = value; Changed?.Invoke(this); }
        }

        /// <summary>Settings "Show full designer": every designer.* flag reads on. Home flags are unchanged.</summary>
        public bool ShowFullDesigner
        {
            get => showFullDesigner;
            set { if (showFullDesigner == value) return; showFullDesigner = value; Changed?.Invoke(this); }
        }

        /// <summary>Unknown keys read off. A key missing from the file keeps its built-in default (on).</summary>
        public bool IsOn(string key)
        {
            if (key == null) return false;
            if (showFullDesigner && key.StartsWith("designer.", StringComparison.Ordinal)) return true;
            if (overrides.TryGetValue(key, out var o)) return o;
            return defaults.TryGetValue(key, out var d) && d;
        }

        public bool Default(string key) => key != null && defaults.TryGetValue(key, out var d) && d;
        public bool HasOverride(string key) => key != null && overrides.ContainsKey(key);

        /// <summary>Settings override for one flag.</summary>
        public void SetOverride(string key, bool on)
        {
            if (key == null) return;
            if (overrides.TryGetValue(key, out var was) && was == on) return;
            overrides[key] = on;
            Changed?.Invoke(this);
        }

        public void ClearOverride(string key)
        {
            if (key != null && overrides.Remove(key)) Changed?.Invoke(this);
        }

        public static string TierId(ExperienceTier t) => ExperienceSettings.TierId(t);
        public static bool TryTier(string id, out ExperienceTier t) => ExperienceSettings.TryTier(id, out t);
    }
}
