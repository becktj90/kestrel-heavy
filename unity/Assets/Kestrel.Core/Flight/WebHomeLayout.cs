// Home composition. Portrait is the phone column (controls under the rocket). Landscape is a
// hero on the left (wordmark over the rocket, stats under the wordmark) and the action cards
// in a column on the right. The wordmark and every card label are sized to the box they sit in,
// and the column stops above the film-reel bar so Codex is not cut off.
using System;

namespace Kestrel.Core.Flight
{
    public sealed class WebHomeType
    {
        public float Title, Tagline, Launch, LaunchCaption, Design, DesignCaption;
        public float TileName, TileCaption, Soon, Stat, Hint;
        public int LaunchCaptionLines, DesignCaptionLines, TileCaptionLines, HintLines;
    }

    public sealed class WebHomeFrame
    {
        public bool Wide;
        /// <summary>True when a readable card stack is taller than the column, so the menu scrolls.</summary>
        public bool Scroll;
        public WebHomeType Type;
        public HudBox Rocket, Menu, Title, Stats, Launch, Design, Missions, Codex, Gear, Exit, Audio, Hint;
    }

    /// <summary>Advances at 100px, including the tracking the wide layout applies.
    /// Fraunces Black / Bold and Space Grotesk Bold / Medium, the shipped menu subsets.</summary>
    public static class HomeWideText
    {
        public const float Kestrel = 517.85f;
        public const float Heavy = 558.60f;
        public const float Launch = 497.55f;
        public const float LaunchCaption = 1232.30f;       // "Next flight, then the pad."
        public const float Design = 777.65f;               // "DESIGN FIRST", Fraunces Bold, 0.03em
        public const float DesignCaption = 1223.70f;       // "Your rocket, this mission."
        public const float Missions = 545.25f;
        public const float Codex = 385.55f;
        public const float Soon = 322.80f;
        public const float MissionsCaption = 1548.00f;     // "Pick any flight you've unlocked."
        public const float CodexCaption = 1458.80f;        // "Parts, places and flight notes."
        public const float Fuel = 422.40f;
        public const float Thrust = 677.00f;
        public const float Hint = 3844.30f;                // Thrum's opening line, Space Grotesk Medium, no tracking

        public static float Width(float at100, float font) => at100 * font / 100f;

        public static int Lines(float at100, float font, float inner)
        {
            if (!(inner > 1f) || !(font > 0f)) return 99;
            float w = Width(at100, font);
            if (w <= inner + 0.05f) return 1;
            return Math.Max(1, (int)Math.Ceiling(w / inner));
        }
    }

    public static class WebHomeLayout
    {
        public const float Gap = 8f;
        public const float Edge = 12f;
        public const float ReelFrac = 0.06f;
        public const float ReelMin = 24f;
        public const float Line = 1.25f;
        // Chrome subtracted from a card before text is measured. Slack covers the USS border
        // (panel px) being a hair wider than the scaled layout px, plus glyph overhang.
        public const float Slack = 6f;
        public const float LaunchChrome = 3f * 2f + 12f * 2f + 16f + 12f + Slack;
        public const float DesignChrome = 2f * 2f + 12f * 2f + 16f + 12f + Slack;
        public const float TileChrome = 3f * 2f + 10f * 2f + Slack;
        public const float LampSlot = 16f;
        public const float SoonPad = 18f;
        /// <summary>Below this width the gear stacks under the page FULL button so they cannot share pixels.</summary>
        public const float NarrowGear = 520f;

        public static float Reel(float height, float safeInset) =>
            Math.Max(height * ReelFrac, Math.Max(0f, safeInset) + ReelMin);

        public static WebHomeFrame Place(float width, float height, float safeTop, float safeBottom, float safeLeft, float safeRight)
        {
            var f = new WebHomeFrame { Wide = WebLayoutMode.IsLandscape(width, height) };
            safeTop = Math.Max(0f, safeTop);
            safeBottom = Math.Max(0f, safeBottom);
            safeLeft = Math.Max(0f, safeLeft);
            safeRight = Math.Max(0f, safeRight);
            // Same corner contract as HomeCornerLayout (reserveExit), shifted inside the safe area
            // so a landscape notch does not push EXIT off the screen. A narrow portrait stacks the
            // gear under FULL: a side-by-side pair overlaps the gear on a ~335px phone.
            float exitX = width - safeRight - HomeCornerLayout.Edge - HomeCornerLayout.ExitW;
            float gearX = exitX - HomeCornerLayout.Edge - HomeCornerLayout.Gear;
            f.Exit = Rect(exitX, safeTop, HomeCornerLayout.ExitW, HomeCornerLayout.ExitH);
            bool stackGear = !f.Wide && width < NarrowGear;
            f.Gear = stackGear
                ? Rect(width - safeRight - HomeCornerLayout.Edge - HomeCornerLayout.Gear,
                    f.Exit.Bottom + HomeCornerLayout.Edge, HomeCornerLayout.Gear, HomeCornerLayout.Gear)
                : Rect(gearX, safeTop, HomeCornerLayout.Gear, HomeCornerLayout.Gear);
            f.Audio = Rect(safeLeft + HomeCornerLayout.Edge, safeTop, HomeCornerLayout.AudioW, HomeCornerLayout.AudioH);

            float topBar = Math.Max(f.Audio.Bottom, Math.Max(f.Gear.Bottom, f.Exit.Bottom));
            float bottom = height - safeBottom;
            float left = safeLeft;
            float right = width - safeRight;
            if (!f.Wide)
            {
                PlacePhone(f, left, right, topBar, bottom);
                return f;
            }
            // Film-reel bars sit on the screen edge. Cards and the wordmark start below the top
            // bar and stop above the bottom bar, even when the safe inset is smaller than the bar.
            float contentTop = Math.Max(topBar, Reel(height, safeTop));
            float contentBottom = height - Reel(height, safeBottom);
            PlaceWide(f, width, left, right, contentTop, contentBottom);
            return f;
        }

        static void PlacePhone(WebHomeFrame f, float left, float right, float topBar, float bottom)
        {
            float innerL = left + Edge;
            float innerW = Math.Max(1f, right - left - 2f * Edge);
            float tileH = 76f, launchH = 64f, designH = 48f, statsH = 58f, hintH = 36f;
            float stack = statsH + Gap + launchH + Gap + designH + Gap + tileH + Gap + hintH;
            float y = bottom - Edge - stack;
            f.Stats = Rect(innerL, y, innerW, statsH); y += statsH + Gap;
            f.Launch = Rect(innerL, y, innerW, launchH); y += launchH + Gap;
            f.Design = Rect(innerL, y, innerW, designH); y += designH + Gap;
            float tileW = (innerW - Gap) * 0.5f;
            f.Missions = Rect(innerL, y, tileW, tileH);
            f.Codex = Rect(innerL + tileW + Gap, y, tileW, tileH);
            y += tileH + Gap;
            f.Hint = Rect(innerL, y, innerW, hintH);
            f.Menu = Rect(innerL, f.Stats.Y, innerW, f.Hint.Bottom - f.Stats.Y);
            float titleH = 64f;
            f.Title = Rect(innerL, topBar + Gap, innerW, titleH);
            float rocketY = f.Title.Bottom + Gap;
            f.Rocket = Rect(innerL, rocketY, innerW, Math.Max(1f, f.Menu.Y - Gap - rocketY));
        }

        static void PlaceWide(WebHomeFrame f, float width, float left, float right, float topBar, float bottom)
        {
            float cx = width * 0.5f;
            float rocketW = Clamp(width * 0.30f, 180f, 560f);
            float menuW = Clamp(width * 0.28f, 220f, 420f);
            float statsW = Clamp(width * 0.20f, 150f, 300f);
            float avail = right - left;
            float need = statsW + rocketW + menuW + Edge * 4f;
            if (need > avail)
            {
                float over = need - avail;
                rocketW = Math.Max(140f, rocketW - over * 0.40f);
                menuW = Math.Max(168f, menuW - over * 0.35f);
                statsW = Math.Max(96f, avail - rocketW - menuW - Edge * 4f);
            }

            float rocketX = cx - rocketW * 0.5f;
            float menuX = rocketX + rocketW + Edge;
            float menuLimit = right - Edge;
            if (menuX + menuW > menuLimit)
            {
                menuX = menuLimit - menuW;
                float rocketRight = menuX - Edge;
                rocketW = Math.Max(120f, Math.Min(rocketW, rocketRight - rocketX));
                rocketX = cx - rocketW * 0.5f;
                if (rocketX + rocketW + Edge > menuX)
                    rocketX = menuX - Edge - rocketW;
            }
            float statsX = left + Edge;
            if (statsX + statsW > rocketX - Edge)
                statsW = Math.Max(72f, rocketX - Edge - statsX);

            float y0 = topBar + Edge;
            float y1 = bottom - Edge;
            if (y1 < y0) y1 = y0;
            float span = Math.Max(1f, y1 - y0);

            // Wordmark spans the hero (left rail through the rocket) and stops before the cards.
            float titleLeft = left + Edge;
            float titleRight = menuX - Edge;
            if (titleRight < titleLeft + 48f) titleRight = titleLeft + 48f;
            float titleW = titleRight - titleLeft;
            float titleInner = Math.Max(1f, titleW - 8f);
            float titleFont = Fit(titleInner, HomeWideText.Kestrel, 72f, 16f);
            float tagFont = Fit(titleInner * 0.62f, HomeWideText.Heavy, 26f, 11f);
            // 14px under the top of the wordmark box for the teal eyebrow. The faces stay the measured block.
            const float Brow = 14f;
            float titleH = TitleBlock(titleFont, tagFont) + Brow;
            float maxTitle = Math.Max(48f, span * 0.30f);
            if (titleH > maxTitle)
                titleH = FitTitleBlock(ref titleFont, ref tagFont, Math.Max(1f, maxTitle - Brow)) + Brow;
            float minRocket = Math.Min(100f, span * 0.28f);
            if (y0 + titleH + Gap + minRocket > y1)
            {
                float cap = Math.Max(32f, y1 - y0 - Gap - minRocket);
                titleH = FitTitleBlock(ref titleFont, ref tagFont, Math.Max(1f, cap - Brow)) + Brow;
                if (titleH > cap) titleH = cap;
            }
            f.Title = Rect(titleLeft, y0, titleW, titleH);
            float rocketY = f.Title.Bottom + Gap;
            f.Rocket = Rect(rocketX, rocketY, rocketW, Math.Max(1f, y1 - rocketY));

            float statsTop = f.Title.Bottom + Gap;
            float statsRoom = Math.Max(1f, y1 - statsTop);
            float statsH = Clamp(statsRoom * 0.28f, 32f, 64f);
            if (statsTop + statsH > y1) statsH = Math.Max(24f, y1 - statsTop);
            float statsY = statsTop + Math.Max(0f, (Math.Min(statsRoom, f.Rocket.H) - statsH) * 0.42f);
            if (statsY + statsH > y1) statsY = Math.Max(statsTop, y1 - statsH);
            f.Stats = Rect(statsX, statsY, statsW, statsH);

            var type = new WebHomeType { Title = titleFont, Tagline = tagFont };
            float launchInner = Math.Max(1f, menuW - LaunchChrome);
            float designInner = Math.Max(1f, menuW - DesignChrome);
            float tileInner = Math.Max(1f, menuW - TileChrome);
            float hintInner = Math.Max(1f, menuW - 16f);
            type.Launch = Fit(launchInner, HomeWideText.Launch, 32f, 13f);
            type.LaunchCaption = FitCaption(launchInner, HomeWideText.LaunchCaption, 16f, 10f, out type.LaunchCaptionLines);
            type.Design = Fit(designInner, HomeWideText.Design, 24f, 12f);
            type.DesignCaption = FitCaption(designInner, HomeWideText.DesignCaption, 15f, 10f, out type.DesignCaptionLines);
            type.Soon = Fit(tileInner * 0.34f, HomeWideText.Soon, 13f, 9f);
            float soonW = HomeWideText.Width(HomeWideText.Soon, type.Soon) + SoonPad;
            float nameInner = Math.Max(1f, tileInner - Math.Max(LampSlot, soonW) - 8f);
            float missionsName = Fit(nameInner, HomeWideText.Missions, 22f, 11f);
            float codexName = Fit(Math.Max(1f, tileInner - soonW - 8f), HomeWideText.Codex, 22f, 11f);
            type.TileName = Math.Min(missionsName, codexName);
            float capAt = Math.Max(HomeWideText.MissionsCaption, HomeWideText.CodexCaption);
            type.TileCaption = FitCaption(tileInner, capAt, 15f, 10f, out type.TileCaptionLines);
            type.Hint = FitCaption(hintInner, HomeWideText.Hint, 13f, 9f, out type.HintLines);
            float statInner = Math.Max(1f, statsW - 16f);
            type.Stat = Fit(Math.Max(1f, statInner - Gap), HomeWideText.Fuel + HomeWideText.Thrust, 15f, 8f);
            float statNeed = type.Stat * Line + 22f;
            if (f.Stats.H + 0.6f < statNeed)
            {
                float h = Math.Min(statNeed, Math.Max(f.Stats.H, y1 - f.Stats.Y));
                float yStats = f.Stats.Y;
                if (yStats + h > y1) yStats = Math.Max(statsTop, y1 - h);
                f.Stats = Rect(f.Stats.X, yStats, f.Stats.W, h);
            }

            float launchNat = type.Launch, launchCapNat = type.LaunchCaption;
            float designNat = type.Design, designCapNat = type.DesignCaption;
            float tileNat = type.TileName, tileCapNat = type.TileCaption;
            float hintH = Math.Max(16f, type.HintLines * type.Hint * Line + 4f);
            float gaps = Gap * 4f;
            float body = span - hintH - gaps;
            // Largest type that still fits the column. Floors match the readable sizes the
            // layout test requires; below that the column scrolls instead of clipping Codex.
            float fit = 1f;
            if (ColumnStack(type, 1f, launchNat, launchCapNat, designNat, designCapNat, tileNat, tileCapNat, launchInner, designInner, tileInner) > body)
            {
                float lo = 0f, hi = 1f;
                for (int i = 0; i < 18; i++)
                {
                    float mid = (lo + hi) * 0.5f;
                    float at = ColumnStack(type, mid, launchNat, launchCapNat, designNat, designCapNat, tileNat, tileCapNat, launchInner, designInner, tileInner);
                    if (at <= body) lo = mid;
                    else hi = mid;
                }
                fit = lo;
            }
            float launchH, designH, tileH;
            float stack = ColumnStack(type, fit, launchNat, launchCapNat, designNat, designCapNat, tileNat, tileCapNat, launchInner, designInner, tileInner, out launchH, out designH, out tileH);
            if (stack > body + 0.6f)
            {
                f.Scroll = true;
                float scale = Math.Max(0.15f, body / Math.Max(1f, stack));
                launchH *= scale;
                designH *= scale;
                tileH *= scale;
                // The boxes shrank. Keep the type inside them or the captions clip.
                type.Launch *= scale;
                type.LaunchCaption *= scale;
                type.Design *= scale;
                type.DesignCaption *= scale;
                type.TileName *= scale;
                type.TileCaption *= scale;
                type.Soon *= scale;
            }
            else if (stack > 1f)
            {
                float extra = body - stack;
                tileH += extra * 0.5f;
            }

            float y = y0;
            f.Launch = Rect(menuX, y, menuW, Math.Max(1f, launchH)); y += f.Launch.H + Gap;
            f.Design = Rect(menuX, y, menuW, Math.Max(1f, designH)); y += f.Design.H + Gap;
            f.Missions = Rect(menuX, y, menuW, Math.Max(1f, tileH)); y += f.Missions.H + Gap;
            f.Codex = Rect(menuX, y, menuW, Math.Max(1f, tileH)); y += f.Codex.H + Gap;
            float hintY = y1 - hintH;
            if (hintY < y) hintY = y;
            if (hintY + hintH > y1) hintH = Math.Max(1f, y1 - hintY);
            float hintNeed = type.HintLines * type.Hint * Line + 4f;
            if (hintH + 0.6f < hintNeed && hintNeed > 0f) type.Hint *= hintH / hintNeed;
            f.Hint = Rect(menuX, hintY, menuW, hintH);
            f.Menu = Rect(menuX, y0, menuW, Math.Max(1f, y1 - y0));
            f.Type = type;
        }

        /// <summary>Null when every wide-home string fits its box. Portrait frames are not checked.</summary>
        public static string TextFitError(WebHomeFrame f)
        {
            if (f == null || !f.Wide || f.Type == null) return "wide type missing";
            var t = f.Type;
            float titleInner = Math.Max(1f, f.Title.W - 8f);
            if (HomeWideText.Width(HomeWideText.Kestrel, t.Title) > titleInner + 0.6f) return "KESTREL wider than the title";
            if (HomeWideText.Width(HomeWideText.Heavy, t.Tagline) > titleInner * 0.7f + 0.6f) return "HEAVY wider than the rule row";
            if (t.Title * 1.05f + 6f + t.Tagline * Line > f.Title.H + 0.6f) return "title block taller than its box";

            string card = CardFit(f.Launch, t.Launch, HomeWideText.Launch, t.LaunchCaption, HomeWideText.LaunchCaption, t.LaunchCaptionLines, LaunchChrome, 8f, 5f, "launch");
            if (card != null) return card;
            card = CardFit(f.Design, t.Design, HomeWideText.Design, t.DesignCaption, HomeWideText.DesignCaption, t.DesignCaptionLines, DesignChrome, 6f, 2f, "design");
            if (card != null) return card;

            float tileInner = Math.Max(1f, f.Missions.W - TileChrome);
            float soonW = HomeWideText.Width(HomeWideText.Soon, t.Soon) + SoonPad;
            float nameInner = Math.Max(1f, tileInner - Math.Max(LampSlot, soonW) - 8f);
            if (HomeWideText.Width(HomeWideText.Missions, t.TileName) > nameInner + 0.6f) return "MISSIONS wider than the tile";
            if (HomeWideText.Width(HomeWideText.Codex, t.TileName) > Math.Max(1f, tileInner - soonW - 8f) + 0.6f) return "CODEX wider than the tile";
            if (t.TileCaptionLines > 2) return "tile caption wraps past two lines";
            if (HomeWideText.Lines(HomeWideText.MissionsCaption, t.TileCaption, tileInner) > t.TileCaptionLines) return "missions caption does not fit";
            if (HomeWideText.Lines(HomeWideText.CodexCaption, t.TileCaption, tileInner) > t.TileCaptionLines) return "codex caption does not fit";
            float tileBlock = Block(t.TileName, t.TileCaption, t.TileCaptionLines, 10f, 3f);
            if (tileBlock > f.Missions.H + 0.6f) return "missions text taller than the card";
            if (tileBlock > f.Codex.H + 0.6f) return "codex text taller than the card";

            float statInner = Math.Max(1f, f.Stats.W - 16f);
            float stats = HomeWideText.Width(HomeWideText.Fuel, t.Stat) + Gap + HomeWideText.Width(HomeWideText.Thrust, t.Stat);
            if (stats > statInner + 0.6f) return "fuel and thrust wider than the stats card";
            if (t.Stat * Line > f.Stats.H + 0.6f) return "stats text taller than the card";

            float hintInner = Math.Max(1f, f.Hint.W - 16f);
            if (t.HintLines > 2) return "hint wraps past two lines";
            if (HomeWideText.Lines(HomeWideText.Hint, t.Hint, hintInner) > t.HintLines) return "hint wider than its row";
            if (t.HintLines * t.Hint * Line + 4f > f.Hint.H + 0.6f) return "hint taller than its row";
            return null;
        }

        static string CardFit(HudBox box, float label, float labelAt, float caption, float captionAt, int lines, float chrome, float padY, float border, string name)
        {
            float inner = Math.Max(1f, box.W - chrome);
            if (HomeWideText.Width(labelAt, label) > inner + 0.6f) return name + " label wider than the card";
            if (lines < 1 || lines > 2) return name + " caption line count";
            if (HomeWideText.Lines(captionAt, caption, inner) > lines) return name + " caption does not fit";
            if (Block(label, caption, lines, padY, border) > box.H + 0.6f) return name + " text taller than the card";
            return null;
        }

        static float TitleBlock(float titleFont, float tagFont) =>
            titleFont * 1.05f + 6f + tagFont * Line;

        /// <summary>Shrinks the wordmark so the gap between the faces stays 6px and the block fits the box.</summary>
        static float FitTitleBlock(ref float titleFont, ref float tagFont, float boxH)
        {
            float faces = titleFont * 1.05f + tagFont * Line;
            float room = Math.Max(1f, boxH - 6f);
            if (faces > room && faces > 0f)
            {
                float s = room / faces;
                titleFont *= s;
                tagFont *= s;
            }
            return Math.Min(TitleBlock(titleFont, tagFont), boxH);
        }

        static float ColumnStack(WebHomeType type, float fit,
            float launchNat, float launchCapNat, float designNat, float designCapNat,
            float tileNat, float tileCapNat, float launchInner, float designInner, float tileInner)
        {
            return ColumnStack(type, fit, launchNat, launchCapNat, designNat, designCapNat, tileNat, tileCapNat,
                launchInner, designInner, tileInner, out _, out _, out _);
        }

        static float ColumnStack(WebHomeType type, float fit,
            float launchNat, float launchCapNat, float designNat, float designCapNat,
            float tileNat, float tileCapNat, float launchInner, float designInner, float tileInner,
            out float launchH, out float designH, out float tileH)
        {
            type.Launch = LerpFloor(13f, launchNat, fit);
            type.LaunchCaption = LerpFloor(10f, launchCapNat, fit);
            type.LaunchCaptionLines = CappedLines(HomeWideText.LaunchCaption, type.LaunchCaption, launchInner);
            type.Design = LerpFloor(12f, designNat, fit);
            type.DesignCaption = LerpFloor(10f, designCapNat, fit);
            type.DesignCaptionLines = CappedLines(HomeWideText.DesignCaption, type.DesignCaption, designInner);
            type.TileName = LerpFloor(11f, tileNat, fit);
            type.TileCaption = LerpFloor(10f, tileCapNat, fit);
            float capAt = Math.Max(HomeWideText.MissionsCaption, HomeWideText.CodexCaption);
            type.TileCaptionLines = CappedLines(capAt, type.TileCaption, tileInner);
            // Launch USS is a 1px top rule plus an 8px bottom lip. 2*5 covers that height.
            launchH = Block(type.Launch, type.LaunchCaption, type.LaunchCaptionLines, 8f, 5f);
            designH = Block(type.Design, type.DesignCaption, type.DesignCaptionLines, 6f, 2f);
            tileH = Block(type.TileName, type.TileCaption, type.TileCaptionLines, 10f, 3f);
            return launchH + designH + tileH * 2f;
        }

        static int CappedLines(float at100, float font, float inner)
        {
            int lines = HomeWideText.Lines(at100, font, inner);
            if (lines < 1) return 1;
            return lines > 2 ? 2 : lines;
        }

        static float LerpFloor(float min, float nat, float t)
        {
            if (nat < min) nat = min;
            return min + (nat - min) * Clamp(t, 0f, 1f);
        }

        static float FitCaption(float inner, float at100, float max, float min, out int lines)
        {
            float font = Fit(inner, at100, max, min);
            lines = HomeWideText.Lines(at100, font, inner);
            if (lines <= 2) return font;
            font = Fit(inner * 2f, at100, max, min);
            lines = Math.Min(2, Math.Max(1, HomeWideText.Lines(at100, font, inner)));
            return font;
        }

        static float Block(float label, float caption, int captionLines, float padY, float border) =>
            2f * border + 2f * padY + label * Line + 2f + Math.Max(1, captionLines) * caption * Line;

        static float Fit(float inner, float at100, float max, float min)
        {
            if (!(at100 > 0f)) return min;
            return Clamp(100f * Math.Max(1f, inner) / at100, min, max);
        }

        static float Clamp(float v, float lo, float hi) => Math.Max(lo, Math.Min(hi, v));

        static HudBox Rect(float x, float y, float w, float h) => new HudBox { X = x, Y = y, W = w, H = h };
    }
}
