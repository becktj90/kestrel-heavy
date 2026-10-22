// Phone-column Home (portrait iOS and portrait web), engine-free. MenuTypeScale (Kestrel.Game.Shell) takes its
// type sizes and block heights from here, so Kestrel.Core.Tests can check the real column: TestFlight Home
// (build 156/158) had three stats lines in a two-line card, a clipped "Your rocket, this mission", tile names on
// their lamps and a cut-off Thrum line, because the heights came from one-line width estimates and a 42% cap.
//
// Units are panel units. The iOS shell panel is a 375x667 reference matched to height (KH_ShellPanelSettings),
// so a 390x844 or 430x932 iPhone is a ~308x667 panel, not 390 wide. Device() does that mapping.
using System;
using Kestrel.Core.Features;

namespace Kestrel.Core.Flight
{
    public struct HomePhoneScreen
    {
        /// <summary>Whole panel, and the content-rect insets (safe area plus the 8 pt side margin).</summary>
        public float W, H, Top, Bottom, Left, Right;
        /// <summary>Panel units per iOS point.</summary>
        public float PerPoint;
        public float SafeW => W - Left - Right;
        public float SafeH => H - Top - Bottom;
    }

    public struct HomePhoneType
    {
        public float Scale, Title, Tagline, LaunchLabel, LaunchCaption, TileName, TileCaption, Soon, Hint, Pad, DesignLabel, DesignCaption, Stat;
    }

    public sealed class HomePhoneFrame
    {
        public HomePhoneType Type;
        public HomeRect Safe, Bar, Gear, Pop, Eyebrow, Title, Stats, Launch, Design, Missions, Codex, Thrum;
        public float Hero, StatFont, ThrumH;
        public int RecordLines, DesignCaptionLines, TileCaptionLines, ThrumLines, LaunchCaptionLines;
        public string Record;
        /// <summary>The primary CTA (STARTER_COPY.md §c): label and sub-line ("" for none).</summary>
        public string Cta, CtaCaption;
        public HomeShow Show;
        /// <summary>The words the strip shows (whole line or the short teaser).</summary>
        public string ThrumShown;
    }

    public static class HomePhoneLayout
    {
        public const float RefW = 390f, RefH = 844f;
        public const float ShellRefH = 667f;          // KH_ShellPanelSettings: 375x667, match = 1 (height)
        public const float SideMarginPt = 8f;         // PortraitLayout content rect
        public const float SideMargin = 12f;          // .kh-mm horizontal padding
        public const float Gap = 6f;
        public const float LineH = 1.4f;
        public const float TopPad = 40f;
        public const float CreditsBand = 44f + 4f;
        public const float HitMin = 44f;
        public const float GuidePopPt = 64f;          // GuidePopLayout.DisplayPt (16 px art x 4)
        public const float GuideMarginPt = 8f;

        // Chrome, from KH_Tokens.uss and the inline styles MainMenuOverlay.ApplyScale writes.
        public const float StatsBorder = 1f, StatsPadX = 10f, StatsPadY = 0f;   // the 1.4 line box carries the air; 1 left the full SE at 42.2%
        // .kh-mm-launch border-top 1 + the 6 px lip. The lip is absolute (bottom 0) inside the button and the button reserves it
        // as padding-bottom (padV + LipH), so it is chrome, not stack. The 8 px bottom border this used to count is gone.
        public const float LaunchBorderV = 1f + 6f;
        public const float LaunchChromeX = 16f + 12f + 36f + 6f;   // lamp + margin, column margin, 28 icon + 8, slack
        // DesignPadY 3 (was 5) pays for DesignGap: the SE controls stay what they were before the gap (ControlsH <= 42%
        // of the usable height, hero clear of LAUNCH), and the card still holds its 1.4 line boxes.
        public const float DesignBorder = 1f, DesignPadX = 12f, DesignPadY = 3f;
        /// <summary>Space between "DESIGN FIRST" and its caption (margin-top on .kh-mm-design-caption, phone column).
        /// Without it the label's ceil'd line box met the caption with 0 px between them, and TextCore's measured
        /// 18 px rect ran 1 px into the caption (GameCI, 390x844 / 430x932 full save).</summary>
        public const float DesignGap = 3f;
        public const float DesignChromeX = 2f * DesignBorder + 2f * DesignPadX + 16f + 12f + 30f + 6f;
        public const float TileBorder = 1f, TileIcon = 18f + 6f, TileLamp = 12f + 4f, TileAir = 2f, NameSlack = 4f;
        public const float ThrumPadX = 10f, ThrumPadY = 4f, ThrumPortrait = 22f + 8f, ThrumColMargin = 12f;

        public static float Clamp(float v, float mn, float mx) => Math.Max(mn, Math.Min(mx, v));
        static float At(float s, float b, float mn, float mx) => Math.Max(mn, Math.Min(mx, s * b));

        /// <summary>The shell panel for an iOS device in points (portrait).</summary>
        public static HomePhoneScreen Device(float ptW, float ptH, float safeTopPt, float safeBottomPt)
        {
            float k = ShellRefH / Math.Max(1f, ptH);
            return new HomePhoneScreen
            {
                W = ptW * k, H = ShellRefH, PerPoint = k,
                Top = safeTopPt * k, Bottom = safeBottomPt * k,
                Left = SideMarginPt * k, Right = SideMarginPt * k,
            };
        }

        public static float Scale(float w, float h, float top, float bottom)
        {
            float usable = Math.Max(1f, h - Math.Max(0f, top) - Math.Max(0f, bottom));
            return Math.Max(0.1f, Math.Min(w / RefW, usable / RefH));
        }

        /// <summary>Type for a column of width <paramref name="col"/> (root width minus its 12 px padding).</summary>
        public static HomePhoneType Type(float w, float h, float top, float bottom, float col)
        {
            var t = new HomePhoneType { Scale = Scale(w, h, top, bottom) };
            float s = t.Scale;
            t.Title = At(s, 76f, 28f, 84f);
            t.Tagline = At(s, 20f, 14f, 30f);
            t.LaunchLabel = At(s, 26f, 20f, 38f);
            t.LaunchCaption = At(s, 14f, 14f, 20f);
            t.TileName = At(s, 19f, 14f, 26f);
            t.TileCaption = At(s, 13f, 14f, 18f);
            t.Soon = At(s, 14f, 14f, 16f);
            t.Hint = At(s, 14f, 14f, 18f);
            t.Title = Math.Max(28f, Math.Min(t.Title, 100f * (col - 4f) / HomeWideText.Kestrel));
            t.Pad = (float)Math.Round(Clamp(8f * s, 6f, 12f));   // whole pixels: Yoga rounds every inset on its own
            t.DesignCaption = Math.Max(14f, t.LaunchCaption);
            float want = Math.Max(14f, t.LaunchLabel * 0.72f);
            t.DesignLabel = Math.Max(14f, Math.Min(want, 100f * (col - DesignChromeX) / (HomeWideText.Design * HomeTypeMetrics.Margin)));
            t.Stat = Math.Max(14f, t.Hint + 2f);
            return t;
        }

        // ---- blocks -----------------------------------------------------------------------------

        public const float StatMax = 15f;
        public const string FuelLine = "Fuel 92%  Thrust 850 kN";

        /// <summary>The record line at the largest stat face (15 down to 14) that keeps it on one line. Long careers
        /// fall back to <see cref="HomeRecord.Short"/>. The card holds exactly two lines: the pills and the record.</summary>
        public static float StatFont(HomePhoneType t, float col, string record, out string shown)
        {
            float inner = col - 2f * StatsBorder - 2f * StatsPadX;
            shown = record ?? "";
            // 15 at most: two 16 px lines need a 51 px card, which pushed the SE controls past 42% of the column.
            float top = Math.Min(t.Stat, StatMax);
            for (float f = top; f >= 13.99f; f -= 0.5f)
                if (HomeTypeMetrics.Fits(HomeFace.GroteskMedium, FuelLine, f, inner) && HomeTypeMetrics.Fits(HomeFace.GroteskMedium, shown, f, inner))
                    return f;
            string brief = HomeRecord.Short(shown);
            for (float f = top; f >= 12f; f -= 0.5f)
                if (HomeTypeMetrics.Fits(HomeFace.GroteskMedium, brief, f, inner)) { shown = brief; return f; }
            shown = brief;
            return 12f;
        }

        public static float StatsH(float statFont) => Math.Max(44f, 2f * StatsBorder + 2f * StatsPadY + 2f * statFont * LineH);

        // Whole pixels (MainMenuOverlay writes these): a fractional inset let Yoga round the CTA column 1 px past its box.
        public static float LaunchPadV(float s) => (float)Math.Round(Clamp(8f * s, 6f, 10f));
        public static float LaunchPadH(float s) => (float)Math.Round(Clamp(14f * s, 10f, 18f));

        public static int LaunchCaptionLines(HomePhoneType t, float col, string caption)
            => Math.Max(1, HomeTypeMetrics.Lines(HomeFace.GroteskMedium, caption, t.LaunchCaption, col - 2f * LaunchPadH(t.Scale) - LaunchChromeX));

        /// <summary>Whole pixels: the lip (MainMenuOverlay.Lip) is absolute, bottom 0, 6 px tall, and Yoga rounds its
        /// top (height - 6) on its own. A fractional height rounded the lip to 545 under a button ending at 544
        /// (GameCI, 390x844 points). With a whole height the lip's top is exact and it ends on the button's edge.</summary>
        public static float LaunchH(HomePhoneType t, int captionLines)
            => (float)Math.Ceiling(Math.Max(HitMin, LaunchBorderV + 2f * LaunchPadV(t.Scale) + t.LaunchLabel * LineH + captionLines * t.LaunchCaption * LineH) - 0.001f);

        /// <summary>TextCore wraps "Your rocket, this mission." (171 px on the advance table at 14 px) to two lines in a
        /// 189 px caption box (GameCI, 390x844 shell, full home), so the table under-reads this string by more than 10%.
        /// The caption's lines are counted at the box width over this factor: two lines at 390/430 shell, one on SE.</summary>
        public const float DesignWrap = 1.15f;

        public static int DesignCaptionLines(HomePhoneType t, float col, string caption)
            => Math.Min(3, Math.Max(1, HomeTypeMetrics.Lines(HomeFace.GroteskMedium, caption, t.DesignCaption, (col - DesignChromeX) / DesignWrap)));

        public static float DesignH(HomePhoneType t, int captionLines)
        {
            // The real boxes: the label's min-height is ceil(face x 1.3) (MainMenuOverlay.CardLabelChrome), then the gap,
            // then the caption lines at 1.25 em (TextCore set 2 lines of 14 px Space Grotesk in 34 px, 1.21 em). Never under the
            // old one-line 1.4 budget. Budgeting every line at 1.4 made the two-line card 4 px taller than its text and
            // pushed the 393x852 stats card under the guide pop.
            float real = (float)Math.Ceiling(t.DesignLabel * DesignTextLine) + DesignGap + captionLines * t.DesignCaption * DesignCaptionLine;
            float floor = t.DesignLabel * LineH + t.DesignCaption * LineH;
            return Math.Max(HitMin, 2f * DesignBorder + 2f * DesignPadY + Math.Max(real, floor));
        }

        /// <summary>Line box of the DESIGN FIRST label (its min-height, same as MainMenuOverlay.CardTextLine).</summary>
        public const float DesignTextLine = 1.3f;
        /// <summary>Caption line pitch: 34 px for two 14 px lines on GameCI (1.21 em), plus a little.</summary>
        public const float DesignCaptionLine = 1.25f;

        /// <summary>Two tiles across with a 2% gutter. 48.5% put "you've unlocked." one px past a 308-wide phone tile.</summary>
        public const float TileShare = 0.49f;
        public static float TileW(float col) => col * TileShare;
        public static float TileInner(HomePhoneType t, float col) => TileW(col) - 2f * TileBorder - 2f * t.Pad;

        /// <summary>Name size beside the glyph and (live tiles) the lamp. <paramref name="shown"/> is the label text ("Missions").</summary>
        public static float TileName(HomePhoneType t, float col, string shown, bool live)
        {
            float avail = TileInner(t, col) - TileIcon - (live ? TileLamp : 0f) - NameSlack;
            return HomeTypeMetrics.FitOneLine(HomeFace.FrauncesBold, shown, t.TileName, 12f, avail, 0.03f);
        }

        public static int TileCaptionLines(HomePhoneType t, float col, string caption)
            => Math.Max(1, HomeTypeMetrics.Lines(HomeFace.GroteskMedium, caption, t.TileCaption, TileInner(t, col)));

        public static float TileH(HomePhoneType t, int lines)
            => Math.Max(HitMin, 2f * TileBorder + 2f * t.Pad + Math.Max(22f, t.TileName * LineH) + 3f + lines * t.TileCaption * LineH + TileAir);

        public static float ThrumInner(float col) => col - 2f * ThrumPadX - ThrumPortrait - ThrumColMargin - 4f;

        public static int ThrumLines(HomePhoneType t, float col, string line)
            => Math.Max(1, HomeTypeMetrics.Lines(HomeFace.GroteskMedium, line, t.Hint, ThrumInner(col)));

        /// <summary>Thrum's whole line when it wraps to two lines or fewer, else the short teaser (the wide row does the
        /// same, WebHomeType.HintShort); tapping the strip still opens the whole line. Never a line cut mid-word.</summary>
        public static string ThrumText(HomePhoneType t, float col, string full, string brief)
        {
            if (string.IsNullOrEmpty(brief) || ThrumLines(t, col, full) <= 2) return full;
            return brief;
        }

        /// <summary>Name row plus every wrapped line, so the strip never cuts Thrum off mid-sentence.</summary>
        public static float ThrumH(HomePhoneType t, int lines)
            => Math.Max(HitMin, 2f * ThrumPadY + 4f + t.Hint * LineH + lines * t.Hint * LineH + 4f);

        public static float TitleH(HomePhoneType t)
            => Math.Max(14f, t.Hint) * LineH + 2f + t.Title * 1.3f + t.Tagline * LineH - 2f;

        public static float BarHeight(float h, float inset) => Math.Max(h * 0.06f, inset + 24f);

        // ---- whole column --------------------------------------------------------------------------

        /// <summary>The phone column stacked the way MainMenuOverlay lays it out: credits band, title, the hero gap,
        /// then stats, LAUNCH, Design first, the tiles and Thrum from the bottom. Gear is the top-right corner of the
        /// content rect; the guide pop rests under the title, right-aligned, clear of the gear.</summary>
        public const string ThrumShort = "Oh! A visitor. Mind the oil. I am Thrum.";

        /// <summary>The pre-Starter Home: every extra shown, LAUNCH as the CTA (credits as given).</summary>
        public static HomePhoneFrame Solve(HomePhoneScreen s, bool credits, string record, string launchCaption, string thrum, bool web = false)
        {
            var show = HomeShow.Full;
            show.Credits = credits;
            return Solve(s, show, false, record, launchCaption, thrum, web);
        }

        public const string Launch = "LAUNCH", Continue = "CONTINUE";
        /// <summary>Sub-line fallbacks when Career has no mission line (STARTER_COPY.md wants KH-n · Name).</summary>
        public const string LaunchCaption = "Next flight, then the pad.";
        public const string ContinueCaption = "Back to your last rocket.";
        public const string MissionsCaption = "Pick any flight you've unlocked.";
        public const string CodexCaption = "Parts, places and flight notes.";
        /// <summary>STARTER_COPY.md "18–20 px labels": a CTA label that needs less than this falls back to the short form.</summary>
        public const float CtaMinLabel = 20f;

        /// <summary>MISSIONS sub-line, STARTER_COPY.md: "{done} of {total} flown". Null when the total is unknown.</summary>
        public static string Flown(int done, int total) =>
            total <= 0 ? null : Math.Max(0, Math.Min(done, total)) + " of " + total + " flown";

        /// <summary>The one primary CTA, STARTER_COPY.md §c. With a save: CONTINUE over "KH-n · Name". Without:
        /// "LAUNCH KH-n · Name" on one line when <paramref name="fits"/> says so, else "LAUNCH KH-n" over the name.</summary>
        public static void Cta(bool save, string missionLine, Func<string, bool> fits, out string label, out string caption)
        {
            string line = string.IsNullOrEmpty(missionLine) ? null : missionLine.Trim();
            if (save)
            {
                label = Continue;
                caption = line ?? ContinueCaption;
                return;
            }
            if (line == null) { label = Launch; caption = LaunchCaption; return; }
            string full = Launch + " " + line;
            if (fits == null || fits(full)) { label = full; caption = ""; return; }
            int dot = line.IndexOf('\u00B7');
            if (dot > 0)
            {
                label = Launch + " " + line.Substring(0, dot).Trim();
                caption = line.Substring(dot + 1).Trim();
                return;
            }
            label = Launch;
            caption = line;
        }

        public static float CtaInner(HomePhoneType t, float col) => col - 2f * LaunchPadH(t.Scale) - LaunchChromeX;

        /// <summary>True when <paramref name="label"/> sits on one line at <see cref="CtaMinLabel"/> or more.</summary>
        public static bool CtaFits(HomePhoneType t, float col, string label) =>
            HomeTypeMetrics.Fits(HomeFace.FrauncesBlack, label, Math.Min(CtaMinLabel, t.LaunchLabel), CtaInner(t, col), 0.10f);

        /// <summary>The CTA label at <paramref name="want"/>, down to 14, so the label fits beside its icon.</summary>
        public static float CtaLabel(HomePhoneType t, float col, string label, float want) =>
            HomeTypeMetrics.FitOneLine(HomeFace.FrauncesBlack, label, want, 14f, CtaInner(t, col), 0.10f);

        /// <summary>Caption lines under the CTA: 0 when the label carries the whole mission line.</summary>
        public static int CtaCaptionLines(HomePhoneType t, float col, string caption) =>
            string.IsNullOrEmpty(caption) ? 0 : LaunchCaptionLines(t, col, caption);

        /// <summary>Starter Home (feat(home-simple)): ONE CTA (CONTINUE when <paramref name="save"/>, else LAUNCH with the
        /// "KH-n · Name" caption), MISSIONS full width, the gear. Extras come back per <paramref name="show"/>. A hidden
        /// block has an empty rect and takes no column height.</summary>
        public static HomePhoneFrame Solve(HomePhoneScreen s, HomeShow show, bool save, string record, string missionLine, string thrum, bool web = false)
        {
            bool credits = show.Credits;   // the bar is the chip only: CONTINUE lives on the CTA
            var f = new HomePhoneFrame();
            float rootX = s.Left, rootY = s.Top, rootW = s.SafeW, rootH = s.SafeH;
            float col = rootW - 2f * SideMargin;
            var t = Type(s.W, s.H, s.Top, s.Bottom, col);
            f.Type = t;
            f.Safe = new HomeRect(rootX, rootY, rootW, rootH);
            var bar = HomeCornerLayout.Credits(rootW, 0f, web);
            f.Bar = credits ? new HomeRect(rootX + bar.X, rootY + bar.Y, bar.W, bar.H) : default;
            HomeCornerLayout.Place(s.W, s.Top, web, out _, out var gear, out _);
            f.Gear = gear;

            // The gear owns the top-right of the same 44 pt band, so the title starts under it with or without the chip
            // (Starter Home has no chip; a 40 px pad put the eyebrow's right end under the gear).
            float padTop = Math.Max(TopPad, CreditsBand);
            float x = rootX + SideMargin;
            float eye = Math.Max(14f, t.Hint) * LineH;
            f.Eyebrow = new HomeRect(x, rootY + padTop, col, eye);
            f.Title = new HomeRect(x, rootY + padTop, col, TitleH(t));

            f.Show = show;
            Cta(save, missionLine, l => CtaFits(t, col, l), out f.Cta, out f.CtaCaption);
            t.LaunchLabel = CtaLabel(t, col, f.Cta, t.LaunchLabel);
            f.Type = t;
            f.StatFont = StatFont(t, col, record, out var shown);
            f.Record = shown;
            f.RecordLines = HomeTypeMetrics.Lines(HomeFace.GroteskMedium, shown, f.StatFont, col - 2f * StatsBorder - 2f * StatsPadX);
            float statsH = StatsH(f.StatFont);
            f.LaunchCaptionLines = CtaCaptionLines(t, col, f.CtaCaption);
            float launchH = LaunchH(t, f.LaunchCaptionLines);
            f.DesignCaptionLines = DesignCaptionLines(t, col, "Your rocket, this mission.");
            float designH = DesignH(t, f.DesignCaptionLines);
            float tw = show.Codex ? TileW(col) : col;
            f.TileCaptionLines = show.Codex
                ? Math.Max(TileCaptionLines(t, col, MissionsCaption), TileCaptionLines(t, col, CodexCaption))
                : Math.Max(1, HomeTypeMetrics.Lines(HomeFace.GroteskMedium, MissionsCaption, t.TileCaption, tw - 2f * TileBorder - 2f * t.Pad));
            float tileH = TileH(t, f.TileCaptionLines);
            f.ThrumShown = ThrumText(t, col, thrum ?? "", ThrumShort);
            f.ThrumLines = ThrumLines(t, col, f.ThrumShown);
            f.ThrumH = ThrumH(t, f.ThrumLines);

            float hintBand = Math.Max(0f, BarHeight(s.H, s.Bottom) - s.Bottom);
            float y = rootY + rootH - hintBand;
            if (show.Thrum) { y -= f.ThrumH; f.Thrum = new HomeRect(x, y, col, f.ThrumH); y -= Gap; }
            y -= tileH;
            f.Missions = new HomeRect(x, y, tw, tileH);
            if (show.Codex) f.Codex = new HomeRect(x + col - tw, y, tw, tileH);
            if (show.DesignFirst) { y -= Gap + designH; f.Design = new HomeRect(x, y, col, designH); }
            y -= Gap + launchH; f.Launch = new HomeRect(x, y, col, launchH);
            if (show.Stats) { y -= Gap + statsH; f.Stats = new HomeRect(x, y, col, statsH); }
            f.Hero = (show.Stats ? f.Stats.Y : f.Launch.Y) - f.Title.Bottom;

            float pop = GuidePopPt * s.PerPoint;
            float popX = s.W - s.Right - GuideMarginPt * s.PerPoint - pop;
            float popY = Math.Max(s.Top + GuideMarginPt * s.PerPoint, Math.Max(f.Gear.Bottom, f.Title.Bottom) + GuideMarginPt * s.PerPoint);
            f.Pop = new HomeRect(popX, popY, pop, pop);
            return f;
        }
    }
}
