// Guided stepper in a column: the landscape web parts column (WebDesignerLayout.Parts) and the phone
// sheet. Points, origin top-left of the column. The live UI Toolkit tree follows these numbers
// (segment size, tiles per row, nav and tab wrapping, check card placement); the Core tests check the
// same frame for overlaps and text fit at 1280x720, 1366x768, 1920x1080 and 335 portrait.
//
// Web overlap audit (beckify.com, 50ae4f3): every block in the column was flex-shrink 1, so in a
// 300 pt column the header, the tiles, the check card and the confirm card were squeezed below their
// text and drew over each other. Here every block keeps its real height; only the scroller gives way,
// and when even the minimum scroller does not fit, the check card moves into the scroller.
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;

namespace Kestrel.Core.Designer.Stepper
{
    /// <summary>Advances at 100 pt: the widest of Space Grotesk Bold/Medium and Fraunces Bold (the shipped faces).</summary>
    public static class StepperText
    {
        /// <summary>Widest step tile, glyph + short name ("OK Recovery").</summary>
        public const float Segment = 641.2f;
        public const float Simple = 340.2f, Engineer = 443.8f, AllParts = 419.9f;
        public const float Back = 352.1f, NextAnyway = 634.6f, FixIt = 244.3f, Next = 230.2f, LaunchAnyway = 764.3f;
        public const float Stay = 225.9f, GoOn = 280.9f;
        public const float Parts = 268.7f, Undo = 261.8f, Redo = 253.8f, Hangar = 372.9f, Designer = 442.8f;
        public const float GuidedSteps = 638.5f, Launch = 366.6f;
        public const float Count = 584.1f;                 // "STEP 7 OF 7"
        public const float LongestTitle = 1041.6f;         // "Propellant & engines"
        public const float Pill = 728.6f;                  // "12 things to fix"
        /// <summary>Average advance per character for wrapped body copy (Space Grotesk Medium, mixed case).</summary>
        public const float BodyChar = 56f;

        public static float Width(float advance100, float pt) => advance100 * pt / 100f;

        /// <summary>Lines a wrapped string of <paramref name="chars"/> characters takes at <paramref name="pt"/> in <paramref name="width"/>.</summary>
        public static int Lines(int chars, float pt, float width)
        {
            if (chars <= 0) return 0;
            float w = chars * BodyChar * pt / 100f;
            // Word breaks waste up to a word per line; 0.85 fill is the measured worst case for the check copy.
            float per = Math.Max(1f, width * 0.85f);
            return Math.Max(1, (int)Math.Ceiling(w / per));
        }
    }

    public sealed class StepperColumnFrame
    {
        public float Width, Height, Inner;
        public int TilesPerRow, TileRows;
        public float TileW, TileH, TilePt, TilePad;
        public bool TabsOwnRow, NavWraps, CheckInScroll;
        public int TitleLines, CheckLines;
        public HudBox Count, Title, Tabs, Simple, Engineer, AllParts, Tiles, Check, Confirm, Scroll, Nav, Back, NextAnyway, Forward;
        public readonly List<HudBox> TileBoxes = new List<HudBox>();
        public float ScrollH => Scroll.H;
    }

    public static class StepperColumnLayout
    {
        // HangarUi: 44 pt targets, 16 pt body, 13 pt small, 18 + 4 pt title, 12 pt button padding, 3 pt margins.
        public const float TargetPt = 44f, BodyPt = 16f, SmallPt = 13f, TitlePt = 22f;
        public const float ButtonPad = 12f, ButtonMargin = 3f, CardPad = 10f, CardMargin = 4f;
        public const float NavH = 52f, NavMargin = 4f;
        public const float Line = 1.3f;
        /// <summary>Tile labels stay at 13 pt (LANDSCAPE_WEB_BRIEF tile minimum).</summary>
        public const float TilePtMin = 13f;
        public const float TileHPt = 44f, TileGap = 2f, TilePadPt = 8f, TileBorder = 3f;
        /// <summary>Below this the check card leaves the sticky stack and scrolls with the controls.</summary>
        public const float MinScrollPt = 120f;
        public const float Gap = 4f;

        /// <summary>
        /// <paramref name="width"/>, <paramref name="height"/>: the stepper root in points (inside the design panel padding).
        /// <paramref name="checkChars"/>, <paramref name="extraChars"/>: the check headline and detail copy.
        /// </summary>
        public static StepperColumnFrame Solve(float width, float height, string title, int checkChars, int extraChars, bool confirm, bool nextAnyway)
        {
            var f = new StepperColumnFrame { Width = width, Height = height, Inner = Math.Max(1f, width) };
            float w = f.Inner;
            float y = 0f;

            // Row 1: "STEP 3 OF 7". Row 2: the step title, wrapped on its own row.
            float countH = SmallPt * Line;
            f.Count = Box(0f, y, Math.Min(w, StepperText.Width(StepperText.Count, SmallPt)), countH);
            y += countH + Gap;
            float titleW = StepperText.Width(TitleAdvance(title), TitlePt);
            f.TitleLines = Math.Max(1, (int)Math.Ceiling(titleW / Math.Max(1f, w)));
            float titleH = f.TitleLines * TitlePt * Line;
            f.Title = Box(0f, y, w, titleH);
            y += titleH + Gap;

            // Row 3: Simple | Engineer toggle and All parts. Wraps onto two rows rather than squeezing a word.
            float simpleW = Btn(StepperText.Simple, BodyPt, 64f);
            float engW = Btn(StepperText.Engineer, BodyPt, 72f);
            float allW = Btn(StepperText.AllParts, BodyPt, 72f);
            float depthW = simpleW + engW + 4f * ButtonMargin + 4f;
            f.TabsOwnRow = depthW + allW + 2f * ButtonMargin <= w;
            float tabsH = f.TabsOwnRow ? TargetPt + 2f * ButtonMargin : 2f * (TargetPt + 2f * ButtonMargin);
            f.Tabs = Box(0f, y, w, tabsH);
            f.Simple = Box(2f + ButtonMargin, y + ButtonMargin, simpleW, TargetPt);
            f.Engineer = Box(f.Simple.Right + 2f * ButtonMargin, y + ButtonMargin, engW, TargetPt);
            f.AllParts = f.TabsOwnRow
                ? Box(w - allW - ButtonMargin, y + ButtonMargin, allW, TargetPt)
                : Box(ButtonMargin, y + TargetPt + 3f * ButtonMargin, allW, TargetPt);
            y += tabsH + Gap;

            // Step tiles at 13 pt, as many per row as fit, never fewer than two.
            f.TilePt = TilePtMin;
            f.TilePad = TilePadPt;
            f.TileW = StepperText.Width(StepperText.Segment, f.TilePt) + 2f * f.TilePad + TileBorder;
            f.TilesPerRow = Math.Max(2, Math.Min(StepperCatalog.Count, (int)Math.Floor((w + TileGap) / (f.TileW + TileGap))));
            f.TileRows = (StepperCatalog.Count + f.TilesPerRow - 1) / f.TilesPerRow;
            f.TileH = TileHPt;
            float cell = (w - (f.TilesPerRow - 1) * TileGap) / f.TilesPerRow;
            for (int i = 0; i < StepperCatalog.Count; i++)
            {
                int r = i / f.TilesPerRow, c = i % f.TilesPerRow;
                int inRow = Math.Min(f.TilesPerRow, StepperCatalog.Count - r * f.TilesPerRow);
                float cw = (w - (inRow - 1) * TileGap) / inRow;
                if (cw < cell) cw = cell;
                f.TileBoxes.Add(Box(c * (cw + TileGap), y + r * (f.TileH + TileGap), cw, f.TileH));
            }
            float tilesH = f.TileRows * f.TileH + (f.TileRows - 1) * TileGap;
            f.Tiles = Box(0f, y, w, tilesH);
            y += tilesH + Gap;

            // Bottom up: nav, then the confirm card above it.
            float backW = Btn(StepperText.Back, BodyPt, 88f);
            float nextW = Btn(StepperText.NextAnyway, BodyPt, 120f);
            float fwdW = Btn(StepperText.FixIt, BodyPt, 120f);
            float navNeed = backW + (nextAnyway ? nextW + 2f * ButtonMargin : 0f) + fwdW + 4f * ButtonMargin;
            f.NavWraps = navNeed > w;
            float navH = f.NavWraps ? 2f * NavH + 3f * NavMargin : NavH + 2f * NavMargin;
            float navY = height - navH;
            f.Nav = Box(0f, navY, w, navH);
            float ny = navY + NavMargin;
            f.Back = Box(ButtonMargin, ny, backW, NavH);
            if (nextAnyway)
            {
                f.NextAnyway = Box(f.Back.Right + 2f * ButtonMargin, ny, nextW, NavH);
                if (f.NavWraps)
                    f.Forward = Box(ButtonMargin, ny + NavH + NavMargin, w - 2f * ButtonMargin, NavH);
                else
                    f.Forward = Box(f.NextAnyway.Right + 2f * ButtonMargin, ny, w - f.NextAnyway.Right - 3f * ButtonMargin, NavH);
            }
            else
                f.Forward = Box(f.Back.Right + 2f * ButtonMargin, ny, Math.Max(fwdW, w - f.Back.Right - 3f * ButtonMargin), NavH);

            float bottom = navY;
            if (confirm)
            {
                int lines = StepperText.Lines(44, BodyPt, w - 2f * CardPad);   // "This step still has a warning. Go on anyway?"
                float h = 2f * CardPad + lines * BodyPt * Line + TargetPt + 2f * ButtonMargin + 6f;
                f.Confirm = Box(0f, bottom - CardMargin - h, w, h);
                bottom = f.Confirm.Y - CardMargin;
            }

            // The check card: section + wrapped headline + wrapped detail, at its real height.
            float textW = w - 2f * CardPad;
            int body = StepperText.Lines(checkChars, BodyPt, textW);
            int extra = StepperText.Lines(extraChars, SmallPt, textW);
            f.CheckLines = body + extra;
            float checkH = 2f * CardPad + 10f + SmallPt * Line + 2f + body * BodyPt * Line + (extra > 0 ? 4f + extra * SmallPt * Line : 0f);
            float room = bottom - y;
            f.CheckInScroll = room - (checkH + 2f * CardMargin) < MinScrollPt;
            if (!f.CheckInScroll)
            {
                f.Check = Box(0f, y + CardMargin, w, checkH);
                y = f.Check.Bottom + CardMargin;
                f.Scroll = Box(0f, y, w, Math.Max(0f, bottom - y));
            }
            else
            {
                f.Scroll = Box(0f, y, w, Math.Max(0f, bottom - y));
                // First item inside the scroller; it scrolls away with the controls.
                f.Check = Box(0f, y + CardMargin, w, Math.Min(checkH, Math.Max(0f, f.Scroll.H - 2f * CardMargin)));
            }
            return f;
        }

        /// <summary>Button width: label + 12 pt padding each side, at least the minimum.</summary>
        public static float Btn(float advance100, float pt, float minPt) => Math.Max(minPt, StepperText.Width(advance100, pt) + 2f * ButtonPad);

        static float TitleAdvance(string title)
        {
            if (string.IsNullOrEmpty(title)) return 0f;
            // Fraunces Bold title copy: measured titles, else the per-character average with the display face's 10% extra.
            switch (title)
            {
                case "Propellant & engines": return 1041.6f;
                case "Tanks & stages": return 753.2f;
                case "Aero & fairing": return 712.3f;
                case "Mission goal": return 610.9f;
                case "Review & launch": return 832.0f;
                default: return title.Length * StepperText.BodyChar * 1.1f;
            }
        }

        static HudBox Box(float x, float y, float w, float h) => new HudBox { X = x, Y = y, W = w, H = h };
    }
}
