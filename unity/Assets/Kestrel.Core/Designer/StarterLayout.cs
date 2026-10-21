// Starter screen composition (PR 5), in points. Portrait (phone, iPad, portrait web): Back and Reset on top,
// the mission header, the rocket preview, then a bottom sheet with the four controls, one hint line, the
// readiness line and Launch. Landscape web: the rocket on the left of the 1280 band and one side panel on
// the right (header, controls, hint, readiness, Launch bottom-right), with WebLandscapeType's type caps.
// Every box is a pure rectangle so the device sizes are checked without a canvas; the Unity view pins
// its elements to these boxes and the EditMode device test re-checks the real rects.
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
    }

    public sealed class StarterRowFrame
    {
        public string Key;
        /// <summary>Relative to nothing: every box in a frame is in screen points.</summary>
        public HudBox Row, Label;
        public HudBox[] Options = Array.Empty<HudBox>();
        /// <summary>Fuel row only: the "Less" and "More" end labels.</summary>
        public HudBox Less, More;
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

        /// <summary>Every tappable box: Back, Reset, each option and notch, Launch.</summary>
        public IEnumerable<KeyValuePair<string, HudBox>> Targets()
        {
            yield return new KeyValuePair<string, HudBox>("back", Back);
            yield return new KeyValuePair<string, HudBox>("reset", Reset);
            foreach (var r in Rows())
                for (int i = 0; i < r.Options.Length; i++) yield return new KeyValuePair<string, HudBox>(r.Key + "-" + i, r.Options[i]);
            yield return new KeyValuePair<string, HudBox>("launch", Launch);
        }

        /// <summary>The leaf boxes that must not intersect one another (containers excluded).</summary>
        public List<KeyValuePair<string, HudBox>> Leaves()
        {
            var o = new List<KeyValuePair<string, HudBox>>();
            void Add(string n, HudBox b) { if (b.Live) o.Add(new KeyValuePair<string, HudBox>(n, b)); }
            Add("back", Back); Add("reset", Reset); Add("exit", Exit);
            Add("title", Title); Add("subtitle", Subtitle); Add("rocket", Rocket);
            foreach (var r in Rows())
            {
                Add(r.Key + "-label", r.Label);
                for (int i = 0; i < r.Options.Length; i++) Add(r.Key + "-" + i, r.Options[i]);
                Add(r.Key + "-less", r.Less); Add(r.Key + "-more", r.More);
            }
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

            if (f.Wide) PlaceWide(f, i, left, right, barBottom + WideGap, bottom);
            else PlacePortrait(f, i, left, right, barBottom + f.GapPt, bottom);
            CollectTexts(f, i);
            return f;
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

            // Header under the corner controls, full column width.
            f.TitleLines = Lines(i.Title, colW, f.TitlePt, TitleMaxLines);
            f.SubtitleLines = string.IsNullOrEmpty(i.Subtitle) ? 0 : Lines(i.Subtitle, colW, f.SubtitlePt, SubtitleMaxLines);
            f.Title = Box(colL, top, colW, f.TitleLines * Line(f.TitlePt));
            f.Subtitle = f.SubtitleLines > 0 ? Box(colL, f.Title.Bottom + 2f, colW, f.SubtitleLines * Line(f.SubtitlePt)) : default;
            f.Header = Box(colL, top, colW, (f.SubtitleLines > 0 ? f.Subtitle.Bottom : f.Title.Bottom) - top);

            // Sheet from the bottom up.
            float innerL = colL + f.SheetPadPt, innerW = colW - 2f * f.SheetPadPt;
            float y = bottom - f.SheetPadPt;
            f.Launch = Box(innerL, y - f.LaunchPt, innerW, f.LaunchPt);
            y = f.Launch.Y - f.GapPt;
            y = PlaceReadiness(f, i, innerL, innerW, y);
            y = PlaceHint(f, i, innerL, innerW, y - f.GapPt);
            y -= f.GapPt;
            float rowsH = RowsHeight(f, i.BoostersVisible);
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
            y += RowsHeight(f, i.BoostersVisible) + Gap;
            // Hint under the controls; readiness and Launch at the bottom of the panel (one primary, bottom-right).
            f.HintLines = Lines(i.Hint, innerW, f.HintPt, HintMaxLines);
            f.Hint = Box(innerL, y, innerW, f.HintLines * Line(f.HintPt));
            float by = f.Sheet.Bottom - WidePad;
            f.Launch = Box(innerL, by - LaunchH, innerW, LaunchH);
            PlaceReadiness(f, i, innerL, innerW, f.Launch.Y - Gap);
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

        static float RowsHeight(StarterFrame f, bool boosters)
            => Touch + f.GapPt + Touch + f.GapPt + FuelRowH(f) + (boosters ? f.GapPt + Touch : 0f);

        static void PlaceRows(StarterFrame f, StarterLayoutInput i, float x, float w, float y)
        {
            float labelW = 0f;
            foreach (var l in new[] { StarterScreen.EngineLabel, StarterScreen.StagesLabel, StarterScreen.BoostersLabel })
                labelW = Math.Max(labelW, FlightHudLayout.TextWidth(l, f.LabelPt));
            labelW = Math.Min((float)Math.Ceiling(labelW) + 4f, w * 0.42f);
            float optX = x + labelW + f.GapPt, optW = Math.Max(Touch, x + w - optX);

            f.Engine = SideRow("engine", x, y, labelW, optX, optW, i.Engines.Length);
            y = f.Engine.Row.Bottom + f.GapPt;
            f.Stages = SideRow("stages", x, y, labelW, optX, optW, i.Stages.Length);
            y = f.Stages.Row.Bottom + f.GapPt;

            // Fuel: the label line carries "Fuel" and the Less / More ends; the five notches span the whole row
            // under it, so each notch stays a 44 pt target at 335.
            var fuel = new StarterRowFrame { Key = "fuel" };
            float lineH = Line(f.LabelPt);
            fuel.Row = Box(x, y, w, FuelRowH(f));
            fuel.Label = Box(x, y, labelW, lineH);
            float endW = (float)Math.Ceiling(Math.Max(FlightHudLayout.TextWidth(StarterScreen.FuelLess, f.EndPt), FlightHudLayout.TextWidth(StarterScreen.FuelMore, f.EndPt))) + 2f;
            fuel.Less = Box(optX, y, endW, lineH);
            fuel.More = Box(x + w - endW, y, endW, lineH);
            fuel.Options = Spread(x, y + lineH + 2f, w, StarterRocket.FuelNotches, OptionGap);
            f.Fuel = fuel;
            y = fuel.Row.Bottom + f.GapPt;

            f.Boosters = i.BoostersVisible ? SideRow("boosters", x, y, labelW, optX, optW, i.Boosters.Length) : null;

            // Option words fit their buttons: shrink the option face (never below 13) rather than clip "Kerosene".
            float pt = f.OptionPt;
            while (pt > OptionMinPt && !OptionsFit(f, i, pt)) pt -= 0.5f;
            f.OptionPt = pt;
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
            T("title", i.Title, f.Title, f.TitlePt, 0f, true);
            T("subtitle", i.Subtitle, f.Subtitle, f.SubtitlePt, 0f, true);
            T("engine-label", StarterScreen.EngineLabel, f.Engine.Label, f.LabelPt, 0f, false);
            T("stages-label", StarterScreen.StagesLabel, f.Stages.Label, f.LabelPt, 0f, false);
            T("fuel-label", StarterScreen.FuelLabel, f.Fuel.Label, f.LabelPt, 0f, false);
            T("fuel-less", StarterScreen.FuelLess, f.Fuel.Less, f.EndPt, 0f, false);
            T("fuel-more", StarterScreen.FuelMore, f.Fuel.More, f.EndPt, 0f, false);
            for (int k = 0; k < i.Engines.Length && k < f.Engine.Options.Length; k++) T("engine-" + k, i.Engines[k], f.Engine.Options[k], f.OptionPt, OptionPadX, false);
            for (int k = 0; k < i.Stages.Length && k < f.Stages.Options.Length; k++) T("stages-" + k, i.Stages[k], f.Stages.Options[k], f.OptionPt, OptionPadX, false);
            if (f.Boosters != null)
            {
                T("boosters-label", StarterScreen.BoostersLabel, f.Boosters.Label, f.LabelPt, 0f, false);
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
                    if (leaves[a].Value.Overlaps(leaves[b].Value)) p.Add(leaves[a].Key + " overlaps " + leaves[b].Key);
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
                if (kv.Key == "rocket" || kv.Key == "back" || kv.Key == "reset" || kv.Key == "exit") continue;
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
