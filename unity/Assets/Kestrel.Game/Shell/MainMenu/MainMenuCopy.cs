// Words and pure maths for the "mix" main-menu overlay (Trevor's pick: dusk title card + 2x2 tiles + reel bars).
// Engine-free so EditMode tests can pin the honest copy. No fake data: no badges, no countdown, no scores.
using System;

namespace Kestrel.Game.Shell
{
    public static class MainMenuCopy
    {
        public const string TitleTop = "KESTREL";
        public const string TitleBottom = "HEAVY";

        /// <summary>Static caption. The live button may show the real next mission once Career is known.</summary>
        public const string LaunchCaption = "Next flight, then the pad.";
        public const string DesignLabel = "DESIGN FIRST";
        public const string DesignCaption = "Your rocket, this mission.";
        public const string TapHint = "TAP A TILE OR LAUNCH TO BEGIN";
        public const string SoonTag = "SOON";
        public const string FuelStat = "Fuel 92%";
        public const string ThrustStat = "Thrust 850 kN";

        public struct Item
        {
            public string Id, Label, Caption, Spoken; public bool Live; public string Glyph;
            public Item(string id, string label, string caption, string spoken, bool live, string glyph)
            { Id = id; Label = label; Caption = caption; Spoken = spoken; Live = live; Glyph = glyph; }
        }

        /// <summary>One row under LAUNCH and Design first. Settings is the corner gear, not a tile.</summary>
        public static readonly Item[] Tiles =
        {
            new Item("missions", "MISSIONS", "Pick any flight you've unlocked.", "Missions. Opens the mission list.", true, "planet"),
            new Item("codex", "CODEX", "Parts, places and flight notes.", "Codex. Coming soon.", false, "book"),
        };
        public static readonly Item Launch = new Item("launch", "LAUNCH", LaunchCaption, "Launch. Next flight, then the pad.", true, "launch");
        public static readonly Item Design = new Item("design", DesignLabel, DesignCaption, "Design first. Opens the designer with this mission.", true, "design");

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

        // The 3D hero (rocket and telemetry) ends near 53% of the height; MenuTypeScale.HeroLift moves it clear of the controls.

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
                case "stats": return 700;
                case "launch": return 750;
                case "design": return 820;
                case "missions": return 890;
                case "codex": return 960;
                case "gear": return 250;
                default: return 0;
            }
        }
    }
}
