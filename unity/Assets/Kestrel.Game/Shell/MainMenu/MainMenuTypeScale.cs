// One place for every size on the main menu. Pure maths (no UnityEngine), so EditMode tests can check each device.
//   size = clamp(min, k * s * base, max),  s = min(width / 390, usableHeight / 844),  usableHeight = height - top - bottom safe insets
// Metrics (MainMenuMetrics) are the real advance widths of the shipped subset fonts, so "fits" is checked, not guessed.
using System;

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
        public const float HitMin = 44f;

        public readonly struct Spec
        {
            public readonly float Base, Min, Max;
            public Spec(float b, float mn, float mx) { Base = b; Min = mn; Max = mx; }
            public float At(float s, float k = 1f) => Math.Max(Min, Math.Min(Max, k * s * Base));
        }
        // base is the size on a 390x844 phone. Captions never go below 11.
        public static readonly Spec Title = new Spec(76f, 28f, 84f);
        public static readonly Spec Tagline = new Spec(20f, 13f, 30f);
        public static readonly Spec LaunchLabel = new Spec(26f, 20f, 38f);
        public static readonly Spec LaunchCaption = new Spec(14f, 11f, 20f);
        public static readonly Spec TileName = new Spec(19f, 13f, 26f);
        public static readonly Spec TileCaption = new Spec(13f, 11f, 18f);
        public static readonly Spec Soon = new Spec(11f, 10f, 13f);
        public static readonly Spec Hint = new Spec(11f, 10f, 14f);
        public const float CaptionFloor = 11f;

        public static float Scale(float w, float h, float topInset = 0f, float bottomInset = 0f)
        {
            float usable = Math.Max(1f, h - Math.Max(0f, topInset) - Math.Max(0f, bottomInset));
            return Math.Max(0.1f, Math.Min(w / RefW, usable / RefH));
        }

        public struct Sizes
        {
            public float Scale, Title, Tagline, LaunchLabel, LaunchCaption, TileName, TileCaption, Soon, Hint;
            public float Pad, TileW, TileInner, TileH, LaunchH, LaunchCaptionW, ControlsH, HintBand, BottomReserve, TitleBottom, ControlsTop, HeroLift;
            public int TileCaptionLines, LaunchCaptionLines;
        }

        public static float TextW(string s, float size) => MainMenuMetrics.W(s) * size / 100f;
        static int Lines(float textW, float avail) => Math.Max(1, (int)Math.Ceiling(textW * 1.08f / Math.Max(1f, avail)));   // 8% word-wrap slack

        public static Sizes Compute(float w, float h, float topInset = 0f, float bottomInset = 0f)
        {
            var z = new Sizes { Scale = Scale(w, h, topInset, bottomInset) };
            float s = z.Scale;
            z.Title = Title.At(s); z.Tagline = Tagline.At(s); z.LaunchLabel = LaunchLabel.At(s); z.LaunchCaption = LaunchCaption.At(s);
            z.TileName = TileName.At(s); z.TileCaption = TileCaption.At(s); z.Soon = Soon.At(s); z.Hint = Hint.At(s);
            // The title also has to fit the column on one line.
            float col = w - 2f * SideMargin - 4f;
            z.Title = Math.Max(Title.Min, Math.Min(z.Title, 100f * col / MainMenuMetrics.W("KESTREL")));
            z.Pad = Clamp(8f * s, 6f, 12f);
            float grid = w - 2f * SideMargin;
            z.TileW = grid * 0.485f;
            z.TileInner = z.TileW - 2f * TileBorder - 2f * z.Pad;
            int worstLines = 1;
            foreach (var t in MainMenuCopy.Tiles)
                worstLines = Math.Max(worstLines, Lines(TextW(t.Caption, z.TileCaption), z.TileInner));
            z.TileCaptionLines = worstLines;
            float head = Math.Max(22f, z.TileName * LineH);
            z.TileH = Math.Max(HitMin, 2f * TileBorder + 2f * z.Pad + head + 3f + worstLines * z.TileCaption * LineH);
            float padH = Clamp(14f * s, 10f, 18f);
            z.LaunchCaptionW = grid - 2f * LaunchBorder - 2f * padH - LampSlot - 12f;
            z.LaunchCaptionLines = Lines(TextW(MainMenuCopy.LaunchCaption, z.LaunchCaption), z.LaunchCaptionW);
            float padV = Clamp(8f * s, 6f, 10f);
            z.LaunchH = Math.Max(HitMin, 2f * LaunchBorder + 2f * padV + z.LaunchLabel * LineH + z.LaunchCaptionLines * z.LaunchCaption * LineH);
            // The bottom film-reel bar is BarHeight(h, inset) tall from the screen edge and covers the hint. The tiles must end above it,
            // so the reserve is the whole bar (it already includes the bottom safe inset), not just the inset. The last tile row's own
            // Gap margin is the breathing room above the bar.
            float bottom = Math.Max(0f, bottomInset);
            z.BottomReserve = MainMenuCopy.BarHeight(h, bottom);
            z.HintBand = Math.Max(0f, z.BottomReserve - bottom);   // root padding-bottom (the root already sits above the safe inset)
            z.ControlsH = z.LaunchH + Gap + 2f * z.TileH + 2f * Gap;
            z.ControlsTop = h - z.BottomReserve - z.ControlsH;
            z.TitleBottom = Math.Max(0f, topInset) + TopPad + z.Title * 1.3f + z.Tagline * LineH;
            z.HeroLift = HeroLift(h, z.TitleBottom, z.ControlsTop);
            return z;
        }

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
