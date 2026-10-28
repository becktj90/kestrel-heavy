// Starter screen composition (PR 5), in points. Portrait (phone, iPad, portrait web): Back and Reset on top,
// the mission header, the rocket preview, then a bottom sheet with the four controls, one hint line, the
// readiness line and Launch. Landscape web: the rocket on the left of the 1280 band and one side panel on
// the right (header, controls, hint, readiness, Launch bottom-right), with WebLandscapeType's type caps.
// Every box is a pure rectangle so the device sizes are checked without a canvas; the Unity view pins
// its elements to these boxes and the EditMode device test re-checks the real rects.
// Designer tiers (docs/DESIGNER_TIERS_SPEC.md, step 2): each row ends in a 44 pt "?" that opens a Kid Why? card under
// it (the cards replace the one hint line, as in the approved mockup), a "Speed to reach orbit" bar sits over the
// readiness line (on SE-height screens it steps aside while a card is open), an X-ray button sits top-right in the
// rocket window, and the Kid / Builder / Engineer switch (when it has more than one chip) sits right of Back, moving
// Reset into the header when the top row is too narrow, or on its own row when even that is too narrow (web EXIT).
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;

namespace Kestrel.Core.Designer
{
    public sealed class StarterLayoutInput
    {
        public float Width, Height, SafeTop, SafeBottom, SafeLeft, SafeRight;
        /// <summary>Landscape web shell (WebLayoutMode.UseWide). iOS and portrait web are never wide.</summary>
        public bool Wide;
        /// <summary>The web page EXIT control drawn over the canvas top-right.</summary>
        public bool ReserveExit;
        public bool BoostersVisible;
        public string Title = "", Subtitle = "", Hint = "", Badge = "", Readiness = "";
        public string[] Engines = { StarterScreen.Kerosene, StarterScreen.Methane };
        public string[] Stages = { "1", "2", "3" };
        public string[] Boosters = { StarterScreen.NoBoosters, "2", "4" };
        /// <summary>Each row ends in a "?" button (needs the tier copy).</summary>
        public bool Help;
        public string HelpText = "?";
        /// <summary>The row whose Why? card is open (None: closed) and its text.</summary>
        public StarterKnob WhyOpen = StarterKnob.None;
        public string WhyText = "";
        /// <summary>The speed bar: its label and its Enough / Short word.</summary>
        public bool Speed;
        public string SpeedLabel = "", SpeedStatus = "";
        /// <summary>The tier switch chips (Kid first). Fewer than two: no switch.</summary>
        public string[] Tiers;
        /// <summary>The X-ray button in the rocket window, and its word.</summary>
        public bool Xray;
        public string XrayText = "";
    }

    public sealed class StarterRowFrame
    {
        public string Key;
        /// <summary>Relative to nothing: every box in a frame is in screen points.</summary>
        public HudBox Row, Label;
        public HudBox[] Options = Array.Empty<HudBox>();
        /// <summary>Fuel row only: the "Less" and "More" end labels.</summary>
        public HudBox Less, More;
        /// <summary>The 44 pt "?" at the end of the row; not live without the tier copy.</summary>
        public HudBox Help;
        /// <summary>The row's label wraps to two lines (narrow phones with the "?" column).</summary>
        public bool LabelWraps;
    }

    public sealed class StarterText
    {
        public string Name, Text;
        public HudBox Box;
        public float Pt, PadX;
        public bool Wraps;
    }

    public sealed class StarterFrame
    {
        public bool Wide;
        public HudBox Back, Reset, Exit, Header, Title, Subtitle, Rocket, Sheet, Hint, Badge, Readiness, Launch;
        /// <summary>The open Why? card (container), its pictogram and its text.</summary>
        public HudBox Why, WhyIcon, WhyText;
        public StarterKnob WhyOpen = StarterKnob.None;
        public int WhyLines;
        /// <summary>The speed bar: label (left), status (right), the bar under them.</summary>
        public HudBox SpeedLabel, SpeedStatus, SpeedBar;
        /// <summary>The tier switch (container) and its chips; Reset sits in the header when the switch takes the top row.</summary>
        public HudBox Switch;
        public HudBox[] Chips = Array.Empty<HudBox>();
        public bool ResetInHeader;
        public float ChipPt = StarterLayout.ChipPt, WhyPt = StarterLayout.WhyPt, SpeedPt = StarterLayout.SpeedPt;
        /// <summary>X-ray button, top-right inside the rocket window.</summary>
        public HudBox Xray;
        public StarterRowFrame Engine, Stages, Fuel, Boosters;
        public float TitlePt, SubtitlePt, LabelPt, OptionPt, EndPt, HintPt, ReadinessPt, CtaPt, CornerPt;
        public int TitleLines, SubtitleLines, HintLines, ReadinessLines;
        /// <summary>The header sits inside the side panel (landscape web), else above the rocket.</summary>
        public bool HeaderInSheet;
        /// <summary>Short portrait screens (under 720 pt, SE) tighten the gaps, sheet padding and Launch height so the rocket keeps its room.</summary>
        public bool Compact;
        public float GapPt = StarterLayout.Gap, SheetPadPt = StarterLayout.SheetPad, LaunchPt = StarterLayout.LaunchH;
        public readonly List<StarterText> Texts = new List<StarterText>();

        public IEnumerable<StarterRowFrame> Rows()
        {
            yield return Engine;
            yield return Stages;
            yield return Fuel;
            if (Boosters != null) yield return Boosters;
        }

        /// <summary>Every tappable box: Back, Reset, the tier chips, each option and notch, each "?", X-ray, Launch.</summary>
        public IEnumerable<KeyValuePair<string, HudBox>> Targets()
        {
            yield return new KeyValuePair<string, HudBox>("back", Back);
            yield return new KeyValuePair<string, HudBox>("reset", Reset);
            for (int i = 0; i < Chips.Length; i++) yield return new KeyValuePair<string, HudBox>("tier-" + i, Chips[i]);
            foreach (var r in Rows())
            {
                for (int i = 0; i < r.Options.Length; i++) yield return new KeyValuePair<string, HudBox>(r.Key + "-" + i, r.Options[i]);
                if (r.Help.Live) yield return new KeyValuePair<string, HudBox>(r.Key + "-help", r.Help);
            }
            if (Xray.Live) yield return new KeyValuePair<string, HudBox>("xray", Xray);
            yield return new KeyValuePair<string, HudBox>("launch", Launch);
        }

        /// <summary>The leaf boxes that must not intersect one another (containers excluded).</summary>
        public List<KeyValuePair<string, HudBox>> Leaves()
        {
            var o = new List<KeyValuePair<string, HudBox>>();
            void Add(string n, HudBox b) { if (b.Live) o.Add(new KeyValuePair<string, HudBox>(n, b)); }
            Add("back", Back); Add("reset", Reset); Add("exit", Exit);
            for (int i = 0; i < Chips.Length; i++) Add("tier-" + i, Chips[i]);
            Add("title", Title); Add("subtitle", Subtitle); Add("rocket", Rocket); Add("xray", Xray);
            foreach (var r in Rows())
            {
                Add(r.Key + "-label", r.Label);
                for (int i = 0; i < r.Options.Length; i++) Add(r.Key + "-" + i, r.Options[i]);
                Add(r.Key + "-less", r.Less); Add(r.Key + "-more", r.More);
                Add(r.Key + "-help", r.Help);
            }
            Add("why-icon", WhyIcon); Add("why-text", WhyText);
            Add("speed-label", SpeedLabel); Add("speed-status", SpeedStatus); Add("speed-bar", SpeedBar);
            Add("hint", Hint); Add("badge", Badge); Add("readiness", Readiness); Add("launch", Launch);
            return o;
        }
    }

    public static class StarterLayout
    {
        public const float Edge = 8f, Gap = 8f, OptionGap = 6f;
        public const float CornerW = 88f, CornerH = 44f, CornerPt = 17f;
        public const float Touch = 44f, LaunchH = 52f, LineH = 1.3f;
        public const float SheetPad = 12f, WidePad = 16f, WideGap = 12f;
        public const float CompactBelow = 720f, CompactGap = 6f, CompactPad = 8f, CompactLaunchH = 48f;
        /// <summary>Portrait column cap (iPad): the controls stay a readable phone-width column, centred.</summary>
        public const float ColumnMax = 560f;
        /// <summary>Landscape web side panel: 30% of the band, 320-400 (DECISIONS #62 caps a side panel at 400).</summary>
        public const float SideMin = 320f, SideFraction = 0.30f;
        public const float OptionPadX = 6f, ButtonPadX = 8f;
        public const float OptionMinPt = 13f, ReadinessMinPt = 13f;
        public const int HintMaxLines = 2, ReadinessMaxLines = 3, TitleMaxLines = 2, SubtitleMaxLines = 2;
        // Designer tiers (step 2).
        public const float ChipPt = 15f, ChipMinPt = 13f, ChipPadX = 4f, ChipMaxW = 120f;
        public const float WhyPt = 14f, WhyCompactPt = 13f, WhyPad = 8f, WhyIcon = 28f;
        public const int WhyMaxLines = 6;
        public const float SpeedPt = 13f, SpeedBarH = 6f, SpeedGap = 3f;
        public const float XrayPt = 15f, XrayMinW = 64f;

        public static float Line(float pt) => (float)Math.Ceiling(pt * LineH);

        public static StarterFrame Place(StarterLayoutInput i)
        {
            if (i == null) throw new ArgumentNullException(nameof(i));
            var f = new StarterFrame { Wide = i.Wide && WebLayoutMode.IsLandscape(i.Width, i.Height) };
            float st = Math.Max(0f, i.SafeTop), sb = Math.Max(0f, i.SafeBottom), sl = Math.Max(0f, i.SafeLeft), sr = Math.Max(0f, i.SafeRight);
            float left = sl + Edge, right = i.Width - sr - Edge, bottom = i.Height - sb - Edge;
            Fonts(f);
            if (!f.Wide && i.Height - st - sb < CompactBelow)
            {
                f.Compact = true;
                f.GapPt = CompactGap; f.SheetPadPt = CompactPad; f.LaunchPt = CompactLaunchH;
            }

            // Corner controls: Back top-left like every screen (WebDesignerLayout.Home), Reset top-right, left of the page EXIT.
            f.Back = Box(left, st + Edge, CornerW, CornerH);
            if (i.ReserveExit) f.Exit = Box(i.Width - sr - Edge - HomeCornerLayout.ExitW, st, HomeCornerLayout.ExitW, HomeCornerLayout.ExitH);
            float resetRight = i.ReserveExit ? f.Exit.X - Gap : right;
            f.Reset = Box(resetRight - CornerW, st + Edge, CornerW, CornerH);
            float barBottom = Math.Max(f.Back.Bottom, f.Exit.Live ? f.Exit.Bottom : 0f);
            if (i.Tiers != null && i.Tiers.Length > 1)
            {
                PlaceSwitch(f, i, resetRight);
                barBottom = Math.Max(barBottom, f.Switch.Bottom);
            }
            f.WhyOpen = i.Help && !string.IsNullOrEmpty(i.WhyText) && (i.WhyOpen != StarterKnob.Boosters || i.BoostersVisible) ? i.WhyOpen : StarterKnob.None;
            f.WhyPt = f.Compact ? WhyCompactPt : WhyPt;

            if (f.Wide) PlaceWide(f, i, left, right, barBottom + WideGap, bottom);
            else PlacePortrait(f, i, left, right, barBottom + f.GapPt, bottom);
            PlaceXray(f, i);
            CollectTexts(f, i);
            return f;
        }

        /// <summary>The segmented Kid / Builder / Engineer switch right of Back. Inline between Back and Reset when the
        /// chips fit there (iPad, web); else it takes the top row to the right edge and Reset moves into the header.</summary>
        static void PlaceSwitch(StarterFrame f, StarterLayoutInput i, float resetRight)
        {
            int n = i.Tiers.Length;
            float x0 = f.Back.Right + Gap;
            float need = 0f;
            foreach (var t in i.Tiers) need += Math.Max(Touch, (float)Math.Ceiling(FlightHudLayout.TextWidth(t ?? "", ChipPt)) + 2f * ChipPadX + 2f);
            float inlineW = f.Reset.X - Gap - x0;
            float w;
            if (f.Wide || inlineW >= need) w = Math.Min(inlineW, Math.Max(need, Math.Min(n * ChipMaxW, inlineW)));
            else
            {
                f.ResetInHeader = !f.Wide;
                w = resetRight - x0;
            }
            f.Switch = Box(x0, f.Back.Y, Math.Max(n * Touch, w), CornerH);
            f.Chips = Spread(f.Switch.X, f.Switch.Y, f.Switch.W, n, 0f);
            float pt = ChipPt;
            while (pt > ChipMinPt && !ChipsFit(f, i, pt)) pt -= 0.5f;
            if (!ChipsFit(f, i, pt))
            {
                // Too narrow even to the right edge (portrait web with its EXIT): its own row under Back, Reset back on top.
                f.ResetInHeader = false;
                float l = f.Back.X, r = resetRight + 0f;
                if (f.Exit.Live) r = Math.Max(r, f.Exit.Right);
                f.Switch = Box(l, Math.Max(f.Back.Bottom, f.Exit.Live ? f.Exit.Bottom : 0f) + Gap, r - l, CornerH);
                f.Chips = Spread(f.Switch.X, f.Switch.Y, f.Switch.W, n, 0f);
                pt = ChipPt;
                while (pt > ChipMinPt && !ChipsFit(f, i, pt)) pt -= 0.5f;
            }
            f.ChipPt = pt;
        }

        static bool ChipsFit(StarterFrame f, StarterLayoutInput i, float pt)
        {
            for (int k = 0; k < i.Tiers.Length && k < f.Chips.Length; k++)
                if (FlightHudLayout.TextWidth(i.Tiers[k] ?? "", pt) > f.Chips[k].W - 2f * ChipPadX) return false;
            return true;
        }

        /// <summary>X-ray: top-right inside the rocket window, when the window can hold a 44 pt button.</summary>
        static void PlaceXray(StarterFrame f, StarterLayoutInput i)
        {
            if (!i.Xray || string.IsNullOrEmpty(i.XrayText)) return;
            float w = Math.Max(XrayMinW, (float)Math.Ceiling(FlightHudLayout.TextWidth(i.XrayText, XrayPt)) + 2f * ButtonPadX + 2f);
            if (f.Rocket.H < Touch || f.Rocket.W < w) return;
            float right = f.Rocket.Right;
            // Portrait: the rocket window is full width; keep the button over the controls' column (iPad).
            if (!f.Wide) right = Math.Min(right, f.Sheet.Right);
            f.Xray = Box(right - w, f.Rocket.Y, w, Touch);
        }

        static void Fonts(StarterFrame f)
        {
            f.CornerPt = CornerPt;
            if (f.Wide)
            {
                f.TitlePt = WebLandscapeType.Title(22f);
                f.SubtitlePt = WebLandscapeType.Body(15f);
                f.LabelPt = WebLandscapeType.Tile(16f);
                f.OptionPt = 15f;
                f.EndPt = 13f;
                f.HintPt = WebLandscapeType.Body(14f);
                f.ReadinessPt = WebLandscapeType.Body(16f);
                f.CtaPt = WebLandscapeType.Cta(22f);
            }
            else
            {
                f.TitlePt = 20f; f.SubtitlePt = 14f; f.LabelPt = 16f; f.OptionPt = 15f; f.EndPt = 13f;
                f.HintPt = 13f; f.ReadinessPt = 16f; f.CtaPt = 20f;
            }
        }

        static void PlacePortrait(StarterFrame f, StarterLayoutInput i, float left, float right, float top, float bottom)
        {
            float avail = Math.Max(1f, right - left);
            float colW = Math.Min(avail, ColumnMax);
            float colL = left + (avail - colW) * 0.5f;

            // Header under the corner controls, full column width (less Reset's corner when the switch took the top row).
            float textW = f.ResetInHeader ? colW - CornerW - Gap : colW;
            f.TitleLines = Lines(i.Title, textW, f.TitlePt, TitleMaxLines);
            f.SubtitleLines = string.IsNullOrEmpty(i.Subtitle) ? 0 : Lines(i.Subtitle, textW, f.SubtitlePt, SubtitleMaxLines);
            f.Title = Box(colL, top, textW, f.TitleLines * Line(f.TitlePt));
            f.Subtitle = f.SubtitleLines > 0 ? Box(colL, f.Title.Bottom + 2f, textW, f.SubtitleLines * Line(f.SubtitlePt)) : default;
            float headerBottom = f.SubtitleLines > 0 ? f.Subtitle.Bottom : f.Title.Bottom;
            if (f.ResetInHeader)
            {
                f.Reset = Box(colL + colW - CornerW, top, CornerW, CornerH);
                headerBottom = Math.Max(headerBottom, f.Reset.Bottom);
            }
            f.Header = Box(colL, top, colW, headerBottom - top);

            // Sheet from the bottom up.
            float innerL = colL + f.SheetPadPt, innerW = colW - 2f * f.SheetPadPt;
            float y = bottom - f.SheetPadPt;
            f.Launch = Box(innerL, y - f.LaunchPt, innerW, f.LaunchPt);
            y = f.Launch.Y - f.GapPt;
            y = PlaceReadiness(f, i, innerL, innerW, y);
            // SE height: the speed bar steps aside while a Why? card is open, so the rocket keeps a window.
            if (!(f.Compact && f.WhyOpen != StarterKnob.None)) y = PlaceSpeed(f, i, innerL, innerW, y);
            if (!i.Help) y = PlaceHint(f, i, innerL, innerW, y - f.GapPt);
            y -= f.GapPt;
            float rowsH = RowsHeight(f, i, innerW);
            float rowsTop = y - rowsH;
            PlaceRows(f, i, innerL, innerW, rowsTop);
            f.Sheet = Box(colL, rowsTop - f.SheetPadPt, colW, bottom - (rowsTop - f.SheetPadPt));

            float rocketTop = f.Header.Bottom + f.GapPt;
            f.Rocket = Box(left, rocketTop, avail, Math.Max(0f, f.Sheet.Y - f.GapPt - rocketTop));
        }

        static void PlaceWide(StarterFrame f, StarterLayoutInput i, float left, float right, float top, float bottom)
        {
            WebLandscapeType.Band(left, right, out float bandL, out float bandR);
            float span = bandR - bandL;
            float side = Math.Max(SideMin, Math.Min(WebLandscapeType.SideCap, span * SideFraction));
            side = Math.Min(side, span * (1f - WebLandscapeType.RocketMinFraction) - WideGap);
            float panelX = bandR - side;
            f.Sheet = Box(panelX, top, side, Math.Max(80f, bottom - top));
            f.Rocket = Box(bandL, top, Math.Max(1f, panelX - WideGap - bandL), f.Sheet.H);
            f.HeaderInSheet = true;

            float innerL = panelX + WidePad, innerW = side - 2f * WidePad;
            float y = top + WidePad;
            f.TitleLines = Lines(i.Title, innerW, f.TitlePt, TitleMaxLines);
            f.SubtitleLines = string.IsNullOrEmpty(i.Subtitle) ? 0 : Lines(i.Subtitle, innerW, f.SubtitlePt, SubtitleMaxLines);
            f.Title = Box(innerL, y, innerW, f.TitleLines * Line(f.TitlePt));
            f.Subtitle = f.SubtitleLines > 0 ? Box(innerL, f.Title.Bottom + 2f, innerW, f.SubtitleLines * Line(f.SubtitlePt)) : default;
            f.Header = Box(innerL, y, innerW, (f.SubtitleLines > 0 ? f.Subtitle.Bottom : f.Title.Bottom) - y);
            y = f.Header.Bottom + WideGap;
            PlaceRows(f, i, innerL, innerW, y);
            y += RowsHeight(f, i, innerW) + Gap;
            // Hint under the controls (the open Why? card takes its place); readiness and Launch at the bottom of the
            // panel (one primary, bottom-right), the speed bar over the readiness line.
            if (!i.Help)
            {
                f.HintLines = Lines(i.Hint, innerW, f.HintPt, HintMaxLines);
                f.Hint = Box(innerL, y, innerW, f.HintLines * Line(f.HintPt));
            }
            float by = f.Sheet.Bottom - WidePad;
            f.Launch = Box(innerL, by - LaunchH, innerW, LaunchH);
            float ry = PlaceReadiness(f, i, innerL, innerW, f.Launch.Y - Gap);
            PlaceSpeed(f, i, innerL, innerW, ry);
        }

        /// <summary>The speed bar ending a gap above <paramref name="aboveY"/>; returns its top (or aboveY when hidden).</summary>
        static float PlaceSpeed(StarterFrame f, StarterLayoutInput i, float x, float w, float aboveY)
        {
            if (!i.Speed || string.IsNullOrEmpty(i.SpeedLabel)) return aboveY;
            float pt = f.SpeedPt, lh = Line(pt);
            float bottom = aboveY - f.GapPt;
            float top = bottom - SpeedBarH - SpeedGap - lh;
            float statusW = string.IsNullOrEmpty(i.SpeedStatus) ? 0f : (float)Math.Ceiling(FlightHudLayout.TextWidth(i.SpeedStatus, pt)) + 2f;
            f.SpeedStatus = statusW > 0f ? Box(x + w - statusW, top, statusW, lh) : default;
            f.SpeedLabel = Box(x, top, Math.Max(8f, w - statusW - (statusW > 0f ? OptionGap : 0f)), lh);
            f.SpeedBar = Box(x, bottom - SpeedBarH, w, SpeedBarH);
            return top;
        }

        /// <summary>The open Why? card: pictogram left, wrapped text right.</summary>
        static float WhyHeight(StarterFrame f, StarterLayoutInput i, float w)
        {
            if (f.WhyOpen == StarterKnob.None) return 0f;
            float textW = w - 2f * WhyPad - WhyIcon - Gap;
            f.WhyLines = Math.Max(1, Math.Min(WhyMaxLines, DesignerSheetLayout.LineCount(i.WhyText, textW, f.WhyPt)));
            return 2f * WhyPad + Math.Max(WhyIcon, f.WhyLines * Line(f.WhyPt));
        }

        static float PlaceWhy(StarterFrame f, StarterLayoutInput i, float x, float w, float y)
        {
            float h = WhyHeight(f, i, w);
            f.Why = Box(x, y, w, h);
            f.WhyIcon = Box(x + WhyPad, y + WhyPad, WhyIcon, WhyIcon);
            float tx = f.WhyIcon.Right + Gap;
            f.WhyText = Box(tx, y + WhyPad, x + w - WhyPad - tx, f.WhyLines * Line(f.WhyPt));
            return f.Why.Bottom;
        }

        /// <summary>Readiness ending at <paramref name="bottomY"/>; returns its top. Wraps to 2-3 lines before it shrinks (UX).</summary>
        static float PlaceReadiness(StarterFrame f, StarterLayoutInput i, float x, float w, float bottomY)
        {
            bool badge = !string.IsNullOrEmpty(i.Badge);
            float pt = f.ReadinessPt;
            float badgeW = badge ? (float)Math.Ceiling(FlightHudLayout.TextWidth(i.Badge, pt)) + 2f : 0f;
            float textX = badge ? x + badgeW + OptionGap : x;
            float textW = Math.Max(8f, x + w - textX);
            int lines = DesignerSheetLayout.LineCount(i.Readiness, textW, pt);
            while (lines > ReadinessMaxLines && pt > ReadinessMinPt)
            {
                pt -= 1f;
                badgeW = badge ? (float)Math.Ceiling(FlightHudLayout.TextWidth(i.Badge, pt)) + 2f : 0f;
                textX = badge ? x + badgeW + OptionGap : x;
                textW = Math.Max(8f, x + w - textX);
                lines = DesignerSheetLayout.LineCount(i.Readiness, textW, pt);
            }
            f.ReadinessPt = pt;
            f.ReadinessLines = Math.Max(1, lines);
            float h = f.ReadinessLines * Line(pt);
            float top = bottomY - h;
            f.Badge = badge ? Box(x, top, badgeW, Line(pt)) : default;
            f.Readiness = Box(textX, top, textW, h);
            return top;
        }

        static float PlaceHint(StarterFrame f, StarterLayoutInput i, float x, float w, float bottomY)
        {
            f.HintLines = Lines(i.Hint, w, f.HintPt, HintMaxLines);
            float h = f.HintLines * Line(f.HintPt);
            f.Hint = Box(x, bottomY - h, w, h);
            return f.Hint.Y;
        }

        static float FuelRowH(StarterFrame f) => Line(f.LabelPt) + 2f + Touch;

        static float RowsHeight(StarterFrame f, StarterLayoutInput i, float w)
        {
            float h = Touch + f.GapPt + Touch + f.GapPt + FuelRowH(f) + (i.BoostersVisible ? f.GapPt + Touch : 0f);
            if (f.WhyOpen != StarterKnob.None) h += f.GapPt + WhyHeight(f, i, w);
            return h;
        }

        static readonly string[] SideLabels = { StarterScreen.EngineLabel, StarterScreen.StagesLabel, StarterScreen.BoostersLabel };

        static float LabelWidth(StarterFrame f, float w, bool words)
        {
            float labelW = 0f;
            foreach (var l in SideLabels)
                foreach (var part in words ? l.Split(' ') : new[] { l })
                    labelW = Math.Max(labelW, FlightHudLayout.TextWidth(part, f.LabelPt));
            return Math.Min((float)Math.Ceiling(labelW) + 4f, w * 0.42f);
        }

        static void PlaceRows(StarterFrame f, StarterLayoutInput i, float x, float w, float y)
        {
            // With the "?" column the options lose 50 pt: on a narrow phone the labels wrap ("Engine / fuel") so
            // "Kerosene" still fits its button.
            float helpW = i.Help ? Touch + f.GapPt : 0f;
            bool wraps = false;
            float labelW = LabelWidth(f, w, false);
            if (i.Help)
            {
                float optW1 = x + w - helpW - (x + labelW + f.GapPt);
                if (!WordsFit(i.Engines, optW1, OptionMinPt) || !WordsFit(i.Stages, optW1, OptionMinPt) || (i.BoostersVisible && !WordsFit(i.Boosters, optW1, OptionMinPt)))
                {
                    wraps = true;
                    labelW = LabelWidth(f, w, true);
                }
            }
            float optX = x + labelW + f.GapPt, optW = Math.Max(Touch, x + w - helpW - optX);

            f.Engine = SideRow("engine", x, y, labelW, optX, optW, i.Engines.Length);
            Finish(f.Engine, i, x, w, wraps);
            y = AfterRow(f, i, StarterKnob.Engine, f.Engine, x, w);
            f.Stages = SideRow("stages", x, y, labelW, optX, optW, i.Stages.Length);
            Finish(f.Stages, i, x, w, wraps);
            y = AfterRow(f, i, StarterKnob.Stages, f.Stages, x, w);

            // Fuel: the label line carries "Fuel" and the Less / More ends; the five notches span the whole row
            // under it, so each notch stays a 44 pt target at 335.
            var fuel = new StarterRowFrame { Key = "fuel" };
            float lineH = Line(f.LabelPt);
            fuel.Row = Box(x, y, w, FuelRowH(f));
            fuel.Label = Box(x, y, labelW, lineH);
            float endW = (float)Math.Ceiling(Math.Max(FlightHudLayout.TextWidth(StarterScreen.FuelLess, f.EndPt), FlightHudLayout.TextWidth(StarterScreen.FuelMore, f.EndPt))) + 2f;
            float notchW = w - helpW;
            fuel.Less = Box(optX, y, endW, lineH);
            fuel.More = Box(x + notchW - endW, y, endW, lineH);
            fuel.Options = Spread(x, y + lineH + 2f, notchW, StarterRocket.FuelNotches, OptionGap);
            if (i.Help) fuel.Help = Box(x + w - Touch, y + lineH + 2f, Touch, Touch);
            f.Fuel = fuel;
            y = AfterRow(f, i, StarterKnob.FuelNotch, fuel, x, w);

            if (i.BoostersVisible)
            {
                f.Boosters = SideRow("boosters", x, y, labelW, optX, optW, i.Boosters.Length);
                Finish(f.Boosters, i, x, w, wraps);
                AfterRow(f, i, StarterKnob.Boosters, f.Boosters, x, w);
            }
            else f.Boosters = null;

            // Option words fit their buttons: shrink the option face (never below 13) rather than clip "Kerosene".
            float pt = f.OptionPt;
            while (pt > OptionMinPt && !OptionsFit(f, i, pt)) pt -= 0.5f;
            f.OptionPt = pt;
        }

        static bool WordsFit(string[] words, float rowW, float pt)
        {
            if (words == null || words.Length == 0) return true;
            float each = (rowW - OptionGap * (words.Length - 1)) / words.Length;
            foreach (var word in words)
                if (FlightHudLayout.TextWidth(word ?? "", pt) > each - 2f * OptionPadX) return false;
            return true;
        }

        /// <summary>The row's "?" at its right end, and the full row width when it has one.</summary>
        static void Finish(StarterRowFrame r, StarterLayoutInput i, float x, float w, bool wraps)
        {
            r.LabelWraps = wraps;
            if (!i.Help) return;
            r.Help = Box(x + w - Touch, r.Row.Y, Touch, Touch);
            r.Row = Box(x, r.Row.Y, w, r.Row.H);
        }

        /// <summary>The y under a row: under its Why? card when that card is open.</summary>
        static float AfterRow(StarterFrame f, StarterLayoutInput i, StarterKnob knob, StarterRowFrame r, float x, float w)
        {
            float y = r.Row.Bottom + f.GapPt;
            if (f.WhyOpen != knob) return y;
            return PlaceWhy(f, i, x, w, y) + f.GapPt;
        }

        static bool OptionsFit(StarterFrame f, StarterLayoutInput i, float pt)
        {
            bool Fits(string[] words, HudBox[] boxes)
            {
                for (int k = 0; k < words.Length && k < boxes.Length; k++)
                    if (FlightHudLayout.TextWidth(words[k], pt) > boxes[k].W - 2f * OptionPadX) return false;
                return true;
            }
            return Fits(i.Engines, f.Engine.Options) && Fits(i.Stages, f.Stages.Options) && (f.Boosters == null || Fits(i.Boosters, f.Boosters.Options));
        }

        static StarterRowFrame SideRow(string key, float x, float y, float labelW, float optX, float optW, int n)
        {
            return new StarterRowFrame
            {
                Key = key,
                Row = Box(x, y, optX + optW - x, Touch),
                Label = Box(x, y, labelW, Touch),
                Options = Spread(optX, y, optW, n, OptionGap),
            };
        }

        static HudBox[] Spread(float x, float y, float w, int n, float gap)
        {
            var o = new HudBox[Math.Max(0, n)];
            if (n <= 0) return o;
            float each = (w - gap * (n - 1)) / n;
            for (int k = 0; k < n; k++) o[k] = Box(x + k * (each + gap), y, each, Touch);
            return o;
        }

        static int Lines(string text, float w, float pt, int max)
        {
            if (string.IsNullOrEmpty(text)) return 1;
            return Math.Max(1, Math.Min(max, DesignerSheetLayout.LineCount(text, w, pt)));
        }

        static void CollectTexts(StarterFrame f, StarterLayoutInput i)
        {
            void T(string name, string text, HudBox b, float pt, float pad, bool wraps)
            {
                if (!b.Live || string.IsNullOrEmpty(text)) return;
                f.Texts.Add(new StarterText { Name = name, Text = text, Box = b, Pt = pt, PadX = pad, Wraps = wraps });
            }
            T("back", StarterScreen.BackText, f.Back, f.CornerPt, ButtonPadX, false);
            T("reset", StarterScreen.ResetText, f.Reset, f.CornerPt, ButtonPadX, false);
            if (i.Tiers != null)
                for (int k = 0; k < i.Tiers.Length && k < f.Chips.Length; k++) T("tier-" + k, i.Tiers[k], f.Chips[k], f.ChipPt, ChipPadX, false);
            T("xray", i.XrayText, f.Xray, XrayPt, ButtonPadX, false);
            foreach (var r in f.Rows()) T(r.Key + "-help", i.HelpText, r.Help, f.OptionPt, 0f, false);
            T("why-text", i.WhyText, f.WhyText, f.WhyPt, 0f, true);
            T("speed-label", i.SpeedLabel, f.SpeedLabel, f.SpeedPt, 0f, false);
            T("speed-status", i.SpeedStatus, f.SpeedStatus, f.SpeedPt, 0f, false);
            T("title", i.Title, f.Title, f.TitlePt, 0f, true);
            T("subtitle", i.Subtitle, f.Subtitle, f.SubtitlePt, 0f, true);
            T("engine-label", StarterScreen.EngineLabel, f.Engine.Label, f.LabelPt, 0f, f.Engine.LabelWraps);
            T("stages-label", StarterScreen.StagesLabel, f.Stages.Label, f.LabelPt, 0f, f.Stages.LabelWraps);
            T("fuel-label", StarterScreen.FuelLabel, f.Fuel.Label, f.LabelPt, 0f, false);
            T("fuel-less", StarterScreen.FuelLess, f.Fuel.Less, f.EndPt, 0f, false);
            T("fuel-more", StarterScreen.FuelMore, f.Fuel.More, f.EndPt, 0f, false);
            for (int k = 0; k < i.Engines.Length && k < f.Engine.Options.Length; k++) T("engine-" + k, i.Engines[k], f.Engine.Options[k], f.OptionPt, OptionPadX, false);
            for (int k = 0; k < i.Stages.Length && k < f.Stages.Options.Length; k++) T("stages-" + k, i.Stages[k], f.Stages.Options[k], f.OptionPt, OptionPadX, false);
            if (f.Boosters != null)
            {
                T("boosters-label", StarterScreen.BoostersLabel, f.Boosters.Label, f.LabelPt, 0f, f.Boosters.LabelWraps);
                for (int k = 0; k < i.Boosters.Length && k < f.Boosters.Options.Length; k++) T("boosters-" + k, i.Boosters[k], f.Boosters.Options[k], f.OptionPt, OptionPadX, false);
            }
            T("hint", i.Hint, f.Hint, f.HintPt, 0f, true);
            T("badge", i.Badge, f.Badge, f.ReadinessPt, 0f, false);
            T("readiness", i.Readiness, f.Readiness, f.ReadinessPt, 0f, true);
            T("launch", StarterScreen.LaunchText, f.Launch, f.CtaPt, ButtonPadX, false);
        }

        /// <summary>Layout problems in words: overlaps, off-screen boxes, small targets and text that does not fit
        /// (FlightHudLayout's conservative 0.62 advance). Empty when the frame is clean.</summary>
        public static List<string> Problems(StarterFrame f, StarterLayoutInput i)
        {
            var p = new List<string>();
            var leaves = f.Leaves();
            for (int a = 0; a < leaves.Count; a++)
                for (int b = a + 1; b < leaves.Count; b++)
                {
                    // X-ray is drawn in the rocket window on purpose (the window is a container for it).
                    if ((leaves[a].Key == "rocket" && leaves[b].Key == "xray") || (leaves[a].Key == "xray" && leaves[b].Key == "rocket")) continue;
                    if (leaves[a].Value.Overlaps(leaves[b].Value)) p.Add(leaves[a].Key + " overlaps " + leaves[b].Key);
                }
            if (f.Xray.Live && (f.Xray.X < f.Rocket.X - 0.5f || f.Xray.Right > f.Rocket.Right + 0.5f || f.Xray.Y < f.Rocket.Y - 0.5f || f.Xray.Bottom > f.Rocket.Bottom + 0.5f))
                p.Add("xray outside the rocket window");
            if (f.Why.Live)
            {
                foreach (var kv in leaves)
                {
                    if (kv.Key == "why-icon" || kv.Key == "why-text") continue;
                    if (f.Why.Overlaps(kv.Value)) p.Add("why card overlaps " + kv.Key);
                }
                if (f.WhyIcon.Right > f.Why.Right + 0.5f || f.WhyText.Right > f.Why.Right + 0.5f || f.WhyText.Bottom > f.Why.Bottom + 0.5f) p.Add("why card content outside the card");
            }
            float l = Math.Max(0f, i.SafeLeft), t = Math.Max(0f, i.SafeTop);
            float r = i.Width - Math.Max(0f, i.SafeRight), btm = i.Height - Math.Max(0f, i.SafeBottom);
            foreach (var kv in leaves)
            {
                var b = kv.Value;
                if (b.X < l - 0.5f || b.Y < t - 0.5f || b.Right > r + 0.5f || b.Bottom > btm + 0.5f) p.Add(kv.Key + " outside the safe area");
            }
            foreach (var kv in f.Targets())
                if (kv.Value.W < Touch - 0.01f || kv.Value.H < Touch - 0.01f) p.Add(kv.Key + " target " + kv.Value.W.ToString("0.#") + "x" + kv.Value.H.ToString("0.#") + " under 44");
            // The rocket preview and the controls are separate: nothing in the sheet is drawn on the rocket.
            if (f.Sheet.Overlaps(f.Rocket)) p.Add("sheet overlaps rocket");
            if (!f.HeaderInSheet && f.Header.Overlaps(f.Sheet)) p.Add("header overlaps sheet");
            foreach (var kv in leaves)
            {
                if (kv.Key == "rocket" || kv.Key == "xray" || kv.Key == "back" || kv.Key == "reset" || kv.Key == "exit" || kv.Key.StartsWith("tier-", StringComparison.Ordinal)) continue;
                if (f.HeaderInSheet || (kv.Key != "title" && kv.Key != "subtitle"))
                {
                    var b = kv.Value; var s = f.Sheet;
                    if (b.X < s.X - 0.5f || b.Right > s.Right + 0.5f || b.Y < s.Y - 0.5f || b.Bottom > s.Bottom + 0.5f) p.Add(kv.Key + " outside the sheet");
                }
            }
            foreach (var tx in f.Texts)
            {
                float w = tx.Box.W - 2f * tx.PadX;
                if (tx.Wraps)
                {
                    int lines = DesignerSheetLayout.LineCount(tx.Text, w, tx.Pt);
                    if (lines * Line(tx.Pt) > tx.Box.H + 0.5f) p.Add(tx.Name + " needs " + lines + " lines in " + tx.Box.H.ToString("0") + " pt");
                    foreach (var word in tx.Text.Split(' '))
                        if (FlightHudLayout.TextWidth(word, tx.Pt) > w + 0.5f) p.Add(tx.Name + " word \"" + word + "\" wider than its box");
                }
                else if (FlightHudLayout.TextWidth(tx.Text, tx.Pt) > w + 0.5f)
                    p.Add(tx.Name + " \"" + tx.Text + "\" needs " + FlightHudLayout.TextWidth(tx.Text, tx.Pt).ToString("0") + " in " + w.ToString("0"));
                if (Line(tx.Pt) > tx.Box.H + 0.5f) p.Add(tx.Name + " line taller than its box");
            }
            return p;
        }

        static HudBox Box(float x, float y, float w, float h) => new HudBox { X = x, Y = y, W = w, H = h };
    }
}
