// Words and pure maths for the "mix" main-menu overlay (Trevor's pick: dusk title card + 2x2 tiles + reel bars).
// Engine-free so EditMode tests can pin the honest copy. No fake data: no badges, no countdown, no scores.
using System;

namespace Kestrel.Game.Shell
{
    public static class MainMenuCopy
    {
        public const string TitleTop = "KESTREL";
        public const string TitleBottom = "HEAVY";

        /// <summary>LAUNCH opens the designer today (no start-mission route exists), so the caption says that
        /// and not "Next: KH-1 · First Hop". Swap the caption when a real route lands.</summary>
        public const string LaunchCaption = "Opens the pad. Build, then launch.";
        public const string TapHint = "TAP LAUNCH TO GO TO THE PAD  \u00B7  TAP A TILE TO OPEN";
        public const string SoonTag = "SOON";

        public struct Item
        {
            public string Id, Label, Caption, Spoken; public bool Live; public string Glyph;
            public Item(string id, string label, string caption, string spoken, bool live, string glyph)
            { Id = id; Label = label; Caption = caption; Spoken = spoken; Live = live; Glyph = glyph; }
        }

        /// <summary>The four tiles (LAUNCH is the bar above them).</summary>
        public static readonly Item[] Tiles =
        {
            new Item("hangar", "HANGAR", "Design and tune your rocket.", "Hangar. Opens the rocket designer.", true, "hangar"),
            new Item("missions", "MISSIONS", "Pick any flight you've unlocked.", "Missions. Coming soon.", false, "planet"),
            new Item("codex", "CODEX", "Parts, places and flight notes.", "Codex. Coming soon.", false, "book"),
            new Item("settings", "SETTINGS", "Sound, motion, accessibility.", "Settings. Coming soon.", false, "gear"),
        };
        public static readonly Item Launch = new Item("launch", "LAUNCH", LaunchCaption, "Launch. Opens the rocket designer on the pad.", true, "launch");

        public const float TitleMax = 80f, TitleMin = 24f;
        /// <summary>Font size that fits <paramref name="avail"/> px, from the width measured at 100 px.</summary>
        public static float FitTitle(float avail, float widthAt100)
        {
            if (avail <= 0f || widthAt100 <= 0f) return TitleMin;
            return Math.Max(TitleMin, Math.Min(TitleMax, 100f * avail / widthAt100));
        }

        /// <summary>Panels narrower than this (points) get the compact type/padding class.</summary>
        public const float NarrowBelow = 500f;
        public static bool IsNarrow(float panelWidth) => panelWidth > 0f && panelWidth < NarrowBelow;

        /// <summary>Minimum height of LAUNCH + two tile rows + gaps + hint padding, matching the USS min-heights.
        /// The 3D rocket and telemetry end near 53% of the height, so controls must start below that.</summary>
        public static float MinControlsHeight(bool narrow) => (narrow ? 60f : 68f) + 8f + 2f * (narrow ? 76f : 88f) + 8f + 34f;
        public const float HeroBottomFrac = 0.53f;

        /// <summary>Letterbox bar height as a share of panel height (reel bars, mockup = 150/2532).</summary>
        public const float BarFrac = 0.06f;
        public static float BarHeight(float panelH, float safeInset) => Math.Max(panelH * BarFrac, safeInset + 24f);

        /// <summary>Stagger for the intro (motion-spec.md): bars first, title, then LAUNCH, then tiles 70 ms apart.</summary>
        public static int IntroDelayMs(string id)
        {
            switch (id)
            {
                case "bars": return 0;
                case "title": return 250;
                case "launch": return 750;
                case "hangar": return 820;
                case "missions": return 890;
                case "codex": return 960;
                case "settings": return 1030;
                default: return 0;
            }
        }
    }
}
