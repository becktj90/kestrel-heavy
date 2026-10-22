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
        public const string BoostersLockedHint = "This mission flies 2 boosters that land.";
        public const string ResetText = "Reset", ResetHint = "Back to the mission's rocket.";
        public const string BackText = "◀ Back", BackHint = "Back to the briefing. Your rocket stays.";
        public const string LaunchText = "LAUNCH";
        public const string ReadyWord = "Ready.";
        public const string ReadyBadge = "✓ Ready.", TightBadge = "! Tight.", ShortBadge = "✗ Short.", HeavyBadge = "✗ Too heavy.", BlockedBadge = "✗ Can't build.";

        public const int LabelMax = 14, HintMax = 44, ReadinessMax = 40;
        /// <summary>Below this margin the line says "fuel almost gone"; below this liftoff push it says "slow off the pad"
        /// (Validate.GOOD_TWR). Information only: Launch stays on.</summary>
        public const double TightMargin = 0.10;

        public static string OptionText(StarterEngine e) => e == StarterEngine.Methane ? Methane : Kerosene;
        public static string BoosterText(int n) => n <= 0 ? NoBoosters : n.ToString(CultureInfo.InvariantCulture);

        /// <summary>The upper stage burns hydrogen. Told when the built rocket has one.</summary>
        public static bool HydrogenAuto(StarterRocket r) => r != null && r.HydrogenUpper;

        /// <summary>The one hint line: the last control the player touched, with its state hint when one applies.
        /// On open (no control yet) a state hint is shown first, so a locked control explains itself.</summary>
        public static string Hint(StarterRocket r, StarterKnob last)
        {
            if (r == null) return EngineHint;
            switch (last)
            {
                case StarterKnob.Engine: return EngineHint;
                case StarterKnob.Stages: return HydrogenAuto(r) ? HydrogenHint : StagesHint;
                case StarterKnob.FuelNotch: return FuelHint;
                case StarterKnob.Boosters: return r.BoostersLocked ? BoostersLockedHint : BoostersHint;
            }
            if (r.BoostersVisible && r.BoostersLocked) return BoostersLockedHint;
            if (HydrogenAuto(r)) return HydrogenHint;
            return EngineHint;
        }

        /// <summary>
        /// The readiness line from the existing readout (Validate.Engineering + Readout.Quick) on the rocket the
        /// controls built: a short badge and the rest in body colour. Plain information. Returns false (Launch off)
        /// only on a hard schema block, with its one plain reason. No numbers: no line, Launch stays on.
        /// </summary>
        public static bool Readiness(StarterRocket r, out string badge, out string rest)
        {
            badge = ""; rest = "";
            if (r == null) return false;
            var c = r.Check();
            if (c.Blocked) { badge = c.BlockCode == "twr-low" ? HeavyBadge : BlockedBadge; rest = c.BlockReason ?? ""; return false; }
            if (!c.HasNumbers) return true;
            var m = r.Mission;
            string target = TargetText(m);
            bool hop = m != null && m.TargetKind == "apogee";
            if (hop) return HopLine(r, c, out badge, out rest);
            if (c.LiftoffTwr < Validate.MIN_TWR)
            {
                badge = HeavyBadge;
                bool more = r.BoostersVisible && !r.BoostersLocked && r.Boosters < 4;
                rest = more ? "Can't lift off. Try less fuel or boosters." : "Can't lift off. Try less fuel.";
                return true;
            }
            if (c.Margin < 0)
            {
                badge = ShortBadge;
                int pct = Math.Max(1, (int)Math.Round(-c.Margin * 100));
                rest = "About " + pct.ToString(CultureInfo.InvariantCulture) + "% short of " + target + ". Try more fuel or a stage.";
                return true;
            }
            if (c.Margin < TightMargin)
            {
                badge = TightBadge;
                rest = hop ? "Just enough fuel for the hop." : "Reaches " + target + ", fuel almost gone.";
                return true;
            }
            if (c.LiftoffTwr < Validate.GOOD_TWR)
            {
                badge = TightBadge;
                rest = "Slow off the pad, but reaches " + target + ".";
                return true;
            }
            badge = ReadyBadge;
            if (hop) rest = "Hops to " + target + " and back.";
            else if (m != null && m.TargetKind == "c3") rest = "Leaves Earth with fuel to spare.";
            else if (r.Boosters > 0 && !r.BoostersLocked) rest = "Boosters help it reach " + target + ".";
            else rest = "Reaches " + target + " with fuel to spare.";
            return true;
        }

        /// <summary>Hops (KH-1, KH-2) compare with the mission's flown rocket, not the readout's orbital margin:
        /// that margin reads "short" on rockets that fly the hop (M-10), so it never reaches this line.</summary>
        public const double HopSame = 0.02;

        static bool HopLine(StarterRocket r, StarterCheck c, out string badge, out string rest)
        {
            var b = r.BaseCheck();
            double rel = b != null && b.HasNumbers && b.DeltaV > 0 ? c.DeltaV / b.DeltaV - 1 : 0;
            if (rel > HopSame) { badge = TightBadge; rest = "More push than the rocket that flew this hop. It may overshoot."; }
            else if (rel < -HopSame) { badge = TightBadge; rest = "Less push than the rocket that flew this hop. It may fall short."; }
            else { badge = ReadyBadge; rest = "Hops to " + TargetText(r.Mission) + " and back."; }
            return true;
        }

        static string TargetText(CampaignMission m)
        {
            if (m == null) return "the target";
            if (m.TargetKind == "c3") return "escape speed";
            return Km(m.ApoKm);
        }

        /// <summary>VoiceOver text for the readiness line: "km" read as kilometres.</summary>
        public static string Spoken(string badge, string rest)
        {
            string b = string.IsNullOrEmpty(badge) ? "" : badge.TrimStart('✓', '!', '✗', ' ') + " ";
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

        /// <summary>Restore this mission's last setting (clamped). False when none was saved.</summary>
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
