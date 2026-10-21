// Stable UI Toolkit element names for the navigation harness (Gameplay Dev). The harness finds a control with
// root.Q(name). Convention: controls are <screen>-<control>[-<option>] in kebab-case; each screen root is
// screen-<id>; a screen with a way out has <screen>-back, and Settings has settings-close.
// Partial: other screens add their ids in their own file (UiIds.Starter.cs on feat(designer-starter)).
namespace Kestrel.Core.Ui
{
    public static partial class UiIds
    {
        // ---- screen roots ----------------------------------------------------------------------
        public const string ScreenHome = "screen-home";
        public const string ScreenMissions = "screen-missions";
        public const string ScreenBriefing = "screen-briefing";
        public const string ScreenStarter = "screen-starter";
        public const string ScreenDesigner = "screen-designer";
        public const string ScreenReport = "screen-report";
        public const string ScreenSettings = "screen-settings";
        // screen-flight and screen-pause live in UiIds.Flight.cs (#304).

        // ---- Home (feat(home-simple)) ----------------------------------------------------------
        /// <summary>The one primary CTA: CONTINUE with a save, else LAUNCH the next mission.</summary>
        public const string HomePrimary = "home-primary";
        public const string HomeMissions = "home-missions";
        /// <summary>The gear. Opens <see cref="ScreenSettings"/>.</summary>
        public const string HomeSettings = "home-settings";
        // Behind home.* flags (hidden on Starter Home).
        public const string HomeDesign = "home-design";
        public const string HomeCodex = "home-codex";
        public const string HomeThrum = "home-thrum";
        public const string HomePatches = "home-patches";
        // Notes Home can open in place (each closes with its own -close button).
        public const string HomePatchesNote = "home-patches-note";
        public const string HomePatchesNoteClose = "home-patches-note-close";
        public const string HomeCodexSoon = "home-codex-soon";
        public const string HomeCodexSoonClose = "home-codex-soon-close";
        public const string HomeThrumNote = "home-thrum-note";
        public const string HomeThrumNoteClose = "home-thrum-note-close";

        // ---- Settings (the sheet the Home gear opens) --------------------------------------------
        public const string SettingsClose = "settings-close";
        public const string SettingsMusic = "settings-music";
        public const string SettingsSound = "settings-sound";
        public const string SettingsVoice = "settings-voice";
        public const string SettingsSubtitles = "settings-subtitles";
        public const string SettingsSandbox = "settings-sandbox";
        public const string SettingsReduceMotion = "settings-reduce-motion";
        /// <summary>Shown only while home.codexTile is off; opens the Codex (codex-back returns).</summary>
        public const string SettingsCodex = "settings-codex";
        public const string SettingsFullDesigner = "settings-full-designer";

        // ---- sub-screens reachable from Home, and their ways out ---------------------------------
        public const string CodexBack = "codex-back";
        public const string PatchesBack = "patches-back";

        /// <summary>Every interactive Home control, in reading order (hidden-by-flag ones included).</summary>
        public static readonly string[] HomeControls =
        {
            HomeSettings, HomePrimary, HomeMissions, HomeDesign, HomeCodex, HomeThrum, HomePatches,
        };

        /// <summary>Every Settings control; <see cref="SettingsClose"/> last.</summary>
        public static readonly string[] SettingsControls =
        {
            SettingsMusic, SettingsSound, SettingsVoice, SettingsSubtitles, SettingsSandbox, SettingsReduceMotion,
            SettingsCodex, SettingsFullDesigner, SettingsClose,
        };
    }
}
