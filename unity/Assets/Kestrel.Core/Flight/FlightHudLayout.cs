// Portrait flight HUD. Points, origin top-left.
//
// Trevor's device shots: the trajectory card and the stages panel were one opaque slab over the
// rocket, the telemetry sat on the curve, and the action dock (230 pt min) covered the lower stack.
// This places four bands that do not overlap on SE, Pro Max, and iPad portrait:
//   top    — mission clock and the stage chip
//   strip  — ALT, SPEED, Q, G, one row, text that fits the cell
//   cards  — stages panel on the right, short and translucent; the trajectory is a
//            transparent line up the left edge, with the center left open
//   dock   — medal timing, auto-assist, nudges; long notes behind the info control
// The chase camera keeps the stack between StackTop and StackBottom of the screen, so the dock
// and the cards stay off the vehicle. Pinch, orbit, and Follow are unchanged.
using System;

namespace Kestrel.Core.Flight
{
    public enum FlightHudMode
    {
        Cruise = 0,
        Notes = 1,
        Prompt = 2,
        PromptNotes = 3,
    }

    public struct HudBox
    {
        public float X, Y, W, H;
        public float Right => X + W;
        public float Bottom => Y + H;
        public bool Live => W > 0.5f && H > 0.5f;

        public bool Overlaps(HudBox o)
        {
            if (!Live || !o.Live) return false;
            return X < o.Right - 0.4f && Right > o.X + 0.4f && Y < o.Bottom - 0.4f && Bottom > o.Y + 0.4f;
        }
    }

    public sealed class FlightHudFrame
    {
        public HudBox Clock, Stage, Health, Telemetry, Plot, Stages, Dock, Info, Medal, Help, Quote, Timer;
        public HudBox Assist, Primary, Skip, Alternate, NudgeDown, NudgeUp, Follow, Pause, Exit;
        /// <summary>The "Cam: Auto" button, and where its five-chip picker opens (it is closed by default).</summary>
        public HudBox Camera, CameraPicker;
        /// <summary>
        /// The stage banner's label box (h-verdict): the stages card less its 4 pt padding. In banner-row mode
        /// (<see cref="BannerRow"/>) the stages card is a full-width row under the stat strip, sized to the banner.
        /// </summary>
        public HudBox Banner;
        public bool BannerRow;
        public HudBox[] Cells = new HudBox[4];
        public FlightHudMode Mode;
    }

    public static class FlightHudLayout
    {
        /// <summary>
        /// A CSS-point box in panel units (AscentHud.LayoutWideAt: ux = panel width / CSS width). The tools row
        /// is laid out GapPt apart in points; the pinned buttons must take exactly these boxes (min = max width),
        /// or a point-sized min-width outgrows a scaled box and eats the gap.
        /// </summary>
        public static HudBox ToPanel(HudBox b, float ux, float uy) =>
            new HudBox { X = b.X * ux, Y = b.Y * uy, W = b.W * ux, H = b.H * uy };

        public const float SideMarginPt = 8f;
        public const float GapPt = 4f;
        public const float PadPt = 4f;
        public const float HeaderPt = 36f;
        public const float HealthPt = 28f;
        /// <summary>
        /// The Eng/Tnk/Hull tabs are buttons: a 28 pt row drawn, a 44 pt hit target (the row grows its taps
        /// <see cref="HealthSlopPt"/> above and below into the non-interactive clock gap and strip). A 44 pt drawn row
        /// pushes the stages card into #304's portrait picker on 320-wide phones and the staging callout past the SE gap.
        /// </summary>
        public const float HitPt = 44f;
        public const float HealthSlopPt = (HitPt - HealthPt) * 0.5f;
        public const float TelemetryPt = 36f;
        public const float PlotPt = 72f;
        public const float CardFraction = 0.32f;
        public const float ButtonPt = 44f;
        public const float InfoPt = 44f;
        public const float HelpPt = 40f;
        public const float QuotePt = 18f;
        public const float TimerPt = 6f;
        public const float StageChipPt = 32f;
        public const float FollowWidthPt = 112f;
        public const float PauseWidthPt = 76f;
        public const float PlotAlpha = 0.32f;
        /// <summary>The trajectory is a line. A filled slab behind it read as a grey panel.</summary>
        public const float TrajectoryAlpha = 0f;
        public const float RailMaxPt = 120f;
        public const float StagesAlpha = 0.32f;
        public const float DockAlpha = 0.90f;
        /// <summary>Chase framing keeps the stack inside this band of the portrait screen (top-left fractions).</summary>
        public const float StackTop = 0.38f;
        public const float StackBottom = 0.62f;
        public const float LabelFontPt = 11f;
        public const float ValueFontPt = 12f;
        public const float FontAdvance = 0.62f;
        public const float CellGapPt = 4f;
        public const float CellPadPt = 4f;
        public const int TelemetryCount = 4;

        /// <summary>Line box per point of face, for the strip's two-line cells.</summary>
        public const float LineHeight = 1.3f;
        /// <summary>The prompt quote face and the smallest face it steps down to before the line is cut.</summary>
        public const float QuoteFontPt = 13f;
        public const float QuoteMinFontPt = 10f;
        /// <summary>The widest strip reading the cells are sized for (a 10 km/s+ speed; "1234.5 km" is shorter).</summary>
        public const string WidestValue = "12.34 km/s";

        public static readonly string[] TelemetryLabels = { "ALT", "SPEED", "Q", "G" };
        public static readonly string[] ValueSamples = { "999.9 km", "0.00 km/s", "999 kPa", "00.0", "100%", "TWR", "PROP" };

        /// <summary>
        /// Clock face that fits "T+ 00.0" and "T-00:00.0" inside the band. The 56 px
        /// stylesheet size spills out of the strip and covers the ALT cell.
        /// </summary>
        /// <summary>The widest clock readings: the countdown and a four-digit mission time (SliceApp: "T+ {t:F1}").</summary>
        public static readonly string[] ClockSamples = { "T-00:00.0", "T+ 1234.5", "T+ 1:12.4" };
        /// <summary>FlightDusk.Clock's frame: 8 pt padding each side, no top or bottom padding, a 2 pt bottom edge.</summary>
        public const float ClockPadXPt = 16f;
        public const float ClockEdgePt = 2f;
        public const float ClockMinFontPt = 13f;

        /// <summary>
        /// #322 CI 1920x1080: h-t "T+ 1:12.4" needed 64 x 26 in a 64 x 22 box (28 px face in a 24 px pin). Clock face
        /// for a box <paramref name="boxW"/> x <paramref name="boxH"/> (the label's border box, one unit): the largest face
        /// from 28 down to 13 whose widest reading, budgeted with <see cref="MeasureFactor"/>, fits inside the frame's
        /// padding and whose line fits above the edge. At the 13 pt floor the box must be at least
        /// <see cref="ClockMinWidth"/> wide (PlaceWide widens it).
        /// </summary>
        public static float ClockFit(float boxW, float boxH)
        {
            float font = 28f;
            for (; font > ClockMinFontPt; font -= 0.5f)
                if (ClockFits(boxW, boxH, font)) return font;
            return ClockMinFontPt;
        }

        public static bool ClockFits(float boxW, float boxH, float font)
        {
            if (font * LineHeight > boxH - ClockEdgePt + 0.01f) return false;
            foreach (var t in ClockSamples)
                if (BudgetWidth(t, font) > boxW - ClockPadXPt - 2f + 0.01f) return false;
            return true;
        }

        /// <summary>The narrowest clock box (border box) that holds every reading at the 13 pt floor.</summary>
        public static float ClockMinWidth()
        {
            float w = 0f;
            foreach (var t in ClockSamples) w = Math.Max(w, BudgetWidth(t, ClockMinFontPt));
            return (float)Math.Ceiling(w + ClockPadXPt + 2f);
        }

        public static float ClockFontPt(float boxW, float boxH)
        {
            float font = 28f;
            float limit = boxH > 8f ? boxH - 4f : font;
            if (font > limit) font = limit;
            if (!(boxW > 8f)) return font > 13f ? font : 13f;
            while (font > 13f && (
                TextWidth("T+ 00.0", font) > boxW - 2f ||
                TextWidth("T-00:00.0", font) > boxW - 2f))
                font -= 0.5f;
            return font;
        }

        /// <summary>
        /// Height of the flight-notes sheet that sits above the button dock.
        /// It stays under the rocket band and scrolls when the copy is longer.
        /// </summary>
        public static float InfoViewPt(float screenH, float safeBottom, bool prompt)
        {
            if (!(screenH > 1f)) return HelpPt;
            float buttons = DockHeight(false, prompt);
            float dockTop = screenH - Math.Max(0f, safeBottom) - buttons;
            float ceiling = screenH * StackBottom + 8f;
            float room = dockTop - ceiling - GapPt;
            if (room < 48f) room = 48f;
            if (room > 96f) room = 96f;
            return room;
        }

        public struct QuoteFit
        {
            public string Text;
            public float FontPt;
            public bool Cut;
        }

        /// <summary>
        /// Unity's TextCore advance runs about 15% over <see cref="FontAdvance"/> (measured on #300 and on #322's SE banner:
        /// "Climbing through max-Q on the pitch progra…" needed 89 x 74 px at 15 px in a 96 x 53 box). Every width this
        /// class budgets for a label goes through <see cref="BudgetWidth"/>.
        /// </summary>
        public const float MeasureFactor = 1.15f;

        public static float BudgetWidth(string text, float fontPt) => TextWidth(text, fontPt) * MeasureFactor;

        /// <summary>
        /// One line of prompt quote for a box <paramref name="boxW"/> points wide (the 18 pt quote row holds one line;
        /// L2 printed two on top of each other). The face steps down from <paramref name="fontPt"/> to
        /// <paramref name="minPt"/>; a quote still too long is cut after the last whole word that fits, with an ellipsis.
        /// The full quote stays in the tooltip and the flight notes.
        /// </summary>
        public static QuoteFit QuoteLine(string text, float boxW, float fontPt, float minPt)
            => WrapFit(text, boxW, float.PositiveInfinity, 1, fontPt, minPt);

        /// <summary>
        /// Face (stepping 0.5 from <paramref name="fontPt"/> to <paramref name="minPt"/>) at which <paramref name="text"/>
        /// word-wraps into at most <paramref name="maxLines"/> lines of <see cref="LineHeight"/> inside a box
        /// <paramref name="boxW"/> x <paramref name="boxH"/> (any one unit: points or pixels), widths budgeted with
        /// <see cref="MeasureFactor"/>. Still too long at the floor: cut after the last whole word that fits, plus "…".
        /// </summary>
        public static QuoteFit WrapFit(string text, float boxW, float boxH, int maxLines, float fontPt, float minPt, bool allowCut = true)
        {
            var fit = new QuoteFit { Text = text ?? "", FontPt = fontPt };
            if (string.IsNullOrEmpty(text) || !(boxW > 1f) || maxLines < 1) return fit;
            float room = boxW - 2f;
            for (float font = fontPt; font >= minPt - 0.001f; font -= 0.5f)
            {
                fit.FontPt = font;
                if (Fits(text, room, boxH, maxLines, font)) return fit;
            }
            float floor = minPt;
            fit.FontPt = floor;
            // Never cut (the stage banner: a tooltip does not help on a phone): the floor face, the whole text.
            if (!allowCut) return fit;
            var words = text.Split(' ');
            for (int n = words.Length - 1; n >= 1; n--)
            {
                string cut = string.Join(" ", words, 0, n).TrimEnd(' ', ',', '.', ';', ':', '·') + "…";
                if (Fits(cut, room, boxH, maxLines, floor)) { fit.Text = cut; fit.Cut = true; return fit; }
            }
            // One word wider than the box: cut by characters.
            string first = words[0];
            for (int n = first.Length - 1; n >= 1; n--)
            {
                string cut = first.Substring(0, n) + "…";
                if (Fits(cut, room, boxH, maxLines, floor)) { fit.Text = cut; fit.Cut = true; return fit; }
            }
            fit.Text = "…";
            fit.Cut = true;
            return fit;
        }

        /// <summary>Greedy word wrap with budgeted widths: the line count, or int.MaxValue if one word is wider than the room.</summary>
        public static int WrapLines(string text, float room, float fontPt)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int lines = 0;
            foreach (var para in text.Split('\n'))
            {
                lines++;
                float line = 0f;
                float space = BudgetWidth(" ", fontPt);
                foreach (var w in para.Split(' '))
                {
                    if (w.Length == 0) continue;
                    float ww = BudgetWidth(w, fontPt);
                    if (ww > room) return int.MaxValue;
                    if (line <= 0f) line = ww;
                    else if (line + space + ww <= room) line += space + ww;
                    else { lines++; line = ww; }
                }
            }
            return lines;
        }

        static bool Fits(string text, float room, float boxH, int maxLines, float font)
        {
            int lines = WrapLines(text, room, font);
            if (lines > maxLines) return false;
            return lines * font * LineHeight <= boxH + 0.01f;
        }

        // ---- Stage banner (h-verdict) ---------------------------------------------------------------

        /// <summary>Banner face: 15 pt, stepping to 11 pt; two lines in the card before it takes its own row.</summary>
        public const float BannerFontPt = 15f;
        public const float BannerMinFontPt = 11f;
        public const int BannerCardLines = 2;
        /// <summary>Stages card padding (each side), then the banner's own frame: 8 pt each side, 4 + 4 pt and a 3 pt edge.</summary>
        public const float BannerCardPadPt = 4f;
        public const float BannerPadXPt = 16f;
        public const float BannerPadYPt = 11f;

        /// <summary>
        /// The longest stage banner line the portrait HUD is laid out and tested for (the device tests' stress line).
        /// The game's own verdicts (<see cref="WebcastBoard.Verdicts"/>) top out at 14 characters and fit the card.
        /// </summary>
        public const string BannerReserve = "Climbing through max-Q on the pitch program, booster burning hot";

        /// <summary>
        /// Banner face for a card <paramref name="cardW"/> wide: the largest face (15 -> 11 pt, widths budgeted with
        /// <see cref="MeasureFactor"/>) that wraps the whole text into two lines. Never cut: past the floor the face is
        /// 11 pt and the text takes as many lines as it needs (<see cref="BannerLinesAt"/>). A card height
        /// (<paramref name="cardH"/>) caps the face too, so a card wider than the model's never grows past its row.
        /// </summary>
        public static QuoteFit BannerFit(string text, float cardW, float cardH = float.PositiveInfinity) =>
            WrapFit(text, BannerTextW(cardW), cardH - 2f * BannerCardPadPt - BannerPadYPt, BannerCardLines, BannerFontPt, BannerMinFontPt, allowCut: false);

        /// <summary>Text width inside a stages card <paramref name="cardW"/> wide (card padding, then the banner's frame).</summary>
        public static float BannerTextW(float cardW) => cardW - 2f * BannerCardPadPt - BannerPadXPt;

        /// <summary>Lines the whole text takes at the fitted face (int.MaxValue when one word is wider than the card).</summary>
        public static int BannerLinesAt(QuoteFit fit, float cardW) => WrapLines(fit.Text, BannerTextW(cardW) - 2f, fit.FontPt);

        /// <summary>The banner label's height for its lines (whole points, rounded up): lines of the line box plus the frame.</summary>
        public static float BannerLabelPt(int lines, float fontPt) => (float)Math.Ceiling(lines * fontPt * LineHeight + BannerPadYPt);

        /// <summary>True when the whole text fits a stages card <paramref name="cardW"/> x <paramref name="cardH"/> in two lines at 11 pt or more.</summary>
        public static bool BannerFitsCard(string text, float cardW, float cardH)
        {
            if (string.IsNullOrEmpty(text)) return true;
            var fit = BannerFit(text, cardW);
            int lines = BannerLinesAt(fit, cardW);
            return lines <= BannerCardLines && BannerLabelPt(lines, fit.FontPt) <= cardH - 2f * BannerCardPadPt + 0.01f;
        }

        /// <summary>
        /// Portrait: 0 when <paramref name="text"/> fits the stages card (32% of the content width) in two lines at 11 pt
        /// or more; otherwise the height of the full-width banner row that holds the whole text (two lines at 15 -> 11 pt,
        /// then more lines at 11 pt), capped at the card's own <see cref="PlotPt"/> so the row never takes more height
        /// than the card it replaces (the picker room under it is the card mode's or larger). Text that still does not
        /// fit at the cap is not cut; <see cref="BannerFitsRow"/> reports it.
        /// </summary>
        public static float BannerRowPt(string text, float width, float safeLeft = 0f, float safeRight = 0f)
        {
            float contentW = Math.Max(1f, width - safeLeft - safeRight - 2f * SideMarginPt);
            if (BannerFitsCard(text, contentW * CardFraction, PlotPt)) return 0f;
            var fit = BannerFit(text, contentW);
            int lines = BannerLinesAt(fit, contentW);
            float h = lines == int.MaxValue ? PlotPt : BannerLabelPt(lines, fit.FontPt) + 2f * BannerCardPadPt;
            return Math.Min(PlotPt, h);
        }

        /// <summary>True when the whole text fits the frame's banner box (no word wider than it, every line inside it).</summary>
        public static bool BannerFitsRow(string text, FlightHudFrame f)
        {
            if (string.IsNullOrEmpty(text)) return true;
            float cardW = f.Banner.W + 2f * BannerCardPadPt;
            var fit = BannerFit(text, cardW);
            int lines = BannerLinesAt(fit, cardW);
            return lines != int.MaxValue && BannerLabelPt(lines, fit.FontPt) <= f.Banner.H + 0.01f;
        }

        /// <summary>
        /// Portrait picker ceiling: the open picker never rises past the stat strip, or in banner-row mode past the
        /// banner row.
        /// </summary>
        public static float PickerCeiling(FlightHudFrame f) =>
            (f.BannerRow ? Math.Max(f.Telemetry.Bottom, f.Stages.Bottom) : f.Telemetry.Bottom) + GapPt;

        public static float TextWidth(string text, float fontPt)
        {
            if (string.IsNullOrEmpty(text) || !(fontPt > 0f)) return 0f;
            return text.Length * fontPt * FontAdvance;
        }

        public readonly struct Device
        {
            public readonly string Name;
            public readonly float W, H, SafeTop, SafeBottom, SafeLeft, SafeRight;
            public Device(string name, float w, float h, float safeTop, float safeBottom, float safeLeft, float safeRight)
            {
                Name = name; W = w; H = h; SafeTop = safeTop; SafeBottom = safeBottom; SafeLeft = safeLeft; SafeRight = safeRight;
            }
        }

        /// <summary>Portrait fixtures Trevor flies: SE, Pro Max, iPad. Insets are points.</summary>
        public static Device[] PortraitDevices() => new[]
        {
            new Device("SE", 375f, 667f, 20f, 0f, 0f, 0f),
            new Device("SE", 375f, 667f, 0f, 20f, 0f, 0f),
            new Device("Pro Max", 440f, 956f, 62f, 34f, 0f, 0f),
            new Device("iPad", 1032f, 1376f, 24f, 20f, 0f, 0f),
        };

        public static FlightHudFrame Place(Device d, FlightHudMode mode, float bannerRowPt = 0f, bool nudges = true) =>
            Place(d.W, d.H, d.SafeTop, d.SafeBottom, d.SafeLeft, d.SafeRight, mode, bannerRowPt, nudges);

        /// <param name="bannerRowPt">0: the stage banner sits in the stages card. Above 0 (<see cref="BannerRowPt"/>):
        /// the stages card is a full-width row that tall under the stat strip, and the trajectory rail starts under it.</param>
        /// <param name="nudges">False when the dock's last row (Alternate / nudges) is closed (Simple cruise, a call
        /// without an alternate): the dock is one row shorter and the tools row sits lower. AscentHud's flow spacer
        /// reserves exactly this dock (#322 CI SE cruise: the spacer had dropped the row the frame still pinned, and the
        /// dock at y 519 ran over Follow at y 514).</param>
        public static FlightHudFrame Place(float width, float height, float safeTop, float safeBottom, float safeLeft, float safeRight, FlightHudMode mode, float bannerRowPt = 0f, bool nudges = true)
        {
            float left = safeLeft + SideMarginPt;
            float right = width - safeRight - SideMarginPt;
            float contentW = right - left;
            if (contentW < 1f) contentW = 1f;
            bool notes = mode == FlightHudMode.Notes || mode == FlightHudMode.PromptNotes;
            bool prompt = mode == FlightHudMode.Prompt || mode == FlightHudMode.PromptNotes;
            bool quote = prompt && !notes;

            float dockH = DockHeight(notes, prompt, ButtonPt, nudges);
            float dockTop = height - safeBottom - dockH;

            var f = new FlightHudFrame { Mode = mode };
            f.Clock = Box(left, safeTop, Math.Max(44f, contentW - StageChipPt - GapPt), HeaderPt);
            f.Stage = Box(right - StageChipPt, safeTop + (HeaderPt - StageChipPt) * 0.5f, StageChipPt, StageChipPt);
            float healthY = safeTop + HeaderPt + GapPt;
            f.Health = Box(left, healthY, contentW, HealthPt);

            float telY = healthY + HealthPt + GapPt;
            f.Telemetry = Box(left, telY, contentW, TelemetryPt);
            float cellW = (contentW - CellGapPt * (TelemetryCount - 1)) / TelemetryCount;
            f.Cells = new HudBox[TelemetryCount];
            for (int i = 0; i < TelemetryCount; i++)
                f.Cells[i] = Box(left + i * (cellW + CellGapPt), telY, cellW, TelemetryPt);

            float cardsY = telY + TelemetryPt + GapPt;
            float cardW = contentW * CardFraction;
            float toolsY = dockTop - GapPt - ButtonPt;
            float railTop = cardsY;
            float railBot = toolsY - GapPt;
            if (railBot < railTop + 96f) railBot = railTop + 96f;
            float maxRight = width * 0.34f;
            float railW = RailMaxPt;
            if (left + railW > maxRight) railW = Math.Max(72f, maxRight - left);
            if (bannerRowPt > 0f)
            {
                // Banner row: the card spans the content width, at most PlotPt tall, so the room under it is never
                // less than the card mode's; the rail starts under it.
                float rowH = Math.Min(PlotPt, bannerRowPt);
                f.BannerRow = true;
                f.Stages = Box(left, cardsY, contentW, rowH);
                railTop = f.Stages.Bottom + GapPt;
                if (railBot < railTop + 96f) railBot = railTop + 96f;
            }
            else f.Stages = Box(right - cardW, cardsY, cardW, PlotPt);
            f.Plot = Box(left, railTop, railW, railBot - railTop);
            f.Banner = Box(f.Stages.X + BannerCardPadPt, f.Stages.Y + BannerCardPadPt, f.Stages.W - 2f * BannerCardPadPt, f.Stages.H - 2f * BannerCardPadPt);

            f.Dock = Box(left, dockTop, contentW, dockH);
            float innerL = left + PadPt;
            float innerW = contentW - 2f * PadPt;
            float y = dockTop + PadPt;
            f.Info = Box(innerL + innerW - InfoPt, y + (ButtonPt - InfoPt) * 0.5f, InfoPt, InfoPt);
            f.Medal = Box(innerL, y, innerW - InfoPt - GapPt, ButtonPt);
            y = Next(y, ButtonPt);
            if (notes)
            {
                f.Help = Box(innerL, y, innerW, HelpPt);
                y = Next(y, HelpPt);
            }
            if (quote)
            {
                f.Quote = Box(innerL, y, innerW, QuotePt);
                y = Next(y, QuotePt);
            }
            if (prompt)
            {
                f.Timer = Box(innerL, y, innerW, TimerPt);
                y = Next(y, TimerPt);
                float col3 = (innerW - 2f * GapPt) / 3f;
                f.Primary = Box(innerL, y, col3, ButtonPt);
                f.Skip = Box(innerL + col3 + GapPt, y, col3, ButtonPt);
                f.Assist = Box(innerL + 2f * (col3 + GapPt), y, col3, ButtonPt);
                y = Next(y, ButtonPt);
                if (nudges)
                {
                    f.Alternate = Box(innerL, y, col3, ButtonPt);
                    f.NudgeDown = Box(innerL + col3 + GapPt, y, col3, ButtonPt);
                    f.NudgeUp = Box(innerL + 2f * (col3 + GapPt), y, col3, ButtonPt);
                }
            }
            else
            {
                f.Assist = Box(innerL, y, innerW, ButtonPt);
                y = Next(y, ButtonPt);
                if (nudges)
                {
                    float col = (innerW - GapPt) * 0.5f;
                    f.NudgeDown = Box(innerL, y, col, ButtonPt);
                    f.NudgeUp = Box(innerL + col + GapPt, y, col, ButtonPt);
                }
            }

            float followW = FollowWidthPt;
            float centerL = width * 0.35f;
            if (left + followW > centerL - GapPt) followW = Math.Max(ButtonPt, centerL - GapPt - left);
            f.Follow = Box(left, toolsY, followW, ButtonPt);
            float pauseX = right - PauseWidthPt;
            float centerR = width * 0.65f;
            if (pauseX < centerR + GapPt) pauseX = centerR + GapPt;
            f.Pause = Box(pauseX, toolsY, PauseWidthPt, ButtonPt);

            // Camera sits centred in the gap between Follow and Pause, the way the flex row lays it out.
            float camW = FlightView.WidestButton();
            float gapL = f.Follow.Right + GapPt;
            float gapR = f.Pause.X - GapPt;
            if (camW > gapR - gapL) camW = Math.Max(ButtonPt, gapR - gapL);
            f.Camera = Box((gapL + gapR - camW) * 0.5f, toolsY, camW, ButtonPt);

            // The picker opens above the tools row, right of the trajectory rail and under the stages card.
            f.CameraPicker = PickerOverRow(PickerRoom(f), ToolsRow(f), f.Camera.X + f.Camera.W * 0.5f, PickerCeiling(f));
            return f;
        }

        /// <summary>
        /// Portrait: where the open picker may go. Right of the trajectory rail, under the stages card and the
        /// stat strip, inside the side margins, down to just above the dock. The tools row splits it in two.
        /// </summary>
        public static HudBox PickerRoom(FlightHudFrame f)
        {
            // The stat strip spans the side margins (safe area + SideMarginPt) on both edges.
            float left = f.Telemetry.X;
            float right = f.Telemetry.Right;
            float x = Math.Max(f.Plot.Right, left) + GapPt;
            float y = Math.Max(f.Stages.Bottom, f.Telemetry.Bottom) + GapPt;
            float b = f.Dock.Live ? f.Dock.Y - GapPt : Math.Max(f.Follow.Bottom, f.Pause.Bottom);
            return Box(x, y, Math.Max(1f, right - x), Math.Max(1f, b - y));
        }

        /// <summary>
        /// Portrait: the pinned trajectory rail against the dock stack as it is really laid out. The rail is pinned from
        /// the frame, but Follow / Cam / Pause and the notes sheet ride the flex flow (the tools row's 8 px margin and
        /// the buttons' own margins put it about GapPt + 1 px above the frame's row): CI 335x720 had the rail's bottom
        /// at 496 and Follow's top at 495. The rail ends one <paramref name="gap"/> above <paramref name="ceilingY"/>
        /// (the highest measured top below it) in whole pixels: top ceiled, bottom and right floored, never grown.
        /// All values are panel pixels.
        /// </summary>
        public static HudBox PlotAbove(HudBox plot, float ceilingY, float gap)
        {
            float x = (float)Math.Round(plot.X);
            float y = (float)Math.Ceiling(plot.Y);
            float r = (float)Math.Floor(plot.Right);
            float b = (float)Math.Floor(plot.Bottom);
            if (!float.IsNaN(ceilingY) && !float.IsInfinity(ceilingY))
                b = Math.Min(b, (float)Math.Floor(ceilingY - gap));
            return Box(x, y, Math.Max(1f, r - x), Math.Max(1f, b - y));
        }

        /// <summary>The shortest rail worth drawing (points). Shorter (a 568 pt screen under a notch with the notes and a
        /// banner row open), it steps aside for the notes sheet instead of squeezing under or into it.</summary>
        public const float RailMinPt = 48f;

        /// <summary>The fitted rail (<see cref="PlotAbove"/>, panel px) is tall enough to show at <paramref name="upp"/> px per point.</summary>
        public static bool RailShows(HudBox fit, float upp) => fit.H >= RailMinPt * (upp > 0f ? upp : 1f) - 0.01f;

        public const float PopoverMinPt = 160f, PopoverMaxPt = 320f;

        /// <summary>
        /// Where the open health detail goes (its height is its text's): never on the health tabs or the stat strip.
        /// Portrait stacks the tabs over the strip at full width, so it opens one gap under both. Landscape puts the
        /// tabs in the left rail under the strip (CI 1280x800: "one gap under the strip" landed on the tabs), so it
        /// opens beside the tabs, under the strip, at most <paramref name="maxW"/> wide; under both when there is no
        /// room beside. Units are the caller's (panel px with px gap and widths); whole pixels: x and y ceiled, right floored.
        /// </summary>
        public static HudBox HealthPopover(HudBox health, HudBox telemetry, float gap, float minW, float maxW)
        {
            float under = Math.Max(health.Bottom, telemetry.Bottom) + gap;
            bool stacked = health.W >= telemetry.W - 1f;
            if (!stacked)
            {
                float x = (float)Math.Ceiling(health.Right + gap);
                float room = (float)Math.Floor(telemetry.Right) - x;
                if (room >= minW)
                {
                    float y = (float)Math.Ceiling(Math.Max(health.Y, telemetry.Bottom + gap));
                    return Box(x, y, (float)Math.Floor(Math.Min(room, maxW)), 0f);
                }
            }
            float ux = (float)Math.Ceiling(Math.Min(health.X, telemetry.X));
            float ur = (float)Math.Floor(Math.Max(health.Right, telemetry.Right));
            return Box(ux, (float)Math.Ceiling(under), Math.Max(1f, ur - ux), 0f);
        }

        /// <summary>The Follow / Cam / Pause row as one box.</summary>
        public static HudBox ToolsRow(FlightHudFrame f)
        {
            float x = float.MaxValue, y = float.MaxValue, r = float.MinValue, b = float.MinValue;
            bool any = false;
            foreach (var t in new[] { f.Follow, f.Camera, f.Pause })
            {
                if (!t.Live) continue;
                any = true;
                x = Math.Min(x, t.X); y = Math.Min(y, t.Y);
                r = Math.Max(r, t.Right); b = Math.Max(b, t.Bottom);
            }
            return any ? Box(x, y, r - x, b - y) : default;
        }

        /// <summary>
        /// The open picker against the tools row as it is really laid out (portrait keeps that row in the flex
        /// flow, so it can sit a few points off the frame's row: CI 335x720 had the picker 1 px into Pause).
        /// Opens one GapPt above the row, centred on the camera button and inside the room; if the room above
        /// is too short it opens one GapPt below the row; it never shares a row's height band with the buttons
        /// while either side has room. Units are points (the chip sizes are), so a panel-px row goes through
        /// ToPanel's inverse first and the result through <see cref="ToPanel"/>.
        /// </summary>
        public static HudBox PickerOverRow(HudBox room, HudBox row, float anchorX, float ceiling = float.NaN)
        {
            if (!room.Live) return default;
            if (!row.Live) row = Box(room.X, room.Bottom, room.W, 0f);
            float aboveTop = room.Y;
            float aboveBot = Math.Min(room.Bottom, row.Y - GapPt);
            float belowTop = Math.Max(room.Y, row.Bottom + GapPt);
            float belowBot = room.Bottom;
            float aboveH = aboveBot - aboveTop;
            float belowH = belowBot - belowTop;

            var up = FitWidth(PickerSize(room.W, aboveH), room.W);
            var down = FitWidth(PickerSize(room.W, belowH), room.W);
            bool upFits = up.H <= aboveH + 0.01f;
            bool downFits = down.H <= belowH + 0.01f;
            bool openUp = upFits || !downFits;
            var size = openUp ? up : down;
            float w = size.W, h = size.H;
            float px = anchorX - w * 0.5f;
            if (px + w > room.Right) px = room.Right - w;
            if (px < room.X) px = room.X;
            float py;
            if (!openUp) py = belowTop;
            else if (upFits) py = aboveBot - h;
            else
            {
                // No side has the room: keep the row clear (a covered Pause cannot be tapped) and let the
                // picker rise over the translucent cards, never past the ceiling (the stat strip) or the room.
                py = aboveBot - h;
                float top = float.IsNaN(ceiling) ? aboveTop : Math.Min(aboveTop, ceiling);
                if (py < top) py = top;
            }
            return Box(px, py, w, h);
        }

        /// <summary>A picker footprint that went wider than the room for a short room: back to as many columns as fit.</summary>
        static HudBox FitWidth(HudBox size, float roomW) =>
            size.W <= roomW + 0.01f ? size : PickerSize(roomW, float.MaxValue);

        /// <summary>
        /// Picker footprint for the room it has: as many chip columns as fit (one row of five on a wide
        /// screen, three by two at 335 pt), every chip a 44 pt target.
        /// </summary>
        public static HudBox PickerSize(float roomW, float roomH)
        {
            int n = FlightView.Picker.Length;
            float chip = FlightView.WidestChip();
            int cols = (int)Math.Floor((roomW - 2f * PadPt + GapPt) / (chip + GapPt));
            if (cols > n) cols = n;
            if (cols < 1) cols = 1;
            int rows = (n + cols - 1) / cols;
            // Too short for that many rows: go wider if the width allows it (never narrower than one chip).
            while (rows > 1 && rows * ButtonPt + (rows - 1) * GapPt + 2f * PadPt > roomH && cols < n)
            {
                cols++;
                rows = (n + cols - 1) / cols;
            }
            float w = cols * chip + (cols - 1) * GapPt + 2f * PadPt;
            float h = rows * ButtonPt + (rows - 1) * GapPt + 2f * PadPt;
            return new HudBox { W = w, H = h };
        }

        /// <summary>Chip boxes inside an open picker, in <see cref="FlightView.Picker"/> order, row by row.</summary>
        public static HudBox[] PickerChips(HudBox picker)
        {
            int n = FlightView.Picker.Length;
            var chips = new HudBox[n];
            if (!picker.Live) return chips;
            float chip = FlightView.WidestChip();
            int cols = (int)Math.Floor((picker.W - 2f * PadPt + GapPt) / (chip + GapPt) + 0.01f);
            if (cols > n) cols = n;
            if (cols < 1) cols = 1;
            float cw = (picker.W - 2f * PadPt - (cols - 1) * GapPt) / cols;
            for (int i = 0; i < n; i++)
            {
                int c = i % cols, r = i / cols;
                chips[i] = Box(picker.X + PadPt + c * (cw + GapPt), picker.Y + PadPt + r * (ButtonPt + GapPt), cw, ButtonPt);
            }
            return chips;
        }

        public static float DockHeight(bool notes, bool prompt) => DockHeight(notes, prompt, ButtonPt);

        /// <summary>Dock height with button rows <paramref name="rowPt"/> tall (landscape: whole panel pixels, never under 44 pt).</summary>
        public static float DockHeight(bool notes, bool prompt, float rowPt, bool nudges = true)
        {
            bool quote = prompt && !notes;
            float h = PadPt + rowPt;
            if (notes) h += GapPt + HelpPt;
            if (quote) h += GapPt + QuotePt;
            if (prompt) h += GapPt + TimerPt;
            h += GapPt + rowPt;
            if (nudges) h += GapPt + rowPt;
            h += PadPt;
            return h;
        }

        static float Next(float y, float h) => y + h + GapPt;

        static HudBox Box(float x, float y, float w, float h) => new HudBox { X = x, Y = y, W = w, H = h };

        /// <summary>
        /// The landscape dock mode for the live state. The dock height depends on it, so a change re-pins
        /// the HUD (a prompt opening mid-flight must lift Follow, the camera button and Pause with the dock).
        /// </summary>
        public static FlightHudMode WideMode(bool prompt, bool notes) =>
            prompt && notes ? FlightHudMode.PromptNotes
            : prompt ? FlightHudMode.Prompt
            : notes ? FlightHudMode.Notes
            : FlightHudMode.Cruise;

        /// <summary>
        /// Landscape web HUD. The stat strip runs across the top, the plot and the stages panel
        /// sit in side rails, and the rocket column between them stays clear down to a short dock.
        /// Portrait <see cref="Place(float, float, float, float, float, float, FlightHudMode, float, bool)"/> is unchanged.
        /// </summary>
        /// <param name="rowPt">Height of the dock's button rows (medal + info, prompt or assist, nudges) in these units.
        /// AscentHud passes 44 pt as whole panel pixels (1280x800 CSS is a 0.948 panel: 44 px buttons need 46.4 CSS pt);
        /// never under <see cref="ButtonPt"/>.</param>
        /// <param name="clockMinW">The clock's narrowest box in these units (AscentHud: <see cref="ClockMinWidth"/> in panel
        /// pixels at the HUD's face scale, over the panel scale); 0 for <see cref="ClockMinWidth"/> itself.</param>
        public static FlightHudFrame PlaceWide(float width, float height, float safeTop, float safeBottom, float safeLeft, float safeRight, FlightHudMode mode, float rowPt = ButtonPt, float clockMinW = 0f)
        {
            if (!(rowPt >= ButtonPt)) rowPt = ButtonPt;
            if (!(clockMinW > 0f)) clockMinW = ClockMinWidth();
            if (!(width > height))
                return Place(width, height, safeTop, safeBottom, safeLeft, safeRight, mode);

            float edge = SideMarginPt;
            safeTop = Math.Max(0f, safeTop);
            safeBottom = Math.Max(0f, safeBottom);
            safeLeft = Math.Max(0f, safeLeft);
            safeRight = Math.Max(0f, safeRight);

            float exitW = HomeCornerLayout.ExitW;
            float exitH = HomeCornerLayout.ExitH;
            var exit = Box(width - safeRight - edge - exitW, safeTop, exitW, exitH);

            float left = safeLeft + edge;
            float right = width - safeRight - edge;
            float stripRight = exit.X - GapPt;
            float stripW = Math.Max(1f, stripRight - left);
            float stripH = Math.Max(HeaderPt, TelemetryPt);
            float stripY = safeTop;

            bool notes = mode == FlightHudMode.Notes || mode == FlightHudMode.PromptNotes;
            bool prompt = mode == FlightHudMode.Prompt || mode == FlightHudMode.PromptNotes;
            bool quote = prompt && !notes;

            // Keep the dock below the chase-camera stack (62% of the screen) so the rocket stays visible.
            float stackFloor = height * StackBottom;
            float maxDock = height - safeBottom - stackFloor - GapPt;
            float wantDock = DockHeight(notes, prompt, rowPt);
            float oneRow = PadPt + rowPt + PadPt;
            float dockH = wantDock;
            if (dockH > maxDock) dockH = Math.Max(oneRow, maxDock);
            if (dockH > height - safeBottom - stripY - stripH - 80f)
                dockH = Math.Max(oneRow, height - safeBottom - stripY - stripH - 80f);
            float dockTop = height - safeBottom - dockH;

            float railTop = Math.Max(stripY + stripH, exit.Bottom) + GapPt;
            float railBot = dockTop - GapPt;

            float innerW = Math.Max(1f, right - left);
            float minCenter = Math.Max(120f, width * 0.28f);
            float railW = innerW * 0.22f;
            if (railW < 132f) railW = 132f;
            if (railW > 320f) railW = 320f;
            if (left + railW * 2f + minCenter > right)
                railW = Math.Max(96f, (innerW - minCenter) * 0.5f);

            var f = new FlightHudFrame { Mode = mode, Exit = exit };
            f.Telemetry = Box(left, stripY, stripW, stripH);
            float clockW = Math.Max(clockMinW, Math.Min(120f, Math.Max(64f, stripW * 0.12f)));
            f.Clock = Box(left, stripY, clockW, stripH);
            f.Stage = Box(stripRight - StageChipPt, stripY + (stripH - StageChipPt) * 0.5f, StageChipPt, StageChipPt);
            float cellsL = f.Clock.Right + GapPt;
            float cellsR = f.Stage.X - GapPt;
            float cellW = (Math.Max(4f, cellsR - cellsL) - CellGapPt * (TelemetryCount - 1)) / TelemetryCount;
            f.Cells = new HudBox[TelemetryCount];
            for (int i = 0; i < TelemetryCount; i++)
                f.Cells[i] = Box(cellsL + i * (cellW + CellGapPt), stripY, Math.Max(1f, cellW), stripH);

            f.Dock = Box(left, dockTop, innerW, dockH);
            PackDock(f, notes, prompt, quote, rowPt);

            float toolsH = ButtonPt;
            bool helpInRail = notes && !Contains(f.Dock, f.Help);
            bool quoteInRail = quote && !Contains(f.Dock, f.Quote);
            float leftExtra = (helpInRail ? HelpPt + GapPt : 0f) + (quoteInRail ? QuotePt + GapPt : 0f);

            float followY = railBot - toolsH;
            f.Health = Box(left, railTop, railW, HealthPt);
            float plotY = f.Health.Bottom + GapPt;
            float plotH = followY - GapPt - leftExtra - plotY;
            if (plotH < 48f) plotH = 48f;
            f.Plot = Box(left, plotY, railW, plotH);
            float yL = f.Plot.Bottom + GapPt;
            if (helpInRail)
            {
                f.Help = Box(left, yL, railW, HelpPt);
                yL = f.Help.Bottom + GapPt;
            }
            if (quoteInRail)
            {
                f.Quote = Box(left, yL, railW, QuotePt);
                yL = f.Quote.Bottom + GapPt;
            }
            f.Follow = Box(left, yL, railW, toolsH);

            bool nudgesInRail = prompt && !Contains(f.Dock, f.NudgeDown);
            // Camera shares the Pause row when the rail is wide enough; a narrow rail stacks it above Pause.
            float camW = FlightView.WidestButton();
            bool camBeside = railW >= camW + GapPt + PauseWidthPt;
            float rightExtra = (nudgesInRail ? ButtonPt + GapPt : 0f) + (camBeside ? 0f : toolsH + GapPt);
            float stagesH = followY - GapPt - rightExtra - railTop;
            if (stagesH < 48f) stagesH = 48f;
            f.Stages = Box(right - railW, railTop, railW, stagesH);
            f.Banner = Box(f.Stages.X + BannerCardPadPt, f.Stages.Y + BannerCardPadPt, f.Stages.W - 2f * BannerCardPadPt, f.Stages.H - 2f * BannerCardPadPt);
            float yR = f.Stages.Bottom + GapPt;
            if (nudgesInRail)
            {
                float col = (railW - 2f * GapPt) / 3f;
                f.Alternate = Box(f.Stages.X, yR, col, ButtonPt);
                f.NudgeDown = Box(f.Stages.X + col + GapPt, yR, col, ButtonPt);
                f.NudgeUp = Box(f.Stages.X + 2f * (col + GapPt), yR, col, ButtonPt);
                yR += ButtonPt + GapPt;
            }
            if (camBeside)
            {
                f.Camera = Box(right - railW, yR, camW, toolsH);
                f.Pause = Box(f.Camera.Right + GapPt, yR, right - f.Camera.Right - GapPt, toolsH);
            }
            else
            {
                f.Camera = Box(right - railW, yR, railW, toolsH);
                f.Pause = Box(right - railW, yR + toolsH + GapPt, railW, toolsH);
            }

            // The picker opens toward the centre: left of the button, inside the rocket column, above the dock.
            var open = RocketColumn(f);
            var size = PickerSize(open.W, open.H);
            float px = f.Camera.X - GapPt - size.W;
            if (px + size.W > open.Right) px = open.Right - size.W;
            if (px < open.X) px = open.X;
            float py = f.Camera.Bottom - size.H;
            if (py + size.H > open.Bottom) py = open.Bottom - size.H;
            if (py < open.Y) py = open.Y;
            f.CameraPicker = Box(px, py, Math.Min(size.W, open.W), Math.Min(size.H, open.H));
            return f;
        }

        static void PackDock(FlightHudFrame f, bool notes, bool prompt, bool quote, float rowPt)
        {
            float inL = f.Dock.X + PadPt;
            float inW = f.Dock.W - 2f * PadPt;
            float y = f.Dock.Y + PadPt;
            float maxY = f.Dock.Bottom - PadPt;
            bool Room(float h) => y + h <= maxY + 0.6f;

            if (!Room(rowPt)) return;
            f.Info = Box(inL + inW - rowPt, y, rowPt, rowPt);
            f.Medal = Box(inL, y, Math.Max(44f, inW - rowPt - GapPt), rowPt);
            y += rowPt + GapPt;

            if (!Room(rowPt)) return;
            if (prompt)
            {
                float col = (inW - 2f * GapPt) / 3f;
                f.Primary = Box(inL, y, col, rowPt);
                f.Skip = Box(inL + col + GapPt, y, col, rowPt);
                f.Assist = Box(inL + 2f * (col + GapPt), y, col, rowPt);
            }
            else
            {
                float col = (inW - 2f * GapPt) / 3f;
                f.Assist = Box(inL, y, col, rowPt);
                f.NudgeDown = Box(inL + col + GapPt, y, col, rowPt);
                f.NudgeUp = Box(inL + 2f * (col + GapPt), y, col, rowPt);
            }
            y += rowPt + GapPt;
            if (prompt && Room(rowPt))
            {
                float col = (inW - 2f * GapPt) / 3f;
                f.Alternate = Box(inL, y, col, rowPt);
                f.NudgeDown = Box(inL + col + GapPt, y, col, rowPt);
                f.NudgeUp = Box(inL + 2f * (col + GapPt), y, col, rowPt);
                y += rowPt + GapPt;
            }
            if (prompt && Room(TimerPt))
            {
                f.Timer = Box(inL, y, inW, TimerPt);
                y += TimerPt + GapPt;
            }
            if (quote && Room(QuotePt))
            {
                f.Quote = Box(inL, y, inW, QuotePt);
                y += QuotePt + GapPt;
            }
            if (notes && Room(HelpPt))
                f.Help = Box(inL, y, inW, HelpPt);
        }

        static bool Contains(HudBox outer, HudBox inner)
        {
            if (!inner.Live || !outer.Live) return false;
            return inner.X >= outer.X - 0.6f && inner.Right <= outer.Right + 0.6f
                && inner.Y >= outer.Y - 0.6f && inner.Bottom <= outer.Bottom + 0.6f;
        }

        /// <summary>Open column between the side rails, under the stat strip and above the dock.</summary>
        public static HudBox RocketColumn(FlightHudFrame f)
        {
            float x = f.Plot.Right + GapPt;
            float y = f.Telemetry.Bottom + GapPt;
            float r = f.Stages.X - GapPt;
            float b = f.Dock.Y - GapPt;
            return Box(x, y, Math.Max(1f, r - x), Math.Max(1f, b - y));
        }
    }
}
