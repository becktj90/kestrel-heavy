// One place for every size on the main menu. Pure maths (no UnityEngine), so EditMode tests can check each device.
//   size = clamp(min, k * s * base, max),  s = min(width / 390, usableHeight / 844),  usableHeight = height - top - bottom safe insets
// Metrics (MainMenuMetrics) are the real advance widths of the shipped subset fonts, so "fits" is checked, not guessed.
using System;
using Kestrel.Core.Features;
using Kestrel.Core.Flight;

namespace Kestrel.Game.Shell
{
    public static class MenuTypeScale
    {
        public const float RefW = 390f, RefH = 844f;
        public const float SideMargin = 12f;      // .kh-mm horizontal padding
        public const float Gap = 6f;              // between LAUNCH and tiles, between tile rows
        public const float TileBorder = 3f, LaunchBorder = 3f;   // 3 = the live (cream) tile border, the widest
        public const float LampSlot = 12f + 4f;   // lamp + margin
        public const float TagPad = 2f * 5f + 2f + 4f;   // tag horizontal padding + border + margin
        public const float NameSlack = 10f;      // spare px beside a tile name: Label chrome and glyph overhang, so names never ellipsize
        public const float LineH = 1.4f;         // line box height / font size used for every wrapped or stacked line
        public const float TopPad = 40f;         // .kh-mm padding-top (below the top bar)
        /// <summary>Phone column with the credits bar (HomeResumeBar, 44 pt at the top-left): the title starts
        /// under it, never under 40. The eyebrow drew on the bar's last 4 pt at 335 and 390 portrait.</summary>
        public const float CreditsBand = 44f + 4f;
        /// <summary>The gear sits in the same band on the right, so the column always starts under it (Starter Home
        /// has no chip, and 40 put the eyebrow under the gear's last 4 pt).</summary>
        public static float TopPadFor(bool creditsBar) => Math.Max(TopPad, CreditsBand);
        public const float HitMin = 44f;

        public readonly struct Spec
        {
            public readonly float Base, Min, Max;
            public Spec(float b, float mn, float mx) { Base = b; Min = mn; Max = mx; }
            public float At(float s, float k = 1f) => Math.Max(Min, Math.Min(Max, k * s * Base));
        }
        // base is the size on a 390x844 phone. Captions never go below 14.
        public static readonly Spec Title = new Spec(76f, 28f, 84f);
        public static readonly Spec Tagline = new Spec(20f, 14f, 30f);
        public static readonly Spec LaunchLabel = new Spec(26f, 20f, 38f);
        public static readonly Spec LaunchCaption = new Spec(14f, 14f, 20f);
        public static readonly Spec TileName = new Spec(19f, 14f, 26f);
        public static readonly Spec TileCaption = new Spec(13f, 14f, 18f);
        public static readonly Spec Soon = new Spec(14f, 14f, 16f);
        public static readonly Spec Hint = new Spec(14f, 14f, 18f);
        public const float CaptionFloor = 14f;

        public static float Scale(float w, float h, float topInset = 0f, float bottomInset = 0f)
        {
            float usable = Math.Max(1f, h - Math.Max(0f, topInset) - Math.Max(0f, bottomInset));
            return Math.Max(0.1f, Math.Min(w / RefW, usable / RefH));
        }

        public struct Sizes
        {
            public float Scale, Title, Tagline, LaunchLabel, LaunchCaption, TileName, TileCaption, Soon, Hint;
            public float Pad, TileW, TileInner, TileH, LaunchH, DesignH, DesignLabel, StatsH, StatFont, LaunchCaptionW, ControlsH, HintBand, BottomReserve, TitleBottom, ControlsTop, HeroLift;
            public int TileCaptionLines, LaunchCaptionLines, DesignCaptionLines, ThrumLines;
            public float ThrumH;
            /// <summary>Inner column width (root minus its 12 px sides).</summary>
            public float Column;
            public string ThrumText;
            /// <summary>The record text the stats card shows (the short form when the long one would wrap).</summary>
            public string Record;
            /// <summary>Which home.* extras this pass lays out (hidden ones take no height).</summary>
            public HomeShow Show;
            /// <summary>The primary CTA words: label and sub-line ("" when the label carries the mission line).</summary>
            public string CtaLabel, CtaCaption;
        }

        public static float TextW(string s, float size) => MainMenuMetrics.W(s) * size / 100f;
        static int Lines(float textW, float avail) => Math.Max(1, (int)Math.Ceiling(textW * 1.08f / Math.Max(1f, avail)));   // 8% word-wrap slack

        /// <summary><paramref name="side"/> is the panel width outside the safe column (content-rect left plus right),
        /// so the column maths uses the width the root really has. <paramref name="record"/> is the stats record line,
        /// <paramref name="launchCaption"/> the live LAUNCH caption (null: the static one). Block heights come from
        /// <see cref="HomePhoneLayout"/> (word-wrapped at the real inner widths), never below the old estimates.</summary>
        public static Sizes Compute(float w, float h, float topInset = 0f, float bottomInset = 0f, float side = 0f, string record = null, string launchCaption = null,
            HomeShow? show = null, bool save = false)
        {
            var sh = show ?? HomeFeatures.Current();
            float colW = Math.Max(1f, w - Math.Max(0f, side));
            float grid = colW - 2f * SideMargin;
            var t = HomePhoneLayout.Type(colW, h, topInset, bottomInset, grid);
            // One primary CTA (STARTER_COPY.md): launchCaption is the mission line ("KH-3 · Tiny Orbit").
            HomePhoneLayout.Cta(save, launchCaption, l => HomePhoneLayout.CtaFits(t, grid, l), out var ctaLabel, out var ctaCaption);
            t.LaunchLabel = HomePhoneLayout.CtaLabel(t, grid, ctaLabel, t.LaunchLabel);
            var z = new Sizes { Scale = t.Scale, Show = sh, CtaLabel = ctaLabel, CtaCaption = ctaCaption };
            float s = z.Scale;
            z.Title = t.Title; z.Tagline = t.Tagline; z.LaunchLabel = t.LaunchLabel; z.LaunchCaption = t.LaunchCaption;
            z.TileName = t.TileName; z.TileCaption = t.TileCaption; z.Soon = t.Soon; z.Hint = t.Hint;
            z.Pad = t.Pad;
            z.Column = grid;
            // Starter Home: MISSIONS alone spans the row.
            z.TileW = sh.Codex ? HomePhoneLayout.TileW(grid) : grid;
            z.TileInner = z.TileW - 2f * TileBorder - 2f * z.Pad;
            int worstLines = 1;
            // Word-wrapped at the real inner width (the old 8% estimate said 2 lines where TextCore wrapped 3, and
            // the third sat on the tile's bottom edge).
            foreach (var it in MainMenuCopy.Tiles)
            {
                if (!sh.Codex && it.Id != "missions") continue;
                worstLines = Math.Max(worstLines, sh.Codex
                    ? HomePhoneLayout.TileCaptionLines(t, grid, it.Caption)
                    : Math.Max(1, HomeTypeMetrics.Lines(HomeFace.GroteskMedium, it.Caption, t.TileCaption, grid - 2f * HomePhoneLayout.TileBorder - 2f * t.Pad)));
            }
            z.TileCaptionLines = worstLines;
            float head = Math.Max(22f, z.TileName * LineH);
            z.TileH = Math.Max(HomePhoneLayout.TileH(t, worstLines), Math.Max(HitMin, 2f * TileBorder + 2f * z.Pad + head + 3f + worstLines * z.TileCaption * LineH));
            float padH = HomePhoneLayout.LaunchPadH(s);
            z.LaunchCaptionW = grid - 2f * LaunchBorder - 2f * padH - LampSlot - 12f;
            z.LaunchCaptionLines = string.IsNullOrEmpty(ctaCaption) ? 0 : Math.Max(Lines(TextW(MainMenuCopy.LaunchCaption, z.LaunchCaption), z.LaunchCaptionW),
                HomePhoneLayout.LaunchCaptionLines(t, grid, ctaCaption));
            z.LaunchH = HomePhoneLayout.LaunchH(t, z.LaunchCaptionLines);
            // DESIGN FIRST: the face shrinks to the inner width, and the button grows with the caption's
            // word-wrapped lines. The old 42% cap cut this height under its own caption ("Your rocket, this
            // mission" clipped on TestFlight), so there is no cap any more; the hero gap pays instead.
            z.DesignLabel = t.DesignLabel;
            z.DesignCaptionLines = HomePhoneLayout.DesignCaptionLines(t, grid, MainMenuCopy.DesignCaption);
            z.DesignH = HomePhoneLayout.DesignH(t, z.DesignCaptionLines);
            // Two lines exactly: the pills and the record, at the largest face that keeps each on one line.
            z.StatFont = HomePhoneLayout.StatFont(t, grid, string.IsNullOrEmpty(record) ? SampleRecord : record, out var shown);
            z.Record = shown;
            z.StatsH = HomePhoneLayout.StatsH(z.StatFont);
            z.ThrumText = HomePhoneLayout.ThrumText(t, grid, MainMenuCopy.ThrumLine, MainMenuCopy.ThrumShort);
            z.ThrumLines = HomePhoneLayout.ThrumLines(t, grid, z.ThrumText);
            z.ThrumH = HomePhoneLayout.ThrumH(t, z.ThrumLines);
            float bottom = Math.Max(0f, bottomInset);
            z.BottomReserve = MainMenuCopy.BarHeight(h, bottom);
            z.HintBand = Math.Max(0f, z.BottomReserve - bottom);   // root padding-bottom (the root already sits above the safe inset)
            z.ControlsH = (sh.Stats ? z.StatsH + Gap : 0f) + z.LaunchH + Gap + (sh.DesignFirst ? z.DesignH + Gap : 0f) + z.TileH + Gap;
            z.ControlsTop = h - z.BottomReserve - z.ControlsH;
            z.TitleBottom = Math.Max(0f, topInset) + TopPadFor(true) + z.Title * 1.3f + z.Tagline * LineH;
            z.HeroLift = HeroLift(h, z.TitleBottom, z.ControlsTop);
            return z;
        }

        /// <summary>Record used for sizing before the save is read: a long career, so the card never grows later.</summary>
        public const string SampleRecord = "20 flights cleared · 888.88M Kc · 20 patches";

        /// <summary>Tile name size: the scaled size, shrunk (never below its Min) until it fits beside the lamp or SOON tag.</summary>
        public static float FitName(Sizes z, string name, bool soon)
        {
            float trailing = soon ? TextW(MainMenuCopy.SoonTag, z.Soon) + TagPad : LampSlot;
            float avail = z.TileInner - trailing - NameSlack;
            float fit = 100f * avail / MainMenuMetrics.W(name);
            return Math.Max(TileName.Min, Math.Min(z.TileName, fit));
        }
        public static bool NameFits(Sizes z, string name, bool soon, float size)
        {
            float trailing = soon ? TextW(MainMenuCopy.SoonTag, z.Soon) + TagPad : LampSlot;
            return TextW(name, size) <= z.TileInner - trailing - NameSlack + 0.01f;
        }

        /// <summary>Size for the tile name as it is drawn ("Missions", mixed case) beside the 18 px glyph and, on a live
        /// tile, the lamp. <see cref="FitName"/> left the glyph out, so "Missions" ran into its lamp on a 308-wide phone panel.</summary>
        public static float FitShownName(Sizes z, string shown, bool live)
        {
            float avail = z.TileInner - HomePhoneLayout.TileIcon - (live ? HomePhoneLayout.TileLamp : 0f) - HomePhoneLayout.NameSlack;
            return HomeTypeMetrics.FitOneLine(HomeFace.FrauncesBold, shown, z.TileName, 12f, avail, 0.03f);
        }

        // 3D hero (rocket + telemetry) in fractions of panel height measured from the top, at the rest camera.
        public const float HeroTopFrac = 0.27f;        // rocket nose
        public const float HeroBottomFrac = 0.53f;     // rocket base (the telemetry panels end at 0.495)
        public const float HeroGapPx = 8f;

        /// <summary>How far (fraction of panel height) the whole 3D view must move up so the hero ends HeroGapPx above the controls,
        /// capped so the nose stays HeroGapPx below the title block. 0 when nothing needs to move.</summary>
        public static float HeroLift(float h, float titleBottomPx, float controlsTopPx)
        {
            if (h <= 0f) return 0f;
            float need = HeroBottomFrac * h + HeroGapPx - controlsTopPx;
            float room = HeroTopFrac * h - HeroGapPx - titleBottomPx;
            return Math.Max(0f, Math.Min(need, Math.Max(0f, room))) / h;
        }

        /// <summary>Px of overlap left between the lifted hero and the controls (<= 0 means clear).</summary>
        public static float HeroOverlapPx(Sizes z, float h) => HeroBottomFrac * h - z.HeroLift * h - z.ControlsTop;

        public static float Clamp(float v, float mn, float mx) => Math.Max(mn, Math.Min(mx, v));
    }
}
