// The single designer tier (Phase 1 PR 3). It replaces three engineer switches:
//   kh.designer.v3.engineer (free designer), kh.hangar.engineer (Hangar), SliceApp.EngineerEnabled.
// Old layer names map: simple -> starter, standard -> builder, engineer -> engineer.
//
// Migration, chosen so nothing changes for a player or a test:
//   - The tier is Engineer while either old engineer pref is on. Those prefs stay the storage for the
//     Engineer switch (the designer and Hangar toggles still write them), so a save from an older
//     build keeps its Engineer setting and a newer save still reads in an older build.
//   - Otherwise the tier is kh.settings.tier (0 starter, 1 builder). A stored 2 with both old prefs
//     off means Engineer was switched off since, so it reads as builder.
//   - No prefs at all: Starter, Engineer off, Show full designer off. That is today's default.
// Pure C#: callers pass PlayerPrefs.HasKey / GetInt / SetInt (or a test dictionary).
using System;
using Kestrel.Core.Flight;
using Kestrel.Core.Flight.Narration;

namespace Kestrel.Core.Features
{
    public static class ExperienceSettings
    {
        public const string TierPref = "kh.settings.tier";
        public const string ShowFullDesignerPref = "kh.settings.fullDesigner";
        /// <summary>Settings > Show corner character (Thrum's portrait in the corner). A key no older build wrote,
        /// so a save from before this setting reads as off. Only a stored 1 turns it on.</summary>
        public const string CornerCharacterPref = "kh.settings.cornerCharacter";
        public const string LegacyDesignerEngineerPref = "kh.designer.v3.engineer";
        public const string LegacyHangarEngineerPref = "kh.hangar.engineer";

        public static ExperienceTier Tier(Func<string, bool> has, Func<string, int> read)
        {
            if (LegacyEngineer(has, read)) return ExperienceTier.Engineer;
            int stored = Read(has, read, TierPref);
            return stored >= (int)ExperienceTier.Builder ? ExperienceTier.Builder : ExperienceTier.Starter;
        }

        /// <summary>Engineer on (either old pref). Same answer GuideHost.EngineerDepth and the ascent HUD used before.</summary>
        public static bool IsEngineer(Func<string, bool> has, Func<string, int> read) => Tier(has, read) == ExperienceTier.Engineer;

        public static bool ShowFullDesigner(Func<string, bool> has, Func<string, int> read) => Read(has, read, ShowFullDesignerPref) != 0;

        public static void SetShowFullDesigner(bool on, Action<string, int> write) => write?.Invoke(ShowFullDesignerPref, on ? 1 : 0);

        /// <summary>The corner character shows only when the player switched it on. No stored value is off.</summary>
        public static bool CornerCharacter(Func<string, bool> has, Func<string, int> read) => Read(has, read, CornerCharacterPref) == 1;

        public static void SetCornerCharacter(bool on, Action<string, int> write) => write?.Invoke(CornerCharacterPref, on ? 1 : 0);

        /// <summary>
        /// Store a tier. Engineer turns both old prefs on; starter/builder turn both off.
        /// Not called by today's toggles (they keep writing their own pref); for the Phase 2/3 tier picker.
        /// </summary>
        public static void SetTier(ExperienceTier tier, Action<string, int> write)
        {
            if (write == null) return;
            write(TierPref, (int)tier);
            int eng = tier == ExperienceTier.Engineer ? 1 : 0;
            write(LegacyDesignerEngineerPref, eng);
            write(LegacyHangarEngineerPref, eng);
        }

        /// <summary>Old Experience layer id for a tier: starter -> simple, builder -> standard, engineer -> engineer.</summary>
        public static string Layer(ExperienceTier t) => t == ExperienceTier.Engineer ? "engineer" : t == ExperienceTier.Builder ? "standard" : "simple";

        /// <summary>Tier for an old Experience layer id. Unknown or empty is starter.</summary>
        public static ExperienceTier FromLayer(string layer)
            => layer == "engineer" ? ExperienceTier.Engineer : layer == "standard" ? ExperienceTier.Builder : ExperienceTier.Starter;

        public static string TierId(ExperienceTier t) => t == ExperienceTier.Engineer ? "engineer" : t == ExperienceTier.Builder ? "builder" : "starter";

        public static bool TryTier(string id, out ExperienceTier t)
        {
            switch (id)
            {
                case "starter": t = ExperienceTier.Starter; return true;
                case "builder": t = ExperienceTier.Builder; return true;
                case "engineer": t = ExperienceTier.Engineer; return true;
                default: t = ExperienceTier.Starter; return false;
            }
        }

        static bool LegacyEngineer(Func<string, bool> has, Func<string, int> read)
            => Read(has, read, LegacyDesignerEngineerPref) != 0 || Read(has, read, LegacyHangarEngineerPref) != 0;

        // ---- Settings > Commentary and Time warp (UX after #384 and #436) ------------------------------------------
        /// <summary>The old voice mute, still written alongside <see cref="Commentary.Pref"/> for older builds.</summary>
        public const string LegacyNarrationMutePref = "kh.audio.narration.muted";
        public const string CommentaryLabel = "Commentary";
        public const string CommentaryNote = "Who talks during the flight.";
        public const string TimeWarpLabel = "Time warp";
        public const string TimeWarpNote = "Speeds up quiet parts of the flight.";

        /// <summary>The saved commentary mode. No pref reads the old voice mute (muted is Off); a fresh save is Full.</summary>
        public static CommentaryMode CommentaryChoice(Func<string, bool> has, Func<string, int> read)
            => Kestrel.Core.Flight.Narration.Commentary.Resolve(has != null && has(Kestrel.Core.Flight.Narration.Commentary.Pref), Read(has, read, Kestrel.Core.Flight.Narration.Commentary.Pref), Read(has, read, LegacyNarrationMutePref) == 1);

        /// <summary>Stores the mode and keeps the old mute key meaning "voice off", as AudioPrefs.SetCommentary does.</summary>
        public static void SetCommentaryChoice(CommentaryMode mode, Action<string, int> write)
        {
            if (write == null) return;
            write(Kestrel.Core.Flight.Narration.Commentary.Pref, (int)mode);
            write(LegacyNarrationMutePref, mode == CommentaryMode.Off ? 1 : 0);
        }

        public static string CommentaryState(CommentaryMode mode)
            => mode == CommentaryMode.CallsOnly ? "Calls only" : mode == CommentaryMode.Off ? "Off" : "Full";

        public static string CommentaryTooltip(CommentaryMode mode)
        {
            switch (mode)
            {
                case CommentaryMode.CallsOnly: return "Commentary: Calls only. Mission control calls only. Tap for Off.";
                case CommentaryMode.Off: return "Commentary: Off. Nobody talks. Tap for Full.";
                default: return "Commentary: Full. Mission control and the hosts talk. Tap for Calls only.";
            }
        }

        /// <summary>The saved time-warp mode. No pref is Auto.</summary>
        public static TimeWarpMode TimeWarp(Func<string, bool> has, Func<string, int> read)
            => TimeWarpSetting.Resolve(has != null && has(TimeWarpSetting.Pref), Read(has, read, TimeWarpSetting.Pref));

        public static void SetTimeWarp(TimeWarpMode mode, Action<string, int> write) => write?.Invoke(TimeWarpSetting.Pref, (int)mode);

        public static TimeWarpMode NextTimeWarp(TimeWarpMode mode) => mode == TimeWarpMode.Auto ? TimeWarpMode.Off : TimeWarpMode.Auto;

        public static string TimeWarpState(TimeWarpMode mode) => mode == TimeWarpMode.Off ? "Off" : "Auto";

        public static string TimeWarpTooltip(TimeWarpMode mode)
            => mode == TimeWarpMode.Off
                ? "Time warp: Off. Flights play in real time. Tap for Auto."
                : "Time warp: Auto. " + TimeWarpNote + " Tap for Off.";

        static int Read(Func<string, bool> has, Func<string, int> read, string key)
        {
            if (read == null) return 0;
            if (has != null && !has(key)) return 0;
            return read(key);
        }
    }
}
