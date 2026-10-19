// Phase 1 "Starter" designer screen (docs/plans/PHASED_DESIGNER_PLAN.md, PR 5): which designer the pad
// opens under the feature flags, the screen's copy (Kestrel UX, STARTER_COPY.md) and its save. Pure: no
// engine types, so the flags gate, the copy and the save round trip are checked in Kestrel.Core.Tests.
using System;
using System.Globalization;
using Kestrel.Core.Features;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Save;

namespace Kestrel.Core.Designer
{
    public static class StarterScreen
    {
        // ---- flags ----

        /// <summary>The flags that open one of the existing designer screens. Any of them on shows the
        /// existing designer instead of Starter; all off (the Phase 1 default) shows Starter.</summary>
        public static readonly string[] FullDesignerKeys =
        {
            FeatureKeys.DesignerStepper, FeatureKeys.DesignerFree, FeatureKeys.DesignerHangar,
        };

        /// <summary>True when the pad shows the Starter screen. Reads the flags every call (never cached).</summary>
        public static bool UseStarter(FeatureFlags flags)
        {
            if (flags == null) return true;
            foreach (var k in FullDesignerKeys) if (flags.IsOn(k)) return false;
            return true;
        }

        /// <summary>One existing-designer element. Starter hides every designer.* element; the existing designer
        /// shows an element only when its flag is on.</summary>
        public static bool Shows(FeatureFlags flags, string key) => flags != null && !UseStarter(flags) && flags.IsOn(key);

        // ---- copy (Kestrel UX, STARTER_COPY.md §b) ----

        public const string EngineLabel = "Engine fuel", StagesLabel = "Stages", FuelLabel = "Fuel", BoostersLabel = "Boosters";
        public const string Kerosene = "Kerosene", Methane = "Methane", NoBoosters = "None";
        public const string FuelLess = "Less", FuelMore = "More";
        public const string EngineHint = "Kerosene is punchy. Methane burns cleaner.";
        public const string StagesHint = "More stages drop weight on the way up.";
        public const string FuelHint = "More fuel goes farther but weighs more.";
        public const string BoostersHint = "Side boosters add push at liftoff.";
        public const string HydrogenHint = "Top stage uses hydrogen on this mission.";
        public const string OneStageHint = "This hop needs only 1 stage.";
        public const string BoostersLockedHint = "This mission flies 2 boosters that land.";
        public const string ResetText = "Reset", ResetHint = "Back to the mission's rocket.";
        public const string BackText = "◀ Back", BackHint = "Back to the briefing. Your rocket stays.";
        public const string LaunchText = "LAUNCH";
        public const string ReadyWord = "Ready.";
        public const string ReadyBadge = "✓ Ready.";
        /// <summary>No flown result for this combination: Launch is off (B-10, never say ready without one).</summary>
        public const string Checking = "Checking this rocket…";

        public const int LabelMax = 14, HintMax = 44, ReadinessMax = 40;

        public static string OptionText(StarterEngine e) => e == StarterEngine.Methane ? Methane : Kerosene;
        public static string BoosterText(int n) => n <= 0 ? NoBoosters : n.ToString(CultureInfo.InvariantCulture);

        /// <summary>Hydrogen upper stage is automatic from KH-9 on (plan). Only told when there is an upper stage.</summary>
        public static bool HydrogenAuto(StarterRocket r) => r != null && r.Mission != null && r.Mission.Index >= 9 && r.Stages > 1;
        public static bool StagesLocked(StarterRocket r) => r != null && r.StageOptions.Count <= 1;

        /// <summary>The one hint line: the last control the player touched, with its state hint when one applies.
        /// On open (no knob yet) a state hint is shown first, so a locked control explains itself.</summary>
        public static string Hint(StarterRocket r, StarterKnob last)
        {
            if (r == null) return EngineHint;
            switch (last)
            {
                case StarterKnob.Engine: return HydrogenAuto(r) ? HydrogenHint : EngineHint;
                case StarterKnob.Stages: return StagesLocked(r) ? OneStageHint : HydrogenAuto(r) ? HydrogenHint : StagesHint;
                case StarterKnob.FuelNotch: return FuelHint;
                case StarterKnob.Boosters: return r.BoostersLocked ? BoostersLockedHint : BoostersHint;
            }
            if (r.BoostersVisible && r.BoostersLocked) return BoostersLockedHint;
            if (StagesLocked(r)) return OneStageHint;
            if (HydrogenAuto(r)) return HydrogenHint;
            return EngineHint;
        }

        /// <summary>The readiness line split for the screen: the green "✓ Ready." badge and the rest in body colour.
        /// No flown result (or the host gate is blocked) gives no badge, <see cref="Checking"/> and Launch off.</summary>
        public static bool Readiness(StarterRocket r, bool hostBlocked, out string badge, out string rest)
        {
            badge = ""; rest = Checking;
            if (r == null || !r.CanLaunch || hostBlocked) return false;
            string line = r.ReadinessLine;
            if (string.IsNullOrWhiteSpace(line)) return false;
            line = line.Trim();
            if (line.StartsWith(ReadyWord, StringComparison.Ordinal))
            {
                badge = ReadyBadge;
                rest = line.Substring(ReadyWord.Length).Trim();
            }
            else rest = line;
            return true;
        }

        /// <summary>VoiceOver text for the readiness line: "km" read as kilometres.</summary>
        public static string Spoken(string badge, string rest)
        {
            string b = string.IsNullOrEmpty(badge) ? "" : ReadyWord + " ";
            return (b + (rest ?? "")).Replace(" km", " kilometres").Trim();
        }

        public static string FuelSpoken(int notch) => FuelLabel + ", " + (notch + 1).ToString(CultureInfo.InvariantCulture) + " of " + StarterRocket.FuelNotches.ToString(CultureInfo.InvariantCulture);

        // ---- header ----

        public static string Title(CampaignMission m)
        {
            if (m == null) return "Your rocket";
            string kh = "KH-" + m.Index.ToString(CultureInfo.InvariantCulture);
            return string.IsNullOrEmpty(m.Name) ? kh : kh + " · " + m.Name;
        }

        /// <summary>The mission target from campaign.json (never a flown number), plus the landing.</summary>
        public static string Subtitle(CampaignMission m)
        {
            if (m == null) return "";
            string target;
            switch (m.TargetKind)
            {
                case "apogee": target = "Hop to " + Km(m.ApoKm) + " and back"; break;
                case "circular": target = "Reach a " + Km(m.ApoKm) + " orbit"; break;
                case "ellipse": target = "Reach a " + Num(m.PeriKm) + " × " + Km(m.ApoKm) + " orbit"; break;
                case "c3": target = "Leave Earth's pull"; break;
                default: target = m.ApoKm > 0 ? "Reach " + Km(m.ApoKm) : "Fly the mission"; break;
            }
            if (m.LandingMode == "rtls") target += " · land at the pad";
            else if (m.LandingMode == "barge") target += " · land on Haven";
            return target;
        }

        static string Km(double km) => Num(km) + " km";
        static string Num(double v) => double.IsNaN(v) || double.IsInfinity(v) ? "?" : Math.Round(v).ToString("#,0", CultureInfo.InvariantCulture);

        // ---- save ----

        /// <summary>Kind written into the record envelope (NativeStore.PutRecord).</summary>
        public const string SaveKind = "starter";

        /// <summary>Write the current combination under <see cref="StarterRocket.SaveKey"/>. False when there is no store.</summary>
        public static bool Save(NativeStore store, StarterRocket r)
        {
            if (store == null || r == null || string.IsNullOrEmpty(r.MissionId)) return false;
            store.PutRecord(StarterRocket.SaveKey(r.MissionId), SaveKind, r.ToSave());
            return true;
        }

        /// <summary>Restore this mission's last combination (snapped to a passing one). False when none was saved.</summary>
        public static bool Restore(NativeStore store, StarterRocket r)
        {
            if (store == null || r == null || string.IsNullOrEmpty(r.MissionId)) return false;
            JsonObject saved;
            try { saved = store.GetRecord(StarterRocket.SaveKey(r.MissionId)); }
            catch (SaveException) { return false; }
            return saved != null && r.ApplySave(saved);
        }
    }
}
